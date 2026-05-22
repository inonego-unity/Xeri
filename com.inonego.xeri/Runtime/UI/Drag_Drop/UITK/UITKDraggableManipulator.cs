/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKDraggableManipulator.cs
수정일 : 2026-05-22

# 설명
UI Toolkit Pointer 이벤트를 Core Draggable에 연결하는 Manipulator.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.DragDrop
{
    // ============================================================
    /// <summary>
    /// UI Toolkit 드래그 Manipulator.
    /// </summary>
    // ============================================================
    public sealed class UITKDraggableManipulator : Manipulator
    {

    #region 필드

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

        private bool canMove = true;

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

        private bool canDrop = true;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그를 허용할 버튼.
        /// </summary>
        // ------------------------------------------------------------
        public int DragButton
        {
            get => dragButton;
            set
            {
                dragButton = value;
                mouseButtonFilter.Button = value;
            }
        }

        private int dragButton = 0;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작으로 인정할 최소 이동 거리.
        /// </summary>
        // ------------------------------------------------------------
        public float DragThreshold
        {
            get => dragThreshold;
            set => dragThreshold = Mathf.Max(0f, value);
        }

        private float dragThreshold = 5f;

        // ------------------------------------------------------------
        /// <summary>
        /// 위치 적용을 위해 absolute position 을 강제할지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool ForceAbsolutePosition
        {
            get => forceAbsolutePosition;
            set
            {
                forceAbsolutePosition = value;
                if (coordinateProvider != null)
                {
                    coordinateProvider.ForceAbsolutePosition = value;
                }
            }
        }

        private bool forceAbsolutePosition = true;

        // ------------------------------------------------------------
        /// <summary>
        /// Core 드래그 대상 상태.
        /// </summary>
        // ------------------------------------------------------------
        public Draggable Draggable => draggable;

        private Draggable draggable = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 드래그 중인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDragging => draggable != null && draggable.IsDragging;

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

        private UITKDragCoordinateProvider coordinateProvider = null;
        private readonly UITKMouseButtonFilter mouseButtonFilter = new();
        private InputPoint beginInput = default;
        private int activeID = -1;
        private bool isWaitingDragBegin = false;

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

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UI Toolkit 드래그 Manipulator 를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public UITKDraggableManipulator(DragDropCoordinator coordinator = null) : base()
        {
            this.coordinator = coordinator;
        }

    #endregion

    #region 콜백 등록

        // ------------------------------------------------------------
        /// <summary>
        /// target VisualElement 에 pointer 이벤트를 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void RegisterCallbacksOnTarget()
        {
            EnsureRuntimeObjects();

            target.RegisterCallback<PointerDownEvent>  (OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>  (OnPointerMove);
            target.RegisterCallback<PointerUpEvent>    (OnPointerUp);
            target.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// target VisualElement 에서 pointer 이벤트를 등록 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>  (OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>  (OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>    (OnPointerUp);
            target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);

            ForceDragCancel();
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Core Draggable과 UI Toolkit 연결 객체가 생성되어 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private void EnsureRuntimeObjects()
        {
            if (draggable != null) return;

            coordinateProvider = new UITKDragCoordinateProvider(target, forceAbsolutePosition);
            draggable          = new Draggable(target, coordinateProvider)
            {
                CanMove = canMove,
                CanDrop = canDrop,
            };
            draggable.OnDragBegin += InvokeDragBegin;
            draggable.OnDrag      += InvokeDrag;
            draggable.OnDragEnd   += InvokeDragEnd;
            mouseButtonFilter.Button = dragButton;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Pointer 이벤트를 Core 입력 지점으로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        private InputPoint CreateInputPoint(IPointerEvent eventData)
        {
            return new InputPoint(eventData.pointerId, eventData.position);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 준비된 입력이 threshold 를 넘었는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool HasPassedThreshold(InputPoint input)
        {
            return Vector2.Distance(beginInput.Pos, input.Pos) >= dragThreshold;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 취소 상태로 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ForceDragCancel()
        {
            if (draggable != null && draggable.IsDragging)
            {
                Coordinator.HandleDragCancel(draggable);
                draggable.ForceDragEnd();
            }

            if (target != null && activeID >= 0 && target.HasPointerCapture(activeID))
            {
                target.ReleasePointer(activeID);
            }

            activeID            = -1;
            isWaitingDragBegin = false;
            beginInput          = default;
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작 후보 입력을 준비한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnPointerDown(PointerDownEvent eventData)
        {
            if (eventData == null) return;
            if (!mouseButtonFilter.CanDrag(eventData)) return;

            EnsureRuntimeObjects();

            beginInput = CreateInputPoint(eventData);
            activeID   = beginInput.ID;

            // ------------------------------------------------------------
            // 드래그 이전 입력 시점에 오프셋과 원점 계산
            // ------------------------------------------------------------
            draggable.PrepareDrag(beginInput);
            target.CapturePointer(activeID);

            isWaitingDragBegin = true;
            eventData.StopPropagation();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// pointer 이동으로 드래그 시작과 진행을 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnPointerMove(PointerMoveEvent eventData)
        {
            if (eventData == null) return;
            if (eventData.pointerId != activeID) return;

            var input = CreateInputPoint(eventData);

            if (isWaitingDragBegin)
            {
                if (!HasPassedThreshold(input)) return;

                isWaitingDragBegin = false;

                // ------------------------------------------------------------
                // Threshold 지연 보정
                // ------------------------------------------------------------
                draggable.InvokeDragBegin(input);

                if (draggable.IsDragging)
                {
                    Coordinator.HandleDragBegin(draggable);
                    Coordinator.HandleDrag(draggable, input);
                }

                eventData.StopPropagation();
                return;
            }

            if (!draggable.IsDragging) return;

            draggable.InvokeDrag(input);

            if (draggable.IsDragging)
            {
                Coordinator.HandleDrag(draggable, input);
            }

            eventData.StopPropagation();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// pointer 해제로 드래그를 정상 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnPointerUp(PointerUpEvent eventData)
        {
            if (eventData == null) return;
            if (eventData.pointerId != activeID) return;

            var input = CreateInputPoint(eventData);

            if (draggable.IsDragging)
            {
                Coordinator.HandleDragEnd(draggable);
                draggable.InvokeDragEnd(input);
            }

            if (target.HasPointerCapture(activeID))
            {
                target.ReleasePointer(activeID);
            }

            activeID            = -1;
            isWaitingDragBegin = false;
            beginInput          = default;
            eventData.StopPropagation();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// pointer 취소로 드래그를 강제 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnPointerCancel(PointerCancelEvent eventData)
        {
            if (eventData == null) return;
            if (eventData.pointerId != activeID) return;

            ForceDragCancel();
            eventData.StopPropagation();
        }

    #endregion

    #region 이벤트 호출

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작 이벤트를 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void InvokeDragBegin(Draggable sender, DragEventArgs e)
        {
            OnDragBegin?.Invoke(sender, e);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 진행 이벤트를 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void InvokeDrag(Draggable sender, DragEventArgs e)
        {
            OnDrag?.Invoke(sender, e);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 종료 이벤트를 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void InvokeDragEnd(Draggable sender, DragEventArgs e)
        {
            OnDragEnd?.Invoke(sender, e);
        }

    #endregion

    }
}
