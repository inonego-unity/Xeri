/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_DragDropManualPlayMode.cs
수정일 : 2026-05-22

# 설명
UGUI / Runtime UI Toolkit DragDrop 을 PlayMode 화면에서 직접 드래그해 확인하는 수동 테스트.
각 단계를 정상 확인한 뒤 Space 키로 다음 단계로 진행한다.

# 테스트 구성
 M: 수동 확인 (UGUI / UITK)

# 특이사항
[Explicit] 과 [Category("Manual")] 로 일반 테스트 실행에서 멈추지 않게 분리한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.UIElements;

using NUnit;
using NUnit.Framework;

using inonego.Xeri.UI.DragDrop;

namespace inonego.Xeri.TEST.UI._Drag_Drop
{
    // ============================================================
    /// <summary>
    /// DragDrop 수동 PlayMode 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_DragDropManualPlayMode
    {

    #region 필드

        private GameObject eventSystemGO = null;
        private GameObject cameraGO = null;
        private GameObject uguiRoot = null;
        private GameObject uitkRoot = null;
        private PanelSettings panelSettings = null;
        private ThemeStyleSheet themeStyleSheet = null;

    #endregion

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// Space키 입력을 체크한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool IsSpaceKeyPressed()
        {
        #if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        #else
            return Input.GetKeyDown(KeyCode.Space);
        #endif
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트용 EventSystem을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private EventSystem CreateEventSystem()
        {
            eventSystemGO = new GameObject("TEST_EventSystem");
            var eventSystem = eventSystemGO.AddComponent<EventSystem>();

        #if ENABLE_INPUT_SYSTEM
            eventSystemGO.AddComponent<InputSystemUIInputModule>();
        #else
            eventSystemGO.AddComponent<StandaloneInputModule>();
        #endif

            return eventSystem;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Game View 기본 카메라 경고가 나오지 않도록 테스트용 카메라를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CreateTestCamera()
        {
            cameraGO = new GameObject("TEST_Camera");

            var testCamera = cameraGO.AddComponent<Camera>();
            testCamera.clearFlags          = CameraClearFlags.SolidColor;
            testCamera.backgroundColor     = new Color(0.25f, 0.25f, 0.25f, 1f);
            testCamera.transform.position  = new Vector3(0f, 0f, -10f);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UGUI RectTransform 기본값을 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        private RectTransform SetupRect
        (
            GameObject go,
            Vector2 anchoredPos,
            Vector2 size
        )
        {
            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.anchorMin        = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax        = new Vector2(0.5f, 0.5f);
            rectTransform.pivot            = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPos;
            rectTransform.sizeDelta        = size;

            return rectTransform;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UGUI 안내 텍스트를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CreateUGUIText(Transform parent, string text)
        {
            var go = new GameObject("GuideText", typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);
            SetupRect(go, new Vector2(0f, 250f), new Vector2(900f, 80f));

            var uiText = go.AddComponent<UnityEngine.UI.Text>();
            uiText.text      = text;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color     = Color.white;
            uiText.fontSize  = 24;
            uiText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UGUI 드래그 샘플을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private Func<bool> CreateUGUISample(EventSystem eventSystem)
        {
            var dropped = false;
            var coordinator = DragDropCoordinator.Default;
            coordinator.DropResolver = new UGUIDropResolver(eventSystem);

            uguiRoot = new GameObject("TEST_UGUI_DragDrop");
            var canvas = uguiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uguiRoot.AddComponent<CanvasScaler>();
            uguiRoot.AddComponent<GraphicRaycaster>();

            CreateUGUIText
            (
                uguiRoot.transform,
                "UGUI: 파란 박스를 오른쪽 초록 영역에 드롭한 뒤 Space 키를 누르세요."
            );

            var zoneGO = new GameObject("UGUI_DropZone", typeof(RectTransform));
            zoneGO.transform.SetParent(uguiRoot.transform, worldPositionStays: false);
            SetupRect(zoneGO, new Vector2(220f, 0f), new Vector2(220f, 160f));

            var zoneImage = zoneGO.AddComponent<UnityEngine.UI.Image>();
            zoneImage.color = new Color(0.15f, 0.45f, 0.18f, 0.8f);

            var dropZone = zoneGO.AddComponent<DropZoneUI>();
            dropZone.OnDropDone += (_, _) =>
            {
                dropped = true;
                zoneImage.color = Color.green;
                Debug.Log("UGUI Drop Done");
            };

            var dragGO = new GameObject("UGUI_Draggable", typeof(RectTransform));
            dragGO.transform.SetParent(uguiRoot.transform, worldPositionStays: false);
            SetupRect(dragGO, new Vector2(-220f, 0f), new Vector2(160f, 120f));

            var dragImage = dragGO.AddComponent<UnityEngine.UI.Image>();
            dragImage.color = new Color(0.1f, 0.35f, 0.9f, 0.9f);
            dragGO.AddComponent<DraggableUI>();

            return () => dropped;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UITK 드래그 샘플을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private Func<bool> CreateUITKSample()
        {
            var dropped = false;

            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "TEST_DragDrop_PanelSettings";
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;

            themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            themeStyleSheet.name = "TEST_DragDrop_ThemeStyleSheet";
            panelSettings.themeStyleSheet = themeStyleSheet;

            uitkRoot = new GameObject("TEST_UITK_DragDrop");
            var document = uitkRoot.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;

            var root = document.rootVisualElement;
            root.style.flexGrow = 1f;
            root.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.9f);

            var guide = new Label("UITK: 파란 박스를 오른쪽 초록 영역에 드롭한 뒤 Space 키를 누르세요.");
            guide.style.position = Position.Absolute;
            guide.style.left = 0f;
            guide.style.right = 0f;
            guide.style.color = Color.white;
            guide.style.fontSize = 24f;
            guide.style.unityFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            guide.style.unityTextAlign = TextAnchor.MiddleCenter;
            guide.style.whiteSpace = WhiteSpace.NoWrap;
            root.Add(guide);

            var coordinator = new DragDropCoordinator();
            var resolver = new UITKDropResolver(root);
            coordinator.DropResolver = resolver;

            var dropElement = new VisualElement();
            dropElement.style.position = Position.Absolute;
            dropElement.style.width = 220f;
            dropElement.style.height = 160f;
            dropElement.style.backgroundColor = new Color(0.15f, 0.45f, 0.18f, 0.85f);
            root.Add(dropElement);

            var dropZone = new UITKDropZoneManipulator(coordinator, resolver);
            dropZone.OnDropDone += (_, _) =>
            {
                dropped = true;
                dropElement.style.backgroundColor = Color.green;
                Debug.Log("UITK Drop Done");
            };
            dropElement.AddManipulator(dropZone);

            var dragElement = new VisualElement();
            dragElement.style.position = Position.Absolute;
            dragElement.style.width = 160f;
            dragElement.style.height = 120f;
            dragElement.style.backgroundColor = new Color(0.1f, 0.35f, 0.9f, 0.9f);
            root.Add(dragElement);

            dragElement.AddManipulator(new UITKDraggableManipulator(coordinator));
            root.RegisterCallback<GeometryChangedEvent>
            (
                (_) => LayoutUITKSample(root, guide, dragElement, dropElement)
            );
            root.schedule.Execute
            (
                () => LayoutUITKSample(root, guide, dragElement, dropElement)
            ).ExecuteLater(0);

            return () => dropped;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UITK 샘플을 UGUI 샘플과 같은 화면 중앙 기준 좌표로 배치한다.
        /// </summary>
        // ------------------------------------------------------------
        private void LayoutUITKSample
        (
            VisualElement root,
            Label guide,
            VisualElement dragElement,
            VisualElement dropElement
        )
        {
            var width = root.resolvedStyle.width;
            var height = root.resolvedStyle.height;

            if (float.IsNaN(width) || float.IsNaN(height)) return;
            if (width <= 0f || height <= 0f) return;

            var centerX = width * 0.5f;
            var centerY = height * 0.5f;

            guide.style.top = centerY - 250f;

            dragElement.style.left = centerX - 300f;
            dragElement.style.top = centerY - 60f;

            dropElement.style.left = centerX + 110f;
            dropElement.style.top = centerY - 80f;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Space 입력을 기다린 뒤 성공 여부를 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        private IEnumerator WaitForSpaceAndAssert(Func<bool> isSuccess, string message)
        {
            while (!IsSpaceKeyPressed())
            {
                yield return null;
            }

            Assert.IsTrue(isSuccess(), message);
            yield return null;
        }

    #endregion

    #region 픽스처

        // ------------------------------------------------------------
        /// <summary>
        /// 생성한 테스트 오브젝트를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            if (uguiRoot      != null) UnityEngine.Object.DestroyImmediate(uguiRoot);
            if (uitkRoot      != null) UnityEngine.Object.DestroyImmediate(uitkRoot);
            if (eventSystemGO != null) UnityEngine.Object.DestroyImmediate(eventSystemGO);
            if (cameraGO      != null) UnityEngine.Object.DestroyImmediate(cameraGO);
            if (panelSettings != null) UnityEngine.Object.DestroyImmediate(panelSettings);
            if (themeStyleSheet != null) UnityEngine.Object.DestroyImmediate(themeStyleSheet);
        }

    #endregion

    #region M-1: UGUI / UITK 수동 확인

        // ----------------------------------------------------------------------
        /// <summary>
        /// UGUI와 UITK DragDrop을 PlayMode 화면에서 순서대로 직접 확인한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Explicit]
        [Category("Manual")]
        [UnityTest]
        public IEnumerator TEST_DragDropManualPlayMode_UGUI_UITK_드래그_수동확인()
        {
            CreateTestCamera();

            var eventSystem = CreateEventSystem();

            var isUGUIDropped = CreateUGUISample(eventSystem);
            yield return WaitForSpaceAndAssert
            (
                isUGUIDropped,
                "UGUI 드롭을 완료한 뒤 Space를 눌러야 합니다."
            );

            UnityEngine.Object.DestroyImmediate(uguiRoot);
            uguiRoot = null;

            var isUITKDropped = CreateUITKSample();
            yield return null;
            yield return WaitForSpaceAndAssert
            (
                isUITKDropped,
                "UITK 드롭을 완료한 뒤 Space를 눌러야 합니다."
            );
        }

    #endregion

    }
}
