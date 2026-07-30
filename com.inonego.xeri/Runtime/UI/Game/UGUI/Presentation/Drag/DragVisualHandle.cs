/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DragVisualHandle.cs
수정일 : 2026-07-30

# 설명
드래그 시각물의 원래 계층·RectTransform pose와 Presentation Layer Usage를 함께 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// 한 UGUI Drag Visual 재배치와 Layer 사용 수명 Handle.
    /// </summary>
    // ============================================================
    public sealed class DragVisualHandle : IDisposable
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Drag Visual과 Layer Usage가 논리적으로 종료됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDisposed => owner == null;

        private DragVisualController owner = null;
        private RectTransform target = null;
        private Lease layerUsage = null;
        private readonly Transform originalParent = null;
        private readonly int originalSibling = 0;
        private readonly Vector2 originalAnchorMin = default;
        private readonly Vector2 originalAnchorMax = default;
        private readonly Vector2 originalPivot = default;
        private readonly Vector2 originalAnchoredPosition = default;
        private readonly Vector2 originalSizeDelta = default;
        private readonly Quaternion originalRotation = default;
        private readonly Vector3 originalScale = default;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// RectTransform의 원래 계층·pose와 선택적 Layer Usage를 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        internal DragVisualHandle
        (
            DragVisualController owner,
            RectTransform target,
            Lease layerUsage
        ) : base()
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.layerUsage = layerUsage;
            originalParent = target.parent;
            originalSibling = target.GetSiblingIndex();
            originalAnchorMin = target.anchorMin;
            originalAnchorMax = target.anchorMax;
            originalPivot = target.pivot;
            originalAnchoredPosition = target.anchoredPosition;
            originalSizeDelta = target.sizeDelta;
            originalRotation = target.localRotation;
            originalScale = target.localScale;
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// Drag Visual과 Layer Usage를 한 번 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            Release();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Handle을 Terminal로 전환한 뒤 Drag Visual pose와 Layer Usage를 한 번 정리한다.
        /// <br/> pose 복원 결과와 관계없이 Layer Usage를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal void Release(bool removeFromHandles = true)
        {
            if (owner == null) return;

            var current = target;
            var currentOwner = owner;
            var usage = layerUsage;
            target = null;
            owner = null;
            layerUsage = null;

            if (removeFromHandles)
            {
                currentOwner.Release(this);
            }

            try
            {
                if (current != null)
                {
                    current.SetParent(originalParent, false);
                    current.SetSiblingIndex(originalSibling);
                    current.anchorMin = originalAnchorMin;
                    current.anchorMax = originalAnchorMax;
                    current.pivot = originalPivot;
                    current.anchoredPosition = originalAnchoredPosition;
                    current.sizeDelta = originalSizeDelta;
                    current.localRotation = originalRotation;
                    current.localScale = originalScale;
                }
            }
            finally
            {
                usage?.Dispose();
            }
        }

    #endregion

    }
}
