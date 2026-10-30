/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ValueDrawerHelper.cs
수정일 : 2026-10-30

# 설명
ValueDrawer·RangeValueDrawer·MValueDrawer가 공유하는 UXML/USS 로딩과 상태 처리.
정적 레이아웃은 ValueDrawer.uxml, 스타일은 ValueDrawer.uss에서 관리한다.
Base 적용과 편집 상태를 한 곳에 모으고 편집 테두리는 SetFieldBorder로 관리한다.
FieldEditor<T>는 단일 필드의 편집 모드(토글 동기화·border·이벤트 등록)를 캡슐화한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// Value 드로어 공용 UI 빌딩 블록.
    /// </summary>
    // ============================================================
    internal static class ValueDrawerHelper
    {
        private static StyleSheet _stylesheet;
        private static VisualTreeAsset _layout;

        private static StyleSheet Stylesheet
        {
            get
            {
                if (_stylesheet != null) return _stylesheet;
                var dir = EditorAssetHelper.GetScriptDirectory(typeof(ValueDrawerHelper));
                _stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dir}/ValueDrawer.uss");
                return _stylesheet;
            }
        }

        private static VisualTreeAsset Layout
        {
            get
            {
                if (_layout != null) return _layout;

                var dir = EditorAssetHelper.GetScriptDirectory(typeof(ValueDrawerHelper));
                _layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{dir}/ValueDrawer.uxml");
                return _layout;
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 공통 UXML 행을 생성하고 USS를 적용한다. Range 행은 필요할 때만 유지한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static VisualElement CreateRoot(bool includeRange = false)
        {
            var layout = Layout ?? throw new InvalidOperationException("ValueDrawer.uxml을 찾을 수 없습니다.");
            var sheet = Stylesheet ?? throw new InvalidOperationException("ValueDrawer.uss를 찾을 수 없습니다.");

            var root = layout.Instantiate();
            if (!includeRange)
            {
                root.Q<VisualElement>("value-range-row").RemoveFromHierarchy();
            }

            root.styleSheets.Add(sheet);
            return root;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 SerializedProperty 경로에서 교체된 owner도 포함해 다시 찾는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static T ResolveInstance<T>(SerializedObject serialized, string propertyPath)
        where T : class
        {
            var property = serialized.FindProperty(propertyPath);
            return property == null
                ? null
                : SerializedPropertyHelper.GetTargetObject(property) as T;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Base 값을 Undo 가능한 Runtime Set 경로로 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void ApplyBaseValue<T>
        (
            SerializedObject serialized, UnityEngine.Object target,
            Serializable.Value<T> owner, T value, string undoName
        )
        {
            if (owner == null) return;

            Undo.RecordObject(target, undoName);
            owner.Set(value, invokeEvent: true);
            EditorUtility.SetDirty(target);
            serialized.Update();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Range 행의 spacer 폭을 첫 행의 label 점유 폭과 동기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void BindSpacer(VisualElement spacer, VisualElement labelRef)
        {
            // layout.xMax = marginLeft + width, marginRight는 별도 — 셋 합산이 flex 행 점유 너비
            void OnGeometryChanged(GeometryChangedEvent _)
            {
                spacer.style.width = labelRef.layout.xMax + labelRef.resolvedStyle.marginRight;
            }

            labelRef.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 패널 연결 시 폴링을 재개하고 분리 시 일시 정지한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void StartPolling(VisualElement root, Action tick, int ms = 50)
        {
            var item = root.schedule.Execute(tick).Every(ms);

            void OnAttach(AttachToPanelEvent _) => item.Resume();
            void OnDetach(DetachFromPanelEvent _) => item.Pause();
            root.RegisterCallback<AttachToPanelEvent>(OnAttach);
            root.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> BaseField의 실제 입력 박스에 border 색상을 적용한다.
        /// <br/> 입력 박스를 찾지 못하면 field 자체에 적용한다.
        /// <br/> editing=false 시 인라인 오버라이드를 제거해 USS 기본 border로 복귀한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static void SetFieldBorder(VisualElement field, bool editing)
        {
            // IntegerField / FloatField 의 실제 회색 박스는 root 가 아닌 inner input element
            var target = field.Q(className: "unity-base-text-field__input") ?? field;

            if (editing)
            {
                var c = new Color(0.27f, 0.71f, 0.35f);
                target.style.borderTopColor    = c;
                target.style.borderBottomColor = c;
                target.style.borderLeftColor   = c;
                target.style.borderRightColor  = c;
            }
            else
            {
                // StyleKeyword.Null: 인라인 오버라이드 제거 → USS 기본 1px 테두리 복귀
                target.style.borderTopColor    = new StyleColor(StyleKeyword.Null);
                target.style.borderBottomColor = new StyleColor(StyleKeyword.Null);
                target.style.borderLeftColor   = new StyleColor(StyleKeyword.Null);
                target.style.borderRightColor  = new StyleColor(StyleKeyword.Null);
            }
        }

        // ============================================================
        /// <summary>
        /// <br/> 단일 필드의 편집 모드를 캡슐화한다.
        /// <br/> 토글·PointerDown·FocusIn 이벤트 등록을 일괄 처리한다.
        /// </summary>
        // ============================================================
        internal sealed class FieldEditor<T> where T : struct
        {

        #region 필드

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 편집 모드 상태.
            /// </summary>
            // ------------------------------------------------------------
            public bool IsEditing { get; private set; }

            private readonly Toggle       _toggle;
            private readonly BaseField<T> _field;
            private readonly Func<T>      _getBase;
            private readonly Action       _onApply;

        #endregion

        #region 생성자

            // ----------------------------------------------------------------------
            /// <summary>
            /// <br/> 토글·필드·진입·적용 콜백을 주입받아 이벤트 핸들러를 등록한다.
            /// <br/> onApply 내에서 Undo·Set·SetDirty·so.Update를 모두 수행한다.
            /// </summary>
            // ----------------------------------------------------------------------
            public FieldEditor
            (
                Toggle       toggle,
                BaseField<T> field,
                Func<T>      getBase,
                Action       onApply
            )
            {
                _toggle  = toggle;
                _field   = field;
                _getBase = getBase;
                _onApply = onApply;

                // 체크박스 체크/해제, 필드 클릭(포인터 캡처), 포커스 진입 — 세 경로 모두 편집 모드 트리거
                void OnToggleChanged(ChangeEvent<bool> evt)
                {
                    if (evt.newValue)
                    {
                        Enter();
                    }
                    else
                    {
                        Exit();
                    }
                }

                // TrickleDown: 자식 요소(drag label 등)가 이벤트를 소비하기 전에 캡처
                void OnPointerDown(PointerDownEvent e) => Enter();
                void OnFocusIn(FocusInEvent e)         => Enter();

                toggle.RegisterValueChangedCallback(OnToggleChanged);
                field.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
                field.RegisterCallback<FocusInEvent>(OnFocusIn);
            }

        #endregion

        #region 메서드

            // ------------------------------------------------------------
            /// <summary>
            /// 편집 모드로 전환한다.
            /// </summary>
            // ------------------------------------------------------------
            private void Enter()
            {
                if (IsEditing) return;

                IsEditing = true;
                _toggle.SetValueWithoutNotify(true);
                _field.SetValueWithoutNotify(_getBase());
                SetFieldBorder(_field, true);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 값을 적용하고 표시 모드로 복귀한다.
            /// </summary>
            // ------------------------------------------------------------
            private void Exit()
            {
                _onApply();
                IsEditing = false;
                _toggle.SetValueWithoutNotify(false);
                SetFieldBorder(_field, false);
            }

        #endregion

        }
    }
}
