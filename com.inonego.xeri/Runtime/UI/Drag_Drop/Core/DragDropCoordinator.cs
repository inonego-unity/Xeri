/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DragDropCoordinator.cs
수정일 : 2026-05-22

# 설명
Draggable 과 DropZone 의 등록, 활성 드래그 추적, 드롭 라우팅을 조율한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.DragDrop
{
    // ============================================================
    /// <summary>
    /// 드래그 드롭 조율자.
    /// </summary>
    // ============================================================
    public sealed class DragDropCoordinator
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 드래그 드롭 조율자.
        /// </summary>
        // ------------------------------------------------------------
        public static DragDropCoordinator Default => defaultCoordinator;

        private static readonly DragDropCoordinator defaultCoordinator = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 활성화된 드래그 대상 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyCollection<Draggable> ActiveDraggables => activeDraggables;

        private readonly HashSet<Draggable> activeDraggables = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 등록된 드롭 영역 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyCollection<DropZone> DropZones => dropZones;

        private readonly HashSet<DropZone> dropZones = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 위치를 기준으로 드롭 영역을 찾는 Resolver.
        /// </summary>
        // ------------------------------------------------------------
        public IDropResolver DropResolver
        {
            get => dropResolver;
            set => dropResolver = value;
        }

        private IDropResolver dropResolver = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 대상별 현재 진입한 드롭 영역.
        /// </summary>
        // ------------------------------------------------------------
        private readonly Dictionary<Draggable, DropZone> currentDropZones = new();

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 활성 드래그 목록이 변경될 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action OnActiveCollectionChange = null;

    #endregion

    #region DropZone 등록

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 영역을 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Register(DropZone dropZone)
        {
            if (dropZone == null) return;

            dropZones.Add(dropZone);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 영역을 등록 해제하고 연결된 드래그 상태를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Unregister(DropZone dropZone)
        {
            if (dropZone == null) return;

            dropZones.Remove(dropZone);

            foreach (var pair in new Dictionary<Draggable, DropZone>(currentDropZones))
            {
                if (pair.Value == dropZone)
                {
                    pair.Value.Exit();
                    currentDropZones.Remove(pair.Key);
                }
            }
        }

    #endregion

    #region 드래그 라우팅

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작 대상을 활성 목록에 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void HandleDragBegin(Draggable draggable)
        {
            if (draggable == null) return;

            if (activeDraggables.Add(draggable))
            {
                OnActiveCollectionChange?.Invoke();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 위치를 기준으로 드롭 영역 진입과 이탈을 조율한다.
        /// </summary>
        // ------------------------------------------------------------
        public void HandleDrag(Draggable draggable, InputPoint input)
        {
            if (draggable == null) return;
            if (!draggable.IsDragging) return;

            DropZone nextDropZone = dropResolver?.Resolve(input, draggable);
            currentDropZones.TryGetValue(draggable, out DropZone currentDropZone);

            if (currentDropZone == nextDropZone)
            {
                return;
            }

            if (currentDropZone != null)
            {
                currentDropZone.Exit();
                currentDropZones.Remove(draggable);
            }

            if (nextDropZone != null && nextDropZone.TryAccept(draggable))
            {
                currentDropZones[draggable] = nextDropZone;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 정상 종료 시 드롭 완료와 활성 목록 정리를 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        public void HandleDragEnd(Draggable draggable)
        {
            if (draggable == null) return;

            if (currentDropZones.TryGetValue(draggable, out DropZone currentDropZone))
            {
                currentDropZone.Drop();
                currentDropZones.Remove(draggable);
            }

            if (activeDraggables.Remove(draggable))
            {
                OnActiveCollectionChange?.Invoke();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 취소 시 드롭 완료 없이 이탈과 활성 목록 정리를 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        public void HandleDragCancel(Draggable draggable)
        {
            if (draggable == null) return;

            if (currentDropZones.TryGetValue(draggable, out DropZone currentDropZone))
            {
                currentDropZone.Exit();
                currentDropZones.Remove(draggable);
            }

            if (activeDraggables.Remove(draggable))
            {
                OnActiveCollectionChange?.Invoke();
            }
        }

    #endregion

    }
}
