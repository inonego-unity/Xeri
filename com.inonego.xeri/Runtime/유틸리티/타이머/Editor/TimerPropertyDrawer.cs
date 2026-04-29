/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TimerPropertyDrawer.cs
수정일 : 2026-04-30

# 설명
Timer/ITimer/IReadOnlyTimer 의 UI Toolkit 기반 PropertyDrawer.
fieldInfo.FieldType 기준으로 ITimer 할당 가능 여부를 판단해 모드를 결정한다.
- 컨트롤 모드  : duration 입력 + Start/Pause/Stop/Reset 버튼 (ITimer 구현체)
- 읽기 전용 모드: 상태·진행률 표시 전용 (IReadOnlyTimer 전용)
진행률은 50ms 간격으로 갱신된다.
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

        private Label         stateLabel;
        private VisualElement progressFill;
        private Label         progressText;

        private bool          isControlMode;
        private TimerState?   lastState;
        private FloatField    durationField;
        private VisualElement buttonArea;

        private IReadOnlyTimer     _IReadOnlyTimer;
        private ITimer             _ITimer;
        private SerializedObject   serializedObj;
        private Object             targetObject;

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
            // fieldInfo.FieldType 기준: ITimer 할당 가능 여부로 모드 결정
            // [SerializeReference] IReadOnlyTimer 선언 필드는 Timer 인스턴스가 있어도 읽기 전용.
            isControlMode = typeof(ITimer).IsAssignableFrom(fieldInfo.FieldType);
            _IReadOnlyTimer = SerializedPropertyHelper.GetTargetObject<IReadOnlyTimer>(property);
            _ITimer       = isControlMode ? _IReadOnlyTimer as ITimer : null;
            serializedObj = property.serializedObject;
            targetObject    = property.serializedObject.targetObject;

            var root = LoadRoot();

            if (root == null) return new Label($"[{GetType().Name}] UXML을 찾을 수 없습니다.");

            root.Q<Label>("field-label").text = ObjectNames.NicifyVariableName(property.name);

            stateLabel   = root.Q<Label>("state-label");
            progressFill = root.Q<VisualElement>("progress-fill");
            progressText = root.Q<Label>("progress-text");

            progressFill.style.backgroundColor = EditorGUIUtility.isProSkin ? ProgressColorPro : ProgressColorPersonal;

            if (isControlMode)
            {
                SetupControls(root);
            }
            else
            {
                root.Q<VisualElement>("duration-field").style.display = DisplayStyle.None;
                root.Q<VisualElement>("button-area").style.display    = DisplayStyle.None;
            }

            root.schedule.Execute(() => Refresh()).Every(50);
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

        // ------------------------------------------------------------
        /// <summary>
        /// 컨트롤 요소를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        private void SetupControls(VisualElement root)
        {
            durationField = root.Q<FloatField>("duration-field");
            durationField.isDelayed = true;

            if (_ITimer != null)
            {
                durationField.SetValueWithoutNotify(_ITimer.cached);
            }

            void OnDurationChanged(ChangeEvent<float> e)
            {
                if (_ITimer == null) return;
                _ITimer.cached = e.newValue;
                Apply();
            }

            durationField.RegisterValueChangedCallback(OnDurationChanged);

            buttonArea = root.Q<VisualElement>("button-area");
        }

    #endregion

    #region 조작

        // ------------------------------------------------------------
        /// <summary>
        /// 오브젝트를 dirty로 표시해 씬 저장 대상에 포함시킨다.
        /// </summary>
        // ------------------------------------------------------------
        private void Apply()
        {
            serializedObj.Update();
            EditorUtility.SetDirty(targetObject);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// _ITimer로 타이머를 조작하고 dirty 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Execute(Action<ITimer> action)
        {
            if (_ITimer == null) return;
            action(_ITimer);
            Apply();
        }

    #endregion

    #region 갱신

        // ------------------------------------------------------------
        /// <summary>
        /// 50ms 간격으로 호출되어 상태·진행률 및 컨트롤 요소를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Refresh()
        {
            if (_IReadOnlyTimer == null) return;

            var state    = _IReadOnlyTimer.Current;
            var duration = _IReadOnlyTimer.Duration;
            var elapsed  = _IReadOnlyTimer.ElapsedTime;

            RefreshStateLabel(stateLabel, state);
            RefreshProgressBar(progressFill, progressText, elapsed, duration);

            if (isControlMode && _ITimer != null)
            {
                RefreshDurationField(durationField, state, duration);
                RefreshButtonArea(buttonArea, durationField, state);
            }
        }

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

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머 상태가 변경된 경우에만 버튼 영역을 재구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshButtonArea(VisualElement area, FloatField field, TimerState state)
        {
            if (lastState == state) return;
            lastState = state;

            area.Clear();

            if (state == TimerState.Ready)
            {
                // Stop/Reset 후 cached 값으로 복원
                if (_ITimer != null) field.SetValueWithoutNotify(_ITimer.cached);

                void Play()
                {
                    var duration = _ITimer?.cached ?? 0f;
                    Execute(t => t.Start(duration));
                }

                area.Add(MakeIconButton("d_PlayButton", Play));
                area.Add(MakeIconButton("d_Refresh",     () => Execute(t => t.Reset())));
            }
            else if (state == TimerState.Run)
            {
                area.Add(MakeIconButton("d_PauseButton", () => Execute(t => t.Pause())));
                area.Add(MakeIconButton("d_PreMatQuad",  () => Execute(t => t.Stop())));
            }
            else if (state == TimerState.Pause)
            {
                area.Add(MakeIconButton("d_PlayButton",  () => Execute(t => t.Resume())));
                area.Add(MakeIconButton("d_PreMatQuad",  () => Execute(t => t.Stop())));
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
