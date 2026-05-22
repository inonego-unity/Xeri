/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Draggable.cs
수정일 : 2026-05-22

# 설명
UI 시스템에 독립적인 드래그 대상 상태 객체.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.DragDrop
{
    // ============================================================
    /// <summary>
    /// 드래그 대상.
    /// </summary>
    // ============================================================
    public sealed class Draggable
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
            set => canMove = value;
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
            set => canDrop = value;
        }

        private bool canDrop = true;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 드래그 중인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDragging => isDragging;

        private bool isDragging = false;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작 기준 위치.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2? OriginPos => originPos;

        private Vector2? originPos = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 입력 좌표와 드래그 대상 위치 간의 오프셋.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2? Offset => offset;

        private Vector2? offset = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 이 드래그 상태를 소유한 UI 객체.
        /// </summary>
        // ------------------------------------------------------------
        public object Owner => owner;

        private readonly object owner;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작 전에 준비한 기준 위치.
        /// </summary>
        // ------------------------------------------------------------
        private Vector2? beginOriginPos = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작 전에 준비한 입력 좌표와 대상 위치 간의 오프셋.
        /// </summary>
        // ------------------------------------------------------------
        private Vector2? beginOffset = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 드래그를 소유한 입력 지점.
        /// </summary>
        // ------------------------------------------------------------
        private InputPoint currentInput = default;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 대상 위치와 입력 좌표 변환을 제공하는 객체.
        /// </summary>
        // ------------------------------------------------------------
        private readonly IDragCoordinateProvider coordinateProvider;

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
        /// 드래그 대상 상태를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public Draggable(object owner, IDragCoordinateProvider coordinateProvider)
        {
            this.owner              = owner;
            this.coordinateProvider = coordinateProvider;
        }

    #endregion

    #region 드래그 생명주기

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작 전에 기준 위치와 오프셋을 준비한다.
        /// </summary>
        // ------------------------------------------------------------
        public void PrepareDrag(InputPoint input)
        {
            if (coordinateProvider == null) return;

            Vector2 localPos = coordinateProvider.ToLocalPos(input.Pos);
            Vector2 pos      = coordinateProvider.Pos;

            beginOriginPos = pos;
            beginOffset    = pos - localPos;
            currentInput   = input;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작 상태로 전환하고 시작 이벤트를 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        public void InvokeDragBegin(InputPoint input)
        {
            if (coordinateProvider == null) return;
            if (isDragging) return;

            // ------------------------------------------------------------
            // 드래그 정보 설정
            // ------------------------------------------------------------
            currentInput = input;

            Vector2 currentPos = coordinateProvider.Pos;
            Vector2 localPos   = coordinateProvider.ToLocalPos(input.Pos);

            originPos = beginOriginPos ?? currentPos;
            offset    = beginOffset ?? currentPos - localPos;

            beginOriginPos = null;
            beginOffset    = null;
            isDragging     = true;

            // --------------------------------------------------------------------------
            // 드래그 시작 시점에 즉시 위치를 입력 위치 + 오프셋으로 이동시켜 Threshold 지연을 보정.
            // --------------------------------------------------------------------------
            if (canMove)
            {
                coordinateProvider.Pos = CalculateGoalPos(localPos);
            }

            OnDragBegin?.Invoke(this, CreateDragEventArgs(localPos));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 진행 상태를 갱신하고 진행 이벤트를 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        public void InvokeDrag(InputPoint input)
        {
            if (!isDragging) return;
            if (input.ID != currentInput.ID) return;

            currentInput = input;
            Vector2 localPos = coordinateProvider.ToLocalPos(input.Pos);

            if (canMove)
            {
                coordinateProvider.Pos = CalculateGoalPos(localPos);
            }

            OnDrag?.Invoke(this, CreateDragEventArgs(localPos));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 종료 상태를 정리하고 종료 이벤트를 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        public void InvokeDragEnd(InputPoint input)
        {
            if (!isDragging) return;
            if (input.ID != currentInput.ID) return;

            Vector2 localPos = coordinateProvider.ToLocalPos(input.Pos);
            var eventArgs    = CreateDragEventArgs(localPos);

            ClearDragState();

            OnDragEnd?.Invoke(this, eventArgs);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 입력으로 드래그를 강제 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ForceDragEnd()
        {
            if (!isDragging) return;

            Vector2 localPos = coordinateProvider != null
                ? coordinateProvider.ToLocalPos(currentInput.Pos)
                : Vector2.zero;
            var eventArgs = CreateDragEventArgs(localPos);

            ClearDragState();

            OnDragEnd?.Invoke(this, eventArgs);
        }

    #endregion

    #region 상태 갱신

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 위치를 기준으로 드래그 원점과 오프셋을 다시 잡는다.
        /// </summary>
        // ------------------------------------------------------------
        public void RebaseDrag(InputPoint input)
        {
            if (!isDragging) return;
            if (input.ID != currentInput.ID) return;

            Vector2 localPos = coordinateProvider.ToLocalPos(input.Pos);
            Vector2 pos      = coordinateProvider.Pos;

            originPos = pos;
            offset    = pos - localPos;
        }

    #endregion

    #region 계산

        // ------------------------------------------------------------
        /// <summary>
        /// 입력 기준 좌표와 오프셋으로 목표 위치를 계산한다.
        /// </summary>
        // ------------------------------------------------------------
        private Vector2 CalculateGoalPos(Vector2 localPos)
        {
            return localPos + (offset ?? Vector2.zero);
        }

    #endregion

    #region 이벤트 인자

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 드래그 상태로 이벤트 인자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private DragEventArgs CreateDragEventArgs(Vector2 localPos)
        {
            return new DragEventArgs
            {
                Pos       = coordinateProvider?.Pos ?? Vector2.zero,
                GoalPos   = CalculateGoalPos(localPos),
                OriginPos = originPos ?? Vector2.zero,
                Offset    = offset ?? Vector2.zero,
            };
        }

    #endregion

    #region 정리

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 진행 상태를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ClearDragState()
        {
            // ------------------------------------------------------------
            // 드래그 정보 정리
            // ------------------------------------------------------------
            isDragging     = false;
            originPos      = null;
            offset         = null;
            beginOriginPos = null;
            beginOffset    = null;
            currentInput   = default;
        }

    #endregion

    }
}
