/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DraggableUI.cs
수정일 : 2026-07-30

# 설명
UGUI EventSystem 입력을 Core Draggable에 연결하고 모든 종료 경로에서 입력 상태를 복원한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.EventSystems;

namespace inonego.Xeri.UI.DragDrop
{
    // ============================================================
    /// <summary>
    /// UGUI 드래그 가능 UI 컴포넌트.
    /// </summary>
    // ============================================================
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class DraggableUI : MonoBehaviour,
        IPointerDownHandler,
        IInitializePotentialDragHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {

    #region 필드

        [Header("설정")]
        [SerializeField]
        private bool canMove = true;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 중 위치 이동을 허용하는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanMove
        {
            get => canMove;
            set
            {
                canMove = value;
                if (draggable != null)
                {
                    draggable.CanMove = value;
                }
            }
        }

        [SerializeField]
        private bool canDrop = true;

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 영역에 드롭될 수 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanDrop
        {
            get => canDrop;
            set
            {
                canDrop = value;
                if (draggable != null)
                {
                    draggable.CanDrop = value;
                }
            }
        }

        [SerializeField]
        private PointerEventData.InputButton dragButton = PointerEventData.InputButton.Left;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작을 허용할 버튼.
        /// </summary>
        // ------------------------------------------------------------
        public PointerEventData.InputButton DragButton
        {
            get => dragButton;
            set
            {
                dragButton = value;
                if (mouseButtonFilter != null)
                {
                    mouseButtonFilter.Button = value;
                }
            }
        }

        [SerializeField]
        private bool disableRaycastDuringDrag = true;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 중 CanvasGroup raycast를 끌지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool DisableRaycastDuringDrag
        {
            get => disableRaycastDuringDrag;
            set
            {
                disableRaycastDuringDrag = value;

                if (raycastPolicy != null)
                {
                    raycastPolicy.End();
                    raycastPolicy = new UGUIRaycastPolicy(canvasGroup, value);
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 드래그 중인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDragging => draggable != null && draggable.IsDragging;

        // ------------------------------------------------------------
        /// <summary>
        /// Core 드래그 대상 상태.
        /// </summary>
        // ------------------------------------------------------------
        public Draggable Draggable => draggable;

        private Draggable draggable = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 드롭 조율자.
        /// </summary>
        // ------------------------------------------------------------
        public DragDropCoordinator Coordinator
        {
            get => coordinator ?? DragDropCoordinator.Default;
            set => coordinator = value;
        }

        private DragDropCoordinator coordinator = null;

        private CanvasGroup canvasGroup = null;
        private RectTransform rectTransform = null;
        private UGUIDragCoordinateProvider coordinateProvider = null;
        private UGUIMouseButtonFilter mouseButtonFilter = null;
        private UGUIRaycastPolicy raycastPolicy = null;
        private Action dragEndCleanup = null;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event DragEventHandler OnDragBegin = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 진행 중 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event DragEventHandler OnDrag = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 종료 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event DragEventHandler OnDragEnd = null;

    #endregion

    #region 유니티 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// UGUI 연결 객체를 준비한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Awake()
        {
            InitializeRuntime();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비활성화 시 진행 중인 드래그를 강제로 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            ForceDragEnd();
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// UGUI 컴포넌트와 Core 드래그 연결 객체를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void InitializeRuntime()
        {
            canvasGroup   = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();

            coordinateProvider = new UGUIDragCoordinateProvider(rectTransform);
            draggable          = new Draggable(this, coordinateProvider)
            {
                CanMove = canMove,
                CanDrop = canDrop,
            };
            mouseButtonFilter = new UGUIMouseButtonFilter(dragButton);
            raycastPolicy     = new UGUIRaycastPolicy(canvasGroup, disableRaycastDuringDrag);

            // 공개 이벤트는 컴포넌트가 소유해 구독만으로 런타임 객체가 생성되지 않게 한다.
            draggable.OnDragBegin += HandleDragBegin;
            draggable.OnDrag      += HandleDrag;
            draggable.OnDragEnd   += HandleDragEnd;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// PointerEventData를 Core 입력 지점으로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        private InputPoint CreateInputPoint(PointerEventData eventData)
        {
            return new InputPoint(eventData.pointerId, eventData.position);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 입력으로 드래그를 시작할 수 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool CanStartDrag(PointerEventData eventData)
        {
            return mouseButtonFilter.CanDrag(eventData);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 허용되지 않은 입력을 부모 UI로 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void PassToParent<TEventFunction>
        (
            PointerEventData eventData,
            ExecuteEvents.EventFunction<TEventFunction> callback
        )
        where TEventFunction : IEventSystemHandler
        {
            var parent = transform.parent;
            if (parent == null) return;

            ExecuteEvents.ExecuteHierarchy(parent.gameObject, eventData, callback);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 종료 경로에서 반드시 실행할 내부 정리 Callback을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void AddDragEndCleanup(Action cleanup)
        {
            dragEndCleanup += cleanup;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 종료 내부 정리 Callback을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void RemoveDragEndCleanup(Action cleanup)
        {
            dragEndCleanup -= cleanup;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 강제로 드래그를 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ForceDragEnd()
        {
            if (draggable == null) return;
            if (!draggable.IsDragging) return;

            try
            {
                Coordinator.HandleDragCancel(draggable);
            }
            finally
            {
                try
                {
                    draggable.ForceDragEnd();
                }
                finally
                {
                    try
                    {
                        dragEndCleanup?.Invoke();
                    }
                    finally
                    {
                        raycastPolicy.End();
                    }
                }
            }
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// Core 드래그 시작을 컴포넌트 구독자에게 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleDragBegin(Draggable sender, DragEventArgs eventData)
        {
            OnDragBegin?.Invoke(sender, eventData);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Core 드래그 진행을 컴포넌트 구독자에게 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleDrag(Draggable sender, DragEventArgs eventData)
        {
            OnDrag?.Invoke(sender, eventData);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Core 드래그 종료를 컴포넌트 구독자에게 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleDragEnd(Draggable sender, DragEventArgs eventData)
        {
            OnDragEnd?.Invoke(sender, eventData);
        }

    #endregion

    #region 인터페이스 구현

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null) return;

            coordinateProvider.RefreshEventData(eventData);

            if (!CanStartDrag(eventData))
            {
                // ------------------------------------------------------------
                // 허용되지 않은 입력은 부모로 전달
                // ------------------------------------------------------------
                PassToParent(eventData, ExecuteEvents.pointerDownHandler);
                return;
            }

            // ----------------------------------------------------------------------
            // 드래그 이전 입력 시점에 오프셋과 원점 계산
            // ----------------------------------------------------------------------
            draggable.PrepareDrag(CreateInputPoint(eventData));
        }

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData == null) return;

            if (!CanStartDrag(eventData))
            {
                eventData.pointerDrag = null;
            }
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (eventData == null) return;

            if (draggable.IsDragging) return;
            if (!CanStartDrag(eventData)) return;

            coordinateProvider.RefreshEventData(eventData);
            var input = CreateInputPoint(eventData);

            try
            {
                raycastPolicy.Begin();
                draggable.InvokeDragBegin(input);

                if (draggable.IsDragging)
                {
                    Coordinator.HandleDragBegin(draggable);
                    Coordinator.HandleDrag(draggable, input);
                }
            }
            catch
            {
                // Begin 구독 또는 Coordinator 실패 뒤 활성 Drag와 Raycast 상태를 남기지 않는다.
                if (draggable.IsDragging)
                {
                    ForceDragEnd();
                }
                else
                {
                    raycastPolicy.End();
                }

                throw;
            }
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (eventData == null) return;

            coordinateProvider.RefreshEventData(eventData);
            var input = CreateInputPoint(eventData);

            // ------------------------------------------------------------
            // 실제 드래그 처리는 Core Draggable과 Coordinator가 수행
            // ------------------------------------------------------------
            draggable.InvokeDrag(input);

            if (draggable.IsDragging)
            {
                Coordinator.HandleDrag(draggable, input);
            }
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (eventData == null) return;

            if (!draggable.IsDragging) return;

            coordinateProvider.RefreshEventData(eventData);
            var input = CreateInputPoint(eventData);

            try
            {
                Coordinator.HandleDragEnd(draggable);
            }
            finally
            {
                try
                {
                    draggable.InvokeDragEnd(input);
                }
                finally
                {
                    try
                    {
                        // 공개 종료 구독자가 실패해도 연결이 소유한 시각 자원은 반환한다.
                        dragEndCleanup?.Invoke();
                    }
                    finally
                    {
                        raycastPolicy.End();
                    }
                }
            }
        }

    #endregion

    }
}
