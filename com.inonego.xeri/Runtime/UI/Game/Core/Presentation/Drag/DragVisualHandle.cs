/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DragVisualHandle.cs
수정일 : 2026-07-29

# 설명
드래그 시각물의 원래 부모, sibling과 RectTransform pose를 보관하고 종료 시 복원한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// 한 Drag Visual 재배치 수명 Handle.
    /// </summary>
    // ============================================================
    public sealed class DragVisualHandle : IDisposable
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Drag Visual이 원래 위치로 복원됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDisposed => owner == null;

        private DragVisualController owner = null;
        private RectTransform target = null;
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
        /// RectTransform의 원래 계층과 pose를 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        internal DragVisualHandle
        (
            DragVisualController owner,
            RectTransform target
        ) : base()
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.target = target ?? throw new ArgumentNullException(nameof(target));
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
        /// Drag Visual을 원래 부모, sibling과 pose로 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (owner == null) return;

            var current = target;

            // 외부 파괴로 pose를 복원할 수 없어도 Controller와 Handle의 소유 연결은 종결한다.
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

            target = null;
            var currentOwner = owner;
            owner = null;
            currentOwner.Release(this);
        }

    #endregion

    }
}
