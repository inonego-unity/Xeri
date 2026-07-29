/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ProjectionController.cs
수정일 : 2026-07-29

# 설명
현재 Camera와 Layer Transform을 사용해 World Anchor를 UI 로컬 위치로 투영한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// World Anchor의 UI Projection을 계산한다.
    /// </summary>
    // ============================================================
    public sealed class ProjectionController
    {
    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// World Anchor를 지정 Layer RectTransform의 로컬 위치로 투영한다.
        /// </summary>
        // ------------------------------------------------------------
        public ProjectionResult Project
        (
            IProjectionAnchor anchor,
            Camera camera,
            RectTransform layerRoot
        )
        {
            if (anchor == null)
            {
                throw new ArgumentNullException(nameof(anchor));
            }

            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera));
            }

            if (layerRoot == null)
            {
                throw new ArgumentNullException(nameof(layerRoot));
            }

            if (!anchor.TryGetWorldPosition(out var worldPosition))
            {
                return new ProjectionResult(false, default, false);
            }

            var viewport = camera.WorldToViewportPoint(worldPosition);
            var behind = viewport.z < 0.0f;
            var screen = camera.WorldToScreenPoint(worldPosition);
            var canvas = layerRoot.GetComponentInParent<Canvas>();
            var eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            var succeeded = RectTransformUtility.ScreenPointToLocalPointInRectangle
            (
                layerRoot,
                screen,
                eventCamera,
                out var localPosition
            );

            return new ProjectionResult(succeeded, localPosition, behind);
        }

    #endregion

    }
}
