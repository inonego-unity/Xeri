/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriContextMenuItem.cs
수정일 : 2026-10-25

# 설명
공용 컨텍스트 메뉴의 명령·라벨·구분선·하위 메뉴를 표현한다.
메뉴 항목의 표시 정보·실행 콜백·하위 항목을 보관한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 공용 컨텍스트 메뉴의 항목 정보.
    /// </summary>
    // ============================================================
    public sealed class XeriContextMenuItem
    {

    #region 필드 및 속성

        // ============================================================
        /// <summary>
        /// 메뉴에 표시할 항목의 종류.
        /// </summary>
        // ============================================================
        internal enum ItemKind
        {
            Action,
            Label,
            Separator,
            Submenu,
        }

        internal ItemKind Kind { get; }
        internal string Label { get; }
        internal string Shortcut { get; }
        internal bool Enabled { get; }
        internal bool IsChecked { get; }
        internal Texture2D Icon { get; }
        internal Action Execute { get; }
        internal IReadOnlyList<XeriContextMenuItem> Children => children;
        private readonly List<XeriContextMenuItem> children = new();

    #endregion

    #region 항목 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 항목의 종류와 표시·실행 정보를 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        private XeriContextMenuItem
        (
            ItemKind kind,
            string label = null,
            string shortcut = null,
            bool enabled = true,
            Action execute = null,
            bool isChecked = false,
            Texture2D icon = null
        )
        {
            Kind = kind;
            Label = label;
            Shortcut = shortcut;
            Enabled = enabled;
            Execute = execute;
            IsChecked = isChecked;
            Icon = icon;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실행 가능한 명령 항목을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        internal static XeriContextMenuItem CreateAction
        (
            string label, Action execute, bool enabled = true,
            string shortcut = null, bool isChecked = false, Texture2D icon = null
        )
        {
            if (string.IsNullOrEmpty(label))
            {
                throw new ArgumentException("메뉴 이름은 비워둘 수 없습니다.", nameof(label));
            }

            return new XeriContextMenuItem
            (
                ItemKind.Action, label, shortcut, enabled, execute, isChecked, icon
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택할 수 없는 텍스트 항목을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        internal static XeriContextMenuItem CreateLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                throw new ArgumentException("메뉴 라벨은 비워둘 수 없습니다.", nameof(label));
            }

            return new XeriContextMenuItem(ItemKind.Label, label, enabled: false);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 항목 사이에 넣을 구분선을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        internal static XeriContextMenuItem CreateSeparator()
        {
            return new XeriContextMenuItem(ItemKind.Separator);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 명령을 추가할 수 있는 하위 메뉴 항목을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        internal static XeriContextMenuItem CreateSubmenu(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                throw new ArgumentException("하위 메뉴 이름은 비워둘 수 없습니다.", nameof(label));
            }

            return new XeriContextMenuItem(ItemKind.Submenu, label);
        }

    #endregion

    #region 하위 메뉴 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 하위 메뉴 안에 명령을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddAction
        (
            string label, Action execute, bool enabled = true,
            string shortcut = null, bool isChecked = false, Texture2D icon = null
        )
        {
            EnsureSubmenu();
            children.Add(CreateAction(label, execute, enabled, shortcut, isChecked, icon));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 하위 메뉴 안에 텍스트 라벨을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddLabel(string label)
        {
            EnsureSubmenu();
            children.Add(CreateLabel(label));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 하위 메뉴 안에 다른 하위 메뉴를 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriContextMenuItem AddSubmenu(string label)
        {
            EnsureSubmenu();
            var item = CreateSubmenu(label);
            children.Add(item);
            return item;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 하위 메뉴에 구분선을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddSeparator()
        {
            EnsureSubmenu();
            children.Add(CreateSeparator());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 하위 메뉴가 아닌 항목에 자식이 추가되지 않도록 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private void EnsureSubmenu()
        {
            if (Kind != ItemKind.Submenu)
            {
                throw new InvalidOperationException("하위 메뉴에만 항목을 추가할 수 있습니다.");
            }
        }

    #endregion

    }
}