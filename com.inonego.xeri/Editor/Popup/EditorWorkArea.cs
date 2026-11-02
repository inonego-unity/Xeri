/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EditorWorkArea.cs
수정일 : 2026-10-28

# 설명
Editor 좌표계에서 기준점이 속한 모니터의 작업 영역(작업 표시줄 등 OS 영역 제외)을 구한다.

# 특이사항, 제약사항
OS가 팝업 창을 작업 영역 안으로 밀어 넣으므로 전체 모니터 경계를 쓰면 팝업 위치가 어긋난다.
DisplayInfo는 모니터의 화면 좌표를 주지 않으므로 해상도 비율이 맞는 디스플레이를 후보로 삼는다.
Windows에서는 모니터마다 배율이 달라 Editor 좌표가 하나로 이어지지 않으므로 ToPopupSpace가 보정한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Editor
{
    // ============================================================
    /// <summary>
    /// 모니터의 작업 영역을 Editor 좌표계로 구한다.
    /// </summary>
    // ============================================================
    internal static class EditorWorkArea
    {

    #region 필드

        private const float displayScaleTolerance = 0.01f;
        private const int maxScaleSearch = 3;

        private static readonly List<DisplayInfo> displayBuffer = new();

    #endregion

    #region 작업 영역

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Unity Editor 좌표계에서 기준점이 속한 모니터의 작업 영역을 구한다.
        /// <br/> 일치하는 디스플레이를 찾지 못하면 모니터 전체 경계를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static Rect Get(Vector2 anchor)
        {
            var desktop = UnityEditorInternal.InternalEditorUtility.GetBoundsOfDesktopAtPoint(anchor);
            var workArea = desktop;
            var matched = false;
            displayBuffer.Clear();
            Screen.GetDisplayLayout(displayBuffer);

            foreach (var display in displayBuffer)
            {
                if (!TryGetScale(display, desktop, out var scale))
                {
                    continue;
                }

                // 작업 영역은 디스플레이 좌상단 기준이므로 Editor 좌표 단위로 바꿔 모니터 경계에 더한다.
                var local = display.workArea;
                var candidate = new Rect
                (
                    desktop.x + local.x / scale, desktop.y + local.y / scale,
                    local.width / scale, local.height / scale
                );

                // 비율이 같은 디스플레이가 여럿이면 어느 쪽에도 들어가도록 가장 좁은 영역을 쓴다.
                workArea = matched ? Intersect(workArea, candidate) : candidate;
                matched = true;
            }

            return workArea;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 소유 창 안의 한 점을 팝업 창을 놓는 모니터의 Editor 좌표로 바꾼다.
        /// <br/> 창 원점의 모니터·창의 배율·점이 놓인 모니터의 배율이 다를 때만 값이 바뀐다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static Vector2 ToPopupSpace
        (
            Vector2 windowOrigin, Vector2 windowPosition, float ownerScale
        )
        {
            var position = windowOrigin + windowPosition;
#if UNITY_EDITOR_WIN
            // Windows의 Editor 좌표는 물리 좌표를 그 점이 놓인 모니터의 배율로 나눈 값이다.
            // 창 원점은 원점이 놓인 모니터의 배율로, 창 안의 오프셋은 창이 그려지는 배율로 물리 좌표가 된다.
            var originScale = GetDesktopScale
            (
                UnityEditorInternal.InternalEditorUtility.GetBoundsOfDesktopAtPoint(windowOrigin)
            );

            if (originScale <= 0f)
            {
                return position;
            }

            var physical = windowOrigin * originScale + windowPosition * ownerScale;
            var scale = originScale;

            // 물리 좌표가 놓인 모니터를 찾는다. 그 모니터의 배율로 나눈 점이 다시 그 모니터에 속해야 한다.
            for (var attempt = 0; attempt < maxScaleSearch; attempt++)
            {
                var candidate = physical / scale;
                var desktopScale = GetDesktopScale
                (
                    UnityEditorInternal.InternalEditorUtility.GetBoundsOfDesktopAtPoint(candidate)
                );

                if (desktopScale <= 0f)
                {
                    break;
                }

                if (Mathf.Abs(desktopScale - scale) <= displayScaleTolerance)
                {
                    return candidate;
                }

                scale = desktopScale;
            }
#endif
            return position;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 모니터 경계의 논리 크기와 해상도가 맞는 디스플레이로 배율을 구한다.
        /// <br/> 배율이 1보다 작은 후보는 제외하고 후보가 여럿이면 가장 작은 배율을 쓴다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static float GetDesktopScale(Rect desktop)
        {
            var result = 0f;
            displayBuffer.Clear();
            Screen.GetDisplayLayout(displayBuffer);

            foreach (var display in displayBuffer)
            {
                if (TryGetScale(display, desktop, out var scale))
                {
                    result = result <= 0f ? scale : Mathf.Min(result, scale);
                }
            }

            return result;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 디스플레이 해상도가 모니터 경계의 논리 크기와 같은 비율인지 확인해 배율을 구한다.
        /// <br/> 배율이 1보다 작은 후보는 실제 모니터가 될 수 없으므로 제외한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static bool TryGetScale(DisplayInfo display, Rect desktop, out float scale)
        {
            scale = 0f;

            if (display.width <= 0 || display.height <= 0 || desktop.width <= 0f)
            {
                return false;
            }

            var scaleX = display.width / desktop.width;
            var scaleY = display.height / desktop.height;

            if (Mathf.Abs(scaleX - scaleY) > displayScaleTolerance
                || scaleX < 1f - displayScaleTolerance)
            {
                return false;
            }

            scale = scaleX;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 두 Rect의 교집합을 구한다.
        /// </summary>
        // ------------------------------------------------------------
        private static Rect Intersect(Rect first, Rect second)
        {
            return Rect.MinMaxRect
            (
                Mathf.Max(first.xMin, second.xMin), Mathf.Max(first.yMin, second.yMin),
                Mathf.Min(first.xMax, second.xMax), Mathf.Min(first.yMax, second.yMax)
            );
        }

    #endregion

    }
}
