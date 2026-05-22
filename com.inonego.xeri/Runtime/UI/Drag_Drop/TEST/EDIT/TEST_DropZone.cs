/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_DropZone.cs
수정일 : 2026-05-22

# 설명
Core DropZone 드롭 생명주기와 드롭 규칙 테스트.

# 테스트 구성
 L: 드롭 lifecycle (Enter/Exit/Drop)
 M: 매칭 조건 (CanDrop/Draggable.CanDrop/IDropRule)
========================================================================= BLOCK_HEADER_END */

using System.Collections;

using UnityEngine;

using NUnit.Framework;

using inonego.Xeri.UI.DragDrop;

namespace inonego.Xeri.TEST.UI._Drag_Drop
{
    // ============================================================
    /// <summary>
    /// Core DropZone 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_DropZone
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
        /// 고정 결과를 반환하는 드롭 규칙.
        /// </summary>
        // ============================================================
        private sealed class DropRule : IDropRule
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 반환할 드롭 허용 결과.
            /// </summary>
            // ------------------------------------------------------------
            public bool Result
            {
                get => result;
                set => result = value;
            }

            private bool result = true;

            // ------------------------------------------------------------
            /// <summary>
            /// 설정된 결과를 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public bool CanDrop(Draggable draggable, DropZone dropZone)
            {
                return result;
            }
        }

    #endregion

    #region 픽스처

        private Draggable draggable = null;
        private DropZone dropZone = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 각 테스트 전 드래그 중인 Draggable과 DropZone을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        [SetUp]
        public void SetUp()
        {
            var provider = new CoordinateProvider();

            draggable = new Draggable(this, provider);
            draggable.PrepareDrag(new InputPoint(1, Vector2.zero));
            draggable.InvokeDragBegin(new InputPoint(1, Vector2.zero));

            dropZone = new DropZone(this);
        }

    #endregion

    #region L-1: Enter

        // ------------------------------------------------------------
        /// <summary>
        /// 조건을 통과하면 OnDropEnter가 발화하고 Draggable이 설정된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DropZone_TryAccept_조건_통과_OnDropEnter_발화()
        {
            var fired = false;
            dropZone.OnDropEnter += (_, _) => fired = true;

            var accepted = dropZone.TryAccept(draggable);

            Assert.IsTrue(accepted);
            Assert.IsTrue(fired);
            Assert.AreSame(draggable, dropZone.Draggable);
        }

    #endregion

    #region L-2: Exit

        // ------------------------------------------------------------
        /// <summary>
        /// Exit은 OnDropExit을 발화하고 드롭 상태를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DropZone_Exit_OnDropExit_발화_및_상태_정리()
        {
            dropZone.TryAccept(draggable);

            var fired = false;
            dropZone.OnDropExit += (_, _) => fired = true;

            dropZone.Exit();

            Assert.IsTrue(fired);
            Assert.IsFalse(dropZone.IsDropping);
            Assert.IsNull(dropZone.Draggable);
        }

    #endregion

    #region L-3: Drop

        // ------------------------------------------------------------
        /// <summary>
        /// Drop은 OnDropDone 이후 OnDropExit 순서로 상태를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DropZone_Drop_OnDropDone_후_OnDropExit_발화()
        {
            dropZone.TryAccept(draggable);

            var eventOrder = "";
            dropZone.OnDropDone += (_, _) => eventOrder += "Done";
            dropZone.OnDropExit += (_, _) => eventOrder += "Exit";

            dropZone.Drop();

            Assert.AreEqual("DoneExit", eventOrder);
            Assert.IsFalse(dropZone.IsDropping);
        }

    #endregion

    #region M-1: Rule

        // ------------------------------------------------------------
        /// <summary>
        /// IDropRule이 false를 반환하면 진입을 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DropZone_IDropRule_false_진입_거부()
        {
            dropZone.AddDropRule(new DropRule { Result = false });

            var fired = false;
            dropZone.OnDropEnter += (_, _) => fired = true;

            var accepted = dropZone.TryAccept(draggable);

            Assert.IsFalse(accepted);
            Assert.IsFalse(fired);
            Assert.IsFalse(dropZone.IsDropping);
        }

    #endregion

    }
}
