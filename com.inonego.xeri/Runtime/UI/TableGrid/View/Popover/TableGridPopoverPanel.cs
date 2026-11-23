/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridPopoverPanel.cs
수정일 : 2026-11-23

# 설명
TableGrid 팝오버(열 메뉴·필터 편집)의 공통 화면 요소. 머리 줄, 구역 이름, 구분선, 누를 수 있는 행, 한 줄 입력칸을 만들고
위아래 키로 행 사이를 옮기며 Enter로 실행한다. Escape는 닫기 요청으로 알린다.

# 특이사항, 제약사항
팝오버는 Editor에서는 별도 팝업 창, Runtime에서는 패널 위 프레임에 놓여 표의 USS를 물려받지 못한다.
그래서 이 요소가 테마·모양 USS를 직접 붙이고 테마 변수를 정의하는 클래스(xeri-table-popover)를 가진다.
팝업 창 크기는 열기 전에 정해야 하므로 높이는 C# 치수(TableGridPopoverMetrics)로 더해 PreferredHeight로 알린다.
색·글꼴은 USS가 정하고 C#은 높이만 지정한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 팝오버 크기 계산에 쓰는 치수.
    /// </summary>
    // ============================================================
    internal static class TableGridPopoverMetrics
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 열 메뉴의 너비.
        /// </summary>
        // ------------------------------------------------------------
        public const float MENU_WIDTH = 232f;

        // ------------------------------------------------------------
        /// <summary>
        /// 필터 편집 창의 너비.
        /// </summary>
        // ------------------------------------------------------------
        public const float FILTER_WIDTH = 256f;

        // ------------------------------------------------------------
        /// <summary>
        /// 테두리 두께.
        /// </summary>
        // ------------------------------------------------------------
        public const float BORDER = 1f;

        // ------------------------------------------------------------
        /// <summary>
        /// 위아래 안쪽 여백.
        /// </summary>
        // ------------------------------------------------------------
        public const float PADDING = 4f;

        // ------------------------------------------------------------
        /// <summary>
        /// 제목 줄 높이.
        /// </summary>
        // ------------------------------------------------------------
        public const float HEADER_HEIGHT = 30f;

        // ------------------------------------------------------------
        /// <summary>
        /// 구역 이름 줄 높이.
        /// </summary>
        // ------------------------------------------------------------
        public const float SECTION_HEIGHT = 22f;

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 행 높이.
        /// </summary>
        // ------------------------------------------------------------
        public const float ROW_HEIGHT = 28f;

        // ------------------------------------------------------------
        /// <summary>
        /// 구분선 영역 높이.
        /// </summary>
        // ------------------------------------------------------------
        public const float SEPARATOR_HEIGHT = 9f;

        // ------------------------------------------------------------
        /// <summary>
        /// 선택 항목 행 높이.
        /// </summary>
        // ------------------------------------------------------------
        public const float CHOICE_ROW_HEIGHT = 26f;

        // ------------------------------------------------------------
        /// <summary>
        /// 입력칸 행 높이.
        /// </summary>
        // ------------------------------------------------------------
        public const float FIELD_ROW_HEIGHT = 36f;

        // ------------------------------------------------------------
        /// <summary>
        /// 안내 문구 줄 높이.
        /// </summary>
        // ------------------------------------------------------------
        public const float HINT_HEIGHT = 20f;

        // ------------------------------------------------------------
        /// <summary>
        /// 하단 버튼 줄 높이.
        /// </summary>
        // ------------------------------------------------------------
        public const float ACTIONS_HEIGHT = 42f;

        // ------------------------------------------------------------
        /// <summary>
        /// 한 번에 보이는 선택 항목 최대 수.
        /// </summary>
        // ------------------------------------------------------------
        public const int MAX_VISIBLE_CHOICES = 8;

        // ------------------------------------------------------------
        /// <summary>
        /// 열 표시·순서 목록에서 한 번에 보이는 열 최대 수.
        /// </summary>
        // ------------------------------------------------------------
        public const int MAX_VISIBLE_COLUMNS = 10;

        // ------------------------------------------------------------
        /// <summary>
        /// 입력칸 하나짜리 창(빠른 이동·위치로 이동)의 폭.
        /// </summary>
        // ------------------------------------------------------------
        public const float PROMPT_WIDTH = 280f;

        // ------------------------------------------------------------
        /// <summary>
        /// 화면 가장자리와의 최소 간격.
        /// </summary>
        // ------------------------------------------------------------
        public const float EDGE_MARGIN = 4f;

        // ------------------------------------------------------------
        /// <summary>
        /// 하위 팝오버와의 간격.
        /// </summary>
        // ------------------------------------------------------------
        public const float SUBMENU_GAP = 1f;
    }

    // ============================================================
    /// <summary>
    /// TableGrid 팝오버의 공통 화면 요소.
    /// </summary>
    // ============================================================
    internal abstract class TableGridPopoverPanel : VisualElement
    {

    #region 필드

        private readonly List<VisualElement> actions = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 내용이 모두 보이는 높이. 팝업 창을 열기 전에 크기를 정할 때 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public float PreferredHeight { get; private set; }

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Escape로 닫기를 요청했을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action CloseRequested;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 테마·모양 USS와 팝오버 클래스를 붙인다.
        /// </summary>
        // ------------------------------------------------------------
        protected TableGridPopoverPanel()
        {
            AddToClassList("xeri-table-popover");
            styleSheets.Add(TableGridResources.Theme);
            styleSheets.Add(TableGridResources.Style);

            // 창 크기 계산과 맞아야 하는 테두리·안쪽 여백은 USS가 아니라 치수에서 정한다.
            style.paddingTop = TableGridPopoverMetrics.PADDING;
            style.paddingBottom = TableGridPopoverMetrics.PADDING;
            style.borderTopWidth = TableGridPopoverMetrics.BORDER;
            style.borderBottomWidth = TableGridPopoverMetrics.BORDER;
            style.borderLeftWidth = TableGridPopoverMetrics.BORDER;
            style.borderRightWidth = TableGridPopoverMetrics.BORDER;

            RegisterCallback<KeyDownEvent>(HandleKeyDown);
        }

    #endregion

    #region 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 내용과 높이를 비운다. 다시 채우기 전에 부른다.
        /// </summary>
        // ------------------------------------------------------------
        protected void ResetContent()
        {
            Clear();
            actions.Clear();
            PreferredHeight = (TableGridPopoverMetrics.PADDING + TableGridPopoverMetrics.BORDER) * 2f;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 컨테이너 안의 행을 비우고 키보드 이동 대상에서도 뺀다.
        /// </summary>
        // ------------------------------------------------------------
        protected void ClearActionsIn(VisualElement container)
        {
            actions.RemoveAll(container.Contains);
            container.Clear();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 높이를 직접 정한 요소를 더하고 높이를 누적한다.
        /// </summary>
        // ------------------------------------------------------------
        protected T AddSized<T>(T element, float height) where T : VisualElement
        {
            element.style.height = height;
            element.style.flexShrink = 0f;
            Add(element);
            PreferredHeight += height;
            return element;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 팝오버 맨 위의 제목 줄을 더한다.
        /// </summary>
        // ------------------------------------------------------------
        protected void AddHeader(string title, string detail)
        {
            var header = new VisualElement();
            header.AddToClassList("xeri-table-popover__header");

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("xeri-table-popover__title");
            header.Add(titleLabel);

            if (!string.IsNullOrEmpty(detail))
            {
                var detailLabel = new Label(detail);
                detailLabel.AddToClassList("xeri-table-popover__detail");
                header.Add(detailLabel);
            }

            AddSized(header, TableGridPopoverMetrics.HEADER_HEIGHT);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구역 이름 줄을 더한다.
        /// </summary>
        // ------------------------------------------------------------
        protected void AddSectionLabel(string text)
        {
            var label = new Label(text);
            label.AddToClassList("xeri-table-popover__section");
            AddSized(label, TableGridPopoverMetrics.SECTION_HEIGHT);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구역 사이 구분선을 더한다.
        /// </summary>
        // ------------------------------------------------------------
        protected void AddSeparator()
        {
            var separator = new VisualElement();
            separator.AddToClassList("xeri-table-popover__separator");
            var line = new VisualElement();
            line.AddToClassList("xeri-table-popover__separator-line");
            separator.Add(line);
            AddSized(separator, TableGridPopoverMetrics.SEPARATOR_HEIGHT);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 누를 수 있는 행을 만든다. 아이콘 이름은 USS 아이콘 수정 클래스가 된다.
        /// <br/> text가 null이면 글자 라벨을 두지 않는다(호출자가 다른 내용을 붙인다).
        /// <br/> 만든 행은 키보드 이동 대상이 된다. 높이와 부모 배치는 호출자가 정한다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected VisualElement CreateAction(string text, string icon, bool active, Action execute)
        {
            var row = new VisualElement();
            row.AddToClassList("xeri-table-popover__action");
            row.EnableInClassList("xeri-table-popover__action--active", active);
            row.focusable = true;

            var iconElement = new VisualElement();
            iconElement.AddToClassList("xeri-table-popover__icon");
            if (!string.IsNullOrEmpty(icon))
            {
                iconElement.AddToClassList($"xeri-table-popover__icon--{icon}");
            }

            row.Add(iconElement);

            // 글자가 없으면(null) 라벨을 두지 않는다. 호출자가 칩 등 다른 내용을 붙인다.
            if (text != null)
            {
                var label = new Label(text);
                label.AddToClassList("xeri-table-popover__label");
                row.Add(label);
            }

            row.AddManipulator(new Clickable(execute));

            // 행에서 Enter나 Space를 누르면 실행한다.
            void HandleActionKeyDown(KeyDownEvent evt)
            {
                if (XeriShortcutKeys.IsSubmit(evt.keyCode) || evt.keyCode == KeyCode.Space)
                {
                    execute();
                    evt.StopPropagation();
                }
            }

            row.RegisterCallback<KeyDownEvent>(HandleActionKeyDown);

            actions.Add(row);
            return row;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 줄 높이의 실행 행을 바로 더한다.
        /// </summary>
        // ------------------------------------------------------------
        protected VisualElement AddAction(string text, string icon, bool active, Action execute)
        {
            return AddSized(CreateAction(text, icon, active, execute), TableGridPopoverMetrics.ROW_HEIGHT);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 한 줄 입력칸을 만든다. 입력칸에서 Enter를 누르면 submit을 부른다.
        /// <br/> 높이와 부모 배치는 호출자가 정한다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected TextField CreateTextField(string value, string placeholder, Action submit)
        {
            var field = new TextField
            {
                value = value ?? string.Empty,
            };
            field.AddToClassList("xeri-table-popover__field");
            field.textEdition.placeholder = placeholder;

            void HandleFieldKeyDown(KeyDownEvent evt)
            {
                if (XeriShortcutKeys.IsSubmit(evt.keyCode))
                {
                    submit();
                    evt.StopPropagation();
                }
            }

            field.RegisterCallback<KeyDownEvent>(HandleFieldKeyDown, TrickleDown.TrickleDown);
            return field;
        }

    #endregion

    #region 키보드

        // ----------------------------------------------------------------------
        /// <summary>
        /// 첫 활성 행이나 첫 입력 요소에 포커스를 준다. 비활성 행은 방향키 이동처럼 건너뛴다.
        /// </summary>
        // ----------------------------------------------------------------------
        public virtual void FocusFirst()
        {
            foreach (var action in actions)
            {
                if (action.enabledInHierarchy)
                {
                    action.Focus();
                    return;
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 닫기 요청을 알린다.
        /// </summary>
        // ------------------------------------------------------------
        protected void RequestClose()
        {
            CloseRequested?.Invoke();
        }

        private void HandleKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                RequestClose();
                evt.StopPropagation();
                return;
            }

            if ((evt.keyCode != KeyCode.UpArrow && evt.keyCode != KeyCode.DownArrow) || actions.Count == 0)
            {
                return;
            }

            // 입력칸 안에서는 방향키를 글자 이동에 남긴다.
            if (evt.target is TextElement || evt.target is TextField)
            {
                return;
            }

            // 비활성 항목은 포커스를 받지 않으므로 건너뛰어 다음 활성 항목으로 간다.
            var index = actions.IndexOf(evt.target as VisualElement);
            var step = evt.keyCode == KeyCode.UpArrow ? -1 : 1;
            var next = index < 0 ? (step > 0 ? -1 : actions.Count) : index;
            for (var tried = 0; tried < actions.Count; tried++)
            {
                next = (next + step + actions.Count) % actions.Count;
                if (actions[next].enabledInHierarchy)
                {
                    actions[next].Focus();
                    break;
                }
            }

            evt.StopPropagation();
        }

    #endregion

    }
}
