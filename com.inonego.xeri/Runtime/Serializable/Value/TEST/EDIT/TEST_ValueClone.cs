/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_ValueClone.cs
수정일 : 2026-11-22

# 설명
Value·RangeValue·MValue의 Clone·CloneFrom 상태 복제를 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.

# 테스트 구성
 V: Value 복제와 거절 조건
 R: RangeValue 복원 순서
 M: MValue Modifier 복제·재사용·구독 복구·복제 실패 시 상태
 D: 파생 Modifier의 @new 재정의 요구
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Primitive;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.TEST.Serializable._Value
{
    // ============================================================
    /// <summary>
    /// Value 계열 상태 복제 테스트.
    /// </summary>
    // ============================================================
    public class TEST_ValueClone
    {

    #region 헬퍼

        // ============================================================
        /// <summary>
        /// @new를 재정의하지 않은 파생 Modifier. 복제 실패를 재현한다.
        /// </summary>
        // ============================================================
        private sealed class MissingNewModifier : NumericFModifier
        {
            // ------------------------------------------------------------
            /// <summary>
            /// MUL 연산과 피연산자로 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public MissingNewModifier(float value) : base(NumericFOperation.MUL, value)
            {
                // NONE
            }
        }

        // ============================================================
        /// <summary>
        /// @new를 재정의한 파생 Modifier.
        /// </summary>
        // ============================================================
        private sealed class OwnNewModifier : NumericFModifier
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 복제용 빈 인스턴스를 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public OwnNewModifier()
            {
                // NONE
            }

            // ------------------------------------------------------------
            /// <summary>
            /// MUL 연산과 피연산자로 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public OwnNewModifier(float value) : base(NumericFOperation.MUL, value)
            {
                // NONE
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 복제에 사용할 같은 타입의 빈 인스턴스를 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public override IModifier<float> @new() => new OwnNewModifier();
        }

        // ============================================================
        /// <summary>
        /// 같은 타입에서도 CloneFrom이 실패하는 사용자 정의 Modifier.
        /// </summary>
        // ============================================================
        private sealed class ThrowingCloneModifier : NumericFModifier
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 복제용 빈 인스턴스를 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public ThrowingCloneModifier()
            {
                // NONE
            }

            // ------------------------------------------------------------
            /// <summary>
            /// ADD 연산과 피연산자로 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public ThrowingCloneModifier(float value) : base(NumericFOperation.ADD, value)
            {
                // NONE
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 복제에 사용할 같은 타입의 빈 인스턴스를 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public override IModifier<float> @new() => new ThrowingCloneModifier();

            // ------------------------------------------------------------
            /// <summary>
            /// 항상 실패한다.
            /// </summary>
            // ------------------------------------------------------------
            public override void CloneFrom(IModifier<float> source)
            {
                throw new InvalidOperationException("테스트 복제 실패");
            }
        }

    #endregion

    #region V: Value

        // ------------------------------------------------------------
        /// <summary>
        /// Value 복제는 Base를 복사하고 외부 이벤트를 내지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueClone_Value_Base복사_이벤트없음()
        {
            var source = new Value<int>(7);
            var target = new Value<int>(1);
            var events = 0;

            target.OnBaseChange += (_, _) => events++;
            target.CloneFrom(source);

            Assert.AreEqual(7, target.Base);
            Assert.AreEqual(0, events);

            var clone = ((IDeepCloneable<Value<int>>)source).Clone();

            Assert.AreNotSame(source, clone);
            Assert.AreEqual(typeof(Value<int>), clone.GetType());
            Assert.AreEqual(7, clone.Base);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// null·다른 구체 타입은 거절하고, 자기 자신 복제는 아무 일도 하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueClone_Value_거절조건과_자기복제()
        {
            var value = new Value<int>(3);

            Assert.Throws<ArgumentNullException>(() => value.CloneFrom(null));
            Assert.Throws<ArgumentException>(() => value.CloneFrom(new MValue<int>(5)));
            Assert.Throws<ArgumentException>(() => new MValue<int>(5).CloneFrom(value));

            value.CloneFrom(value);

            Assert.AreEqual(3, value.Base);
        }

    #endregion

    #region R: RangeValue

        // ----------------------------------------------------------------------
        /// <summary>
        /// RangeValue는 Range를 먼저 복원해 Base가 이전 범위로 잘리지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueClone_RangeValue_Range먼저_복원()
        {
            var source = new RangeValue<int>(80, (0, 100));
            var target = new RangeValue<int>(5, (0, 10));

            target.CloneFrom(source);

            Assert.AreEqual(80, target.Base);
            Assert.AreEqual(new Range<int>(0, 100), target.Range.Base);

            // 복원 뒤 범위 제한이 새 범위로 동작한다.
            target.Set(150);

            Assert.AreEqual(100, target.Base);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// RangeValue 복제본은 원본과 Range 인스턴스를 공유하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueClone_RangeValue_복제본_독립()
        {
            var source = new RangeValue<int>(5, (0, 10));
            var clone = (RangeValue<int>)((IDeepCloneable<Value<int>>)source).Clone();

            Assert.AreNotSame(source.Range, clone.Range);

            clone.Range.Set((0, 3));

            Assert.AreEqual(new Range<int>(0, 10), source.Range.Base);
            Assert.AreEqual(5, source.Base);
            Assert.AreEqual(3, clone.Base);
        }

    #endregion

    #region M: MValue

        // ----------------------------------------------------------------------
        /// <summary>
        /// MValue 복제는 Base·Modifier·Order를 복사하고 외부 이벤트를 내지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueClone_MValue_Modifier복사_이벤트없음()
        {
            var source = new MValue<int>(10);

            source.AddModifier("mul", new NumericIModifier(NumericIOperation.MUL, 3), order: 1);
            source.AddModifier("add", new NumericIModifier(NumericIOperation.ADD, 2), order: 0);

            var target = new MValue<int>(0);
            var events = 0;

            target.OnBaseChange += (_, _) => events++;
            target.OnModifiedChange += (_, _) => events++;
            target.CloneFrom(source);

            Assert.AreEqual(10, target.Base);
            Assert.AreEqual(36, target.Modified);
            Assert.AreEqual(0, events);
            Assert.AreEqual("add", target.Modifiers[0].Key);
            Assert.AreEqual("mul", target.Modifiers[1].Key);
            Assert.AreEqual(1, target.Modifiers[1].Order);

            // 복제본의 Modifier는 원본과 공유하지 않는다.
            Assert.AreNotSame(source.Modifiers[0].Value, target.Modifiers[0].Value);

            ((NumericIModifier)source.Modifiers[0].Value).Value = 100;

            Assert.AreEqual(36, target.Modified);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 저장한 복제본으로 되돌리면 Modified와 Modifier 구독이 함께 복구된다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueClone_MValue_스냅샷복원_구독복구()
        {
            var live = new MValue<float>(10f);
            var slow = new NumericFModifier(NumericFOperation.MUL, 0.5f);

            live.AddModifier("slow", slow);

            var snapshot = new MValue<float>();

            snapshot.CloneFrom(live);

            // 스냅샷 이후 상태를 바꾼다.
            live.Set(20f);
            slow.Value = 0f;
            live.AddModifier("stun", new NumericFModifier(NumericFOperation.SET, 0f), order: 5);

            live.CloneFrom(snapshot);

            Assert.AreEqual(10f, live.Base);
            Assert.AreEqual(5f, live.Modified);
            Assert.AreEqual(1, live.Modifiers.Count);

            // 같은 키·같은 타입의 기존 인스턴스를 재사용한다.
            Assert.AreSame(slow, live.Modifiers[0].Value);
            Assert.AreEqual(0.5f, slow.Value);

            // 복원 뒤에도 Modifier 변경이 Modified로 전파된다.
            slow.Value = 2f;

            Assert.AreEqual(20f, live.Modified);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키 구성이 같으면 인스턴스와 순서를 그대로 두고 값만 되돌린다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueClone_MValue_같은구성복원_제자리복사()
        {
            var live = new MValue<int>(10);
            var add = new NumericIModifier(NumericIOperation.ADD, 1);
            var mul = new NumericIModifier(NumericIOperation.MUL, 2);

            live.AddModifier("add", add, order: 0);
            live.AddModifier("mul", mul, order: 0);

            var snapshot = new MValue<int>();

            snapshot.CloneFrom(live);

            var events = 0;

            live.OnBaseChange += (_, _) => events++;
            live.OnModifiedChange += (_, _) => events++;

            live.Set(3, invokeEvent: false);
            add.Value = 5;
            mul.Value = 3;

            live.CloneFrom(snapshot);

            Assert.AreSame(add, live.Modifiers[0].Value);
            Assert.AreSame(mul, live.Modifiers[1].Value);
            Assert.AreEqual("add", live.Modifiers[0].Key);
            Assert.AreEqual(1, add.Value);
            Assert.AreEqual(2, mul.Value);
            Assert.AreEqual(22, live.Modified);

            // 이벤트는 위의 무통지 Base 변경 뒤 Modifier 변경 2번만 발생한다.
            Assert.AreEqual(2, events);

            // 복원 뒤에도 구독이 유지된다.
            add.Value = 2;

            Assert.AreEqual(24, live.Modified);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 키라도 구체 타입이 다르면 재사용하지 않고 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueClone_MValue_타입다르면_새로복제()
        {
            var source = new MValue<int>(4);

            source.AddModifier("a", new NumericIModifier(NumericIOperation.ADD, 1));

            var target = new MValue<int>(4);
            var lambda = new LambdaModifier<int>(x => x * 10);

            target.AddModifier("a", lambda);
            target.CloneFrom(source);

            Assert.AreNotSame(lambda, target.Modifiers[0].Value);
            Assert.IsInstanceOf<NumericIModifier>(target.Modifiers[0].Value);
            Assert.AreEqual(5, target.Modified);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 여러 키가 공유하던 인스턴스는 한 키에만 재사용하고 나머지는 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueClone_MValue_공유인스턴스_한번만재사용()
        {
            var source = new MValue<int>(0);

            source.AddModifier("a", new NumericIModifier(NumericIOperation.ADD, 1), order: 0);
            source.AddModifier("b", new NumericIModifier(NumericIOperation.ADD, 10), order: 1);

            var shared = new NumericIModifier(NumericIOperation.ADD, 0);
            var target = new MValue<int>(0);

            target.AddModifier("a", shared, order: 0);
            target.AddModifier("b", shared, order: 1);
            target.CloneFrom(source);

            Assert.AreSame(shared, target.Modifiers[0].Value);
            Assert.AreNotSame(shared, target.Modifiers[1].Value);
            Assert.AreEqual(11, target.Modified);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// source와 공유하는 인스턴스는 재사용하지 않아 source 상태가 바뀌지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueClone_MValue_source공유인스턴스_재사용안함()
        {
            var shared = new NumericIModifier(NumericIOperation.ADD, 1);
            var source = new MValue<int>(0);

            source.AddModifier("k1", new NumericIModifier(NumericIOperation.ADD, 100), order: 0);
            source.AddModifier("k2", shared, order: 1);

            var target = new MValue<int>(0);

            target.AddModifier("k1", shared, order: 0);
            target.CloneFrom(source);

            Assert.AreEqual(1, shared.Value);
            Assert.AreEqual(101, source.Modified);
            Assert.AreEqual(101, target.Modified);
            Assert.AreNotSame(shared, target.Modifiers[0].Value);
            Assert.AreNotSame(shared, target.Modifiers[1].Value);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 파생 Modifier는 @new를 재정의해야 복제되고, 없으면 타입 불일치로 실패한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueClone_파생Modifier_new재정의필요()
        {
            IModifier<float> missing = new MissingNewModifier(0.5f);
            IModifier<float> own = new OwnNewModifier(0.5f);

            Assert.Throws<ArgumentException>(() => missing.Clone());

            var clone = own.Clone();

            Assert.IsInstanceOf<OwnNewModifier>(clone);
            Assert.AreEqual(5f, clone.Modify(10f));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// MValue 복제 중 Modifier 복제가 실패하면 대상 MValue는 바뀌지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueClone_MValue_복제실패시_대상불변()
        {
            var source = new MValue<float>(20f);

            source.AddModifier("a", new NumericFModifier(NumericFOperation.ADD, 1f), order: 0);
            source.AddModifier("b", new MissingNewModifier(0.5f), order: 1);

            var target = new MValue<float>(10f);
            var keep = new NumericFModifier(NumericFOperation.ADD, 5f);

            target.AddModifier("x", keep);

            Assert.Throws<ArgumentException>(() => target.CloneFrom(source));

            Assert.AreEqual(10f, target.Base);
            Assert.AreEqual(15f, target.Modified);
            Assert.AreEqual(1, target.Modifiers.Count);
            Assert.AreSame(keep, target.Modifiers[0].Value);

            // 구독도 그대로 유지된다.
            keep.Value = 7f;

            Assert.AreEqual(17f, target.Modified);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 구성이 같아도 source와 공유하는 인스턴스가 있으면 제자리 복사하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueClone_MValue_같은구성_source공유면_재구성()
        {
            var shared = new NumericIModifier(NumericIOperation.ADD, 1);
            var source = new MValue<int>(10);

            source.AddModifier("k", shared);

            var target = new MValue<int>(0);

            target.AddModifier("k", shared);
            shared.Value = 1;
            target.CloneFrom(source);

            Assert.AreNotSame(shared, target.Modifiers[0].Value);
            Assert.AreEqual(11, target.Modified);

            // 복제본은 source와 독립이다.
            shared.Value = 100;

            Assert.AreEqual(110, source.Modified);
            Assert.AreEqual(11, target.Modified);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Modifier의 CloneFrom이 실패해도 구독과 Modified 캐시가 현재 상태와 맞는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueClone_MValue_Modifier복사실패_구독캐시유지()
        {
            var source = new MValue<float>(10f);

            source.AddModifier("x", new NumericFModifier(NumericFOperation.ADD, 1f), order: 0);
            source.AddModifier("y", new ThrowingCloneModifier(2f), order: 1);

            var x = new NumericFModifier(NumericFOperation.ADD, 5f);
            var target = new MValue<float>(10f);

            target.AddModifier("x", x, order: 0);
            target.AddModifier("y", new ThrowingCloneModifier(3f), order: 1);

            Assert.Throws<InvalidOperationException>(() => target.CloneFrom(source));

            // 실패 전까지 복사된 상태로 Modified가 다시 계산된다.
            Assert.AreEqual(1f, x.Value);
            Assert.AreEqual(14f, target.Modified);

            // 구독이 복구되어 이후 변경이 반영된다.
            x.Value = 7f;

            Assert.AreEqual(20f, target.Modified);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 재구성 중 재사용 인스턴스의 CloneFrom이 실패해도
        /// <br/> Modifier 목록은 그대로 남는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueClone_MValue_재구성중복사실패_목록유지()
        {
            var source = new MValue<float>(20f);

            source.AddModifier("x", new ThrowingCloneModifier(1f), order: 0);
            source.AddModifier("z", new NumericFModifier(NumericFOperation.ADD, 100f), order: 1);

            var x = new ThrowingCloneModifier(3f);
            var target = new MValue<float>(10f);

            target.AddModifier("x", x, order: 0);

            Assert.Throws<InvalidOperationException>(() => target.CloneFrom(source));

            Assert.AreEqual(1, target.Modifiers.Count);
            Assert.AreSame(x, target.Modifiers[0].Value);
            Assert.AreEqual(23f, target.Modified);

            // 구독이 복구되어 이후 변경이 반영된다.
            x.Value = 5f;

            Assert.AreEqual(25f, target.Modified);
        }

    #endregion

    }
}
