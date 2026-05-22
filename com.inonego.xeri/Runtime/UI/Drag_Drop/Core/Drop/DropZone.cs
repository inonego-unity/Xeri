/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DropZone.cs
수정일 : 2026-05-22

# 설명
UI 시스템에 독립적인 드롭존 상태 객체.
========================================================================= BLOCK_HEADER_END */

using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.DragDrop
{
    // ============================================================
    /// <summary>
    /// 드롭존.
    /// </summary>
    // ============================================================
    public sealed class DropZone
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 허용 여부.
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
        /// 현재 드래그 대상이 진입해 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDropping => currentDraggable != null;

        // ------------------------------------------------------------
        /// <summary>
        /// 이 드롭 영역 상태를 소유한 UI 객체.
        /// </summary>
        // ------------------------------------------------------------
        public object Owner => owner;

        private readonly object owner;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 진입한 드래그 대상.
        /// </summary>
        // ------------------------------------------------------------
        public Draggable Draggable => currentDraggable;

        private Draggable currentDraggable = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 가능 여부를 판단하는 규칙 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<IDropRule> DropRules => dropRules;

        private readonly List<IDropRule> dropRules = new();

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 대상이 드롭 영역에 진입할 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event DropEventHandler OnDropEnter = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 대상이 드롭 영역에서 이탈할 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event DropEventHandler OnDropExit = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 대상이 드롭 영역에 드롭될 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event DropEventHandler OnDropDone = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 영역 상태를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public DropZone(object owner)
        {
            this.owner = owner;
        }

    #endregion

    #region 드롭 규칙

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 규칙을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddDropRule(IDropRule dropRule)
        {
            if (dropRule == null) return;
            if (dropRules.Contains(dropRule)) return;

            dropRules.Add(dropRule);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 규칙을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void RemoveDropRule(IDropRule dropRule)
        {
            if (dropRule == null) return;

            dropRules.Remove(dropRule);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 등록된 드롭 규칙을 모두 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ClearDropRules()
        {
            dropRules.Clear();
        }

    #endregion

    #region 진입

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 대상을 드롭 영역에 진입시킨다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryAccept(Draggable draggable)
        {
            var dragAvailable = canDrop && draggable != null &&
                                draggable.IsDragging && draggable.CanDrop;

            if (!dragAvailable) return false;

            foreach (var dropRule in dropRules)
            {
                if (dropRule != null && !dropRule.CanDrop(draggable, this)) return false;
            }

            if (currentDraggable == draggable)
            {
                return true;
            }

            if (currentDraggable != null)
            {
                Exit();
            }

            // ------------------------------------------------------------
            // 드롭 정보 설정
            // ------------------------------------------------------------
            currentDraggable = draggable;
            OnDropEnter?.Invoke(this, CreateDropEventArgs());

            return true;
        }

    #endregion

    #region 완료

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 드래그 대상을 드롭 완료 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Drop()
        {
            if (currentDraggable == null) return;

            var eventArgs = CreateDropEventArgs();
            OnDropDone?.Invoke(this, eventArgs);
            Exit();
        }

    #endregion

    #region 이탈

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 드래그 대상을 드롭 영역에서 이탈시킨다.
        /// </summary>
        // ------------------------------------------------------------
        public void Exit()
        {
            if (currentDraggable == null) return;

            var eventArgs = CreateDropEventArgs();
            currentDraggable = null;

            // ------------------------------------------------------------
            // 드롭 정보 정리 후 이탈 이벤트 발화
            // ------------------------------------------------------------
            OnDropExit?.Invoke(this, eventArgs);
        }

    #endregion

    #region 이벤트 인자

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 드롭 상태로 이벤트 인자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private DropEventArgs CreateDropEventArgs()
        {
            return new DropEventArgs
            {
                Draggable = currentDraggable,
                DropZone  = this,
            };
        }

    #endregion

    }
}
