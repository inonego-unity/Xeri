/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UGUIDropResolver.cs
수정일 : 2026-05-22

# 설명
UGUI EventSystem raycast 결과로 현재 드롭존을 찾는다.
========================================================================= BLOCK_HEADER_END */

using System.Collections;
using System.Collections.Generic;

using UnityEngine.EventSystems;

namespace inonego.Xeri.UI.DragDrop
{
    // ============================================================
    /// <summary>
    /// UGUI 드롭 대상 결정자.
    /// </summary>
    // ============================================================
    public sealed class UGUIDropResolver : IDropResolver
    {

    #region 필드

        private readonly EventSystem eventSystem;
        private readonly PointerEventData eventData;
        private readonly List<RaycastResult> raycastResults = new();

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UGUI 드롭 대상 결정자를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public UGUIDropResolver(EventSystem eventSystem) : base()
        {
            this.eventSystem = eventSystem;
            eventData = eventSystem != null ? new PointerEventData(eventSystem) : null;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 입력 위치에서 가장 먼저 감지되는 DropZone을 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        public DropZone Resolve(InputPoint input, Draggable draggable)
        {
            var currentEventSystem = eventSystem ?? EventSystem.current;
            if (currentEventSystem == null) return null;

            var data = eventData ?? new PointerEventData(currentEventSystem);
            data.Reset();
            data.position = input.Pos;

            raycastResults.Clear();
            currentEventSystem.RaycastAll(data, raycastResults);

            foreach (var result in raycastResults)
            {
                if (result.gameObject == null) continue;

                var dropZoneUI = result.gameObject.GetComponentInParent<DropZoneUI>();
                if (dropZoneUI != null)
                {
                    return dropZoneUI.DropZone;
                }
            }

            return null;
        }

    #endregion

    }
}
