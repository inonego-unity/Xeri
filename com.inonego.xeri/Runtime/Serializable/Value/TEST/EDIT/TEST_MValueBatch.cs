/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_MValueBatch.cs
수정일 : 2026-11-22

# 설명
MValue.BeginBatch 묶음 범위의 재계산·이벤트 합치기를 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.

# 테스트 구성
 재계산·통지: 한 번 계산, 되돌린 변경, 무통지 변경, Base·Modifier 변경
 범위 관리: 중첩, 예외 뒤 닫힘, 이중 닫기, 순서 위반 복구, 닫힌·낡은 범위 재닫기
 재진입: 종료 이벤트 중 새 범위를 열어도 통지 유지
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.TEST.Serializable._Value
{
    // ============================================================
    /// <summary>
    /// MValue 묶음 갱신 테스트.
    /// </summary>
    // ============================================================
    public class TEST_MValueBatch
    {

    #region 묶음 갱신

        // ------------------------------------------------------------
        /// <summary>
        /// 범위 안의 여러 변경은 범위가 닫힐 때 한 번만 계산·통지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_MValueBatch_여러변경_한번계산_한번통지()
        {
            var folds = 0;
            var value = new MValue<int>(10);
            var add = new NumericIModifier(NumericIOperation.ADD, 0);
            var mul = new NumericIModifier(NumericIOperation.MUL, 1);

            value.AddModifier("add", add, order: 0);
            value.AddModifier("mul", mul, order: 1);
            value.AddModifier("count", new LambdaModifier<int>(x => { folds++; return x; }), order: 2);

            var modifiedEvents = 0;

            value.OnModifiedChange += (_, _) => modifiedEvents++;
            folds = 0;

            using (value.BeginBatch())
            {
                add.Value = 2;
                mul.Value = 3;
                value.Set(20);

                // 범위 안에서는 범위를 연 시점의 값이 유지된다.
                Assert.AreEqual(10, value.Modified);
            }

            Assert.AreEqual(66, value.Modified);
            Assert.AreEqual(1, folds);
            Assert.AreEqual(1, modifiedEvents);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 중첩 범위는 가장 바깥 범위가 닫힐 때만 계산한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_MValueBatch_중첩_바깥범위에서만_계산()
        {
            var value = new MValue<int>(1);
            var add = new NumericIModifier(NumericIOperation.ADD, 0);

            value.AddModifier("add", add);

            using (value.BeginBatch())
            {
                using (value.BeginBatch())
                {
                    add.Value = 5;
                }

                Assert.AreEqual(1, value.Modified);
            }

            Assert.AreEqual(6, value.Modified);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 범위 안에서 바꿨다가 되돌리면 이벤트가 나지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_MValueBatch_되돌린변경은_이벤트없음()
        {
            var value = new MValue<int>(1);
            var add = new NumericIModifier(NumericIOperation.ADD, 1);
            var events = 0;

            value.AddModifier("add", add);
            value.OnModifiedChange += (_, _) => events++;
            value.OnBaseChange += (_, _) => events++;

            using (value.BeginBatch())
            {
                add.Value = 9;
                value.Set(5);
                add.Value = 1;
                value.Set(1);
            }

            Assert.AreEqual(2, value.Modified);
            Assert.AreEqual(0, events);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 범위 안의 변경이 모두 invokeEvent: false면 값만 갱신하고 통지하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_MValueBatch_무통지변경만있으면_값만갱신()
        {
            var value = new MValue<int>(1);
            var events = 0;

            value.OnModifiedChange += (_, _) => events++;
            value.OnBaseChange += (_, _) => events++;

            using (value.BeginBatch())
            {
                value.Set(4, invokeEvent: false);
                value.AddModifier("add", new NumericIModifier(NumericIOperation.ADD, 1), invokeEvent: false);
            }

            Assert.AreEqual(5, value.Modified);
            Assert.AreEqual(0, events);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Base 변경은 범위가 닫힐 때 OnBaseChange 한 번으로 통지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_MValueBatch_Base변경_한번통지()
        {
            var value = new MValue<int>(1);
            var baseEvents = 0;
            var last = 0;

            value.OnBaseChange += (_, args) =>
            {
                baseEvents++;
                last = args.Current;
            };

            using (value.BeginBatch())
            {
                value.Set(2);
                value.Set(3);
            }

            Assert.AreEqual(1, baseEvents);
            Assert.AreEqual(3, last);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 범위 안에서 예외가 나도 범위가 닫혀 이후 갱신이 정상 동작한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_MValueBatch_예외후에도_범위닫힘()
        {
            var value = new MValue<int>(1);
            var add = new NumericIModifier(NumericIOperation.ADD, 0);

            value.AddModifier("add", add);

            Assert.Throws<InvalidOperationException>
            (
                () =>
                {
                    using (value.BeginBatch())
                    {
                        add.Value = 2;
                        throw new InvalidOperationException("테스트 예외");
                    }
                }
            );

            Assert.AreEqual(3, value.Modified);

            // 범위 밖 변경은 즉시 반영된다.
            add.Value = 10;

            Assert.AreEqual(11, value.Modified);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 범위 안의 Modifier 추가·제거도 범위가 닫힐 때 반영된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_MValueBatch_Modifier추가제거_반영()
        {
            var value = new MValue<int>(10);

            value.AddModifier("old", new NumericIModifier(NumericIOperation.ADD, 5));

            using (value.BeginBatch())
            {
                value.RemoveModifier("old");
                value.AddModifier("new", new NumericIModifier(NumericIOperation.MUL, 2));
            }

            Assert.AreEqual(20, value.Modified);
            Assert.AreEqual(1, value.Modifiers.Count);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 무통지 Base 변경은 다른 변경이 이벤트를 요청해도 Base 이벤트를 내지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_MValueBatch_무통지Base변경은_Base이벤트없음()
        {
            var value = new MValue<int>(1);
            var add = new NumericIModifier(NumericIOperation.ADD, 0);
            var baseEvents = 0;
            var modifiedEvents = 0;

            value.AddModifier("add", add);
            value.OnBaseChange += (_, _) => baseEvents++;
            value.OnModifiedChange += (_, _) => modifiedEvents++;

            using (value.BeginBatch())
            {
                value.Set(5, invokeEvent: false);
                add.Value = 3;
            }

            Assert.AreEqual(8, value.Modified);
            Assert.AreEqual(0, baseEvents);
            Assert.AreEqual(1, modifiedEvents);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 범위를 두 번 닫거나 순서를 어기면 예외를 던진다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_MValueBatch_범위_이중닫기와_순서위반_예외()
        {
            var value = new MValue<int>(0);
            var outer = value.BeginBatch();
            var inner = value.BeginBatch();

            inner.Dispose();

            // ref struct는 람다가 캡처할 수 없어 Assert.Throws 대신 직접 받는다.
            var doubleDisposeThrew = false;

            try
            {
                inner.Dispose();
            }
            catch (InvalidOperationException)
            {
                doubleDisposeThrew = true;
            }

            Assert.IsTrue(doubleDisposeThrew);

            outer.Dispose();

            var add = new NumericIModifier(NumericIOperation.ADD, 0);

            value.AddModifier("add", add);

            var first = value.BeginBatch();
            var second = value.BeginBatch();
            var outOfOrderThrew = false;

            add.Value = 3;

            try
            {
                first.Dispose();
            }
            catch (InvalidOperationException)
            {
                outOfOrderThrew = true;
            }

            Assert.IsTrue(outOfOrderThrew);

            // 순서를 어기면 안쪽 범위까지 닫혀 미룬 변경이 반영되고 이후 변경도 즉시 반영된다.
            Assert.AreEqual(3, value.Modified);

            add.Value = 4;

            Assert.AreEqual(4, value.Modified);

            // 함께 닫힌 안쪽 범위를 다시 닫으면 상태를 바꾸지 않고 예외만 던진다.
            var closedInnerThrew = false;

            try
            {
                second.Dispose();
            }
            catch (InvalidOperationException)
            {
                closedInnerThrew = true;
            }

            Assert.IsTrue(closedInnerThrew);

            add.Value = 5;

            Assert.AreEqual(5, value.Modified);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 범위가 닫히며 낸 Base 이벤트에서 새 범위를 열어도 Modified 통지가 유지된다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_MValueBatch_종료이벤트중_재진입_통지유지()
        {
            var value = new MValue<int>(10);

            value.AddModifier("mul", new NumericIModifier(NumericIOperation.MUL, 2));

            var modifiedEvents = 0;
            var previous = 0;
            var current = 0;

            value.OnBaseChange += (_, _) =>
            {
                using (value.BeginBatch())
                {
                    // NONE
                }
            };

            value.OnModifiedChange += (_, args) =>
            {
                modifiedEvents++;
                previous = args.Previous;
                current = args.Current;
            };

            using (value.BeginBatch())
            {
                value.Set(15);
            }

            Assert.AreEqual(1, modifiedEvents);
            Assert.AreEqual(20, previous);
            Assert.AreEqual(30, current);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 이미 닫힌 범위를 다시 닫으면 같은 깊이에 새로 연 범위가 있어도
        /// <br/> 그 범위를 닫지 않고 예외만 던진다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_MValueBatch_닫힌범위재닫기_새범위유지()
        {
            var value = new MValue<int>(0);
            var add = new NumericIModifier(NumericIOperation.ADD, 0);

            value.AddModifier("add", add);

            var stale = value.BeginBatch();

            stale.Dispose();

            var fresh = value.BeginBatch();

            add.Value = 3;

            Assert.IsTrue(ThrowsOnDispose(stale));

            // 새 범위는 열린 채로 남아 재계산이 계속 미뤄진다.
            Assert.AreEqual(0, value.Modified);

            fresh.Dispose();

            Assert.AreEqual(3, value.Modified);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 순서 위반으로 함께 닫힌 안쪽 범위를 나중에 닫아도
        /// <br/> 새로 연 중첩 범위에는 영향이 없다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_MValueBatch_복구후_낡은안쪽범위_새중첩유지()
        {
            var value = new MValue<int>(0);
            var add = new NumericIModifier(NumericIOperation.ADD, 0);

            value.AddModifier("add", add);

            var outer = value.BeginBatch();
            var staleInner = value.BeginBatch();

            Assert.IsTrue(ThrowsOnDispose(outer));

            var newOuter = value.BeginBatch();
            var newInner = value.BeginBatch();

            add.Value = 7;

            Assert.IsTrue(ThrowsOnDispose(staleInner));
            Assert.AreEqual(0, value.Modified);

            newInner.Dispose();
            newOuter.Dispose();

            Assert.AreEqual(7, value.Modified);
        }

    #endregion

    #region 헬퍼

        // ref struct는 람다가 캡처할 수 없어 Dispose 예외를 직접 받는다.
        private static bool ThrowsOnDispose(MValue<int>.BatchScope scope)
        {
            try
            {
                scope.Dispose();
            }
            catch (InvalidOperationException)
            {
                return true;
            }

            return false;
        }

    #endregion

    }
}
