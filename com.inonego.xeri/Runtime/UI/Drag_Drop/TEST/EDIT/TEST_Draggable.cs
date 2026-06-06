/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Draggable.cs
수정일 : 2026-05-22

# 설명
Core Draggable 드래그 생명주기 테스트.

# 테스트 구성
 L: 드래그 lifecycle (Prepare/Begin/Drag/End/ForceEnd)
 M: 이동 가능 여부 (CanMove)
 I: 입력 ID 매칭
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
    /// Core Draggable 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_Draggable
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

    #endregion

    #region 픽스처

        private CoordinateProvider provider = null;
        private Draggable draggable = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 각 테스트 전 새로운 Draggable을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        [SetUp]
        public void SetUp()
        {
            provider = new CoordinateProvider
            {
                Pos = new Vector2(10f, 20f),
            };
            draggable = new Draggable(this, provider);
        }

    #endregion

    #region L-1: Begin

        // ------------------------------------------------------------
        /// <summary>
        /// PrepareDrag 후 InvokeDragBegin은 원점과 오프셋을 보존한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Draggable_PrepareDrag_InvokeDragBegin_Origin_Offset_사용()
        {
            var input = new InputPoint(1, new Vector2(3f, 4f));

            draggable.PrepareDrag(input);
            draggable.InvokeDragBegin(input);

            Assert.IsTrue(draggable.IsDragging);
            Assert.AreEqual(new Vector2(10f, 20f), draggable.OriginPos);
            Assert.AreEqual(new Vector2(7f, 16f), draggable.Offset);
        }

    #endregion

    #region L-2: Drag

        // ------------------------------------------------------------
        /// <summary>
        /// InvokeDrag는 입력 위치와 오프셋으로 GoalPos를 계산해 Pos에 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Draggable_InvokeDrag_GoalPos_계산_및_Pos_적용()
        {
            draggable.PrepareDrag(new InputPoint(1, new Vector2(3f, 4f)));
            draggable.InvokeDragBegin(new InputPoint(1, new Vector2(3f, 4f)));

            DragEventArgs eventArgs = default;
            draggable.OnDrag += (_, e) => eventArgs = e;

            draggable.InvokeDrag(new InputPoint(1, new Vector2(30f, 40f)));

            Assert.AreEqual(new Vector2(37f, 56f), provider.Pos);
            Assert.AreEqual(provider.Pos, eventArgs.Pos);
            Assert.AreEqual(provider.Pos, eventArgs.GoalPos);
        }

    #endregion

    #region M-1: CanMove

        // ------------------------------------------------------------
        /// <summary>
        /// CanMove가 false이면 GoalPos는 계산하지만 실제 Pos는 이동하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Draggable_CanMove_false_GoalPos_계산하되_Pos_미적용()
        {
            draggable.CanMove = false;
            draggable.PrepareDrag(new InputPoint(1, new Vector2(3f, 4f)));
            draggable.InvokeDragBegin(new InputPoint(1, new Vector2(3f, 4f)));

            DragEventArgs eventArgs = default;
            draggable.OnDrag += (_, e) => eventArgs = e;

            draggable.InvokeDrag(new InputPoint(1, new Vector2(30f, 40f)));

            Assert.AreEqual(new Vector2(10f, 20f), provider.Pos);
            Assert.AreEqual(new Vector2(37f, 56f), eventArgs.GoalPos);
        }

    #endregion

    #region I-1: 입력 ID

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 ID의 입력은 진행 중인 드래그에 영향을 주지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Draggable_InvokeDrag_다른_ID_무시()
        {
            draggable.PrepareDrag(new InputPoint(1, new Vector2(3f, 4f)));
            draggable.InvokeDragBegin(new InputPoint(1, new Vector2(3f, 4f)));

            draggable.InvokeDrag(new InputPoint(2, new Vector2(30f, 40f)));

            Assert.AreEqual(new Vector2(10f, 20f), provider.Pos);
        }

    #endregion

    #region L-3: End

        // ------------------------------------------------------------
        /// <summary>
        /// InvokeDragEnd는 상태를 정리하고 종료 이벤트를 발화한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Draggable_InvokeDragEnd_상태_정리_및_OnDragEnd_발화()
        {
            draggable.PrepareDrag(new InputPoint(1, new Vector2(3f, 4f)));
            draggable.InvokeDragBegin(new InputPoint(1, new Vector2(3f, 4f)));

            var fired = false;
            draggable.OnDragEnd += (_, _) => fired = true;

            draggable.InvokeDragEnd(new InputPoint(1, new Vector2(30f, 40f)));

            Assert.IsTrue(fired);
            Assert.IsFalse(draggable.IsDragging);
            Assert.IsNull(draggable.OriginPos);
            Assert.IsNull(draggable.Offset);
        }

    #endregion

    }
}
