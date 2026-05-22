/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_UITKDragDrop.cs
수정일 : 2026-05-22

# 설명
Runtime UI Toolkit DragDrop Manipulator 기본 연결 테스트.

# 테스트 구성
 L: UITK lifecycle
 R: UITK resolver 등록
========================================================================= BLOCK_HEADER_END */

using System.Collections;

using UnityEngine.UIElements;

using NUnit.Framework;

using inonego.Xeri.UI.DragDrop;

namespace inonego.Xeri.TEST.UI._Drag_Drop
{
    // ============================================================
    /// <summary>
    /// UI Toolkit DragDrop 연결 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_UITKDragDrop
    {

    #region L-1: Draggable Manipulator

        // ------------------------------------------------------------
        /// <summary>
        /// VisualElement에 UITKDraggableManipulator를 붙이면 Core Draggable이 생성된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UITKDragDrop_AddManipulator_Draggable_생성()
        {
            var element = new VisualElement();
            var manipulator = new UITKDraggableManipulator();

            element.AddManipulator(manipulator);

            Assert.IsNotNull(manipulator.Draggable);
            Assert.IsFalse(manipulator.IsDragging);
        }

    #endregion

    #region R-1: DropZone Manipulator

        // ------------------------------------------------------------
        /// <summary>
        /// DropZone Manipulator는 Coordinator와 Resolver에 DropZone을 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UITKDragDrop_DropZoneManipulator_Register()
        {
            var root = new VisualElement();
            var zone = new VisualElement();
            var coordinator = new DragDropCoordinator();
            var resolver = new UITKDropResolver(root);
            var manipulator = new UITKDropZoneManipulator(coordinator, resolver);

            root.Add(zone);
            zone.AddManipulator(manipulator);

            CollectionAssert.Contains(coordinator.DropZones, manipulator.DropZone);
        }

    #endregion

    }
}
