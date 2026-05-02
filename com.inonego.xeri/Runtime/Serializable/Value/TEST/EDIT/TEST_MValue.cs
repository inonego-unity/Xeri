/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_MValue.cs
수정일 : 2026-05-02

# 설명
MValue<T> 와 4 개 구체 Modifier(BooleanModifier / NumericFModifier / NumericIModifier / StringModifier)의 핵심 기능 테스트.
========================================================================= BLOCK_HEADER_END */

using System;

using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.Serializable;

// ============================================================================
/// <summary>
/// MValue 시스템의 핵심 기능 테스트 클래스.
/// </summary>
// ============================================================================
public class TEST_MValue
{

#region MValue 기본 기능 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// MValue 의 기본 생성 및 초기값을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void MValue_01_기본_생성_테스트()
    {
        // Arrange & Act
        var value = new MValue<int>();

        // Assert
        Assert.AreEqual(0, value.Base);
        Assert.AreEqual(0, value.Modified);
        Assert.AreEqual(0, value.Modifiers.Count);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 초기값 지정 생성자 테스트.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void MValue_02_초기값_생성자_테스트()
    {
        // Arrange & Act
        var value = new MValue<int>(42);

        // Assert
        Assert.AreEqual(42, value.Base);
        Assert.AreEqual(42, value.Modified);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Base 변경 시 Modified 가 재계산되며 두 이벤트가 모두 발생함을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void MValue_03_Base_변경시_Modified_갱신_및_이벤트_테스트()
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

#region 수정자 추가 / 제거 / Clear 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// 키 명시 AddModifier 와 RemoveModifier 동작을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void MValue_04_AddRemoveModifier_명시키_테스트()
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
        Assert.AreEqual(1,  value.Modifiers.Count);
        Assert.AreSame(add5, value.Modifiers[0].Modifier);
        Assert.AreEqual(0,   value.Modifiers[0].Order);

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

    // ------------------------------------------------------------
    /// <summary>
    /// ClearModifiers 동작을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void MValue_05_ClearModifiers_테스트()
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

    // ------------------------------------------------------------
    /// <summary>
    /// IKeyable<string> 미구현 수정자를 키 없는 오버로드로 추가하면 ArgumentException 이 발생함을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void MValue_06_AddModifier_키없음_예외_테스트()
    {
        // Arrange
        var value    = new MValue<int>(10);
        var modifier = new NumericIModifier(NumericIOperation.ADD, 5);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => value.AddModifier(modifier));
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 키 명시 AddModifier 에 null 수정자를 넣으면 ArgumentNullException 이 발생함을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void MValue_07_AddModifier_Null_예외_테스트()
    {
        var value = new MValue<int>(10);

        Assert.Throws<ArgumentNullException>(() => value.AddModifier("k", null));
    }

#endregion

#region Order 적용 순서 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// 수정자가 Order 오름차순으로 순차 적용됨을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void MValue_08_Order_적용순서_테스트()
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
        Assert.AreEqual(0, value.Modifiers[0].Order);
        Assert.AreEqual(1, value.Modifiers[1].Order);
    }

#endregion

#region 복제 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// CloneFrom 이 Base / cached / modifiers 를 모두 깊은 복제함을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void MValue_09_CloneFrom_깊은복제_테스트()
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
        Assert.AreNotSame(source.Modifiers[0].Modifier, clone.Modifiers[0].Modifier);

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

    // ------------------------------------------------------------
    /// <summary>
    /// CloneFrom 에 null 인자를 주면 ArgumentNullException 이 발생함을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void MValue_10_CloneFrom_Null_예외_테스트()
    {
        var clone = new MValue<int>();

        Assert.Throws<ArgumentNullException>(() => clone.CloneFrom(null));
    }

#endregion

#region 암시적 변환

    // ------------------------------------------------------------
    /// <summary>
    /// MValue<T> 에서 T 로의 암시적 변환은 Modified 를 반환한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void MValue_11_암시적_변환_테스트()
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

#region BooleanModifier 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// BooleanModifier 의 모든 Operation 케이스를 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Modifier_01_BooleanModifier_모든_Operation_테스트()
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

    // ------------------------------------------------------------
    /// <summary>
    /// BooleanModifier.NOT 정적 인스턴스가 NOT 동작을 수행함을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Modifier_02_BooleanModifier_NOT_테스트()
    {
        Assert.AreEqual(true,  BooleanModifier.NOT.Modify(false));
        Assert.AreEqual(false, BooleanModifier.NOT.Modify(true));
    }

#endregion

#region NumericModifier 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// NumericFModifier 의 모든 Operation 케이스를 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Modifier_03_NumericFModifier_모든_Operation_테스트()
    {
        Assert.AreEqual(5f, new NumericFModifier(NumericFOperation.SET, 5f ).Modify(10f));
        Assert.AreEqual(15f, new NumericFModifier(NumericFOperation.ADD, 5f ).Modify(10f));
        Assert.AreEqual(5f, new NumericFModifier(NumericFOperation.SUB, 5f ).Modify(10f));
        Assert.AreEqual(50f, new NumericFModifier(NumericFOperation.MUL, 5f ).Modify(10f));
        Assert.AreEqual(2f, new NumericFModifier(NumericFOperation.DIV, 5f ).Modify(10f));
    }

    // ------------------------------------------------------------
    /// <summary>
    /// NumericIModifier 의 모든 Operation 케이스를 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Modifier_04_NumericIModifier_모든_Operation_테스트()
    {
        Assert.AreEqual(5,  new NumericIModifier(NumericIOperation.SET, 5).Modify(10));
        Assert.AreEqual(15, new NumericIModifier(NumericIOperation.ADD, 5).Modify(10));
        Assert.AreEqual(5,  new NumericIModifier(NumericIOperation.SUB, 5).Modify(10));
        Assert.AreEqual(50, new NumericIModifier(NumericIOperation.MUL, 5).Modify(10));
        Assert.AreEqual(2,  new NumericIModifier(NumericIOperation.DIV, 5).Modify(10));
    }

#endregion

#region StringModifier 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// StringModifier 의 SET Operation 을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Modifier_05_StringModifier_SET_테스트()
    {
        Assert.AreEqual("new", new StringModifier(StringOperation.SET, "new").Modify("old"));
    }

#endregion

#region LambdaModifier 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// LambdaModifier 가 주어진 람다를 적용하는지 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Modifier_06_LambdaModifier_적용_테스트()
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

    // ------------------------------------------------------------
    /// <summary>
    /// LambdaModifier 의 람다가 null 이면 입력값을 그대로 반환한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Modifier_07_LambdaModifier_Null_람다_테스트()
    {
        var modifier = new LambdaModifier<int>();

        Assert.AreEqual(10, modifier.Modify(10));
    }

    // ------------------------------------------------------------
    /// <summary>
    /// MValue 에 LambdaModifier 를 추가하여 정상 적용됨을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Modifier_08_LambdaModifier_MValue_연동_테스트()
    {
        var value = new MValue<int>(10);

        value.AddModifier("triple", new LambdaModifier<int>(x => x * 3));

        Assert.AreEqual(30, value.Modified);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// LambdaModifier 의 Clone 은 람다 참조를 그대로 복사한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Modifier_09_LambdaModifier_DeepClone_테스트()
    {
        var src   = new LambdaModifier<int>(x => x + 1);
        var clone = ((IDeepCloneable<IModifier<int>>)src).Clone() as LambdaModifier<int>;

        Assert.AreNotSame(src, clone);
        Assert.AreSame(src.Lambda, clone.Lambda);
        Assert.AreEqual(11, clone.Modify(10));
    }

#endregion

#region Modifier 깊은 복제 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// IDeepCloneable<IModifier<T>>.Clone() 이 동일 상태의 다른 인스턴스를 만든다는 것을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Modifier_10_DeepClone_테스트()
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

}
