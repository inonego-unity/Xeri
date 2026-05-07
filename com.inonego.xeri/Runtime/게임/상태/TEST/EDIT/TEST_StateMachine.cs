/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_StateMachine.cs
수정일 : 2026-05-08

# 설명
StateMachine<TOwner> 핵심 동작 테스트.
TestOwner / StateA / StateB 콘크리트를 파일 내부에 정의해 추상 동작을 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.
========================================================================= BLOCK_HEADER_END */

using NUnit.Framework;

using inonego.Xeri.Game;

// ============================================================
/// <summary>
/// StateMachine 핵심 기능 테스트.
/// </summary>
// ============================================================
public class TEST_StateMachine
{

#region 테스트용 콘크리트

    // ------------------------------------------------------------
    /// <summary>
    /// Update / FixedUpdate / LateUpdate 카운터를 가진 소유자.
    /// </summary>
    // ------------------------------------------------------------
    private class TestOwner
    {
        public int Counter;
        public int FixedCounter;
        public int LateCounter;
    }

    // ------------------------------------------------------------
    /// <summary>
    /// OnEnter / OnExit / OnUpdate 만 구현한 단순 상태.
    /// </summary>
    // ------------------------------------------------------------
    private class StateA : StateBase<TestOwner>
    {
        public override void OnEnter()  => Owner.Counter += 1;
        public override void OnExit()   => Owner.Counter += 10;
        public override void OnUpdate() => Owner.Counter += 100;
    }

    // ------------------------------------------------------------
    /// <summary>
    /// IFixedUpdatable / ILateUpdatable 도 구현한 상태.
    /// </summary>
    // ------------------------------------------------------------
    private class StateB : StateBase<TestOwner>, IFixedUpdatable, ILateUpdatable
    {
        public void OnFixedUpdate() => Owner.FixedCounter += 1000;
        public void OnLateUpdate()  => Owner.LateCounter  += 10000;
    }

#endregion

#region 기본 기능

    // ------------------------------------------------------------
    /// <summary>
    /// 초기 생성 시 Owner 보유, Current=null 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void StateMachine_01_초기_생성_테스트()
    {
        var owner = new TestOwner();
        var fsm   = new StateMachine<TestOwner>(owner);

        Assert.AreSame(owner, fsm.Owner);
        Assert.IsNull(fsm.Current);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// AddState 등록 후 GetState 조회 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void StateMachine_02_AddState_GetState_테스트()
    {
        var owner = new TestOwner();
        var fsm   = new StateMachine<TestOwner>(owner);

        var added = fsm.AddState(new StateA());

        Assert.IsNotNull(added);
        Assert.AreSame(added, fsm.GetState<StateA>());
        Assert.IsNull(fsm.GetState<StateB>());
    }

#endregion

#region 전이

    // ------------------------------------------------------------
    /// <summary>
    /// 타입 전이 시 OnEnter 호출 + Current 갱신.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void StateMachine_03_MoveTo_타입_전이_테스트()
    {
        var owner = new TestOwner();
        var fsm   = new StateMachine<TestOwner>(owner);

        fsm.AddState(new StateA());
        fsm.MoveTo<StateA>();

        Assert.IsNotNull(fsm.Current);
        Assert.AreEqual(1, owner.Counter);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 인스턴스 전이 시 OnEnter 호출 + Current 갱신.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void StateMachine_04_MoveTo_인스턴스_전이_테스트()
    {
        var owner = new TestOwner();
        var fsm   = new StateMachine<TestOwner>(owner);

        var state = fsm.AddState(new StateA());
        fsm.MoveTo(state);

        Assert.AreSame(state, fsm.Current);
        Assert.AreEqual(1, owner.Counter);
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// A → B 전이 시 A.OnExit(+10) → B.OnEnter 호출 순서 확인.
    /// </summary>
    // ----------------------------------------------------------------------
    [Test]
    public void StateMachine_05_Exit_Enter_순서_테스트()
    {
        var owner = new TestOwner();
        var fsm   = new StateMachine<TestOwner>(owner);

        fsm.AddState(new StateA());
        fsm.AddState(new StateB());

        fsm.MoveTo<StateA>();
        Assert.AreEqual(1, owner.Counter);

        fsm.MoveTo<StateB>();
        Assert.AreEqual(11, owner.Counter);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 같은 인스턴스로 재진입 시 OnExit/OnEnter 미호출.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void StateMachine_06_같은_상태_재진입_무시_테스트()
    {
        var owner = new TestOwner();
        var fsm   = new StateMachine<TestOwner>(owner);

        var state = fsm.AddState(new StateA());

        fsm.MoveTo(state);
        Assert.AreEqual(1, owner.Counter);

        fsm.MoveTo(state);
        Assert.AreEqual(1, owner.Counter);
    }

#endregion

#region 갱신

    // ------------------------------------------------------------
    /// <summary>
    /// Update 호출 시 Current.OnUpdate 호출.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void StateMachine_07_Update_호출_테스트()
    {
        var owner = new TestOwner();
        var fsm   = new StateMachine<TestOwner>(owner);

        fsm.AddState(new StateA());
        fsm.MoveTo<StateA>();

        owner.Counter = 0;
        fsm.Update();

        Assert.AreEqual(100, owner.Counter);
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// FixedUpdate 는 Current 가 IFixedUpdatable 일 때만 호출된다.
    /// </summary>
    // ----------------------------------------------------------------------
    [Test]
    public void StateMachine_08_FixedUpdate_옵션_인터페이스_테스트()
    {
        var owner = new TestOwner();
        var fsm   = new StateMachine<TestOwner>(owner);

        fsm.AddState(new StateA());
        fsm.AddState(new StateB());

        // A 는 IFixedUpdatable 미구현 → 무시
        fsm.MoveTo<StateA>();
        fsm.FixedUpdate();
        Assert.AreEqual(0, owner.FixedCounter);

        // B 는 IFixedUpdatable 구현 → 호출
        fsm.MoveTo<StateB>();
        fsm.FixedUpdate();
        Assert.AreEqual(1000, owner.FixedCounter);
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// LateUpdate 는 Current 가 ILateUpdatable 일 때만 호출된다.
    /// </summary>
    // ----------------------------------------------------------------------
    [Test]
    public void StateMachine_09_LateUpdate_옵션_인터페이스_테스트()
    {
        var owner = new TestOwner();
        var fsm   = new StateMachine<TestOwner>(owner);

        fsm.AddState(new StateA());
        fsm.AddState(new StateB());

        fsm.MoveTo<StateA>();
        fsm.LateUpdate();
        Assert.AreEqual(0, owner.LateCounter);

        fsm.MoveTo<StateB>();
        fsm.LateUpdate();
        Assert.AreEqual(10000, owner.LateCounter);
    }

#endregion

#region 이벤트

    // ----------------------------------------------------------------------
    /// <summary>
    /// 상태 변경 시 OnStateChanged 이벤트가 prev/next 로 발화한다.
    /// </summary>
    // ----------------------------------------------------------------------
    [Test]
    public void StateMachine_10_OnStateChanged_이벤트_테스트()
    {
        var owner = new TestOwner();
        var fsm   = new StateMachine<TestOwner>(owner);

        var a = fsm.AddState(new StateA());
        var b = fsm.AddState(new StateB());

        IState capturedPrev = a;
        IState capturedNext = a;

        fsm.OnStateChanged += (prev, next) =>
        {
            capturedPrev = prev;
            capturedNext = next;
        };

        fsm.MoveTo<StateA>();
        Assert.IsNull(capturedPrev);
        Assert.AreSame(a, capturedNext);

        fsm.MoveTo<StateB>();
        Assert.AreSame(a, capturedPrev);
        Assert.AreSame(b, capturedNext);
    }

#endregion

}
