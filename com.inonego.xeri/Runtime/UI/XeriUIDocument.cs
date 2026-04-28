using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

// GammaRTBlit + USS loop animation 통합 컴포넌트
//
// GammaRTBlit: offscreen RT로 UI Toolkit panel 렌더링 후 gamma→linear blit 합성
// Loop Animation: USS --xeri-next custom property + TransitionEndEvent 기반 무한 루프
//   UXML: class="xeri-loop"
//   USS:  --xeri-next: "step-class"; transition: -unity-material 1.5s;
//
[ExecuteAlways]
[RequireComponent(typeof(UIDocument))]
public class XeriUIDocument : MonoBehaviour
{
    #region GammaRTBlit Members

    const string k_BlitShaderName = "Research/GammaToLinearBlit";
    static readonly int s_BlitTextureId = Shader.PropertyToID("_BlitTexture");
    static readonly int s_BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");

    static readonly List<XeriUIDocument> s_ManagedPanels = new();
    static readonly Dictionary<PanelSettings, XeriUIDocument> s_PanelOwners = new();

    static Material s_BlitMaterial;
    static bool s_HasMissingShaderWarning;

    [SerializeField]
    bool m_ForceGammaRendering = true;

    UIDocument m_Document;
    PanelSettings m_ManagedPanelSettings;
    RenderTexture m_TargetTexture;
    RenderTexture m_PreviousTargetTexture;
    bool m_PreviousForceGammaRendering;
    bool m_IsPanelOwner;
    int m_LastWidth;
    int m_LastHeight;
    string m_LastValidationWarning;
    bool m_HasDuplicateOwnerWarning;

    #endregion

    #region Loop Animation Members

    static readonly CustomStyleProperty<string> k_XeriNext =
        new CustomStyleProperty<string>("--xeri-next");

    readonly Dictionary<VisualElement, string> m_CurrentClasses = new();

    #endregion

    #region Lifecycle

    void OnEnable()
    {
        CacheDocument();
        TryRefreshManagedPanel();
        SetupLoopAnimations();
    }

    void OnDisable()
    {
        ReleaseManagedPanel();
        CleanupLoopAnimations();
    }

    void OnDestroy()
    {
        ReleaseManagedPanel();
    }

    void OnValidate()
    {
        if (m_IsPanelOwner)
            ApplyPanelOverrides();
    }

    void Update()
    {
        CacheDocument();

        if (m_IsPanelOwner)
        {
            PanelSettings currentPanelSettings = m_Document != null ? m_Document.panelSettings : null;
            if (currentPanelSettings != m_ManagedPanelSettings || GetUnsupportedReason(currentPanelSettings) != null)
            {
                ReleaseManagedPanel();
            }
        }

        if (!m_IsPanelOwner)
        {
            TryRefreshManagedPanel();
            return;
        }

        EnsureRenderTexture();
        ApplyPanelOverrides();
    }

    #endregion

    #region GammaRTBlit Implementation

    void CacheDocument()
    {
        if (m_Document == null)
            m_Document = GetComponent<UIDocument>();
    }

    void TryRefreshManagedPanel()
    {
        PanelSettings panelSettings = m_Document != null ? m_Document.panelSettings : null;
        string unsupportedReason = GetUnsupportedReason(panelSettings);
        if (unsupportedReason != null)
        {
            WarnOnce(unsupportedReason);
            return;
        }

        m_LastValidationWarning = null;
        m_HasDuplicateOwnerWarning = false;

        if (s_PanelOwners.TryGetValue(panelSettings, out XeriUIDocument owner) && owner != null && owner != this)
        {
            if (!m_HasDuplicateOwnerWarning)
            {
                Debug.LogWarning(
                    $"[XeriUIDocument] PanelSettings '{panelSettings.name}' already has an active owner on '{owner.gameObject.name}'. " +
                    "Only one managed component can own a PanelSettings asset at a time.",
                    this);
                m_HasDuplicateOwnerWarning = true;
            }
            return;
        }

        s_PanelOwners[panelSettings] = this;
        s_ManagedPanels.Add(this);
        m_ManagedPanelSettings = panelSettings;
        m_PreviousTargetTexture = panelSettings.targetTexture;
        m_PreviousForceGammaRendering = panelSettings.forceGammaRendering;
        m_IsPanelOwner = true;

        EnsureBlitMaterial();
        EnsureRenderTexture();
        ApplyPanelOverrides();

        if (s_ManagedPanels.Count == 1)
            RenderPipelineManager.endCameraRendering += HandleEndCameraRendering;
    }

    string GetUnsupportedReason(PanelSettings panelSettings)
    {
        if (m_Document == null)
            return "[XeriUIDocument] UIDocument component is missing.";

        if (panelSettings == null)
            return "[XeriUIDocument] Assign PanelSettings before enabling managed compositing.";

        if (m_Document.parentUI != null)
            return "[XeriUIDocument] Nested UIDocument is not supported. Attach the component to the top-level panel owner only.";

        return null;
    }

    void WarnOnce(string message)
    {
        if (m_LastValidationWarning == message)
            return;

        m_LastValidationWarning = message;
        Debug.LogWarning(message, this);
    }

    void EnsureRenderTexture()
    {
        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);
        if (m_TargetTexture != null && m_LastWidth == width && m_LastHeight == height)
            return;

        ReleaseRenderTexture();

        m_TargetTexture = new RenderTexture(width, height, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
        {
            name = $"{gameObject.name}_ManagedGammaPanelRT",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            autoGenerateMips = false,
            useMipMap = false,
            hideFlags = HideFlags.HideAndDontSave
        };
        m_TargetTexture.Create();

        m_LastWidth = width;
        m_LastHeight = height;
    }

    void ApplyPanelOverrides()
    {
        if (!m_IsPanelOwner || m_ManagedPanelSettings == null || m_TargetTexture == null)
            return;

        if (m_ManagedPanelSettings.targetTexture != m_TargetTexture)
            m_ManagedPanelSettings.targetTexture = m_TargetTexture;

        if (m_ManagedPanelSettings.forceGammaRendering != m_ForceGammaRendering)
            m_ManagedPanelSettings.forceGammaRendering = m_ForceGammaRendering;
    }

    void ReleaseManagedPanel()
    {
        if (m_IsPanelOwner && m_ManagedPanelSettings != null)
        {
            if (s_PanelOwners.TryGetValue(m_ManagedPanelSettings, out XeriUIDocument owner) && owner == this)
            {
                s_PanelOwners.Remove(m_ManagedPanelSettings);
                m_ManagedPanelSettings.targetTexture = m_PreviousTargetTexture;
                m_ManagedPanelSettings.forceGammaRendering = m_PreviousForceGammaRendering;
            }

            s_ManagedPanels.Remove(this);
        }

        if (s_ManagedPanels.Count == 0)
            RenderPipelineManager.endCameraRendering -= HandleEndCameraRendering;

        m_IsPanelOwner = false;
        m_ManagedPanelSettings = null;
        m_PreviousTargetTexture = null;
        ReleaseRenderTexture();
    }

    void ReleaseRenderTexture()
    {
        if (m_TargetTexture == null)
            return;

        m_TargetTexture.Release();
        if (Application.isPlaying)
            Destroy(m_TargetTexture);
        else
            DestroyImmediate(m_TargetTexture);

        m_TargetTexture = null;
    }

    static void EnsureBlitMaterial()
    {
        if (s_BlitMaterial != null)
            return;

        Shader shader = Shader.Find(k_BlitShaderName);
        if (shader == null)
        {
            if (!s_HasMissingShaderWarning)
            {
                Debug.LogWarning($"[XeriUIDocument] Shader not found: {k_BlitShaderName}");
                s_HasMissingShaderWarning = true;
            }
            return;
        }

        s_BlitMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    static void HandleEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (s_ManagedPanels.Count == 0)
            return;

        // Game view + Scene view 모두 허용 (Edit/Play 공통)
        if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView)
            return;

        EnsureBlitMaterial();
        if (s_BlitMaterial == null)
            return;

        s_ManagedPanels.Sort(static (a, b) =>
        {
            int panelOrderCompare = a.GetPanelSortOrder().CompareTo(b.GetPanelSortOrder());
            if (panelOrderCompare != 0)
                return panelOrderCompare;

            int documentOrderCompare = a.GetDocumentSortOrder().CompareTo(b.GetDocumentSortOrder());
            if (documentOrderCompare != 0)
                return documentOrderCompare;

            return a.GetInstanceID().CompareTo(b.GetInstanceID());
        });

        RenderTargetIdentifier target = camera.targetTexture != null
            ? new RenderTargetIdentifier(camera.targetTexture)
            : BuiltinRenderTextureType.CameraTarget;

        CommandBuffer commandBuffer = new CommandBuffer { name = "UI Toolkit Managed Gamma Panels" };
        commandBuffer.SetRenderTarget(target);

        foreach (XeriUIDocument panel in s_ManagedPanels)
        {
            if (!panel.IsReadyToComposite())
                continue;

            commandBuffer.SetGlobalTexture(s_BlitTextureId, panel.m_TargetTexture);
            commandBuffer.SetGlobalVector(s_BlitScaleBiasId, new Vector4(1f, -1f, 0f, 1f));
            commandBuffer.DrawProcedural(Matrix4x4.identity, s_BlitMaterial, 1, MeshTopology.Triangles, 3);
        }

        context.ExecuteCommandBuffer(commandBuffer);
        context.Submit();
        commandBuffer.Release();
    }

    bool IsReadyToComposite()
    {
        return isActiveAndEnabled &&
               m_IsPanelOwner &&
               m_ManagedPanelSettings != null &&
               m_TargetTexture != null &&
               m_TargetTexture.IsCreated();
    }

    float GetPanelSortOrder()
    {
        return m_ManagedPanelSettings != null ? m_ManagedPanelSettings.sortingOrder : 0;
    }

    float GetDocumentSortOrder()
    {
        return m_Document != null ? m_Document.sortingOrder : 0f;
    }

    #endregion

    #region Loop Animation Implementation

    void SetupLoopAnimations()
    {
        if (m_Document == null || m_Document.rootVisualElement == null)
            return;

        VisualElement root = m_Document.rootVisualElement;

        root.Query<VisualElement>(className: "xeri-loop").ForEach(element =>
        {
            element.RegisterCallback<TransitionEndEvent>(OnTransitionEnd);

            // OnEnable 시점에는 USS가 아직 resolve 안 됨
            // CustomStyleResolvedEvent로 스타일 준비 완료 후 첫 step 시작
            element.RegisterCallback<CustomStyleResolvedEvent>(OnInitialStyleResolved);
            m_CurrentClasses[element] = "";
        });
    }

    void CleanupLoopAnimations()
    {
        foreach (var kvp in m_CurrentClasses)
        {
            VisualElement element = kvp.Key;
            string currentClass = kvp.Value;

            element.UnregisterCallback<TransitionEndEvent>(OnTransitionEnd);
            element.UnregisterCallback<CustomStyleResolvedEvent>(OnInitialStyleResolved);

            if (!string.IsNullOrEmpty(currentClass))
                element.RemoveFromClassList(currentClass);
        }

        m_CurrentClasses.Clear();
    }

    // 스타일이 resolve된 후 첫 애니메이션 시작
    void OnInitialStyleResolved(CustomStyleResolvedEvent e)
    {
        VisualElement element = e.currentTarget as VisualElement;
        if (element == null || !m_CurrentClasses.ContainsKey(element))
            return;

        // 이미 시작된 element는 무시
        if (!string.IsNullOrEmpty(m_CurrentClasses[element]))
            return;

        // 한 번만 필요 — 등록 해제
        element.UnregisterCallback<CustomStyleResolvedEvent>(OnInitialStyleResolved);

        string nextClass = ReadXeriNext(element);
        if (!string.IsNullOrEmpty(nextClass))
        {
            element.AddToClassList(nextClass);
            m_CurrentClasses[element] = nextClass;
        }
    }

    void OnTransitionEnd(TransitionEndEvent e)
    {
        VisualElement element = e.currentTarget as VisualElement;
        if (element == null || !m_CurrentClasses.ContainsKey(element))
            return;

        // 현재 state에서 --xeri-next를 먼저 읽은 후 class 교체
        string nextClass = ReadXeriNext(element);

        // transition 비활성화 → class 제거 시 즉시 base로 점프 (역방향 애니메이션 방지)
        element.style.transitionDuration = new List<TimeValue> { new TimeValue(0) };

        string previousClass = m_CurrentClasses[element];
        if (!string.IsNullOrEmpty(previousClass))
            element.RemoveFromClassList(previousClass);

        m_CurrentClasses[element] = "";

        // 다음 프레임: transition 복원 + 새 class 추가 → 정방향 transition 시작
        if (!string.IsNullOrEmpty(nextClass))
        {
            string capturedNextClass = nextClass;
            element.schedule.Execute(() =>
            {
                element.style.transitionDuration = StyleKeyword.Null;
                element.AddToClassList(capturedNextClass);
                m_CurrentClasses[element] = capturedNextClass;
            });
        }
    }

    string ReadXeriNext(VisualElement element)
    {
        if (element.customStyle.TryGetValue(k_XeriNext, out string value))
            return value ?? "";
        return "";
    }

    #endregion
}
