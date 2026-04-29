/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Timer.cs
수정일 : 2026-04-30

# 설명
Timer 핵심 기능 유닛 테스트.
Unity Test Runner (Edit Mode) 에서 실행한다.
========================================================================= BLOCK_HEADER_END */

using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.Utility;

// ============================================================
/// <summary>
/// Timer 핵심 기능 테스트.
/// </summary>
// ============================================================
public class TEST_Timer
{

#region Timer 기본 기능 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// Timer의 기본 생성 및 초기값을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Timer_01_기본_생성_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var timer = new Timer();

        // ------------------------------------------------------------
        // 테스트 결과
        // ------------------------------------------------------------
        Assert.AreEqual(0.0f, timer.Duration);
        Assert.AreEqual(0.0f, timer.ElapsedTime);
        Assert.AreEqual(0.0f, timer.RemainingTime);
        Assert.AreEqual(TimerState.Ready, timer.Current);
        Assert.IsFalse(timer.IsRunning);
        Assert.IsFalse(timer.IsPaused);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Timer의 시작·일시정지·재개·중지를 통합 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Timer_02_상태_변경_통합_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var timer = new Timer();
        float duration = 5.0f;

        // ------------------------------------------------------------
        // 시작
        // ------------------------------------------------------------
        timer.Start(duration);
        Assert.AreEqual(duration, timer.Duration);
        Assert.AreEqual(0.0f, timer.ElapsedTime);
        Assert.AreEqual(duration, timer.RemainingTime);
        Assert.AreEqual(TimerState.Run, timer.Current);
        Assert.IsTrue(timer.IsRunning);

        // ------------------------------------------------------------
        // 일시정지
        // ------------------------------------------------------------
        timer.Pause();
        Assert.AreEqual(TimerState.Pause, timer.Current);
        Assert.IsFalse(timer.IsRunning);
        Assert.IsTrue(timer.IsPaused);

        // ------------------------------------------------------------
        // 재개
        // ------------------------------------------------------------
        timer.Resume();
        Assert.AreEqual(TimerState.Run, timer.Current);
        Assert.IsTrue(timer.IsRunning);
        Assert.IsFalse(timer.IsPaused);

        // ------------------------------------------------------------
        // 중지
        // ------------------------------------------------------------
        timer.Stop();
        Assert.AreEqual(TimerState.Ready, timer.Current);
        Assert.IsFalse(timer.IsRunning);
        Assert.IsFalse(timer.IsPaused);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Timer의 Tick 및 완료 기능을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Timer_03_Tick_및_완료_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var timer = new Timer();
        timer.Start(3.0f);

        // ------------------------------------------------------------
        // 1초 경과
        // ------------------------------------------------------------
        timer.Tick(1.0f);
        Assert.AreEqual(1.0f, timer.ElapsedTime);
        Assert.AreEqual(2.0f, timer.RemainingTime);
        Assert.IsTrue(timer.IsRunning);

        // ------------------------------------------------------------
        // 2초 경과
        // ------------------------------------------------------------
        timer.Tick(1.0f);
        Assert.AreEqual(2.0f, timer.ElapsedTime);
        Assert.AreEqual(1.0f, timer.RemainingTime);
        Assert.IsTrue(timer.IsRunning);

        // ------------------------------------------------------------
        // 완료
        // ------------------------------------------------------------
        timer.Tick(1.0f);
        Assert.AreEqual(3.0f, timer.ElapsedTime);
        Assert.AreEqual(0.0f, timer.RemainingTime);
        Assert.AreEqual(TimerState.Ready, timer.Current);
        Assert.IsFalse(timer.IsRunning);

        // ------------------------------------------------------------
        // 오버플로우 (Duration 초과)
        // ------------------------------------------------------------
        timer.Start(2.0f);
        timer.Tick(5.0f);
        Assert.AreEqual(2.0f, timer.ElapsedTime,  "ElapsedTime은 Duration으로 clamp되어야 합니다");
        Assert.AreEqual(0.0f, timer.RemainingTime, "RemainingTime은 0이 되어야 합니다");
        Assert.AreEqual(TimerState.Ready, timer.Current, "오버플로우 시 타이머가 완료되어야 합니다");
        Assert.IsFalse(timer.IsRunning, "오버플로우 시 타이머가 중지되어야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Timer의 리셋 기능을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Timer_04_리셋_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var timer = new Timer();
        timer.Start(5.0f);
        timer.Tick(2.0f);
        timer.Stop();

        // ------------------------------------------------------------
        // 리셋
        // ------------------------------------------------------------
        timer.Reset();

        Assert.AreEqual(0.0f, timer.Duration);
        Assert.AreEqual(0.0f, timer.ElapsedTime);
        Assert.AreEqual(0.0f, timer.RemainingTime);
        Assert.AreEqual(TimerState.Ready, timer.Current);
    }

#endregion

#region Timer 예외 처리 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// Timer의 예외 상황들을 통합 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Timer_05_예외_처리_통합_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var timer = new Timer();

        // ------------------------------------------------------------
        // 음수 Duration으로 Start 시도
        // ------------------------------------------------------------
        Assert.Throws<Timer.InvalidTimeException>(() => timer.Start(-1.0f));

        // ------------------------------------------------------------
        // 이미 실행 중일 때 Start 시도
        // ------------------------------------------------------------
        timer.Start(5.0f);
        Assert.Throws<Timer.AlreadyRunningException>(() => timer.Start(3.0f));

        // ------------------------------------------------------------
        // 실행 중일 때 Reset 시도
        // ------------------------------------------------------------
        Assert.Throws<Timer.FailedToResetException>(() => timer.Reset());

        // ------------------------------------------------------------
        // 중지 후 Reset - 정상 동작
        // ------------------------------------------------------------
        timer.Stop();
        timer.Reset();
    }

#endregion

#region Timer Setter 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// Timer의 setter 기능들을 통합 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Timer_06_Setter_통합_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var timer = new Timer();
        timer.Start(10.0f);

        // ------------------------------------------------------------
        // Duration setter
        // ------------------------------------------------------------
        timer.Duration = 15.0f;
        Assert.AreEqual(15.0f, timer.Duration);
        Assert.AreEqual(0.0f,  timer.ElapsedTime);
        Assert.AreEqual(15.0f, timer.RemainingTime);

        // ------------------------------------------------------------
        // ElapsedTime setter
        // ------------------------------------------------------------
        timer.ElapsedTime = 5.0f;
        Assert.AreEqual(5.0f,  timer.ElapsedTime);
        Assert.AreEqual(10.0f, timer.RemainingTime);

        // ------------------------------------------------------------
        // ElapsedTime clamp (Duration 초과)
        // ------------------------------------------------------------
        timer.ElapsedTime = 20.0f;
        Assert.AreEqual(15.0f, timer.ElapsedTime);
        Assert.AreEqual(0.0f,  timer.RemainingTime);

        // ------------------------------------------------------------
        // RemainingTime setter
        // ------------------------------------------------------------
        timer.RemainingTime = 8.0f;
        Assert.AreEqual(8.0f, timer.RemainingTime);
        Assert.AreEqual(7.0f, timer.ElapsedTime);

        // ------------------------------------------------------------
        // RemainingTime clamp (Duration 초과)
        // ------------------------------------------------------------
        timer.RemainingTime = 20.0f;
        Assert.AreEqual(15.0f, timer.RemainingTime);
        Assert.AreEqual(0.0f,  timer.ElapsedTime);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Timer setter의 상태 검증을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Timer_07_Setter_상태_검증_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var timer = new Timer();

        // ------------------------------------------------------------
        // Ready 상태에서 setter 호출 시 예외
        // ------------------------------------------------------------
        Assert.Throws<System.InvalidOperationException>(() => timer.Duration      = 5.0f);
        Assert.Throws<System.InvalidOperationException>(() => timer.ElapsedTime   = 2.0f);
        Assert.Throws<System.InvalidOperationException>(() => timer.RemainingTime = 3.0f);

        // ------------------------------------------------------------
        // Pause 상태에서 setter 호출 시 정상 동작
        // ------------------------------------------------------------
        timer.Start(5.0f);
        timer.Pause();
        timer.Duration      = 10.0f;
        timer.ElapsedTime   = 2.0f;
        timer.RemainingTime = 3.0f;
        Assert.AreEqual(10.0f, timer.Duration);
        Assert.AreEqual(7.0f,  timer.ElapsedTime);
        Assert.AreEqual(3.0f,  timer.RemainingTime);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Timer setter의 값 검증을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Timer_08_Setter_값_검증_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var timer = new Timer();
        timer.Start(5.0f);

        // ------------------------------------------------------------
        // 음수 값으로 setter 호출 시 예외
        // ------------------------------------------------------------
        Assert.Throws<Timer.InvalidTimeException>(() => timer.Duration      = -1.0f);
        Assert.Throws<Timer.InvalidTimeException>(() => timer.ElapsedTime   = -1.0f);
        Assert.Throws<Timer.InvalidTimeException>(() => timer.RemainingTime = -1.0f);
    }

#endregion

#region Timer 이벤트 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// Timer의 OnEnd 이벤트 발생을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Timer_09_OnEnd_이벤트_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var timer = new Timer();
        bool endEventFired      = false;
        Timer endEventSender    = null;
        TimerEndEventArgs endEventArgs = default;

        timer.OnEnd += (sender, e) =>
        {
            endEventFired  = true;
            endEventSender = sender as Timer;
            endEventArgs   = e;
        };

        // ------------------------------------------------------------
        // 아직 완료되지 않은 상태
        // ------------------------------------------------------------
        timer.Start(2.0f);
        timer.Tick(1.0f);
        Assert.IsFalse(endEventFired, "아직 완료되지 않았으므로 이벤트가 발생하지 않아야 합니다");

        // ------------------------------------------------------------
        // 완료
        // ------------------------------------------------------------
        timer.Tick(1.0f);
        Assert.IsTrue(endEventFired,        "타이머 완료 시 OnEnd 이벤트가 발생해야 합니다");
        Assert.AreEqual(timer, endEventSender, "이벤트 발신자는 타이머 자신이어야 합니다");
        Assert.AreEqual(TimerState.Ready, timer.Current, "완료 후 상태는 Ready여야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Timer의 OnStateChange 이벤트 발생을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Timer_10_OnStateChange_이벤트_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var timer = new Timer();
        bool stateChangeEventFired = false;
        Timer stateChangeSender    = null;
        ValueChangeEventArgs<TimerState> stateChangeEventArgs = default;

        void Reset()
        {
            stateChangeEventFired  = false;
            stateChangeSender      = null;
            stateChangeEventArgs   = default;
        }

        timer.OnStateChange += (sender, e) =>
        {
            stateChangeEventFired  = true;
            stateChangeSender      = sender as Timer;
            stateChangeEventArgs   = e;
        };

        // ------------------------------------------------------------
        // 시작
        // ------------------------------------------------------------
        timer.Start(5.0f);
        Assert.IsTrue(stateChangeEventFired, "시작 시 OnStateChange 이벤트가 발생해야 합니다");
        Assert.AreEqual(timer,             stateChangeSender,              "이벤트 발신자는 타이머 자신이어야 합니다");
        Assert.AreEqual(TimerState.Ready,  stateChangeEventArgs.Previous, "이전 상태는 Ready여야 합니다");
        Assert.AreEqual(TimerState.Run,    stateChangeEventArgs.Current,  "현재 상태는 Run이어야 합니다");

        Reset();

        // ------------------------------------------------------------
        // 일시정지
        // ------------------------------------------------------------
        timer.Pause();
        Assert.IsTrue(stateChangeEventFired, "일시정지 시 OnStateChange 이벤트가 발생해야 합니다");
        Assert.AreEqual(TimerState.Run,   stateChangeEventArgs.Previous, "이전 상태는 Run이어야 합니다");
        Assert.AreEqual(TimerState.Pause, stateChangeEventArgs.Current,  "현재 상태는 Pause여야 합니다");

        Reset();

        // ------------------------------------------------------------
        // 재개
        // ------------------------------------------------------------
        timer.Resume();
        Assert.IsTrue(stateChangeEventFired, "재개 시 OnStateChange 이벤트가 발생해야 합니다");
        Assert.AreEqual(TimerState.Pause, stateChangeEventArgs.Previous, "이전 상태는 Pause여야 합니다");
        Assert.AreEqual(TimerState.Run,   stateChangeEventArgs.Current,  "현재 상태는 Run이어야 합니다");

        Reset();

        // ------------------------------------------------------------
        // 중지
        // ------------------------------------------------------------
        timer.Stop();
        Assert.IsTrue(stateChangeEventFired, "중지 시 OnStateChange 이벤트가 발생해야 합니다");
        Assert.AreEqual(TimerState.Run,   stateChangeEventArgs.Previous, "이전 상태는 Run이어야 합니다");
        Assert.AreEqual(TimerState.Ready, stateChangeEventArgs.Current,  "현재 상태는 Ready여야 합니다");
    }

#endregion

}
