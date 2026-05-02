/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TimerPropertyDrawer.cs
수정일 : 2026-05-02

# 설명
Timer/ITimer/IReadOnlyTimer 의 UI Toolkit 기반 PropertyDrawer.
fieldInfo.FieldType 기준으로 ITimer 할당 가능 여부를 판단해 모드를 결정한다.
- 컨트롤 모드  : duration 입력 + Start/Pause/Stop/Reset 버튼 (ITimer 구현체)
- 읽기 전용 모드: 상태·진행률 표시 전용 (IReadOnlyTimer 전용)
진행률은 50ms 간격으로 갱신되며, 패널에서 분리되면 스케줄러가 정지된다.

# 특이사항
PropertyDrawer 인스턴스는 타입당 하나를 공유한다.
인스턴스 필드에 상태를 저장하면 동일 타입의 다른 필드 갱신 시 덮어써지므로,
모든 상태를 CreatePropertyGUI 로컬로 선언하고 클로저로 캡처한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;

namespace inonego.Xeri
{
    using Utility;

    // ============================================================
    /// <summary>
    /// Timer PropertyDrawer — 상태·진행률 표시 + 선택적 컨트롤.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(ITimer), useForChildren: true)]
    [CustomPropertyDrawer(typeof(IReadOnlyTimer), useForChildren: true)]
    public class TimerPropertyDrawer : PropertyDrawer
    {

    #region 필드

        private static readonly Color ProgressColorPro      = new(0.3f, 0.6f, 1f, 0.8f);
        private static readonly Color ProgressColorPersonal = new(0.2f, 0.4f, 0.8f, 0.8f);

    #endregion

    #region 초기화

        // ------------------------------------------------------------
        /// <summary>
        /// UI Toolkit 기반 Inspector 요소를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // 모든 상태를 로컬로 선언해 호출마다 격리된 복사본을 만든다.
            bool           isControlMode = typeof(ITimer).IsAssignableFrom(fieldInfo.FieldType);
            IReadOnlyTimer readOnlyTimer = SerializedPropertyHelper.GetTargetObject<IReadOnlyTimer>(property);
            ITimer         timer         = isControlMode ? readOnlyTimer as ITimer : null;
            SerializedObject serializedObj = property.serializedObject;
            Object           targetObject  = property.serializedObject.targetObject;

            var root = LoadRoot();

            if (root == null) return new Label($"[{GetType().Name}] UXML을 찾을 수 없습니다.");

            root.Q<Label>("field-label").text = ObjectNames.NicifyVariableName(property.name);

            var stateLabel   = root.Q<Label>("state-label");
            var progressFill = root.Q<VisualElement>("progress-fill");
            var progressText = root.Q<Label>("progress-text");

            progressFill.style.backgroundColor = EditorGUIUtility.isProSkin ? ProgressColorPro : ProgressColorPersonal;

            FloatField    durationField = null;
            VisualElement buttonArea    = null;
            TimerState?   lastState     = null;

            // 수정 전 스냅샷 등록 — 이후 변경이 Undo 대상이 된다.
            void Execute(string undoName, Action<ITimer> action)
            {
                if (timer == null) return;

                Undo.RecordObject(targetObject, undoName);

                action(timer);

                EditorUtility.SetDirty(targetObject);
                serializedObj.Update();
            }

            // 타이머 상태가 변경된 경우에만 버튼 영역을 재구성한다.
            void RefreshButtonArea(TimerState state)
            {
                if (lastState == state) return;
                lastState = state;

                buttonArea.Clear();

                if (state == TimerState.Ready)
                {
                    // Stop/Reset 후 cached 값으로 복원
                    if (timer != null) durationField.SetValueWithoutNotify(timer.cached);

                    void Play()
                    {
                        var duration = timer?.cached ?? 0f;
                        Execute("타이머 시작", t => t.Start(duration));
                    }

                    buttonArea.Add(MakeIconButton("d_PlayButton", Play));
                    buttonArea.Add(MakeIconButton("d_Refresh",    () => Execute("타이머 리셋",  t => t.Reset())));
                }
                else if (state == TimerState.Run)
                {
                    buttonArea.Add(MakeIconButton("d_PauseButton", () => Execute("타이머 일시정지", t => t.Pause())));
                    buttonArea.Add(MakeIconButton("d_PreMatQuad",  () => Execute("타이머 정지",    t => t.Stop())));
                }
                else if (state == TimerState.Pause)
                {
                    buttonArea.Add(MakeIconButton("d_PlayButton",  () => Execute("타이머 재개", t => t.Resume())));
                    buttonArea.Add(MakeIconButton("d_PreMatQuad",  () => Execute("타이머 정지", t => t.Stop())));
                }
            }

            // 50ms 간격으로 호출되어 상태·진행률 및 컨트롤 요소를 갱신한다.
            void Refresh()
            {
                if (readOnlyTimer == null) return;

                var state    = readOnlyTimer.Current;
                var duration = readOnlyTimer.Duration;
                var elapsed  = readOnlyTimer.ElapsedTime;

                RefreshStateLabel(stateLabel, state);
                RefreshProgressBar(progressFill, progressText, elapsed, duration);

                if (isControlMode && timer != null)
                {
                    RefreshDurationField(durationField, state, duration);
                    RefreshButtonArea(state);
                }
            }

            if (isControlMode)
            {
                durationField = root.Q<FloatField>("duration-field");
                durationField.isDelayed = true;

                if (timer != null)
                {
                    durationField.SetValueWithoutNotify(timer.cached);
                }

                durationField.RegisterValueChangedCallback(e => Execute("타이머 Duration 변경", t => t.cached = e.newValue));

                buttonArea = root.Q<VisualElement>("button-area");
            }
            else
            {
                root.Q<VisualElement>("duration-field").style.display = DisplayStyle.None;
                root.Q<VisualElement>("button-area").style.display    = DisplayStyle.None;
            }

            // 패널에서 분리될 때 스케줄러를 정지해 불필요한 갱신을 막는다.
            var scheduledItem = root.schedule.Execute(Refresh).Every(50);
            root.RegisterCallback<DetachFromPanelEvent>(_ => scheduledItem.Pause());

            return root;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UXML과 공유 USS를 로드해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private VisualElement LoadRoot()
        {
            var dir  = EditorAssetHelper.GetScriptDirectory(GetType());
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{dir}/TimerPropertyDrawer.uxml");
            var uss  = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dir}/TimerPropertyDrawer.uss");

            if (uxml == null) return null;

            var root = uxml.CloneTree();

            if (uss != null) root.styleSheets.Add(uss);

            return root;
        }

    #endregion

    #region 갱신

        // ------------------------------------------------------------
        /// <summary>
        /// 상태 레이블의 텍스트와 색상을 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void RefreshStateLabel(Label label, TimerState state)
        {
            label.text = state.ToString();
            label.style.color = state switch
            {
                TimerState.Ready => Color.gray,
                TimerState.Run   => Color.green,
                TimerState.Pause => Color.yellow,
                _                => Color.white,
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 진행률 바의 채움 너비와 시간 텍스트를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void RefreshProgressBar(VisualElement fill, Label text, float elapsed, float duration)
        {
            var progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 0f;
            fill.style.width = Length.Percent(progress * 100f);
            text.text = $"{elapsed:F2} / {duration:F2}";
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지속 시간 입력 필드를 갱신한다. Ready면 편집 가능, 그 외면 비활성화.
        /// </summary>
        // ------------------------------------------------------------
        private static void RefreshDurationField(FloatField field, TimerState state, float duration)
        {
            var isReady = state == TimerState.Ready;
            field.SetEnabled(isReady);

            if (!isReady)
            {
                field.SetValueWithoutNotify(duration);
            }
        }

    #endregion

    #region 유틸리티

        // ------------------------------------------------------------
        /// <summary>
        /// 아이콘 버튼을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static Button MakeIconButton(string iconName, Action onClick)
        {
            var btn = new Button(onClick);
            btn.AddToClassList("timer-button");

            var img = new Image();
            img.image = EditorGUIUtility.IconContent(iconName).image;
            img.AddToClassList("timer-button__icon");

            btn.Add(img);
            return btn;
        }

    #endregion

    }
}
