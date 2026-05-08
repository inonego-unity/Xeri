/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_MValue.cs
수정일 : 2026-05-08

# 설명
MValue<T> 와 4 개 구체 Modifier(BooleanModifier / NumericFModifier / NumericIModifier / StringModifier)의 핵심 기능 테스트.

# 테스트 구성
 E: 기본 기능 (생성/Base 변경/암시적 변환/이벤트)
 M: 수정자 (Add/Remove/Clear/Order/InvokeOnModifiedChange)
 C: 복제 (CloneFrom 깊은 복제)
 D: Modifier 카탈로그 (Boolean/NumericF/NumericI/String/Lambda 동작 + DeepClone)
 X: 예외 처리 (키 없음/null 인자)
========================================================================= BLOCK_HEADER_END */

using System;

using NUnit.Framework;

namespace inonego.Xeri.TEST.Serializable._Value
{

    using inonego.Xeri.Serializable;

    // ============================================================
    /// <summary>
    /// MValue 시스템의 핵심 기능 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_MValue
    {

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

    #region E-3: 암시적 변환

        [Test]
        public void TEST_MValue_암시적_변환_Modified_반환()
        {
            // Arrange
            var value = new MValue<int>(10);
            value.AddModifier("add5", new NumericIModifier(NumericIOperation.ADD, 5));

            // Act
            int direct = value;

            // Assert
            Assert.AreEqual(15, direct);
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
            value.AddModifier("add5", add5);

            Assert.AreEqual(15, value.Modified);
            Assert.AreEqual(1, value.Modifiers.Count);

            ModifierEntry<int> pair0 = value.Modifiers[0];
            Assert.AreSame(add5, pair0.Modifier);
            Assert.AreEqual(0,   pair0.Order);

            // ------------------------------------------------------------
            // Remove 성공 - true, Modified 원복
            // ------------------------------------------------------------
            bool removed = value.RemoveModifier("add5");

            Assert.IsTrue(removed);
            Assert.AreEqual(10, value.Modified);
            Assert.AreEqual(0,  value.Modifiers.Count);

            // ------------------------------------------------------------
            // Remove 실패(없는 키) - false, 변화 없음
            // ------------------------------------------------------------
            bool removedAgain = value.RemoveModifier("add5");

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
            value.AddModifier("mul", new NumericIModifier(NumericIOperation.MUL, 2), order: 1);
            value.AddModifier("add", new NumericIModifier(NumericIOperation.ADD, 5), order: 0);

            // ------------------------------------------------------------
            // 기대값: (10 + 5) * 2 = 30
            // ------------------------------------------------------------
            Assert.AreEqual(30, value.Modified);

            // ------------------------------------------------------------
            // Modifiers 노출 순서도 Order 오름차순
            // ------------------------------------------------------------
            Assert.AreEqual(2, value.Modifiers.Count);

            ModifierEntry<int> p0 = value.Modifiers[0];
            ModifierEntry<int> p1 = value.Modifiers[1];
            Assert.AreEqual(0, p0.Order);
            Assert.AreEqual(1, p1.Order);
        }

    #endregion

    #region M-4: InvokeOnModifiedChange 강제 발화

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

    #region C-1: CloneFrom 깊은 복제

        [Test]
        public void TEST_MValue_CloneFrom_깊은복제()
        {
            // Arrange
            var source = new MValue<int>(10);
            var add5   = new NumericIModifier(NumericIOperation.ADD, 5);

            source.AddModifier("add5", add5);

            var clone = new MValue<int>();
            clone.CloneFrom(source);

            // ------------------------------------------------------------
            // 값 일치
            // ------------------------------------------------------------
            Assert.AreEqual(source.Base,     clone.Base);
            Assert.AreEqual(source.Modified, clone.Modified);
            Assert.AreEqual(source.Modifiers.Count, clone.Modifiers.Count);

            // ------------------------------------------------------------
            // modifier 인스턴스는 다른 객체여야 함(깊은 복제)
            // ------------------------------------------------------------
            ModifierEntry<int> srcPair   = source.Modifiers[0];
            ModifierEntry<int> clonePair = clone.Modifiers[0];
            Assert.AreNotSame(srcPair.Modifier, clonePair.Modifier);

            // ------------------------------------------------------------------------
            // source modifier 값 변경이 clone 에 영향 없음(cross-reference identity 검증)
            // - source.add5.Value 를 100 으로 바꾸고 source 를 강제로 갱신해도 clone 은 그대로여야 한다.
            // - clone 측 add5 사본은 별도 인스턴스이므로 100 이 아닌 5 가 적용되어야 한다.
            // ------------------------------------------------------------------------
            add5.Value = 100;

            // source 측 갱신: 같은 키를 다시 set 해서 Refresh 트리거
            source.RemoveModifier("add5");
            source.AddModifier("add5", add5);

            Assert.AreEqual(110, source.Modified);
            Assert.AreEqual(15,  clone.Modified, "clone 의 add5 사본은 source 의 add5 변경에 영향받지 않아야 함");
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
            var doubleI = new LambdaModifier<int>(x => x * 2);

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

            value.AddModifier("triple", new LambdaModifier<int>(x => x * 3));

            Assert.AreEqual(30, value.Modified);
        }

        [Test]
        public void TEST_MValue_LambdaModifier_DeepClone_람다_참조_복사()
        {
            var src   = new LambdaModifier<int>(x => x + 1);
            var clone = ((IDeepCloneable<IModifier<int>>)src).Clone() as LambdaModifier<int>;

            Assert.AreNotSame(src, clone);
            Assert.AreSame(src.Lambda, clone.Lambda);
            Assert.AreEqual(11, clone.Modify(10));
        }

    #endregion

    #region D-5: Modifier 깊은 복제

        [Test]
        public void TEST_MValue_Modifier_DeepClone_모든_타입()
        {
            // ------------------------------------------------------------
            // BooleanModifier
            // ------------------------------------------------------------
            var b = new BooleanModifier(BooleanOperation.AND, true);
            var bClone = ((IDeepCloneable<IModifier<bool>>)b).Clone() as BooleanModifier;

            Assert.AreNotSame(b, bClone);
            Assert.AreEqual(BooleanOperation.AND, bClone.Operation);
            Assert.AreEqual(true,                 bClone.Value);

            // ------------------------------------------------------------
            // NumericFModifier
            // ------------------------------------------------------------
            var f = new NumericFModifier(NumericFOperation.MUL, 2.5f);
            var fClone = ((IDeepCloneable<IModifier<float>>)f).Clone() as NumericFModifier;

            Assert.AreNotSame(f, fClone);
            Assert.AreEqual(NumericFOperation.MUL, fClone.Operation);
            Assert.AreEqual(2.5f,                  fClone.Value);

            // ------------------------------------------------------------
            // NumericIModifier
            // ------------------------------------------------------------
            var i = new NumericIModifier(NumericIOperation.SUB, 7);
            var iClone = ((IDeepCloneable<IModifier<int>>)i).Clone() as NumericIModifier;

            Assert.AreNotSame(i, iClone);
            Assert.AreEqual(NumericIOperation.SUB, iClone.Operation);
            Assert.AreEqual(7,                     iClone.Value);

            // ------------------------------------------------------------
            // StringModifier
            // ------------------------------------------------------------
            var s = new StringModifier(StringOperation.SET, "hi");
            var sClone = ((IDeepCloneable<IModifier<string>>)s).Clone() as StringModifier;

            Assert.AreNotSame(s, sClone);
            Assert.AreEqual(StringOperation.SET, sClone.Operation);
            Assert.AreEqual("hi",                sClone.Value);
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

            Assert.Throws<ArgumentNullException>(() => value.AddModifier("k", null));
        }

    #endregion

    #region X-3: CloneFrom null 예외

        [Test]
        public void TEST_MValue_CloneFrom_Null_ArgumentNullException()
        {
            var clone = new MValue<int>();

            Assert.Throws<ArgumentNullException>(() => clone.CloneFrom(null));
        }

    #endregion

    }

}
