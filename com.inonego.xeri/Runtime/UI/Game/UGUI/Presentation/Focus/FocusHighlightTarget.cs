/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : FocusHighlightTarget.cs
수정일 : 2026-07-31

# 설명
문자열 탐색 없이 실제 RectTransform과 여백으로 Focus Highlight 구멍 대상을 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Focus Highlight의 실제 UI 대상과 사방 여백.
    /// </summary>
    // ============================================================
    public readonly struct FocusHighlightTarget
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Highlight 구멍을 계산할 실제 RectTransform.
        /// </summary>
        // ------------------------------------------------------------
        public RectTransform Target { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 왼쪽, 오른쪽, 위, 아래 순서의 추가 여백.
        /// </summary>
        // ------------------------------------------------------------
        public Vector4 Padding { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 RectTransform과 선택적 여백으로 Highlight 대상을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public FocusHighlightTarget
        (
            RectTransform target,
            Vector4 padding = default
        ) : this()
        {
            Target = target != null
                ? target
                : throw new ArgumentNullException(nameof(target));
            Padding = padding;
        }

    #endregion

    }
}
