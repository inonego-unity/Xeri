/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UGUIDragCoordinateProvider.cs
수정일 : 2026-05-22

# 설명
UGUI RectTransform 좌표를 Core 드래그 좌표 Provider 로 연결한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.EventSystems;

namespace inonego.Xeri.UI.DragDrop
{
    // ============================================================
    /// <summary>
    /// UGUI 드래그 좌표 Provider.
    /// </summary>
    // ============================================================
    public sealed class UGUIDragCoordinateProvider : IDragCoordinateProvider
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 좌표를 읽고 쓸 RectTransform.
        /// </summary>
        // ------------------------------------------------------------
        private readonly RectTransform rectTransform;

        // ------------------------------------------------------------
        /// <summary>
        /// 화면 좌표를 로컬 좌표로 변환할 때 사용하는 이벤트 카메라.
        /// </summary>
        // ------------------------------------------------------------
        private Camera eventCamera = null;

        // ------------------------------------------------------------
        /// <summary>
        /// RectTransform 의 드래그 기준 위치.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 Pos
        {
            get
            {
                if (rectTransform == null) return Vector2.zero;

                return rectTransform.anchoredPosition;
            }
            set
            {
                if (rectTransform == null) return;

                rectTransform.anchoredPosition = value;
            }
        }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UGUI 좌표 Provider 를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public UGUIDragCoordinateProvider(RectTransform rectTransform, PointerEventData eventData = null)
        {
            this.rectTransform = rectTransform;
            RefreshEventData(eventData);
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 입력 이벤트의 카메라 정보를 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        public void RefreshEventData(PointerEventData eventData)
        {
            eventCamera = eventData?.pressEventCamera;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 화면 입력 좌표를 부모 RectTransform 기준 좌표로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 ToLocalPos(Vector2 inputPos)
        {
            if (rectTransform == null)
            {
                return Vector2.zero;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle
            (
                rectTransform.parent as RectTransform,
                inputPos,
                eventCamera,
                out Vector2 localPos
            );

            return localPos;
        }

    #endregion

    }
}
