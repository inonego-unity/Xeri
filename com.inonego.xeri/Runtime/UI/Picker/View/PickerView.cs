/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerView.cs
수정일 : 2026-11-23

# 설명
Picker session을 표시하고 Resources 기반 UXML/USS를 적용하는 root VisualElement.
preview·검색창(TableGridSearchField)·filter chip은 Picker UXML이, 목록 표는 PickerTableGridBinder가 만든 TableGrid가 맡는다.
검색창은 표를 직접 검색한다(SearchesGrid). 검색·정렬·스크롤·키보드 선택 이동은 표가 맡는다.

# 특이사항, 제약사항
Enter(현재 항목 확정)·Escape(선택 비우기)는 root에서 먼저 받는다.
표 밖(검색창·root)에서 누른 방향키·PageUp/Down은 표로 넘기고, 표에 포커스가 있으면 표가 직접 처리한다.
표 안의 하위 요소(입력칸 등)에서 온 키는 그 요소가 처리하도록 넘긴다.
Picker 크롬도 표와 같은 테마 수정 클래스(xeri-table--editor, xeri-table--light)를 붙여 Unity 스킨을 따른다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TableGrid;

namespace inonego.Xeri.UI.Picker
{
    // ============================================================
    /// <summary>
    /// Picker session을 표시하는 root VisualElement.
    /// </summary>
    // ============================================================
    public sealed class PickerView<TEntry, TValue> : VisualElement
    {

    #region 필드

        private readonly PickerSession<TEntry, TValue> session;
        private readonly PickerPreviewBinder<TEntry, TValue> previewBinder;
        private readonly PickerFilterBinder<TEntry, TValue> filterBinder;
        private readonly PickerTableGridBinder<TEntry, TValue> gridBinder;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// PickerView를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PickerView(PickerSession<TEntry, TValue> session) : this
        (
            session,
            PickerViewResourceLoader.LoadLayout(),
            PickerViewResourceLoader.LoadThemeStyle(),
            PickerViewResourceLoader.LoadViewStyle()
        )
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 또는 특수 host에서 명시 asset으로 PickerView를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PickerView
        (
            PickerSession<TEntry, TValue> session,
            VisualTreeAsset layout,
            StyleSheet themeStyle,
            StyleSheet viewStyle
        ) : base()
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));

            focusable = true;
            AddToClassList("xeri-picker-host");
            AddToClassList("xeri-picker");
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            layout.CloneTree(this);

            var search = this.Q<TableGridSearchField>("search-field");
            var filters = this.Q<VisualElement>("filter-chip-container");
            var table = this.Q<VisualElement>("table-pane");
            if (this.Q<VisualElement>("preview-pane") == null || search == null || filters == null || table == null)
            {
                throw new InvalidOperationException("PickerView 전용 콘텐츠 UXML이 올바르지 않습니다.");
            }

            // filter가 없는 Picker는 filter 행을 배치에서 뺀다.
            if (session.Filters.Count > 0)
            {
                filterBinder = new PickerFilterBinder<TEntry, TValue>(session, this);
            }
            else
            {
                filters.style.display = DisplayStyle.None;
            }

            if (themeStyle != null)
            {
                styleSheets.Add(themeStyle);
            }

            if (viewStyle != null)
            {
                styleSheets.Add(viewStyle);
            }

            ApplyPreviewVisibility();

            if (session.ShowPreview)
            {
                previewBinder = new PickerPreviewBinder<TEntry, TValue>(session, this);
            }

            gridBinder = new PickerTableGridBinder<TEntry, TValue>(session, table);

            // 검색창은 표를 직접 검색한다. 결과가 바뀌면 빠진 현재 항목을 비우고 보이는 자리를 맞춘다.
            search.Grid = gridBinder.Grid;
            search.SearchesGrid = true;
            search.SearchTextChanged += (_, _) => gridBinder.HandleResultsChanged();

            // 크롬도 표와 같은 기준(패널 종류, Unity 스킨)으로 테마 수정 클래스를 붙인다.
            TableGridTheming.Attach(this, gridBinder.Grid.Options);

            RegisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);
            RegisterCallback<AttachToPanelEvent>(HandleAttached);
            RegisterCallback<DetachFromPanelEvent>(HandleDetached);

            Refresh();
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 binder 표시 상태를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Refresh()
        {
            previewBinder?.Refresh();
            filterBinder?.Refresh();
            gridBinder.Refresh();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// spec 옵션에 따라 preview pane을 layout에서 제외한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyPreviewVisibility()
        {
            var previewPane = this.Q<VisualElement>("preview-pane");
            if (previewPane == null) return;

            previewPane.style.display = session.ShowPreview ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// root shortcut이 소유한 key event를 내부 control 기본 처리로 넘기지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static void StopShortcutEvent(KeyDownEvent evt)
        {
            evt.StopPropagation();
            evt.StopImmediatePropagation();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 텍스트 입력 내부의 좌우·Home/End 커서 이동은 표로 넘기지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsTextInputEvent(KeyDownEvent evt)
        {
            if (evt.target is TextField) return true;

            var element = evt.target as VisualElement;
            while (element != null)
            {
                if (element is TextField) return true;

                element = element.parent;
            }

            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표 루트가 아닌 표 안의 요소(입력칸·버튼 등)에서 온 키인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool IsGridChildEvent(KeyDownEvent evt)
        {
            var grid = gridBinder.Grid;

            return evt.target is VisualElement target && target != grid && grid.Contains(target);
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// session 변경을 UI에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleSessionChanged(object sender, EventArgs e)
        {
            Refresh();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Enter·Escape를 session 명령으로, 표 밖의 이동 키를 표로 연결한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void HandleKeyDown(KeyDownEvent evt)
        {
            // 표 안의 입력칸과 버튼은 Picker 전체 단축키보다 우선한다.
            if (IsGridChildEvent(evt)) return;

            if (evt.keyCode == KeyCode.Escape)
            {
                session.ClearCurrentEntry();
                StopShortcutEvent(evt);
                return;
            }

            if (XeriShortcutKeys.IsSubmit(evt.keyCode))
            {
                session.ConfirmCurrent();
                StopShortcutEvent(evt);
                return;
            }

            // 표에 포커스가 있으면 표가 직접 처리한다. 검색창의 좌우·Home/End는 글자 커서에 남긴다.
            if (evt.target == gridBinder.Grid) return;

            var isCursorKey = evt.keyCode == KeyCode.LeftArrow || evt.keyCode == KeyCode.RightArrow
                || evt.keyCode == KeyCode.Home || evt.keyCode == KeyCode.End;
            if (isCursorKey && IsTextInputEvent(evt)) return;

            if (gridBinder.Grid.HandleForwardedKey(evt.keyCode, evt.shiftKey))
            {
                StopShortcutEvent(evt);
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// picker가 열린 직후에도 keyboard shortcut을 받을 수 있도록 root focus를 확보한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private void HandleAttached(AttachToPanelEvent evt)
        {
            session.Changed -= HandleSessionChanged;
            session.Changed += HandleSessionChanged;
            Refresh();
            schedule.Execute(Focus);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// view가 panel에서 분리될 때 session 구독을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleDetached(DetachFromPanelEvent evt)
        {
            session.Changed -= HandleSessionChanged;
        }

    #endregion

    }
}
