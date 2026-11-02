/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionRowDragController.cs
수정일 : 2026-10-28

# 설명
Unity 기본 ListView 드래그와 원본 행의 삽입형 이동 요청을 연결한다.
검색·정렬 상태와 현재 페이지의 원본 인덱스를 검증하며 데이터를 직접 변경하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// Collection 행의 기본 드래그 이벤트와 삽입 위치 변환을 소유한다.
    /// </summary>
    // ============================================================
    internal sealed class CollectionRowDragController
    {

    #region 필드

        private const string dragSourceKey = "UniXeri.CollectionEditor.DragSource";

        private readonly ICollectionEditorModel model;
        private readonly IReadOnlyList<int> sourceRows;
        private readonly SearchPagedList<int> searchPagedRows;
        private readonly BaseListView listView;
        private readonly Action<int> selectPageRow;
        private readonly Action<int, int> onMoveRow;

        private readonly Func<CanStartDragArgs, bool> canStartDrag;
        private readonly Func<SetupDragAndDropArgs, StartDragArgs> setupDragAndDrop;
        private readonly Func<HandleDragAndDropArgs, DragVisualMode> dragAndDropUpdate;
        private readonly Func<HandleDragAndDropArgs, DragVisualMode> handleDrop;

        private int dragSourceIndex = -1;
        private ulong originalRevision;
        private bool isBound;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// ListView의 드래그 이벤트와 원본 행 이동 요청을 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionRowDragController
        (
            ICollectionEditorModel model,
            IReadOnlyList<int> sourceRows,
            SearchPagedList<int> searchPagedRows,
            BaseListView listView,
            Action<int> selectPageRow,
            Action<int, int> onMoveRow
        )
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.sourceRows = sourceRows ?? throw new ArgumentNullException(nameof(sourceRows));
            this.searchPagedRows = searchPagedRows ?? throw new ArgumentNullException(nameof(searchPagedRows));
            this.listView = listView ?? throw new ArgumentNullException(nameof(listView));
            this.selectPageRow = selectPageRow ?? throw new ArgumentNullException(nameof(selectPageRow));
            this.onMoveRow = onMoveRow ?? throw new ArgumentNullException(nameof(onMoveRow));

            canStartDrag = CanStartDrag;
            setupDragAndDrop = SetupDragAndDrop;
            dragAndDropUpdate = UpdateDrag;
            handleDrop = HandleDrop;
        }

    #endregion

    #region 드래그 이벤트 연결

        // ------------------------------------------------------------
        /// <summary>
        /// 화면에 연결된 동안 기본 ListView 드래그 이벤트를 구독한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Bind()
        {
            if (isBound)
            {
                return;
            }

            listView.canStartDrag += canStartDrag;
            listView.setupDragAndDrop += setupDragAndDrop;
            listView.dragAndDropUpdate += dragAndDropUpdate;
            listView.handleDrop += handleDrop;
            listView.RegisterCallback<DragExitedEvent>(HandleDragExited);
            isBound = true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 화면 해제 시 드래그 상태와 모든 이벤트 구독을 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Unbind()
        {
            if (!isBound)
            {
                return;
            }

            listView.canStartDrag -= canStartDrag;
            listView.setupDragAndDrop -= setupDragAndDrop;
            listView.dragAndDropUpdate -= dragAndDropUpdate;
            listView.handleDrop -= handleDrop;
            listView.UnregisterCallback<DragExitedEvent>(HandleDragExited);
            isBound = false;
            CancelDrag();
        }

    #endregion

    #region 드래그 핸들

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 기본 행 드래그를 안내하는 핸들 아이콘을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement CreateReorderHandle()
        {
            var handle = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                tooltip = "이 페이지에서 드래그하여 이동",
            };
            handle.AddToClassList("xeri-collection__drag-handle");

            var icon = new Image
            {
                vectorImage = XeriIcon.Load("grip-vertical"),
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
            };
            icon.AddToClassList("xeri-collection__drag-icon");
            handle.Add(icon);
            return handle;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 재사용된 행의 이동 가능 상태와 원본 드래그 표시를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public void UpdateReorderHandle(VisualElement handle, int pageIndex, int sourceIndex)
        {
            handle.SetEnabled(CanDragReorder(pageIndex, sourceIndex));
            handle.EnableInClassList("xeri-collection__drag-handle--active", sourceIndex == dragSourceIndex);
        }

    #endregion

    #region 드래그 처리

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 순서가 직렬화 원본 순서인 행에서만 이동을 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool CanStartDrag(CanStartDragArgs args)
        {
            CancelDrag();

            var pageIndex = args.id;
            if (pageIndex < 0 || pageIndex >= searchPagedRows.PageItems.Count)
            {
                return false;
            }

            var sourceIndex = searchPagedRows.PageItems[pageIndex];
            if (!CanDragReorder(pageIndex, sourceIndex))
            {
                return false;
            }

            dragSourceIndex = sourceIndex;
            originalRevision = model.Revision;
            selectPageRow(pageIndex);
            return true;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 페이지의 임시 데이터가 아닌 컨트롤러의 원본 행을 드래그 데이터로 보관한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private StartDragArgs SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            var drag = new StartDragArgs("컬렉션 행 이동", DragVisualMode.Move);
            drag.SetGenericData(dragSourceKey, this);

            listView.Query<VisualElement>(className: "xeri-collection__drag-handle").ForEach
            (
                handle => handle.EnableInClassList
                (
                    "xeri-collection__drag-handle--active",
                    handle.parent?.userData is int index && index == dragSourceIndex
                )
            );
            return drag;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Unity ListView의 삽입 위치를 확인하고 허용 가능한 드롭 모드를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private DragVisualMode UpdateDrag(HandleDragAndDropArgs args)
        {
            return CanDrop(args)
                ? DragVisualMode.Move
                : DragVisualMode.Rejected;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 페이지 삽입 위치를 원본 최종 인덱스로 변환해 한 번만 이동한다.
        /// </summary>
        // ------------------------------------------------------------
        private DragVisualMode HandleDrop(HandleDragAndDropArgs args)
        {
            if (!CanDrop(args) || model.Revision != originalRevision)
            {
                return DragVisualMode.Rejected;
            }

            var sourceIndex = dragSourceIndex;
            var insertionIndex = searchPagedRows.PageItems[0] + args.insertAtIndex;
            var destinationIndex = insertionIndex > sourceIndex
                ? insertionIndex - 1
                : insertionIndex;

            CancelDrag();
            if (destinationIndex != sourceIndex)
            {
                onMoveRow(sourceIndex, destinationIndex);
            }

            return DragVisualMode.Move;
        }

        private void HandleDragExited(DragExitedEvent _)
        {
            CancelDrag();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 화면 갱신·드롭 완료·취소 시 임시 드래그 표시를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CancelDrag()
        {
            if (dragSourceIndex < 0)
            {
                return;
            }

            dragSourceIndex = -1;
            originalRevision = 0;
            listView.Query<VisualElement>(className: "xeri-collection__drag-handle").ForEach
            (
                handle => handle.RemoveFromClassList("xeri-collection__drag-handle--active")
            );
        }

    #endregion

    #region 드래그 가능 범위

        // ------------------------------------------------------------
        /// <summary>
        /// 검색·정렬되지 않은 현재 페이지의 직렬화 행만 이동 가능하다.
        /// </summary>
        // ------------------------------------------------------------
        private bool CanDragReorder(int pageIndex, int sourceIndex)
        {
            if (!model.CanReorder
                || sourceRows.Count != model.Count
                || !string.IsNullOrWhiteSpace(searchPagedRows.SearchText)
                || pageIndex < 0
                || pageIndex >= searchPagedRows.PageItems.Count
                || searchPagedRows.PageItems[pageIndex] != sourceIndex)
            {
                return false;
            }

            for (var index = 0; index < sourceRows.Count; index++)
            {
                if (sourceRows[index] != index)
                {
                    return false;
                }
            }

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 드래그와 현재 페이지의 유효한 삽입 위치만 허용한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool CanDrop(HandleDragAndDropArgs args)
        {
            return ReferenceEquals(args.dragAndDropData.GetGenericData(dragSourceKey), this)
                && dragSourceIndex >= 0
                && model.CanReorder
                && sourceRows.Count == model.Count
                && string.IsNullOrWhiteSpace(searchPagedRows.SearchText)
                && listView.Q<ScrollView>()?.contentViewport.worldBound.Contains(args.position) == true
                && args.insertAtIndex >= 0
                && args.insertAtIndex <= searchPagedRows.PageItems.Count
                && searchPagedRows.IndexOfPage(dragSourceIndex) >= 0;
        }

    #endregion

    }
}
