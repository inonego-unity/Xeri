/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableColumnLayout.cs
수정일 : 2026-11-18

# 설명
Table column의 폭과 stretch 정책을 표현하는 layout 모델.
Picker와 Collection Editor의 표가 열 선언과 폭 배분에 공통으로 사용한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// Table column의 layout mode.
    /// </summary>
    // ============================================================
    public enum TableColumnLayoutMode
    {
        Fixed,
        Flexible,
    }

    // ============================================================
    /// <summary>
    /// Table column의 폭과 stretch 정책.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class TableColumnLayout
    {

    #region 필드

        public readonly TableColumnLayoutMode Mode;
        public readonly float Width;
        public readonly float MinWidth;
        public readonly float MaxWidth;
        public readonly float StretchWeight;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// column layout 정책을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableColumnLayout
        (
            TableColumnLayoutMode mode,
            float width,
            float minWidth,
            float maxWidth,
            float stretchWeight
        )
        {
            if (mode == TableColumnLayoutMode.Flexible
                && (stretchWeight <= 0f || float.IsNaN(stretchWeight) || float.IsInfinity(stretchWeight)))
            {
                throw new ArgumentOutOfRangeException(nameof(stretchWeight), "가변 열 가중치는 양의 유한한 값이어야 합니다.");
            }

            Mode          = mode;
            Width         = Math.Max(1f, width);
            MinWidth      = Math.Max(1f, minWidth);
            MaxWidth      = Math.Max(0f, maxWidth);
            StretchWeight = mode == TableColumnLayoutMode.Flexible ? stretchWeight : 0f;
        }

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 폭 column layout을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableColumnLayout Fixed(float width)
        {
            var safeWidth = Math.Max(1f, width);

            return new TableColumnLayout
            (
                TableColumnLayoutMode.Fixed,
                safeWidth,
                safeWidth,
                safeWidth,
                0f
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 가변 폭 column layout을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableColumnLayout Flexible
        (
            float width,
            float minWidth,
            float maxWidth,
            float stretchWeight
        )
        {
            return new TableColumnLayout
            (
                TableColumnLayoutMode.Flexible,
                width,
                minWidth,
                maxWidth,
                stretchWeight
            );
        }

    #endregion

    }
}
