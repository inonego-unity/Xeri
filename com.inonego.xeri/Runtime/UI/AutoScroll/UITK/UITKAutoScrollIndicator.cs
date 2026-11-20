/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKAutoScrollIndicator.cs
수정일 : 2026-11-20

# 설명
UI Toolkit 자동 스크롤 표시기. 기준점에 둥근 표식을 띄우고, 스크롤하는 방향의 화살표를 강조한다.

# 특이사항, 제약사항
표식은 대상 요소가 속한 패널의 최상위(visualTree)에 붙여 다른 요소에 가려지지 않게 한다. 입력은 받지 않는다.
모양은 Resources/Xeri/UI/AutoScroll/AutoScrollIndicator.uss와 --xeri-auto-scroll-* 변수가 정한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.AutoScroll
{
    // ============================================================
    /// <summary>
    /// UI Toolkit 자동 스크롤 표시기.
    /// </summary>
    // ============================================================
    public sealed class UITKAutoScrollIndicator : IAutoScrollIndicator
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 표식 요소의 USS 클래스.
        /// </summary>
        // ------------------------------------------------------------
        public static readonly string UssClassName = "xeri-auto-scroll-indicator";

        private const string styleSheetPath = "Xeri/UI/AutoScroll/AutoScrollIndicator";
        private static StyleSheet styleSheet;

        private readonly VisualElement owner;
        private readonly VisualElement up;
        private readonly VisualElement down;
        private readonly VisualElement left;
        private readonly VisualElement right;

        // ------------------------------------------------------------
        /// <summary>
        /// 기준점 표식 요소.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement Element { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// owner가 속한 패널에 띄울 표시기를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public UITKAutoScrollIndicator(VisualElement owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));

            Element = CreatePart(UssClassName);
            Element.styleSheets.Add(LoadStyle());
            Element.Add(CreatePart(UssClassName + "__dot"));
            up = AddArrow("up");
            down = AddArrow("down");
            left = AddArrow("left");
            right = AddArrow("right");
        }

        private VisualElement AddArrow(string direction)
        {
            var arrow = CreatePart(UssClassName + "__arrow");
            arrow.AddToClassList($"{UssClassName}__arrow--{direction}");
            Element.Add(arrow);
            return arrow;
        }

        private static VisualElement CreatePart(string className)
        {
            var element = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
            };
            element.AddToClassList(className);
            return element;
        }

        private static StyleSheet LoadStyle()
        {
            if (styleSheet == null)
            {
                styleSheet = Resources.Load<StyleSheet>(styleSheetPath)
                    ?? throw new InvalidOperationException($"자동 스크롤 표시기 스타일을 찾을 수 없습니다: Resources/{styleSheetPath}.uss");
            }

            return styleSheet;
        }

    #endregion

    #region 표시

        // ------------------------------------------------------------
        /// <summary>
        /// 기준점(패널 좌표)에 표식을 띄운다.
        /// </summary>
        // ------------------------------------------------------------
        public void Show(Vector2 origin)
        {
            var root = owner.panel?.visualTree;
            if (root == null)
            {
                return;
            }

            if (Element.parent != root)
            {
                root.Add(Element);
            }

            var local = root.WorldToLocal(origin);
            Element.style.left = local.x;
            Element.style.top = local.y;
            Element.BringToFront();
            SetVelocity(Vector2.zero);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 스크롤하는 방향의 화살표를 강조한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetVelocity(Vector2 velocity)
        {
            var activeClass = UssClassName + "__arrow--active";
            up.EnableInClassList(activeClass, velocity.y < 0f);
            down.EnableInClassList(activeClass, velocity.y > 0f);
            left.EnableInClassList(activeClass, velocity.x < 0f);
            right.EnableInClassList(activeClass, velocity.x > 0f);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표식을 패널에서 뗀다.
        /// </summary>
        // ------------------------------------------------------------
        public void Hide()
        {
            Element.RemoveFromHierarchy();
        }

    #endregion

    }
}
