/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_MValue.cs
수정일 : 2026-09-19

# 설명
MValue<T>와 built-in Modifier의 값 계산, 변경 전파와 구독 lifecycle을 검증한다.

# 테스트 구성
 E: 기본 기능 (생성/Base 변경/암시적 변환/이벤트)
 M: Modifier 등록·순서·자동 변경 전파·구독 lifecycle
 S: 직렬화 후 runtime state 재구성
 D: Modifier 연산
 X: 예외 처리 (키 없음/null 인자)
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
    /// MValue 시스템의 핵심 기능 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_MValue
    {

    #region 테스트 헬퍼

        // ============================================================
        /// <summary>
        /// Modified 내부 hook 호출 내용을 기록하는 테스트 전용 MValue.
        /// </summary>
        // ============================================================
        private sealed class TestHookMValue : MValue<int>
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 내부 hook이 호출된 횟수.
            /// </summary>
            // ------------------------------------------------------------
            public int HookCount { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 마지막 hook이 받은 이전 Modified 값.
            /// </summary>
            // ------------------------------------------------------------
            public int Previous { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 마지막 hook이 받은 현재 Modified 값.
            /// </summary>
            // ------------------------------------------------------------
            public int Current { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 초기 Base 값을 가진 테스트 MValue를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public TestHookMValue(int value) : base(value)
            {
                // NONE
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Modified 내부 hook 호출 인자를 기록한다.
            /// </summary>
            // ------------------------------------------------------------
            protected override void OnModifiedUpdated
            (
                in int prev,
                in int next
            )
            {
                HookCount++;
                Previous = prev;
                Current = next;
            }
        }

    #endregion

    #region E-1: 기본 생성

        [Test]
        public void TEST_MValue_기본_생성_초기값()
        {
            // Arrange & Act
            var value = new MValue<int>();

            // Assert
            Assert.AreEqual(0, value.Base);
            Assert.AreEqual(0, value.Modified);
            Assert.AreEqual(0, value.Modifiers.Count);
        }

        [Test]
        public void TEST_MValue_초기값_생성자()
        {
            // Arrange & Act
            var value = new MValue<int>(42);

            // Assert
            Assert.AreEqual(42, value.Base);
            Assert.AreEqual(42, value.Modified);
        }

    #endregion

    #region E-2: Base 변경 시 Modified 갱신 및 이벤트

        [Test]
        public void TEST_MValue_Base_변경시_Modified_갱신_및_이벤트()
        {
            // ------------------------------------------------------------
            // 테스트 준비
            // ------------------------------------------------------------
            var value = new MValue<int>();

            bool baseChangeFired = false;
            ValueChangeEventArgs<int> baseChangeArgs = default;

            bool modifiedChangeFired = false;
            ValueChangeEventArgs<int> modifiedChangeArgs = default;

            void Reset()
            {
                baseChangeFired = false;
                baseChangeArgs  = default;
                modifiedChangeFired = false;
                modifiedChangeArgs  = default;
            }

            value.OnBaseChange += (sender, e) =>
            {
                baseChangeFired = true;
                baseChangeArgs  = e;
            };

            value.OnModifiedChange += (sender, e) =>
            {
                modifiedChangeFired = true;
                modifiedChangeArgs  = e;
            };

            // ------------------------------------------------------------
            // 수정자 없이 Base 변경 - 두 이벤트 모두 발생, 값 동일
            // ------------------------------------------------------------
            value.Base = 10;

            Assert.AreEqual(10, value.Base);
            Assert.AreEqual(10, value.Modified);
            Assert.IsTrue(baseChangeFired);
            Assert.IsTrue(modifiedChangeFired);
            Assert.AreEqual(0,  baseChangeArgs.Previous);
            Assert.AreEqual(10, baseChangeArgs.Current);
            Assert.AreEqual(0,  modifiedChangeArgs.Previous);
            Assert.AreEqual(10, modifiedChangeArgs.Current);

            Reset();

            // ------------------------------------------------------------
            // 동일 값 - 이벤트 미발생
            // ------------------------------------------------------------
            value.Base = 10;
            Assert.IsFalse(baseChangeFired);
            Assert.IsFalse(modifiedChangeFired);

            Reset();

            // ------------------------------------------------------------
            // invokeEvent: false - 이벤트 미발생, 값은 변경
            // ------------------------------------------------------------
            value.Set(20, invokeEvent: false);
            Assert.AreEqual(20, value.Base);
            Assert.AreEqual(20, value.Modified);
            Assert.IsFalse(baseChangeFired);
            Assert.IsFalse(modifiedChangeFired);
        }

    #endregion

    #region E-3: silent 변경 hook

        [Test]
        public void TEST_MValue_invokeEventFalse_내부Hook호출_외부이벤트억제()
        {
            var value = new TestHookMValue(10);
            var eventCount = 0;
            value.OnModifiedChange += (_, _) => eventCount++;

            value.Set(20, invokeEvent: false);

            Assert.AreEqual(1, value.HookCount);
            Assert.AreEqual(10, value.Previous);
            Assert.AreEqual(20, value.Current);
            Assert.AreEqual(0, eventCount);

            value.AddModifier
            (
                "add",
                new NumericIModifier(NumericIOperation.ADD, 5),
                invokeEvent: false
            );

            Assert.AreEqual(2, value.HookCount);
            Assert.AreEqual(20, value.Previous);
            Assert.AreEqual(25, value.Current);
            Assert.AreEqual(0, eventCount);
        }

    #endregion

    #region E-4: 암시적 변환

        [Test]
        public void TEST_MValue_암시적_변환_Modified_반환()
        {
            // Arrange
            var value = new MValue<int>(10);
            value.AddModifier("a", new NumericIModifier(NumericIOperation.ADD, 5));

            // Act
            int direct = value;

            // Assert
            Assert.AreEqual(15, direct);
        }

    #endregion

    #region E-5: OnModifiedChange 강제 발화

        [Test]
        public void TEST_MValue_InvokeOnModifiedChange_강제_발화()
        {
            var value = new MValue<int>(5);
            ValueChangeEventArgs<int> fired = default;
            value.OnModifiedChange += (_, e) => fired = e;

            value.InvokeOnModifiedChange(previousValue: 8);

            Assert.AreEqual(8, fired.Previous);
            Assert.AreEqual(5, fired.Current);
        }

    #endregion

    #region M-1: AddModifier / RemoveModifier

        [Test]
        public void TEST_MValue_AddRemoveModifier_명시키()
        {
            // ------------------------------------------------------------
            // 테스트 준비
            // ------------------------------------------------------------
            var value = new MValue<int>(10);
            var add5  = new NumericIModifier(NumericIOperation.ADD, 5);

            // ------------------------------------------------------------
            // Add - Modified 갱신
            // ------------------------------------------------------------
            value.AddModifier("a", add5);

            Assert.AreEqual(15, value.Modified);
            Assert.AreEqual(1, value.Modifiers.Count);

            var entry0 = value.Modifiers[0];
            Assert.AreEqual("a", entry0.Key);
            Assert.AreSame(add5, entry0.Value);
            Assert.AreEqual(0, entry0.Order);

            // ------------------------------------------------------------
            // Remove 성공 - true, Modified 원복
            // ------------------------------------------------------------
            bool removed = value.RemoveModifier("a");

            Assert.IsTrue(removed);
            Assert.AreEqual(10, value.Modified);
            Assert.AreEqual(0,  value.Modifiers.Count);

            // ------------------------------------------------------------
            // Remove 실패(없는 키) - false, 변화 없음
            // ------------------------------------------------------------
            bool removedAgain = value.RemoveModifier("a");

            Assert.IsFalse(removedAgain);
            Assert.AreEqual(10, value.Modified);
        }

    #endregion

    #region M-2: ClearModifiers

        [Test]
        public void TEST_MValue_ClearModifiers_전체_제거()
        {
            // Arrange
            var value = new MValue<int>(10);

            value.AddModifier("a", new NumericIModifier(NumericIOperation.ADD, 5));
            value.AddModifier("b", new NumericIModifier(NumericIOperation.MUL, 2));

            Assert.AreEqual(30, value.Modified); // (10+5)*2

            // Act
            value.ClearModifiers();

            // Assert
            Assert.AreEqual(10, value.Modified);
            Assert.AreEqual(0,  value.Modifiers.Count);
        }

    #endregion

    #region M-3: Order 적용 순서

        [Test]
        public void TEST_MValue_Order_오름차순_적용()
        {
            // Arrange
            var value = new MValue<int>(10);

            // 추가 순서와 Order 가 다르도록 의도적으로 섞어서 추가
            value.AddModifier("b", new NumericIModifier(NumericIOperation.MUL, 2), order: 1);
            value.AddModifier("a", new NumericIModifier(NumericIOperation.ADD, 5), order: 0);

            // ------------------------------------------------------------
            // 기대값: (10 + 5) * 2 = 30
            // ------------------------------------------------------------
            Assert.AreEqual(30, value.Modified);

            // ------------------------------------------------------------
            // Modifiers 노출 순서도 Order 오름차순
            // ------------------------------------------------------------
            Assert.AreEqual(2, value.Modifiers.Count);

            var entry0 = value.Modifiers[0];
            var entry1 = value.Modifiers[1];
            Assert.AreEqual("a", entry0.Key);
            Assert.AreEqual(0, entry0.Order);
            Assert.AreEqual("b", entry1.Key);
            Assert.AreEqual(1, entry1.Order);
        }

    #endregion

    #region M-4: Modifier 상태 변경 자동 갱신

        [Test]
        public void TEST_MValue_Modifier_상태변경_자동갱신_및_이벤트()
        {
            var value = new MValue<int>(10);
            var modifier = new NumericIModifier(NumericIOperation.ADD, 5);

            value.AddModifier("a", modifier);

            var firedCount = 0;
            ValueChangeEventArgs<int> fired = default;
            value.OnModifiedChange += (_, e) =>
            {
                firedCount++;
                fired = e;
            };

            modifier.Value = 7;

            Assert.AreEqual(17, value.Modified);
            Assert.AreEqual(1, firedCount);
            Assert.AreEqual(15, fired.Previous);
            Assert.AreEqual(17, fired.Current);

            modifier.Operation = NumericIOperation.MUL;

            Assert.AreEqual(70, value.Modified);
            Assert.AreEqual(2, firedCount);
            Assert.AreEqual(17, fired.Previous);
            Assert.AreEqual(70, fired.Current);
        }

        [Test]
        public void TEST_MValue_Modifier_최종값동일_이벤트미발생()
        {
            var value = new MValue<bool>(true);
            var modifier = new BooleanModifier(BooleanOperation.OR, false);

            value.AddModifier("a", modifier);

            var firedCount = 0;
            value.OnModifiedChange += (_, _) => firedCount++;

            modifier.Value = true;

            Assert.AreEqual(true, value.Modified);
            Assert.AreEqual(0, firedCount);
        }

        [Test]
        public void TEST_MValue_RemoveClearModifier_구독해제()
        {
            var value = new MValue<int>(10);
            var modifier = new NumericIModifier(NumericIOperation.ADD, 5);

            value.AddModifier("a", modifier);
            value.RemoveModifier("a");

            modifier.Value = 7;

            Assert.AreEqual(10, value.Modified);

            value.AddModifier("b", modifier);
            Assert.AreEqual(17, value.Modified);

            value.ClearModifiers();

            modifier.Value = 9;

            Assert.AreEqual(10, value.Modified);
        }

        [Test]
        public void TEST_MValue_같은Modifier_다중등록_다중MValue()
        {
            var modifier = new NumericIModifier(NumericIOperation.ADD, 5);
            var first = new MValue<int>(10);
            var second = new MValue<int>(20);

            first.AddModifier("a", modifier);
            first.AddModifier("b", modifier);
            second.AddModifier("c", modifier);

            var firstEventCount = 0;
            var secondEventCount = 0;
            first.OnModifiedChange += (_, _) => firstEventCount++;
            second.OnModifiedChange += (_, _) => secondEventCount++;

            modifier.Value = 2;

            Assert.AreEqual(14, first.Modified);
            Assert.AreEqual(22, second.Modified);
            Assert.AreEqual(1, firstEventCount);
            Assert.AreEqual(1, secondEventCount);

            Assert.IsTrue(first.RemoveModifier("a", invokeEvent: false));

            modifier.Value = 3;

            Assert.AreEqual(13, first.Modified);
            Assert.AreEqual(23, second.Modified);
            Assert.AreEqual(2, firstEventCount);
            Assert.AreEqual(2, secondEventCount);

            Assert.IsTrue(first.RemoveModifier("b", invokeEvent: false));

            modifier.Value = 4;

            Assert.AreEqual(10, first.Modified);
            Assert.AreEqual(24, second.Modified);
            Assert.AreEqual(2, firstEventCount);
            Assert.AreEqual(3, secondEventCount);
        }

        [Test]
        public void TEST_MValue_Modifier_OnChange_실제상태변경만_발화()
        {
            var numericF = new NumericFModifier(NumericFOperation.ADD, 1f);
            var numericI = new NumericIModifier(NumericIOperation.ADD, 1);
            var boolean = new BooleanModifier(BooleanOperation.OR, false);
            var text = new StringModifier(StringOperation.SET, "a");

            var numericFCount = 0;
            var numericICount = 0;
            var booleanCount = 0;
            var textCount = 0;

            numericF.OnChange += () => numericFCount++;
            numericI.OnChange += () => numericICount++;
            boolean.OnChange += () => booleanCount++;
            text.OnChange += () => textCount++;

            numericF.Value = 2f;
            numericF.Value = 2f;
            numericF.Operation = NumericFOperation.MUL;

            numericI.Value = 2;
            numericI.Value = 2;
            numericI.Operation = NumericIOperation.MUL;

            boolean.Value = true;
            boolean.Value = true;
            boolean.Operation = BooleanOperation.XOR;

            text.Value = "b";
            text.Value = "b";
            text.Operation = StringOperation.SET;

            Assert.AreEqual(2, numericFCount);
            Assert.AreEqual(2, numericICount);
            Assert.AreEqual(2, booleanCount);
            Assert.AreEqual(1, textCount);
        }

    #endregion

    #region S-1: 직렬화 후 runtime state 재구성

        [Test]
        public void TEST_MValue_JSON_직렬화_캐시와구독_재구성()
        {
            // Arrange
            var original = new MValue<int>(10);

            original.AddModifier
            (
                "a",
                new NumericIModifier(NumericIOperation.ADD, 5)
            );

            // Act
            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<MValue<int>>(json);
            var modifier = restored.Modifiers[0].Value as NumericIModifier;

            // Assert
            Assert.AreEqual(15, restored.Modified);
            Assert.IsNotNull(modifier);

            modifier.Value = 7;

            Assert.AreEqual(17, restored.Modified);
        }

    #endregion

    #region D-1: BooleanModifier

        [Test]
        public void TEST_MValue_BooleanModifier_모든_Operation()
        {
            // SET
            Assert.AreEqual(true,  new BooleanModifier(BooleanOperation.SET, true ).Modify(false));
            Assert.AreEqual(false, new BooleanModifier(BooleanOperation.SET, false).Modify(true));

            // AND
            Assert.AreEqual(true,  new BooleanModifier(BooleanOperation.AND, true ).Modify(true));
            Assert.AreEqual(false, new BooleanModifier(BooleanOperation.AND, true ).Modify(false));
            Assert.AreEqual(false, new BooleanModifier(BooleanOperation.AND, false).Modify(true));

            // OR
            Assert.AreEqual(true,  new BooleanModifier(BooleanOperation.OR, true ).Modify(false));
            Assert.AreEqual(true,  new BooleanModifier(BooleanOperation.OR, false).Modify(true));
            Assert.AreEqual(false, new BooleanModifier(BooleanOperation.OR, false).Modify(false));

            // XOR
            Assert.AreEqual(true,  new BooleanModifier(BooleanOperation.XOR, true ).Modify(false));
            Assert.AreEqual(false, new BooleanModifier(BooleanOperation.XOR, true ).Modify(true));
            Assert.AreEqual(false, new BooleanModifier(BooleanOperation.XOR, false).Modify(false));
        }

        [Test]
        public void TEST_MValue_BooleanModifier_NOT_정적_인스턴스()
        {
            Assert.AreEqual(true,  BooleanModifier.NOT.Modify(false));
            Assert.AreEqual(false, BooleanModifier.NOT.Modify(true));
        }

    #endregion

    #region D-2: NumericModifier

        [Test]
        public void TEST_MValue_NumericFModifier_모든_Operation()
        {
            Assert.AreEqual(5f, new NumericFModifier(NumericFOperation.SET, 5f ).Modify(10f));
            Assert.AreEqual(15f, new NumericFModifier(NumericFOperation.ADD, 5f ).Modify(10f));
            Assert.AreEqual(5f, new NumericFModifier(NumericFOperation.SUB, 5f ).Modify(10f));
            Assert.AreEqual(50f, new NumericFModifier(NumericFOperation.MUL, 5f ).Modify(10f));
            Assert.AreEqual(2f, new NumericFModifier(NumericFOperation.DIV, 5f ).Modify(10f));
        }

        [Test]
        public void TEST_MValue_NumericIModifier_모든_Operation()
        {
            Assert.AreEqual(5,  new NumericIModifier(NumericIOperation.SET, 5).Modify(10));
            Assert.AreEqual(15, new NumericIModifier(NumericIOperation.ADD, 5).Modify(10));
            Assert.AreEqual(5,  new NumericIModifier(NumericIOperation.SUB, 5).Modify(10));
            Assert.AreEqual(50, new NumericIModifier(NumericIOperation.MUL, 5).Modify(10));
            Assert.AreEqual(2,  new NumericIModifier(NumericIOperation.DIV, 5).Modify(10));
        }

    #endregion

    #region D-3: StringModifier

        [Test]
        public void TEST_MValue_StringModifier_SET()
        {
            Assert.AreEqual("new", new StringModifier(StringOperation.SET, "new").Modify("old"));
        }

    #endregion

    #region D-4: LambdaModifier

        [Test]
        public void TEST_MValue_LambdaModifier_적용()
        {
            // ------------------------------------------------------------
            // int → int 람다
            // ------------------------------------------------------------
            Func<int, int> doubleLambda = x => x * 2;
            var doubleI = new LambdaModifier<int>(doubleLambda);

            Assert.AreSame(doubleLambda, doubleI.Lambda);
            Assert.IsFalse
            (
                typeof(LambdaModifier<int>)
                    .GetProperty(nameof(LambdaModifier<int>.Lambda))
                    .CanWrite
            );
            Assert.AreEqual(20, doubleI.Modify(10));

            // ------------------------------------------------------------
            // string → string 람다
            // ------------------------------------------------------------
            var upper = new LambdaModifier<string>(s => s.ToUpper());

            Assert.AreEqual("ABC", upper.Modify("abc"));
        }

        [Test]
        public void TEST_MValue_LambdaModifier_Null_람다()
        {
            var modifier = new LambdaModifier<int>();

            Assert.AreEqual(10, modifier.Modify(10));
        }

        [Test]
        public void TEST_MValue_LambdaModifier_MValue_연동()
        {
            var value = new MValue<int>(10);

            value.AddModifier("a", new LambdaModifier<int>(x => x * 3));

            Assert.AreEqual(30, value.Modified);
        }

    #endregion

    #region X-1: AddModifier 키 없음 예외

        [Test]
        public void TEST_MValue_AddModifier_키없음_ArgumentException()
        {
            // Arrange
            var value    = new MValue<int>(10);
            var modifier = new NumericIModifier(NumericIOperation.ADD, 5);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => value.AddModifier(modifier));
        }

    #endregion

    #region X-2: AddModifier null 예외

        [Test]
        public void TEST_MValue_AddModifier_Null_ArgumentNullException()
        {
            var value = new MValue<int>(10);

            Assert.Throws<ArgumentNullException>(() => value.AddModifier("a", null));
        }

    #endregion

    }

}
