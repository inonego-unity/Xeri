/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriContextMenuRow.cs
수정일 : 2026-11-18

# 설명
컨텍스트 메뉴의 명령 행 하나를 UXML 요소로 보여주고 클릭·포인터·포커스 입력을 메뉴에 전달한다.
Xeri Pool로 메뉴 사이에서 재사용하므로 해제할 때 표시 상태를 모두 되돌린다.
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
    /// UXML로 만든 명령 행을 메뉴 사이에서 안전하게 재사용한다.
    /// </summary>
    // ============================================================
    internal sealed class XeriContextMenuRow
    {

    #region 상태 및 요소

        internal readonly Button Button;
        internal readonly Image Icon;
        internal readonly Label Check;
        internal readonly Label Label;
        internal readonly Label Shortcut;
        internal XeriContextMenuLevel Level;
        internal XeriContextMenuItem Item;

    #endregion

    #region 생성 및 수명주기

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 항목의 UXML 요소와 이벤트를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriContextMenuRow()
        {
            var layout = XeriContextMenuResources.ActionLayout.Instantiate();
            Button = layout.Q<Button>("context-menu-action");
            Icon = layout.Q<Image>("context-menu-icon");
            Check = layout.Q<Label>("context-menu-check");
            Label = layout.Q<Label>("context-menu-label");
            Shortcut = layout.Q<Label>("context-menu-shortcut");

            if (Button == null || Icon == null || Check == null
                || Label == null || Shortcut == null)
            {
                throw new InvalidOperationException("컨텍스트 메뉴 항목 요소를 찾을 수 없습니다.");
            }

            Button.RemoveFromHierarchy();
            Icon.pickingMode = PickingMode.Ignore;
            Check.pickingMode = PickingMode.Ignore;
            Label.pickingMode = PickingMode.Ignore;
            Shortcut.pickingMode = PickingMode.Ignore;
            Button.clicked += HandleClick;
            Button.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
            Button.RegisterCallback<FocusInEvent>(HandleFocusIn);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 명령 항목을 행 UI에 연결하고 활성 여부를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal bool Bind(XeriContextMenuLevel level, XeriContextMenuItem item)
        {
            var isSubmenu = item.Kind == XeriContextMenuItem.ItemKind.Submenu;
            var enabled = item.Enabled && (!isSubmenu || item.Children.Count > 0);
            Level = level;
            Item = item;
            Button.style.height = level.Menu.Metrics.ActionHeight;
            Button.style.minHeight = level.Menu.Metrics.ActionHeight;
            Button.SetEnabled(enabled);
            Label.text = item.Text;
            Shortcut.text = isSubmenu ? "▸" : item.Shortcut ?? string.Empty;

            if (item.Icon != null)
            {
                Icon.image = item.Icon;
                Icon.scaleMode = ScaleMode.ScaleToFit;
                Icon.style.display = DisplayStyle.Flex;
            }
            else if (item.IsChecked)
            {
                Check.style.display = DisplayStyle.Flex;
            }

            if (isSubmenu)
            {
                Shortcut.AddToClassList("xeri-context-menu__shortcut--submenu");
            }
            else if (string.IsNullOrEmpty(item.Shortcut))
            {
                Shortcut.style.display = DisplayStyle.None;
            }

            return enabled;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 명령 행의 데이터와 표시 상태를 해제해 풀에 반환할 수 있게 한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Unbind()
        {
            Button.RemoveFromHierarchy();
            Button.RemoveFromClassList("xeri-context-menu__action--active");
            Button.SetEnabled(true);
            Icon.image = null;
            Icon.style.display = DisplayStyle.None;
            Check.style.display = DisplayStyle.None;
            Label.text = string.Empty;
            Shortcut.text = string.Empty;
            Shortcut.style.display = DisplayStyle.Flex;
            Shortcut.RemoveFromClassList("xeri-context-menu__shortcut--submenu");
            Level = null;
            Item = null;
        }

    #endregion

    #region 이벤트 처리

        // ------------------------------------------------------------
        /// <summary>
        /// 항목을 실행하거나 해당 하위 메뉴를 전환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleClick()
        {
            var menu = Level?.Menu;
            var item = Item;

            if (menu == null || item == null)
            {
                return;
            }

            if (item.Kind == XeriContextMenuItem.ItemKind.Submenu)
            {
                menu.ToggleSubmenu(Level, Button, item);
                return;
            }

            menu.Close();
            menu.FocusOwner();
            item.Execute?.Invoke();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 포인터가 들어온 명령 행을 선택하고 필요하면 하위 메뉴를 연다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePointerEnter(PointerEnterEvent evt)
        {
            Level?.Menu?.HandleItemPointerEnter(Level, Button);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 포커스를 받은 명령 행을 현재 메뉴 단계의 활성 항목으로 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleFocusIn(FocusInEvent evt)
        {
            Level?.Menu?.HandleItemFocusIn(Level, Button);
        }

    #endregion

    }
}
