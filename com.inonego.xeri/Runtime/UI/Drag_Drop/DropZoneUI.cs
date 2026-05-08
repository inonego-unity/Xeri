/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DropZoneUI.cs
수정일 : 2026-05-08

# 설명
DraggableUI 가 드롭될 수 있는 UGUI 영역. SpecificDropZone 지정 + DropEnter/Done/Exit 이벤트를 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.EventSystems;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 드롭 이벤트 인자.
    /// </summary>
    // ============================================================
    [Serializable]
    public struct DropEventArgs
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 드롭된 게임오브젝트.
        /// </summary>
        // ------------------------------------------------------------
        public GameObject DroppedGO;

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭된 DraggableUI 컴포넌트.
        /// </summary>
        // ------------------------------------------------------------
        public DraggableUI DraggableUI;
    }

    // ============================================================
    /// <summary>
    /// 드래그된 UI 요소를 받을 수 있는 드롭존 컴포넌트.
    /// </summary>
    // ============================================================
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class DropZoneUI : MonoBehaviour,
        IDropHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {

    #region 필드

        [Header("설정")]
        public bool CanDrop = true;

        [Header("드롭 정보")]
        public bool IsDropping => current != null;

        private PointerEventData current = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 드롭 진입한 DraggableUI.
        /// </summary>
        // ------------------------------------------------------------
        public DraggableUI Draggable => draggable;

        [SerializeField, ReadOnly]
        private DraggableUI draggable = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 부착된 CanvasGroup.
        /// </summary>
        // ------------------------------------------------------------
        public CanvasGroup CanvasGroup => canvasGroup;

        [SerializeField, ReadOnly]
        private CanvasGroup canvasGroup = null;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 이벤트 핸들러.
        /// </summary>
        // ------------------------------------------------------------
        public delegate void DropEventHandler(DropZoneUI sender, DropEventArgs e);

        // ------------------------------------------------------------
        /// <summary>
        /// 포인터가 드롭존에 들어올 때 호출.
        /// </summary>
        // ------------------------------------------------------------
        public event DropEventHandler OnDropEnter = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 포인터가 드롭존에서 나갈 때 호출.
        /// </summary>
        // ------------------------------------------------------------
        public event DropEventHandler OnDropExit = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭이 완료될 때 호출.
        /// </summary>
        // ------------------------------------------------------------
        public event DropEventHandler OnDropDone = null;

    #endregion

    #region 유니티 이벤트

        private void Awake()
        {
            GetComponents();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 필요한 컴포넌트를 캐시한다.
        /// </summary>
        // ------------------------------------------------------------
        private void GetComponents()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (IsDropping)
            {
                if (draggable != null)
                {
                    if (!draggable.IsDragging)
                    {
                        _OnDropExit(current);
                    }
                }
            }
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 가능 여부를 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool CheckCanDrop(DraggableUI draggableUI)
        {
            if (draggableUI == null) return false;

            var specificDropZone = draggableUI.SpecificDropZone;

            var check = specificDropZone == null || specificDropZone == this;

            return draggableUI.IsDragging && draggableUI.CanDrop && CanDrop && check;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 이벤트 인자를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private DropEventArgs CreateDropEventArgs(DraggableUI draggableUI)
        {
            if (draggableUI == null)
            {
                return new DropEventArgs();
            }

            return new DropEventArgs
            {
                DroppedGO   = draggableUI.gameObject,
                DraggableUI = draggableUI,
            };
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// 포인터가 드롭존에 들어올 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        private void _OnDropEnter(PointerEventData eventData)
        {
            if (eventData == null) return;

            var pointerDrag = eventData.pointerDrag;
            if (pointerDrag == null) return;

            var draggable = pointerDrag.GetComponent<DraggableUI>();
            if (draggable == null) return;

            if (!CheckCanDrop(draggable)) return;

            // ------------------------------------------------------------
            // 드롭 정보 설정
            // ------------------------------------------------------------
            this.draggable = draggable;

            current = eventData;

            var dropEventArgs = CreateDropEventArgs(draggable);

            OnDropEnter?.Invoke(this, dropEventArgs);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭존에 드래그된 객체가 드롭될 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        private void _OnDrop(PointerEventData eventData)
        {
            if (eventData == null) return;

            var pointerDrag = eventData.pointerDrag;
            if (pointerDrag == null) return;

            if (draggable == null) return;

            if (pointerDrag != draggable.gameObject) return;

            if (!CheckCanDrop(draggable)) return;

            var dropEventArgs = CreateDropEventArgs(draggable);

            OnDropDone?.Invoke(this, dropEventArgs);

            _OnDropExit(eventData);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 포인터가 드롭존에서 나갈 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        private void _OnDropExit(PointerEventData eventData)
        {
            if (eventData == null) return;

            var dropEventArgs = CreateDropEventArgs(draggable);

            // ------------------------------------------------------------
            // 드롭 정보 정리
            // ------------------------------------------------------------
            current = null;

            this.draggable = null;

            OnDropExit?.Invoke(this, dropEventArgs);
        }

    #endregion

    #region 인터페이스 구현

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if (!IsDropping)
            {
                _OnDropEnter(eventData);
            }
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            if (IsDropping)
            {
                _OnDropExit(eventData);
            }
        }

        void IDropHandler.OnDrop(PointerEventData eventData)
        {
            if (IsDropping)
            {
                _OnDrop(eventData);
            }
        }

    #endregion

    }
}
