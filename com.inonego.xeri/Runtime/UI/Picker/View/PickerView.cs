/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerView.cs
수정일 : 2026-10-28

# 설명
Picker session을 표시하고 Resources 기반 UXML/USS를 적용하는 root VisualElement.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

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
        private readonly PickerTableBinder<TEntry, TValue> tableBinder;
        private readonly PagedListChrome chrome;
        private readonly PaginationBar paginationBar;

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

            var content = new VisualElement();
            layout.CloneTree(content);

            chrome = new PagedListChrome();
            Add(chrome);
            paginationBar = new PaginationBar
            (
                session.Paginator,
                HandlePageNavigation
            );
            chrome.PaginationHost.Add(paginationBar);

            var preview = content.Q<VisualElement>("preview-pane");
            var search = content.Q<VisualElement>("search-field");
            var filters = content.Q<VisualElement>("filter-chip-container");
            var table = content.Q<VisualElement>("table-pane");
            if (preview == null || search == null || filters == null || table == null)
            {
                throw new InvalidOperationException("PickerView 전용 콘텐츠 UXML이 올바르지 않습니다.");
            }

            chrome.HeaderHost.Add(preview);
            chrome.SearchHost.Add(search);
            if (session.Filters.Count > 0)
            {
                chrome.AddToolbarExtra(filters);
                filterBinder = new PickerFilterBinder<TEntry, TValue>(session, this);
            }

            chrome.ContentHost.Add(table);
            search.RegisterCallback<ChangeEvent<string>>
            (
                evt => session.SetSearchText(evt.newValue)
            );

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

            tableBinder = new PickerTableBinder<TEntry, TValue>(session, this);
            chrome.BindPageListView(this.Q<MultiColumnListView>("entry-table"));

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
            chrome.CancelPageScroll();
            previewBinder?.Refresh();
            filterBinder?.Refresh();
            tableBinder?.Refresh();
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

        // ----------------------------------------------------------------------
        /// <summary>
        /// 텍스트 입력 내부의 좌우 커서 이동은 picker shortcut으로 처리하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
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

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// 페이지네이터 입력을 session의 공통 이동 명령으로 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePageNavigation(PageNavigationRequest request)
        {
            session.NavigatePage(request);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 페이지 데이터와 선택 상태를 갱신한 뒤 목록 스크롤을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePageNavigated(PageNavigationResult result)
        {
            chrome.ScrollToPageBoundary(result.ScrollAnchor);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// session 변경을 UI에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleSessionChanged(object sender, EventArgs e)
        {
            Refresh();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// keyboard shortcut을 session 명령으로 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleKeyDown(KeyDownEvent evt)
        {
            // 페이지 입력과 버튼은 Picker 전체 선택 단축키보다 우선한다.
            if (evt.target is VisualElement target && paginationBar.Contains(target)) return;

            if (evt.keyCode == KeyCode.Escape)
            {
                session.ClearCurrentEntry();
                StopShortcutEvent(evt);
            }
            else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                session.ConfirmCurrent();
                StopShortcutEvent(evt);
            }
            else if (evt.keyCode == KeyCode.UpArrow)
            {
                StopShortcutEvent(evt);
                session.MoveSelectionPrev();
            }
            else if (evt.keyCode == KeyCode.DownArrow)
            {
                StopShortcutEvent(evt);
                session.MoveSelectionNext();
            }
            else if (evt.keyCode == KeyCode.LeftArrow && !IsTextInputEvent(evt))
            {
                session.NavigatePage(new PageNavigationRequest(PageNavigationKind.Previous));
                StopShortcutEvent(evt);
            }
            else if (evt.keyCode == KeyCode.RightArrow && !IsTextInputEvent(evt))
            {
                session.NavigatePage(new PageNavigationRequest(PageNavigationKind.Next));
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
            session.PageNavigated -= HandlePageNavigated;
            session.PageNavigated += HandlePageNavigated;
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
            session.PageNavigated -= HandlePageNavigated;
        }

    #endregion

    }
}
