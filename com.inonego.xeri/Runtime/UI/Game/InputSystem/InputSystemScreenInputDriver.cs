/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : InputSystemScreenInputDriver.cs
수정일 : 2026-07-29

# 설명
Screen 입력 정책을 Input System Action Map과 Cursor 상태에 합성하고 입력 해제 장벽을 갱신한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Input System 기반 Screen 입력 정책 backend.
    /// </summary>
    // ============================================================
    public sealed class InputSystemScreenInputDriver : MonoBehaviour, IScreenInputDriver
    {
    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// 획득 순서와 입력 해제 안정화 상태를 함께 보관한다.
        /// </summary>
        // ============================================================
        private sealed class Entry
        {
            // ------------------------------------------------------------
            /// <summary>
            /// Screen 입력 정책 Session.
            /// </summary>
            // ------------------------------------------------------------
            public ScreenInputSession Session { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 같은 우선순위에서 최신 정책을 선택할 획득 순서.
            /// </summary>
            // ------------------------------------------------------------
            public long Sequence { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 입력 해제 후 복원을 허용할 최초 Frame.
            /// </summary>
            // ------------------------------------------------------------
            public int ReleaseFrame { get; set; } = -1;

            // ------------------------------------------------------------
            /// <summary>
            /// 입력 Session Entry를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public Entry
            (
                ScreenInputSession session,
                long sequence
            ) : base()
            {
                Session = session ?? throw new ArgumentNullException(nameof(session));
                Sequence = sequence;
            }
        }

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// backend 초기화가 끝났는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsInitialized { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 가장 최근 수행된 Input Action의 장치.
        /// </summary>
        // ------------------------------------------------------------
        public InputDevice LastInputDevice { get; private set; }

        private readonly List<Entry> entries = new List<Entry>();
        private readonly List<Entry> readyToRelease = new List<Entry>();
        private readonly List<InputAction> releaseActions = new List<InputAction>();

        private InputSystemUIInputModule inputModule = null;
        private InputActionMap uiActionMap = null;
        private InputActionMap gameplayActionMap = null;

        private bool baselineCaptured = false;
        private bool baselineUIEnabled = false;
        private bool baselineGameplayEnabled = false;
        private bool baselineCursorVisible = false;
        private CursorLockMode baselineCursorLockMode = CursorLockMode.None;

        private bool appliedUIEnabled = false;
        private bool appliedGameplayEnabled = false;
        private bool appliedCursorVisible = false;
        private CursorLockMode appliedCursorLockMode = CursorLockMode.None;

        private int batchDepth = 0;
        private bool applyPending = false;
        private long nextSequence = 0;
        private bool isDisposed = false;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막 입력 장치가 바뀌었을 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<InputDevice> OnLastInputDeviceChanged = null;

    #endregion

    #region 초기화

        // ------------------------------------------------------------
        /// <summary>
        /// Host Input Module과 Settings의 Action Map 구성을 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Initialize
        (
            InputSystemUIInputModule inputModule,
            GameUISettingsAsset settings
        )
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Input System Screen Input Driver가 이미 초기화됐습니다.");
            }

            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(InputSystemScreenInputDriver));
            }

            this.inputModule = inputModule ?? throw new ArgumentNullException(nameof(inputModule));

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var asset = inputModule.actionsAsset;

            if (asset == null)
            {
                throw new InvalidOperationException("InputSystemUIInputModule Actions Asset이 설정되지 않았습니다.");
            }

            uiActionMap = FindMap(asset, settings.UIActionMap, "UI");
            gameplayActionMap = FindMap(asset, settings.GameplayActionMap, "Gameplay");

            if (ReferenceEquals(uiActionMap, gameplayActionMap))
            {
                throw new InvalidOperationException("UI와 Gameplay Action Map은 서로 달라야 합니다.");
            }

            BuildReleaseActions(settings.ReleaseActionNames);
            InputSystem.onActionChange += HandleActionChange;
            IsInitialized = true;
        }

    #endregion

    #region Unity 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 입력 해제 대기 Session과 한 Frame 안정화 장벽을 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Update()
        {
            if (!IsInitialized || isDisposed || entries.Count == 0) return;

            var isReleaseInputPressed = IsReleaseInputPressed();
            readyToRelease.Clear();

            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];

                if (!entry.Session.IsAwaitingRelease) continue;

                if (isReleaseInputPressed)
                {
                    entry.ReleaseFrame = -1;
                    continue;
                }

                if (entry.ReleaseFrame < 0)
                {
                    entry.ReleaseFrame = Time.frameCount + 1;
                    continue;
                }

                if (Time.frameCount < entry.ReleaseFrame) continue;

                readyToRelease.Add(entry);
            }

            if (readyToRelease.Count == 0) return;

            for (var i = 0; i < readyToRelease.Count; i++)
            {
                entries.Remove(readyToRelease[i]);
            }

            try
            {
                RequestApply();
            }
            catch (Exception exception)
            {
                entries.AddRange(readyToRelease);
                entries.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));

                try
                {
                    RequestApply();
                }
                catch (Exception rollbackException)
                {
                    readyToRelease.Clear();
                    throw new AggregateException
                    (
                        "입력 해제 완료 적용과 이전 유효 상태 복원이 실패했습니다.",
                        exception,
                        rollbackException
                    );
                }

                readyToRelease.Clear();
                throw;
            }

            List<Exception> releaseErrors = null;

            for (var i = 0; i < readyToRelease.Count; i++)
            {
                try
                {
                    readyToRelease[i].Session.MarkReleased();
                }
                catch (Exception exception)
                {
                    releaseErrors ??= new List<Exception>();
                    releaseErrors.Add(exception);
                }
            }

            readyToRelease.Clear();

            if (releaseErrors != null)
            {
                throw new AggregateException
                (
                    "입력 해제 완료 callback 중 하나 이상이 실패했습니다.",
                    releaseErrors
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 파괴 시 명시적 해제가 누락된 backend 구독을 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDestroy()
        {
            Dispose();
        }

    #endregion

    #region IScreenInputDriver

        // ------------------------------------------------------------
        /// <summary>
        /// Screen Options에 맞는 입력 정책 Session을 획득한다.
        /// </summary>
        // ------------------------------------------------------------
        public ScreenInputSession Acquire(ScreenOptions options)
        {
            ThrowIfUnavailable();

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (!baselineCaptured)
            {
                CaptureBaseline();
            }

            var session = new ScreenInputSession(options, Release);
            var entry = new Entry(session, nextSequence++);
            entries.Add(entry);

            try
            {
                RequestApply();
                return session;
            }
            catch (Exception exception)
            {
                entries.Remove(entry);

                try
                {
                    RequestApply();
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException
                    (
                        "Screen 입력 정책 획득과 이전 유효 상태 복원이 실패했습니다.",
                        exception,
                        rollbackException
                    );
                }

                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 여러 Session 해제 중 중간 입력 상태 적용을 보류한다.
        /// </summary>
        // ------------------------------------------------------------
        public void BeginBatch()
        {
            ThrowIfUnavailable();
            batchDepth++;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 배치 해제 뒤 최종 입력 상태를 한 번 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void EndBatch()
        {
            ThrowIfUnavailable();

            if (batchDepth <= 0)
            {
                throw new InvalidOperationException("입력 정책 Batch가 시작되지 않았습니다.");
            }

            batchDepth--;

            if (batchDepth == 0 && applyPending)
            {
                ApplyEffectiveState();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 입력 해제를 기다리지 않고 모든 Session을 역순 강제 종결한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ForceReleaseAll()
        {
            if (!IsInitialized || isDisposed) return;

            for (var i = entries.Count - 1; i >= 0; i--)
            {
                entries[i].Session.MarkReleased();
            }

            entries.Clear();
            batchDepth = 0;
            applyPending = true;
            ApplyEffectiveState();
        }

    #endregion

    #region 입력 정책

        // ------------------------------------------------------------
        /// <summary>
        /// Session 반환 요청을 즉시 종료하거나 입력 해제 대기 상태로 전환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Release
        (
            ScreenInputSession session,
            bool waitForInputRelease,
            bool retainCursorWhileAwaitingRelease
        )
        {
            var index = FindEntry(session);

            if (index < 0)
            {
                session.MarkReleased();
                return;
            }

            var entry = entries[index];

            if (waitForInputRelease && IsReleaseInputPressed())
            {
                entry.ReleaseFrame = -1;
                session.MarkAwaitingRelease(retainCursorWhileAwaitingRelease);

                try
                {
                    RequestApply();
                }
                catch (Exception exception)
                {
                    session.ClearAwaitingRelease();

                    try
                    {
                        RequestApply();
                    }
                    catch (Exception rollbackException)
                    {
                        throw new AggregateException
                        (
                            "입력 해제 대기 적용과 이전 유효 상태 복원이 실패했습니다.",
                            exception,
                            rollbackException
                        );
                    }

                    throw;
                }

                return;
            }

            entries.RemoveAt(index);

            try
            {
                RequestApply();
            }
            catch (Exception exception)
            {
                entries.Insert(index, entry);

                try
                {
                    RequestApply();
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException
                    (
                        "입력 정책 해제와 이전 유효 상태 복원이 실패했습니다.",
                        exception,
                        rollbackException
                    );
                }

                throw;
            }

            session.MarkReleased();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Session Entry의 현재 인덱스를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private int FindEntry(ScreenInputSession session)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                if (ReferenceEquals(entries[i].Session, session))
                {
                    return i;
                }
            }

            return -1;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 입력 정책 적용 또는 Batch 종료 시점 적용을 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RequestApply()
        {
            applyPending = true;

            if (batchDepth == 0)
            {
                ApplyEffectiveState();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 생존 Session 정책을 합성해 Action Map과 Cursor에 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyEffectiveState()
        {
            if (!baselineCaptured)
            {
                applyPending = false;
                return;
            }

            if (entries.Count == 0)
            {
                ApplyState
                (
                    baselineUIEnabled,
                    baselineGameplayEnabled,
                    baselineCursorVisible,
                    baselineCursorLockMode
                );

                baselineCaptured = false;
                applyPending = false;
                return;
            }

            var blocksGameplay = false;
            var hasReleaseBarrier = false;
            Entry cursorEntry = null;
            Entry retainedCursorEntry = null;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                if (entry.Session.IsAwaitingRelease)
                {
                    hasReleaseBarrier = true;

                    if (entry.Session.RetainsCursorWhileAwaitingRelease &&
                        (retainedCursorEntry == null ||
                        entry.Sequence > retainedCursorEntry.Sequence))
                    {
                        retainedCursorEntry = entry;
                    }

                    continue;
                }

                blocksGameplay |= entry.Session.Options.BlocksGameplayInput;

                if (cursorEntry == null ||
                    entry.Session.Options.InputPriority > cursorEntry.Session.Options.InputPriority ||
                    (entry.Session.Options.InputPriority == cursorEntry.Session.Options.InputPriority &&
                    entry.Sequence > cursorEntry.Sequence))
                {
                    cursorEntry = entry;
                }
            }

            var effectiveCursorEntry = retainedCursorEntry ?? cursorEntry;
            var cursorVisible = effectiveCursorEntry != null
                ? effectiveCursorEntry.Session.Options.ShowsCursor
                : baselineCursorVisible;
            var cursorLockMode = effectiveCursorEntry != null
                ? effectiveCursorEntry.Session.Options.CursorLockMode
                : baselineCursorLockMode;

            ApplyState
            (
                true,
                hasReleaseBarrier || blocksGameplay ? false : baselineGameplayEnabled,
                cursorVisible,
                cursorLockMode
            );

            applyPending = false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 변경된 값만 실제 Action Map과 Cursor에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyState
        (
            bool uiEnabled,
            bool gameplayEnabled,
            bool cursorVisible,
            CursorLockMode cursorLockMode
        )
        {
            if (uiEnabled != appliedUIEnabled)
            {
                SetMapEnabled(uiActionMap, uiEnabled);
                appliedUIEnabled = uiEnabled;
            }

            if (gameplayEnabled != appliedGameplayEnabled)
            {
                SetMapEnabled(gameplayActionMap, gameplayEnabled);
                appliedGameplayEnabled = gameplayEnabled;
            }

            if (cursorVisible != appliedCursorVisible)
            {
                Cursor.visible = cursorVisible;
                appliedCursorVisible = cursorVisible;
            }

            if (cursorLockMode != appliedCursorLockMode)
            {
                Cursor.lockState = cursorLockMode;
                appliedCursorLockMode = cursorLockMode;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Action Map의 활성 상태를 중복 호출 없이 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        private static void SetMapEnabled
        (
            InputActionMap map,
            bool enabled
        )
        {
            if (enabled)
            {
                if (!map.enabled)
                {
                    map.Enable();
                }
            }
            else if (map.enabled)
            {
                map.Disable();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 첫 Session 직전 복원 기준 상태를 캡처한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CaptureBaseline()
        {
            baselineUIEnabled = uiActionMap.enabled;
            baselineGameplayEnabled = gameplayActionMap.enabled;
            baselineCursorVisible = Cursor.visible;
            baselineCursorLockMode = Cursor.lockState;

            appliedUIEnabled = baselineUIEnabled;
            appliedGameplayEnabled = baselineGameplayEnabled;
            appliedCursorVisible = baselineCursorVisible;
            appliedCursorLockMode = baselineCursorLockMode;
            baselineCaptured = true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 설정된 닫기 입력 중 하나라도 유지 중인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool IsReleaseInputPressed()
        {
            for (var i = 0; i < releaseActions.Count; i++)
            {
                var controls = releaseActions[i].controls;

                // 비활성 Action도 실제 Control 상태는 유지하므로 Gameplay Map 차단 중 해제를 관찰할 수 있다.
                for (var controlIndex = 0; controlIndex < controls.Count; controlIndex++)
                {
                    if (controls[controlIndex].IsPressed())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

    #endregion

    #region 설정

        // ------------------------------------------------------------
        /// <summary>
        /// Actions Asset에서 필수 Action Map을 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private static InputActionMap FindMap
        (
            InputActionAsset asset,
            string mapName,
            string role
        )
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                throw new InvalidOperationException($"{role} Action Map 이름이 비어 있습니다.");
            }

            var map = asset.FindActionMap(mapName, false);

            if (map == null)
            {
                throw new InvalidOperationException
                (
                    $"{role} Action Map '{mapName}'을 Input Actions Asset에서 찾을 수 없습니다."
                );
            }

            return map;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UI와 Gameplay Map에서 입력 해제 장벽 Action을 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void BuildReleaseActions(IReadOnlyList<string> actionNames)
        {
            releaseActions.Clear();

            if (actionNames == null) return;

            for (var i = 0; i < actionNames.Count; i++)
            {
                var actionName = actionNames[i];

                if (string.IsNullOrWhiteSpace(actionName)) continue;

                var uiAction = uiActionMap.FindAction(actionName, false);
                var gameplayAction = gameplayActionMap.FindAction(actionName, false);

                if (uiAction == null && gameplayAction == null)
                {
                    throw new InvalidOperationException
                    (
                        $"입력 해제 Action '{actionName}'을 UI 또는 Gameplay Map에서 찾을 수 없습니다."
                    );
                }

                if (uiAction != null && !releaseActions.Contains(uiAction))
                {
                    releaseActions.Add(uiAction);
                }

                if (gameplayAction != null && !releaseActions.Contains(gameplayAction))
                {
                    releaseActions.Add(gameplayAction);
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 수행된 Input Action의 장치가 바뀌면 마지막 입력 장치를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleActionChange
        (
            object changedObject,
            InputActionChange change
        )
        {
            if (change != InputActionChange.ActionPerformed ||
                changedObject is not InputAction action)
            {
                return;
            }

            var device = action.activeControl?.device;

            if (device == null || ReferenceEquals(device, LastInputDevice)) return;

            LastInputDevice = device;
            InvokeDeviceChanged(device);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막 입력 장치 구독자를 독립 호출해 Input System 갱신을 보호한다.
        /// </summary>
        // ------------------------------------------------------------
        private void InvokeDeviceChanged(InputDevice device)
        {
            if (OnLastInputDeviceChanged == null) return;

            var invocationList = OnLastInputDeviceChanged.GetInvocationList();

            for (var i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    ((Action<InputDevice>)invocationList[i]).Invoke(device);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 초기화·해제 상태가 유효한지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ThrowIfUnavailable()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(InputSystemScreenInputDriver));
            }

            if (!IsInitialized)
            {
                throw new InvalidOperationException("Input System Screen Input Driver가 초기화되지 않았습니다.");
            }
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 입력 Session과 backend 구독을 해제하고 기준 상태를 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            if (IsInitialized)
            {
                ForceReleaseAll();
                InputSystem.onActionChange -= HandleActionChange;
            }

            releaseActions.Clear();
            entries.Clear();
            readyToRelease.Clear();
            inputModule = null;
            uiActionMap = null;
            gameplayActionMap = null;
            OnLastInputDeviceChanged = null;
            IsInitialized = false;
            isDisposed = true;
        }

    #endregion

    }
}
