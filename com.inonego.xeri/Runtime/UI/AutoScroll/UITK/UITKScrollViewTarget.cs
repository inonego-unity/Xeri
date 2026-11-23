/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKScrollViewTarget.cs
수정일 : 2026-11-22

# 설명
UI Toolkit ScrollView를 자동 스크롤 대상으로 연결한다. scrollOffset을 옮기며 ScrollView가 범위로 맞춘다.

# 특이사항, 제약사항
UI Toolkit 패널 좌표는 아래쪽이 +y라 delta를 그대로 더한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.AutoScroll
{
    // ============================================================
    /// <summary>
    /// UI Toolkit ScrollView 자동 스크롤 대상.
    /// </summary>
    // ============================================================
    public sealed class UITKScrollViewTarget : IAutoScrollTarget
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 움직일 ScrollView.
        /// </summary>
        // ------------------------------------------------------------
        public ScrollView ScrollView { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// ScrollView를 대상으로 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public UITKScrollViewTarget(ScrollView scrollView)
        {
            ScrollView = scrollView ?? throw new ArgumentNullException(nameof(scrollView));
        }

    #endregion

    #region 스크롤

        // ------------------------------------------------------------
        /// <summary>
        /// 내용이 보이는 영역보다 커서 가로나 세로로 움직일 수 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanScroll
        {
            get
            {
                var vertical = ScrollView.verticalScroller;
                var horizontal = ScrollView.horizontalScroller;
                return vertical.highValue > vertical.lowValue || horizontal.highValue > horizontal.lowValue;
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// scrollOffset을 delta만큼 옮긴다. 범위 끝이라 움직이지 않았으면 false다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool ScrollBy(Vector2 delta)
        {
            var before = ScrollView.scrollOffset;
            ScrollView.scrollOffset = before + delta;
            return ScrollView.scrollOffset != before;
        }

    #endregion

    }
}
