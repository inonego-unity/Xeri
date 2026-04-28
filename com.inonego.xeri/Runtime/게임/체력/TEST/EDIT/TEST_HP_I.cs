/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_HP_I.cs
수정일 : 2026-04-28

# 설명
HP_I (int 기반 체력) 유닛 테스트.
Unity Test Runner (Edit Mode) 에서 실행한다.
========================================================================= BLOCK_HEADER_END */

using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.Game;

// ============================================================
/// <summary>
/// HP_I (int 기반 체력) 핵심 기능 테스트.
/// </summary>
// ============================================================
public class TEST_HP_I
{

#region HP 기본 기능 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// HP_I 의 기본 생성 및 초기값을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void HP_01_기본_생성_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var hp = new HP_I();

        // ------------------------------------------------------------
        // 테스트 결과
        // ------------------------------------------------------------
        Assert.AreEqual(0, hp.Value);
        Assert.AreEqual(0, hp.MaxValue);

        Assert.AreEqual(0.0f, hp.Ratio);

        Assert.AreEqual(HPState.Dead, hp.Current);
        Assert.IsFalse(hp.IsAlive);
        Assert.IsTrue(hp.IsDead);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// HP_I 의 상태 변경 및 자동 상태 전환을 통합 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void HP_02_상태_변경_통합_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var hp = new HP_I();
        hp.MaxValue = 100;

        // ------------------------------------------------------------
        // MakeAlive - 살아있는 상태로 변경
        // ------------------------------------------------------------
        hp.MakeAlive();

        Assert.AreEqual(HPState.Alive, hp.Current);
        Assert.AreEqual(100, hp.Value);
        Assert.AreEqual(100, hp.MaxValue);
        Assert.IsTrue(hp.IsAlive);
        Assert.IsFalse(hp.IsDead);

        // ------------------------------------------------------------
        // MakeDead - 죽은 상태로 변경
        // ------------------------------------------------------------
        hp.MakeDead();

        Assert.AreEqual(HPState.Dead, hp.Current);
        Assert.AreEqual(0, hp.Value);
        Assert.IsFalse(hp.IsAlive);
        Assert.IsTrue(hp.IsDead);

        // ------------------------------------------------------------
        // Value 설정으로 자동 상태 전환 - Alive
        // ------------------------------------------------------------
        hp.Value = 50;

        Assert.AreEqual(HPState.Alive, hp.Current);
        Assert.AreEqual(50, hp.Value);
        Assert.IsTrue(hp.IsAlive);

        // ------------------------------------------------------------
        // Value 설정으로 자동 상태 전환 - Dead
        // ------------------------------------------------------------
        hp.Value = 0;

        Assert.AreEqual(HPState.Dead, hp.Current);
        Assert.AreEqual(0, hp.Value);
        Assert.IsTrue(hp.IsDead);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// HP_I 의 힐과 데미지 적용을 통합 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void HP_03_힐_데미지_통합_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var hp = new HP_I();
        hp.MaxValue = 100;
        hp.MakeAlive();
        hp.Value = 50;

        // ------------------------------------------------------------
        // ApplyHeal - 힐 적용
        // ------------------------------------------------------------
        hp.ApplyHeal(30);

        Assert.AreEqual(80, hp.Value);
        Assert.AreEqual(0.8f, hp.Ratio);

        // ------------------------------------------------------------
        // ApplyHeal - 최대 체력 초과 힐 제한
        // ------------------------------------------------------------
        hp.ApplyHeal(50);

        Assert.AreEqual(100, hp.Value);
        Assert.AreEqual(1.0f, hp.Ratio);

        // ------------------------------------------------------------
        // ApplyDamage - 데미지 적용
        // ------------------------------------------------------------
        hp.ApplyDamage(40);

        Assert.AreEqual(60, hp.Value);
        Assert.AreEqual(0.6f, hp.Ratio);

        // ------------------------------------------------------------
        // ApplyDamage - 치명적 데미지로 상태 전환
        // ------------------------------------------------------------
        hp.ApplyDamage(100);

        Assert.AreEqual(0, hp.Value);
        Assert.AreEqual(HPState.Dead, hp.Current);
        Assert.IsTrue(hp.IsDead);

        // ------------------------------------------------------------
        // ApplyHeal - 죽은 상태에서 힐 무시
        // ------------------------------------------------------------
        hp.ApplyHeal(50);

        Assert.AreEqual(0, hp.Value);
        Assert.IsTrue(hp.IsDead);

        // ------------------------------------------------------------
        // ApplyDamage - 죽은 상태에서 데미지 무시
        // ------------------------------------------------------------
        hp.ApplyDamage(30);

        Assert.AreEqual(0, hp.Value);
        Assert.IsTrue(hp.IsDead);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// HP_I 의 최대 체력 변경 및 자동 조정을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void HP_04_최대체력_변경_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var hp = new HP_I();
        hp.MaxValue = 100;
        hp.MakeAlive();
        hp.Value = 80;

        // ------------------------------------------------------------
        // MaxValue 증가 - 현재 체력 유지
        // ------------------------------------------------------------
        hp.MaxValue = 150;

        Assert.AreEqual(150, hp.MaxValue);
        Assert.AreEqual(80, hp.Value);
        Assert.AreEqual(80.0f / 150.0f, hp.Ratio);

        // ------------------------------------------------------------
        // MaxValue 감소 - 현재 체력 클램핑
        // ------------------------------------------------------------
        hp.MaxValue = 50;

        Assert.AreEqual(50, hp.MaxValue);
        Assert.AreEqual(50, hp.Value);
        Assert.AreEqual(1.0f, hp.Ratio);

        // ------------------------------------------------------------
        // MaxValue 0 설정 - 죽은 상태 전환
        // ------------------------------------------------------------
        hp.MaxValue = 0;

        Assert.AreEqual(0, hp.MaxValue);
        Assert.AreEqual(0, hp.Value);
        Assert.AreEqual(0.0f, hp.Ratio);
        Assert.IsTrue(hp.IsDead);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// HP_I 의 비율 계산 기능을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void HP_05_비율_계산_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var hp = new HP_I();
        hp.MaxValue = 100;
        hp.MakeAlive();

        // ------------------------------------------------------------
        // Ratio 계산 - 다양한 체력 값
        // ------------------------------------------------------------
        hp.Value = 0;
        Assert.AreEqual(0.0f, hp.Ratio);

        hp.Value = 25;
        Assert.AreEqual(0.25f, hp.Ratio);

        hp.Value = 50;
        Assert.AreEqual(0.5f, hp.Ratio);

        hp.Value = 75;
        Assert.AreEqual(0.75f, hp.Ratio);

        hp.Value = 100;
        Assert.AreEqual(1.0f, hp.Ratio);

        // ------------------------------------------------------------
        // Ratio 계산 - MaxValue 0 일 때
        // ------------------------------------------------------------
        hp.MaxValue = 0;
        Assert.AreEqual(0.0f, hp.Ratio);
    }

#endregion

#region HP 이벤트 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// HP_I 의 이벤트 발생을 통합 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void HP_06_이벤트_통합_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var hp = new HP_I();

        bool valueChangeFired    = false;
        bool maxValueChangeFired = false;
        bool stateChangeFired    = false;
        bool healFired           = false;
        bool damageFired         = false;

        HP_I valueChangeSender    = null;
        HP_I maxValueChangeSender = null;
        HP_I stateChangeSender    = null;
        HP_I healSender           = null;
        HP_I damageSender         = null;

        ValueChangeEventArgs<int>     valueChangeArgs = default;
        ValueChangeEventArgs<int>     maxValueArgs    = default;
        ValueChangeEventArgs<HPState> stateChangeArgs = default;
        HPApplyEventArgs<int>         healArgs        = default;
        HPApplyEventArgs<int>         damageArgs      = default;

        void Reset()
        {
            valueChangeFired = maxValueChangeFired = stateChangeFired = healFired = damageFired = false;

            valueChangeSender = maxValueChangeSender = stateChangeSender = healSender = damageSender = null;

            valueChangeArgs = maxValueArgs = default;
            stateChangeArgs = default;
            healArgs = damageArgs = default;
        }

        hp.OnValueChange    += (sender, e) => { valueChangeFired    = true; valueChangeSender    = sender as HP_I; valueChangeArgs = e; };
        hp.OnMaxValueChange += (sender, e) => { maxValueChangeFired = true; maxValueChangeSender = sender as HP_I; maxValueArgs    = e; };
        hp.OnStateChange    += (sender, e) => { stateChangeFired    = true; stateChangeSender    = sender as HP_I; stateChangeArgs = e; };
        hp.OnHeal           += (sender, e) => { healFired           = true; healSender           = sender as HP_I; healArgs        = e; };
        hp.OnDamage         += (sender, e) => { damageFired         = true; damageSender         = sender as HP_I; damageArgs      = e; };

        // ------------------------------------------------------------
        // OnMaxValueChange 이벤트 확인
        // ------------------------------------------------------------
        hp.MaxValue = 100;

        Assert.IsTrue(maxValueChangeFired);
        Assert.AreEqual(hp,  maxValueChangeSender);
        Assert.AreEqual(0,   maxValueArgs.Previous);
        Assert.AreEqual(100, maxValueArgs.Current);

        Reset();

        // ------------------------------------------------------------
        // OnStateChange 이벤트 확인
        // ------------------------------------------------------------
        hp.MakeAlive();

        Assert.IsTrue(stateChangeFired);
        Assert.AreEqual(hp,            stateChangeSender);
        Assert.AreEqual(HPState.Dead,  stateChangeArgs.Previous);
        Assert.AreEqual(HPState.Alive, stateChangeArgs.Current);

        Reset();

        // ------------------------------------------------------------
        // OnValueChange 이벤트 확인
        // ------------------------------------------------------------
        hp.Value = 50;

        Assert.IsTrue(valueChangeFired);
        Assert.AreEqual(hp,  valueChangeSender);
        Assert.AreEqual(100, valueChangeArgs.Previous);
        Assert.AreEqual(50,  valueChangeArgs.Current);

        Reset();

        // ------------------------------------------------------------
        // OnHeal 이벤트 확인
        // ------------------------------------------------------------
        hp.ApplyHeal(30);

        Assert.IsTrue(healFired);
        Assert.AreEqual(hp, healSender);
        Assert.AreEqual(30, healArgs.Amount);

        Reset();

        // ------------------------------------------------------------
        // OnDamage 이벤트 확인
        // ------------------------------------------------------------
        hp.ApplyDamage(20);

        Assert.IsTrue(damageFired);
        Assert.AreEqual(hp, damageSender);
        Assert.AreEqual(20, damageArgs.Amount);
    }

#endregion

#region HP 유틸리티 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// HP_I 의 깊은 복사 기능을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void HP_07_깊은_복사_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var original = new HP_I();
        original.MaxValue = 100;
        original.MakeAlive();
        original.Value = 75;

        // ------------------------------------------------------------
        // CloneFrom - 상태 복사 확인
        // ------------------------------------------------------------
        var clone = original.@new();
        clone.CloneFrom(original);

        Assert.AreEqual(original.Current,  clone.Current);
        Assert.AreEqual(original.Value,    clone.Value);
        Assert.AreEqual(original.MaxValue, clone.MaxValue);
        Assert.AreEqual(original.Ratio,    clone.Ratio);
        Assert.AreNotSame(original, clone);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// HP_I 의 비율 기반 적용량 계산을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void HP_08_비율_기반_계산_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var hp = new HP_I();
        hp.MaxValue = 100;
        hp.MakeAlive();
        hp.Value = 60;

        // ------------------------------------------------------------
        // CalculateApplyAmount - ByValue (현재 체력 기준)
        // ------------------------------------------------------------
        int amount1 = hp.CalculateApplyAmount(0.5f, HPApplyRatioType.ByValue);

        Assert.AreEqual(30, amount1);

        // ------------------------------------------------------------
        // CalculateApplyAmount - ByMaxValue (최대 체력 기준)
        // ------------------------------------------------------------
        int amount2 = hp.CalculateApplyAmount(0.3f, HPApplyRatioType.ByMaxValue);

        Assert.AreEqual(30, amount2);

        // ------------------------------------------------------------
        // CalculateApplyAmount - ByMissingValue (부족한 체력 기준)
        // ------------------------------------------------------------
        int amount3 = hp.CalculateApplyAmount(0.5f, HPApplyRatioType.ByMissingValue);

        Assert.AreEqual(20, amount3);
    }

#endregion

}
