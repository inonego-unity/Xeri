/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PopupPlacementOptions.cs
수정일 : 2026-10-27

# 설명
Popup 배치의 선호·대체 방향, 교차축 정렬, 간격, 여백과 크기·겹침 허용 정책을 정의한다.

# 특이사항, 제약사항
방향 Flip과 교차축 Shift는 항상 적용되며 성공한 결과는 안전 영역 안에 포함된다.
교차축 정렬 반전(Start↔End)은 AllowAlignmentFlip일 때만 Shift보다 먼저 시도한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// Popup 배치 계산 옵션.
    /// </summary>
    // ============================================================
    public readonly struct PopupPlacementOptions
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 가장 먼저 시도할 열림 방향.
        /// </summary>
        // ------------------------------------------------------------
        public PopupPlacementDirection PreferredDirection { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 선호 방향에 들어가지 않을 때 반대 방향보다 먼저 시도할 방향.
        /// </summary>
        // ------------------------------------------------------------
        public PopupPlacementDirection? FallbackDirection { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열림 방향의 교차축 정렬.
        /// </summary>
        // ------------------------------------------------------------
        public PopupPlacementAlignment Alignment { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 기준 영역과 Popup 사이의 간격.
        /// </summary>
        // ------------------------------------------------------------
        public float Gap { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 안전 영역 안쪽으로 비워 둘 여백.
        /// </summary>
        // ------------------------------------------------------------
        public float Padding { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 선호 방향이 부족할 때 반대 방향을 시도할지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool AllowFlip { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 어느 방향에도 전체 길이가 들어가지 않을 때 열림 방향 길이를 줄일지 여부.
        /// <br/> 교차축 크기는 이 값과 관계없이 항상 안전 영역 크기로 제한된다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool AllowMainShrink { get; }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 열림 방향 길이를 줄일 때 허용하는 최솟값. 줄일 수 있는 공간이 이보다 작으면 줄이지 않는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public float MinMainSize { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 기준 영역을 피할 공간이 없을 때 가장 덜 겹치는 방향으로 겹쳐 배치할지 여부.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool AllowAnchorOverlap { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 정렬한 교차축 시작 위치에 더하는 값. 기준 영역 안의 특정 지점에 맞출 때 쓴다.
        /// </summary>
        // ----------------------------------------------------------------------
        public float CrossOffset { get; }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 교차축이 안전 영역을 넘으면 Shift하기 전에 Start와 End 정렬을 맞바꿔 시도할지 여부.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool AllowAlignmentFlip { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Popup 배치 옵션을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PopupPlacementOptions
        (
            PopupPlacementDirection preferredDirection,
            PopupPlacementAlignment alignment = PopupPlacementAlignment.Start,
            PopupPlacementDirection? fallbackDirection = null,
            float gap = 0f,
            float padding = 0f,
            bool allowFlip = true,
            bool allowMainShrink = true,
            float minMainSize = 0f,
            bool allowAnchorOverlap = false,
            float crossOffset = 0f,
            bool allowAlignmentFlip = false
        ) : this()
        {
            PreferredDirection = preferredDirection;
            Alignment = alignment;
            FallbackDirection = fallbackDirection;
            Gap = gap;
            Padding = padding;
            AllowFlip = allowFlip;
            AllowMainShrink = allowMainShrink;
            MinMainSize = minMainSize;
            AllowAnchorOverlap = allowAnchorOverlap;
            CrossOffset = crossOffset;
            AllowAlignmentFlip = allowAlignmentFlip;
        }

    #endregion

    }
}
