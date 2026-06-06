/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_DragDropCoordinator.cs
수정일 : 2026-05-22

# 설명
DragDropCoordinator 활성 드래그 추적과 드롭 라우팅 테스트.

# 테스트 구성
 A: 활성 드래그 추적
 R: 드롭 라우팅
 C: 취소 처리
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;

using UnityEngine;

using NUnit;
using NUnit.Framework;

using inonego.Xeri.UI.DragDrop;

namespace inonego.Xeri.TEST.UI._Drag_Drop
{
    // ============================================================
    /// <summary>
    /// DragDropCoordinator 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_DragDropCoordinator
    {

    #region 헬퍼

        // ============================================================
        /// <summary>
        /// 테스트용 좌표 Provider.
        /// </summary>
        // ============================================================
        private sealed class CoordinateProvider : IDragCoordinateProvider
        {
            public Vector2 Pos { get; set; } = Vector2.zero;

            // ------------------------------------------------------------
            /// <summary>
            /// 입력 좌표를 그대로 로컬 좌표로 사용한다.
            /// </summary>
            // ------------------------------------------------------------
            public Vector2 ToLocalPos(Vector2 inputPos)
            {
                return inputPos;
            }
        }

        // ============================================================
        /// <summary>
        /// 입력 위치에 따라 반환 DropZone을 바꾸는 Resolver.
        /// </summary>
        // ============================================================
        private sealed class Resolver : IDropResolver
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 반환할 DropZone.
            /// </summary>
            // ------------------------------------------------------------
            public DropZone DropZone
            {
                get => dropZone;
                set => dropZone = value;
            }

            private DropZone dropZone = null;

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 설정된 DropZone을 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public DropZone Resolve(InputPoint input, Draggable draggable)
            {
                return dropZone;
            }
        }

    #endregion

    #region 픽스처

        private Resolver resolver = null;
        private Draggable draggable = null;
        private DropZone dropZone = null;
        private DragDropCoordinator coordinator = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 각 테스트 전 조율 대상 객체들을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        [SetUp]
        public void SetUp()
        {
            var provider = new CoordinateProvider();

            resolver    = new Resolver();
            draggable   = new Draggable(this, provider);
            dropZone    = new DropZone(this);
            coordinator = new DragDropCoordinator
            {
                DropResolver = resolver,
            };

            draggable.PrepareDrag(new InputPoint(1, Vector2.zero));
            draggable.InvokeDragBegin(new InputPoint(1, Vector2.zero));
        }

    #endregion

    #region A-1: Begin

        // ------------------------------------------------------------
        /// <summary>
        /// HandleDragBegin은 활성 드래그 목록에 대상을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DragDropCoordinator_HandleDragBegin_ActiveCollection_추가()
        {
            coordinator.HandleDragBegin(draggable);

            CollectionAssert.Contains(coordinator.ActiveDraggables, draggable);
        }

    #endregion

    #region R-1: Enter

        // ------------------------------------------------------------
        /// <summary>
        /// HandleDrag는 Resolver가 반환한 DropZone에 진입시킨다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DragDropCoordinator_HandleDrag_DropZone_Enter_처리()
        {
            resolver.DropZone = dropZone;

            var fired = false;
            dropZone.OnDropEnter += (_, _) => fired = true;

            coordinator.HandleDragBegin(draggable);
            coordinator.HandleDrag(draggable, new InputPoint(1, Vector2.zero));

            Assert.IsTrue(fired);
            Assert.AreSame(draggable, dropZone.Draggable);
        }

    #endregion

    #region R-2: Drop

        // ------------------------------------------------------------
        /// <summary>
        /// HandleDragEnd는 현재 DropZone에 DropDone을 발화하고 활성 목록에서 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DragDropCoordinator_HandleDragEnd_DropDone_처리()
        {
            resolver.DropZone = dropZone;
            coordinator.HandleDragBegin(draggable);
            coordinator.HandleDrag(draggable, new InputPoint(1, Vector2.zero));

            var fired = false;
            dropZone.OnDropDone += (_, _) => fired = true;

            coordinator.HandleDragEnd(draggable);

            Assert.IsTrue(fired);
            CollectionAssert.DoesNotContain(coordinator.ActiveDraggables, draggable);
        }

    #endregion

    #region C-1: Cancel

        // ------------------------------------------------------------
        /// <summary>
        /// HandleDragCancel은 DropDone 없이 DropExit만 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DragDropCoordinator_HandleDragCancel_DropExit만_처리()
        {
            resolver.DropZone = dropZone;
            coordinator.HandleDragBegin(draggable);
            coordinator.HandleDrag(draggable, new InputPoint(1, Vector2.zero));

            var doneFired = false;
            var exitFired = false;
            dropZone.OnDropDone += (_, _) => doneFired = true;
            dropZone.OnDropExit += (_, _) => exitFired = true;

            coordinator.HandleDragCancel(draggable);

            Assert.IsFalse(doneFired);
            Assert.IsTrue(exitFired);
            CollectionAssert.DoesNotContain(coordinator.ActiveDraggables, draggable);
        }

    #endregion

    }
}
