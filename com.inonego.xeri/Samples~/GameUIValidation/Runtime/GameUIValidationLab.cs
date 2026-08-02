/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GameUIValidationLab.cs
수정일 : 2026-08-02

# 설명
Xeri Package Sample의 단일 검증 Scene에서 실제 Game UI Runtime의 Profile, Screen Stack,
Modal, Scene Fade, Focus와 Input 경로를 조립한다.

# 특이사항, 제약사항
전용 검증 Scene은 Sample Settings와 표준 Host로 독립 Runtime을 만들고 그 전체 수명을 소유한다.
이미 활성 App Runtime Host가 있으면 다른 UI 수명을 변경하지 않고 검증 시작을 거부한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego.Xeri.UI.Game;

namespace inonego.Xeri.Samples.GameUIValidation
{
    // ============================================================
    /// <summary>
    /// 검증용 Screen에 전달할 깊이와 Replace 표시 정보.
    /// </summary>
    // ============================================================
    internal readonly struct GameUIValidationScreenPayload
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 화면에 표시할 기대 Stack 깊이.
        /// </summary>
        // ------------------------------------------------------------
        public int Depth { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 화면이 Replace 결과인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsReplacement { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 검증용 화면 호출 정보를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public GameUIValidationScreenPayload
        (
            int depth,
            bool isReplacement
        ) : this()
        {
            Depth = Mathf.Max(1, depth);
            IsReplacement = isReplacement;
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// Xeri Game UI Core의 실제 공개 경로를 한 Scene에서 조작하는 검증 조립점.
    /// </summary>
    // ============================================================
    public sealed class GameUIValidationLab : MonoBehaviour
    {

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// Validation Layer에 Toast VisualElement를 획득·반환하는 Overlay Source.
        /// </summary>
        // ============================================================
        private sealed class ValidationOverlaySource : IOverlaySource<VisualElement>
        {

        #region 필드

            private readonly StyleSheet styleSheet = null;
            private readonly Action close = null;

        #endregion

        #region 생성자

            // ------------------------------------------------------------
            /// <summary>
            /// Toast StyleSheet와 닫기 명령을 연결한다.
            /// </summary>
            // ------------------------------------------------------------
            public ValidationOverlaySource
            (
                StyleSheet styleSheet,
                Action close
            ) : base()
            {
                this.styleSheet = styleSheet ?? throw new ArgumentNullException(nameof(styleSheet));
                this.close = close ?? throw new ArgumentNullException(nameof(close));
            }

        #endregion

        #region IOverlaySource

            // ------------------------------------------------------------
            /// <summary>
            /// 지정 UITK Layer에 검증용 Toast를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public VisualElement Acquire(IPresentationLayerDriver layer)
            {
                if (!(layer is IPresentationLayerDriver<VisualElement> typedLayer))
                {
                    throw new InvalidOperationException
                    (
                        "Validation Overlay Layer가 UITK Root를 제공하지 않습니다."
                    );
                }

                var root = new VisualElement
                {
                    name = "ValidationOverlayToast",
                };
                root.AddToClassList("overlay-toast");
                root.styleSheets.Add(styleSheet);

                var signal = new VisualElement();
                signal.AddToClassList("overlay-toast__signal");
                root.Add(signal);

                var content = new VisualElement();
                content.AddToClassList("overlay-toast__content");
                root.Add(content);

                var title = new Label("Overlay ownership acquired");
                title.AddToClassList("overlay-toast__title");
                content.Add(title);

                var copy = new Label("View + Layer Usage are held by one handle.");
                copy.AddToClassList("overlay-toast__copy");
                content.Add(copy);

                var closeButton = new Button(close)
                {
                    text = "×",
                };
                closeButton.AddToClassList("overlay-toast__close");
                root.Add(closeButton);

                typedLayer.Root.Add(root);
                return root;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Overlay Handle이 반환한 Toast를 Visual Tree에서 제거한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Release(VisualElement view)
            {
                if (view == null)
                {
                    throw new ArgumentNullException(nameof(view));
                }

                view.RemoveFromHierarchy();
            }

        #endregion

        }

        // ============================================================
        /// <summary>
        /// Modal VisualElement를 계층에서 제거하는 표시 수명 Handle.
        /// </summary>
        // ============================================================
        private sealed class VisualElementHandle : IDisposable
        {

        #region 필드

            private VisualElement element = null;

        #endregion

        #region 생성자

            // ------------------------------------------------------------
            /// <summary>
            /// 제거할 VisualElement를 소유한다.
            /// </summary>
            // ------------------------------------------------------------
            public VisualElementHandle(VisualElement element) : base()
            {
                this.element = element ?? throw new ArgumentNullException(nameof(element));
            }

        #endregion

        #region IDisposable

            // ------------------------------------------------------------
            /// <summary>
            /// 소유 VisualElement를 현재 Visual Tree에서 제거한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Dispose()
            {
                if (element == null) return;

                var current = element;
                element = null;
                current.RemoveFromHierarchy();
            }

        #endregion

        }

    #endregion

    #region 상수

        internal const string LAYER_ID = "ValidationScreen";
        internal const string DASHBOARD_SCREEN_ID = "GameUI.Validation.Dashboard";
        internal const string DETAIL_SCREEN_ID = "GameUI.Validation.Detail";

    #endregion

    #region 필드

        [SerializeField]
        private GameUIProfileAsset profile = null;

        [SerializeField]
        private VisualTreeAsset screenTemplate = null;

        [SerializeField]
        private StyleSheet screenStyle = null;

        [SerializeField]
        private GameObject runtimeHostPrefab = null;

        [SerializeField]
        private GameUISettingsAsset settings = null;

        private GameUIRuntime runtime = null;
        private GameObject ownedRuntimeHost = null;
        private GameUIProfileHandle profileHandle = null;
        private ScreenRegistrationHandle dashboardRegistration = null;
        private ScreenRegistrationHandle detailRegistration = null;
        private GameUIValidationScreenSource screenSource = null;
        private ModalHandle modalHandle = null;
        private OverlayHandle<VisualElement> overlayHandle = null;
        private Coroutine clearRoutine = null;
        private Coroutine fadeRoutine = null;
        private bool isComposed = false;
        private bool ownsFadeRequest = false;
        private string lastActivity = "Waiting for runtime composition…";

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Screen Stack 항목 수.
        /// </summary>
        // ------------------------------------------------------------
        internal int ScreenCount => IsRuntimeAvailable
            ? runtime.Screens.Count
            : 0;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Modal Stack 항목 수.
        /// </summary>
        // ------------------------------------------------------------
        internal int ModalCount => IsRuntimeAvailable
            ? runtime.Modals.Count
            : 0;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 top Screen ID.
        /// </summary>
        // ------------------------------------------------------------
        internal string TopScreenID => IsRuntimeAvailable && runtime.Screens.Top != null
            ? runtime.Screens.Top.ID
            : "—";

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Scene Fade 상태 이름.
        /// </summary>
        // ------------------------------------------------------------
        internal string FadeState => IsRuntimeAvailable
            ? runtime.SceneFader.State.ToString().ToUpperInvariant()
            : "OFFLINE";

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 UI 입력 장치 이름.
        /// </summary>
        // ------------------------------------------------------------
        internal string InputDeviceName
        {
            get
            {
                if (!IsRuntimeAvailable) return "—";

                var device = runtime.LastInputDevice;
                return device != null ? device.displayName : "NO INPUT";
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Color Space에 따른 Gamma 합성 설명.
        /// </summary>
        // ------------------------------------------------------------
        internal string GammaDescription => QualitySettings.activeColorSpace == ColorSpace.Linear
            ? "Linear / Gamma composite"
            : "Gamma / Direct panel";

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막 검증 명령 또는 수명 이벤트 설명.
        /// </summary>
        // ------------------------------------------------------------
        internal string LastActivity => lastActivity;

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime이 현재 검증 명령을 받을 수 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        private bool IsRuntimeAvailable =>
            runtime != null &&
            runtime.IsInitialized &&
            !runtime.IsReleasing &&
            !runtime.IsReleased;

    #endregion

    #region Unity 이벤트

        // ----------------------------------------------------------------------
        /// <summary>
        /// 기존 App Runtime이 없는지 확인한 뒤 독립 검증 Runtime과 화면을 조립한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private IEnumerator Start()
        {
            // BeforeSceneLoad Bootstrapper의 생성까지 기다려 App Runtime과 소유권이 겹치지 않게 한다.
            yield return null;

            if (!isActiveAndEnabled) yield break;

            try
            {
                if (FindAnyObjectByType<GameUIRuntime>() != null)
                {
                    throw new InvalidOperationException
                    (
                        "Game UI Validation은 독립 Runtime을 소유해야 합니다. " +
                        "App Game UI Bootstrapper가 비활성인 상태에서 검증 Scene을 실행하십시오."
                    );
                }

                CreateValidationRuntime();
                Compose();
            }
            catch (Exception exception)
            {
                ReleaseComposition();
                Debug.LogException(exception, this);
                enabled = false;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 검증 Scene이 닫히면 자신이 획득한 Screen, 등록과 Profile을 역순으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            ReleaseComposition();
        }

    #endregion

    #region 조립과 해제

        // ----------------------------------------------------------------------
        /// <summary>
        /// 검증 Profile, Screen Source와 두 Screen 등록을 준비하고 Dashboard를 연다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void Compose()
        {
            ValidateConfiguration();

            profileHandle = runtime.AcquireProfile(profile);

            screenSource = new GameUIValidationScreenSource
            (
                this,
                screenTemplate,
                screenStyle
            );

            dashboardRegistration = runtime.ScreenRegistry.Register
            (
                new ScreenOptions
                (
                    DASHBOARD_SCREEN_ID,
                    LAYER_ID,
                    ScreenDuplicatePolicy.Reject,
                    openDuration: 0.28f,
                    closeDuration: 0.2f
                ),
                screenSource
            );

            detailRegistration = runtime.ScreenRegistry.Register
            (
                new ScreenOptions
                (
                    DETAIL_SCREEN_ID,
                    LAYER_ID,
                    ScreenDuplicatePolicy.Allow,
                    openDuration: 0.24f,
                    closeDuration: 0.18f
                ),
                screenSource
            );

            isComposed = true;
            RecordActivity("Profile acquired · screens registered");
            RequireAccepted(runtime.Screens.Open(DASHBOARD_SCREEN_ID), "Dashboard Open");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Scene 직렬화 참조와 Runtime 상태를 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ValidateConfiguration()
        {
            if (!IsRuntimeAvailable)
            {
                throw new InvalidOperationException("초기화된 GameUIRuntime을 찾지 못했습니다.");
            }

            if (profile == null)
            {
                throw new InvalidOperationException("검증용 Game UI Profile이 연결되지 않았습니다.");
            }

            if (screenTemplate == null)
            {
                throw new InvalidOperationException("검증용 Screen UXML이 연결되지 않았습니다.");
            }

            if (screenStyle == null)
            {
                throw new InvalidOperationException("검증용 Screen USS가 연결되지 않았습니다.");
            }

            if (runtimeHostPrefab == null)
            {
                throw new InvalidOperationException("검증용 Game UI Host Prefab이 연결되지 않았습니다.");
            }

            if (settings == null)
            {
                throw new InvalidOperationException("검증용 Game UI Settings가 연결되지 않았습니다.");
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 표준 Host Prefab과 Sample Settings로 검증 Scene 전용 Runtime을 만든다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void CreateValidationRuntime()
        {
            if (runtimeHostPrefab == null)
            {
                throw new InvalidOperationException("검증용 Game UI Host Prefab이 연결되지 않았습니다.");
            }

            if (settings == null)
            {
                throw new InvalidOperationException("검증용 Game UI Settings가 연결되지 않았습니다.");
            }

            ownedRuntimeHost = Instantiate(runtimeHostPrefab);
            ownedRuntimeHost.name = "GameUIValidationRuntime";
            runtime = ownedRuntimeHost.GetComponent<GameUIRuntime>();

            if (runtime == null)
            {
                Destroy(ownedRuntimeHost);
                ownedRuntimeHost = null;
                throw new InvalidOperationException
                (
                    "검증용 Host Prefab Root에 GameUIRuntime이 없습니다."
                );
            }

            try
            {
                runtime.Initialize(settings);
            }
            catch
            {
                Destroy(ownedRuntimeHost);
                ownedRuntimeHost = null;
                runtime = null;
                throw;
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 진행 중 표시 요청을 끝내고 Screen, 등록, Source와 Profile을 역순으로 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ReleaseComposition()
        {
            if
            (
                !isComposed &&
                profileHandle == null &&
                dashboardRegistration == null &&
                detailRegistration == null &&
                screenSource == null &&
                ownedRuntimeHost == null
            )
            {
                return;
            }

            isComposed = false;

            if (clearRoutine != null)
            {
                StopCoroutine(clearRoutine);
                clearRoutine = null;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            var errors = new List<Exception>();

            ReleaseOwnedFade(errors);
            DisposeOwned(overlayHandle, errors);
            overlayHandle = null;
            DisposeOwned(modalHandle, errors);
            modalHandle = null;

            if (IsRuntimeAvailable)
            {
                try
                {
                    runtime.Screens.Clear();
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            DisposeOwned(detailRegistration, errors);
            detailRegistration = null;
            DisposeOwned(dashboardRegistration, errors);
            dashboardRegistration = null;
            DisposeOwned(screenSource, errors);
            screenSource = null;
            DisposeOwned(profileHandle, errors);
            profileHandle = null;

            if (ownedRuntimeHost != null)
            {
                try
                {
                    runtime?.Shutdown();
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }

                Destroy(ownedRuntimeHost);
                ownedRuntimeHost = null;
            }

            runtime = null;

            if (errors.Count > 0)
            {
                Debug.LogException
                (
                    new AggregateException
                    (
                        "Game UI Validation 조립 해제 중 하나 이상의 정리가 실패했습니다.",
                        errors
                    ),
                    this
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 검증 Scene이 시작한 Fade를 즉시 Clear 상태로 돌려놓는다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseOwnedFade(List<Exception> errors)
        {
            if (!ownsFadeRequest || !IsRuntimeAvailable) return;

            ownsFadeRequest = false;

            if (runtime.SceneFader.State == SceneFadeState.Clear) return;

            try
            {
                runtime.SceneFader.Reveal
                (
                    new SceneFadeParams(Color.black, 0.0f)
                );
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 단일 소유 객체를 해제하고 후속 정리가 계속되도록 오류를 수집한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void DisposeOwned
        (
            IDisposable owned,
            List<Exception> errors
        )
        {
            if (owned == null) return;

            try
            {
                owned.Dispose();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

    #endregion

    #region Screen 명령

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 top 위에 Detail Screen을 Push한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void PushDetail(int currentDepth)
        {
            if (!IsRuntimeAvailable) return;

            var payload = new GameUIValidationScreenPayload
            (
                currentDepth + 1,
                false
            );
            var response = runtime.Screens.Open
            (
                DETAIL_SCREEN_ID,
                new ScreenOpenParams(payload)
            );

            RecordResponse("Push Detail", response);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Detail top을 새 Detail 인스턴스로 Replace한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void ReplaceDetail(int currentDepth)
        {
            if (!IsRuntimeAvailable) return;

            var payload = new GameUIValidationScreenPayload
            (
                currentDepth,
                true
            );
            var response = runtime.Screens.Replace
            (
                DETAIL_SCREEN_ID,
                new ScreenOpenParams(payload)
            );

            RecordResponse("Replace Detail", response);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 top Screen 하나를 일반 Close 경로로 Pop한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void PopScreen()
        {
            if (!IsRuntimeAvailable) return;

            var accepted = runtime.Screens.Close();
            RecordActivity(accepted ? "Pop accepted" : "Pop rejected");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Screen Stack을 강제 Clear한 다음 다음 프레임에 Dashboard를 다시 연다.
        /// </summary>
        // ------------------------------------------------------------
        internal void ClearAndRestore()
        {
            if (!IsRuntimeAvailable || clearRoutine != null) return;

            clearRoutine = StartCoroutine(ClearAndRestoreRoutine());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Clear 완료 뒤 등록이 유지된 Dashboard를 새 Session으로 다시 연다.
        /// </summary>
        // ------------------------------------------------------------
        private IEnumerator ClearAndRestoreRoutine()
        {
            RecordActivity("Clear requested · releasing all screens");
            runtime.Screens.Clear();
            yield return null;

            clearRoutine = null;

            if (!isComposed || !IsRuntimeAvailable) yield break;

            var response = runtime.Screens.Open(DASHBOARD_SCREEN_ID);
            RecordResponse("Dashboard restore", response);
        }

    #endregion

    #region Modal과 Fade

        // ----------------------------------------------------------------------
        /// <summary>
        /// Validation Layer에서 Toast Overlay를 획득하거나 현재 Handle을 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal void ToggleOverlay(ScreenSession ownerSession)
        {
            if (!IsRuntimeAvailable || ownerSession == null) return;

            if (overlayHandle != null && !overlayHandle.IsDisposed)
            {
                CloseOverlay();
                return;
            }

            var source = new ValidationOverlaySource(screenStyle, CloseOverlay);
            var opened = OverlayHandle<VisualElement>.Acquire
            (
                runtime.LayerRegistry,
                LAYER_ID,
                source
            );

            try
            {
                ownerSession.RegisterChild(opened);
                overlayHandle = opened;
                RecordActivity("Overlay acquired · view and layer usage owned");
            }
            catch
            {
                opened.Dispose();
                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Toast Overlay의 View와 Layer Usage를 함께 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CloseOverlay()
        {
            if (overlayHandle == null || overlayHandle.IsDisposed) return;

            var current = overlayHandle;
            overlayHandle = null;
            current.Dispose();
            RecordActivity("Overlay released · view and layer usage returned");
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 Validation Layer Usage와 VisualElement 수명을 Modal Handle에 이전한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal void OpenModal
        (
            ScreenSession ownerSession,
            VisualElement layerRoot
        )
        {
            if (!IsRuntimeAvailable || ownerSession == null || layerRoot == null) return;

            if (modalHandle != null && !modalHandle.IsDisposed)
            {
                RecordActivity("Modal Open rejected · already active");
                return;
            }

            var modalRoot = CreateModalRoot();
            layerRoot.Add(modalRoot);
            var visualHandle = new VisualElementHandle(modalRoot);
            ModalHandle opened = null;

            try
            {
                opened = runtime.Modals.Open
                (
                    new UITKModalDriver(modalRoot),
                    visualHandle
                );
                ownerSession.RegisterChild(opened);
                modalHandle = opened;
                RecordActivity("Modal opened · handle owned by screen session");

                modalRoot.schedule.Execute
                (
                    () => modalRoot.Q<Button>("CloseModalButton")?.Focus()
                );
            }
            catch
            {
                if (opened != null)
                {
                    opened.Dispose();
                }
                else
                {
                    visualHandle.Dispose();
                }

                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Modal Handle을 닫고 표시 소유권을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CloseModal()
        {
            if (modalHandle == null || modalHandle.IsDisposed) return;

            var current = modalHandle;
            modalHandle = null;
            current.Dispose();
            RecordActivity("Modal closed · visual and layer usage released");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 검증용 Modal Visual Tree를 생성하고 닫기 명령을 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private VisualElement CreateModalRoot()
        {
            var root = new VisualElement
            {
                name = "ValidationModal",
            };
            root.AddToClassList("modal-surface");
            root.styleSheets.Add(screenStyle);

            var card = new VisualElement();
            card.AddToClassList("modal-card");
            root.Add(card);

            var eyebrow = new Label("MODAL CONTROLLER / OWNED HANDLE");
            eyebrow.AddToClassList("modal-card__eyebrow");
            card.Add(eyebrow);

            var title = new Label("Nothing leaks past this card.");
            title.AddToClassList("modal-card__title");
            card.Add(title);

            var copy = new Label
            (
                "The modal owns its VisualElement removal and Presentation Layer usage. " +
                "Closing the parent Screen also releases this card."
            );
            copy.AddToClassList("modal-card__copy");
            card.Add(copy);

            var rule = new VisualElement();
            rule.AddToClassList("modal-card__rule");
            card.Add(rule);

            var closeButton = new Button(CloseModal)
            {
                name = "CloseModalButton",
                text = "CLOSE MODAL",
            };
            closeButton.AddToClassList("button");
            closeButton.AddToClassList("button--primary");
            card.Add(closeButton);

            return root;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// App 기본 SceneFader로 Cover 후 잠시 유지하고 Reveal한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void RunFade()
        {
            if (!IsRuntimeAvailable || fadeRoutine != null) return;

            fadeRoutine = StartCoroutine(FadeRoutine());
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Cover와 Reveal 완료를 실제 Fade callback 경계에서 관찰한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private IEnumerator FadeRoutine()
        {
            ownsFadeRequest = true;
            var coverCompleted = false;
            Exception fadeFailure = null;
            var fade = new SceneFadeParams
            (
                new Color(0.02f, 0.05f, 0.12f, 1.0f),
                0.28f
            );

            RecordActivity("SceneFader Cover requested");
            runtime.SceneFader.Cover
            (
                fade,
                () => coverCompleted = true,
                exception => fadeFailure = exception
            );

            while (!coverCompleted && fadeFailure == null)
            {
                yield return null;
            }

            if (fadeFailure != null)
            {
                ownsFadeRequest = false;
                fadeRoutine = null;
                Debug.LogException(fadeFailure, this);
                RecordActivity("SceneFader Cover failed");
                yield break;
            }

            RecordActivity("SceneFader Covered · reveal queued");
            yield return new WaitForSecondsRealtime(0.18f);

            var revealCompleted = false;
            runtime.SceneFader.Reveal
            (
                fade,
                () => revealCompleted = true,
                exception => fadeFailure = exception
            );

            while (!revealCompleted && fadeFailure == null)
            {
                yield return null;
            }

            ownsFadeRequest = false;
            fadeRoutine = null;

            if (fadeFailure != null)
            {
                Debug.LogException(fadeFailure, this);
                RecordActivity("SceneFader Reveal failed");
                yield break;
            }

            RecordActivity("SceneFader Reveal completed");
        }

    #endregion

    #region 상태 보고

        // ------------------------------------------------------------
        /// <summary>
        /// Screen 상태 훅에서 관찰한 이벤트를 Dashboard에 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void RecordLifecycle
        (
            string screenID,
            ScreenState state
        )
        {
            RecordActivity($"{ShortScreenID(screenID)} · {state}");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Screen Open 계열 응답을 사용자 표시와 오류 로그에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RecordResponse
        (
            string action,
            ScreenOpenResponse response
        )
        {
            if (response.Accepted)
            {
                RecordActivity($"{action} accepted · stack {ScreenCount}");
                return;
            }

            RecordActivity($"{action} {response.Kind} · {response.Error}");

            if (response.Exception != null)
            {
                Debug.LogException(response.Exception, this);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 필수 초기 Open이 거부되면 조립 실패로 전환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void RequireAccepted
        (
            ScreenOpenResponse response,
            string operation
        )
        {
            if (response.Accepted) return;

            throw new InvalidOperationException
            (
                $"{operation}이 거부됐습니다. {response.Kind}: {response.Error}",
                response.Exception
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 최신 검증 명령 또는 수명 이벤트를 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RecordActivity(string activity)
        {
            lastActivity = activity ?? "";
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Stable Screen ID에서 검증 화면에 필요한 마지막 구간만 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static string ShortScreenID(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "—";

            var separator = id.LastIndexOf('.');
            return separator >= 0 && separator + 1 < id.Length
                ? id[(separator + 1)..]
                : id;
        }

    #endregion

    }
}
