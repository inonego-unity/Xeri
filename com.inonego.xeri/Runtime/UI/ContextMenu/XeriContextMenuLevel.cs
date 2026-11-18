/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriContextMenuLevel.cs
수정일 : 2026-11-18

# 설명
컨텍스트 메뉴 한 단계(루트 또는 하위 메뉴)의 UI와 선택·입력 상태를 보관한다.
Xeri Pool로 재사용하며 메뉴가 단계를 열 때 연결하고 닫을 때 해제한다.

# 특이사항, 제약사항
입력 이벤트의 실제 처리와 단계 스택 관리는 소유 메뉴(XeriContextMenu)가 맡는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 중첩 메뉴 한 단계의 UI와 선택·입력 상태를 보관한다.
    /// </summary>
    // ============================================================
    internal sealed class XeriContextMenuLevel
    {

    #region 상태 및 요소

        internal XeriContextMenu Menu;
        internal readonly VisualElement Element;
        internal readonly ScrollView Scroll;
        internal Button ParentButton;
        internal Button ActiveButton;
        internal Rect Bounds;
        internal PopupPlacementDirection Direction;
        internal int Depth;
        internal readonly List<Button> Buttons = new();
        internal readonly Dictionary<Button, XeriContextMenuItem> Items = new();
        internal readonly List<XeriContextMenuRow> Actions = new();
        internal readonly List<Label> Labels = new();
        internal readonly List<VisualElement> Separators = new();

    #endregion

    #region 생성 및 수명주기

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 단계의 UXML 요소와 입력 이벤트를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriContextMenuLevel()
        {
            var layout = XeriContextMenuResources.LevelLayout.Instantiate();
            Element = layout.Q<VisualElement>("context-menu-level");
            Scroll = layout.Q<ScrollView>("context-menu-scroll");

            if (Element == null || Scroll == null)
            {
                throw new InvalidOperationException("컨텍스트 메뉴 레벨 요소를 찾을 수 없습니다.");
            }

            Element.RemoveFromHierarchy();
            Element.styleSheets.Add(XeriContextMenuResources.MenuStyle);
            Element.focusable = true;
            Element.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            Element.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
            Element.RegisterCallback<KeyDownEvent>
            (
                HandleKeyDown, TrickleDown.TrickleDown
            );
            Element.RegisterCallback<NavigationSubmitEvent>
            (
                HandleSubmit, TrickleDown.TrickleDown
            );
            Scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Scroll.verticalScroller.valueChanged += HandleScrollChanged;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 재사용할 메뉴 단계에 소유자와 표시 상태를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Bind(XeriContextMenu menu, int depth, Button parentButton)
        {
            Menu = menu;
            Depth = depth;
            ParentButton = parentButton;

            // 색과 글꼴은 USS 클래스로, 배치에 쓰는 치수는 스타일 값을 직접 적용한다.
            var metrics = menu.Metrics;
            Element.EnableInClassList("xeri-context-menu--light", menu.UseLightTheme);
            Element.EnableInClassList
            (
                "xeri-context-menu--expanded",
                menu.Style.Density == XeriContextMenuDensity.Expanded
            );
            Element.style.width = metrics.Width;
            Element.style.minWidth = metrics.Width;
            Element.style.paddingTop = metrics.VerticalPadding;
            Element.style.paddingBottom = metrics.VerticalPadding;
            Element.style.borderTopWidth = metrics.BorderWidth;
            Element.style.borderBottomWidth = metrics.BorderWidth;
            Element.style.borderLeftWidth = metrics.BorderWidth;
            Element.style.borderRightWidth = metrics.BorderWidth;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 단계의 선택·스크롤·소유 상태를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Unbind()
        {
            Element.RemoveFromHierarchy();
            Element.RemoveFromClassList("xeri-context-menu--light");
            Element.RemoveFromClassList("xeri-context-menu--expanded");
            ActiveButton?.RemoveFromClassList("xeri-context-menu__action--active");
            Menu = null;
            ActiveButton = null;
            ParentButton = null;
            Scroll.scrollOffset = Vector2.zero;
            Depth = 0;
            Bounds = default;
            Direction = PopupPlacementDirection.Right;
        }

    #endregion

    #region 활성 항목

        // ------------------------------------------------------------
        /// <summary>
        /// 항목의 선택 색상과 키보드 포커스를 별도로 관리한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void SetActive(Button button, bool focus)
        {
            if (ActiveButton != button)
            {
                ActiveButton?.RemoveFromClassList("xeri-context-menu__action--active");
                ActiveButton = button;
                button?.AddToClassList("xeri-context-menu__action--active");
            }

            if (focus)
            {
                button?.Focus();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키보드로 하위 메뉴를 열 때 첫 번째 활성 항목을 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void FocusFirst()
        {
            if (Buttons.Count > 0)
            {
                SetActive(Buttons[0], true);
            }
            else
            {
                Element.Focus();
            }
        }

    #endregion

    #region 이벤트 처리

        // ------------------------------------------------------------
        /// <summary>
        /// 명령 행 이외를 클릭하면 하위 메뉴와 선택 상태를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePointerDown(PointerDownEvent evt)
        {
            if (Menu == null)
            {
                return;
            }

            if (evt.target is not Button button || !Items.ContainsKey(button))
            {
                Menu.CloseLevelsFrom(Depth + 1);
                SetActive(null, false);
            }

            evt.StopPropagation();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 하위 메뉴에 포인터가 진입하면 부모의 연결된 항목을 활성화한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePointerEnter(PointerEnterEvent evt)
        {
            if (Menu != null && Depth > 0)
            {
                var parent = Menu.GetLevel(Depth - 1);
                parent.SetActive(ParentButton, false);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 라벨·구분선 진입 시 선택과 하위 메뉴를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void HandleNonActionPointerEnter(PointerEnterEvent evt)
        {
            Menu?.HandleItemPointerEnter(this, null);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 메뉴 단계의 키 입력을 컨텍스트 메뉴의 탐색 처리기로 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleKeyDown(KeyDownEvent evt)
        {
            Menu?.HandleMenuKeyDown(evt, this);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴의 선택 확정 입력을 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleSubmit(NavigationSubmitEvent evt)
        {
            Menu?.HandleMenuSubmit(evt, this);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 부모 메뉴가 스크롤되면 항목 위치가 달라진 하위 메뉴를 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleScrollChanged(float value)
        {
            if (Menu != null && Menu.LevelCount > Depth + 1)
            {
                Menu.CloseLevelsFrom(Depth + 1);
            }
        }

    #endregion

    }
}
