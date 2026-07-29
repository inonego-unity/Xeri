/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UGUIPointerRaycastQuery.cs
수정일 : 2026-07-29

# 설명
명시적으로 연결한 EventSystem과 GraphicRaycaster로 현재 포인터의 UGUI hit를 조회한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UGUI 포인터 raycast 조회 backend.
    /// </summary>
    // ============================================================
    public sealed class UGUIPointerRaycastQuery : MonoBehaviour
    {
    #region 필드

        [SerializeField]
        private EventSystem eventSystem = null;

        [SerializeField]
        private GraphicRaycaster raycaster = null;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 화면 좌표의 UGUI raycast 결과를 호출자 목록에 채운다.
        /// </summary>
        // ------------------------------------------------------------
        public void Raycast
        (
            Vector2 screenPosition,
            List<RaycastResult> results
        )
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            if (eventSystem == null || raycaster == null)
            {
                throw new InvalidOperationException("EventSystem 또는 GraphicRaycaster가 연결되지 않았습니다.");
            }

            results.Clear();
            var eventData = new PointerEventData(eventSystem)
            {
                position = screenPosition,
            };

            raycaster.Raycast(eventData, results);
        }

    #endregion

    }
}
