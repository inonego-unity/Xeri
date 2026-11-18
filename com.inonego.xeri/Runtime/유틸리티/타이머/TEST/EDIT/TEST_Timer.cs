/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Timer.cs
수정일 : 2026-11-19

# 설명
Timer 핵심 기능 유닛 테스트.
Unity Test Runner (Edit Mode) 에서 실행한다.

# 테스트 구성
 E: 생성과 상태 제어
 T: 시간 진행·역행·완료
 R: Reset
 X: 상태 제약과 시간 입력 검증
 P: 시간 속성 설정과 범위 제한
 V: 완료 이벤트와 상태 전이
 C: 복제
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Utility._Timer
{

    using inonego;
    using inonego.Xeri;
    using inonego.Xeri.Utility;

    // ============================================================
    /// <summary>
    /// Timer 핵심 기능 테스트.
    /// </summary>
    // ============================================================
    public class TEST_Timer
    {

    #region E-1: 기본 생성

        [Test]
        public void TEST_Timer_기본_생성_초기값()
        {
            var timer = new Timer();

            Assert.AreEqual(0.0f, timer.Duration);
            Assert.AreEqual(0.0f, timer.ElapsedTime);
            Assert.AreEqual(0.0f, timer.RemainingTime);
            Assert.AreEqual(0.0f, timer.ElapsedTime01);
            Assert.AreEqual(0.0f, timer.RemainingTime01);
            Assert.AreEqual(TimerState.Ready, timer.Current);
            Assert.IsFalse(timer.IsRunning);
            Assert.IsFalse(timer.IsPaused);
        }

    #endregion

    #region E-2: Start / Pause / Resume / Stop

        [Test]
        public void TEST_Timer_상태_변경_Start_Pause_Resume_Stop()
        {
            var timer = new Timer();
            float duration = 5.0f;

            // 시작
            timer.Start(duration);
            Assert.AreEqual(duration, timer.Duration);
            Assert.AreEqual(0.0f, timer.ElapsedTime);
            Assert.AreEqual(duration, timer.RemainingTime);
            Assert.AreEqual(TimerState.Run, timer.Current);
            Assert.IsTrue(timer.IsRunning);

            // 일시정지
            timer.Pause();
            Assert.AreEqual(TimerState.Pause, timer.Current);
            Assert.IsFalse(timer.IsRunning);
            Assert.IsTrue(timer.IsPaused);

            // 재개
            timer.Resume();
            Assert.AreEqual(TimerState.Run, timer.Current);
            Assert.IsTrue(timer.IsRunning);
            Assert.IsFalse(timer.IsPaused);

            // 중지
            timer.Stop();
            Assert.AreEqual(TimerState.Ready, timer.Current);
            Assert.IsFalse(timer.IsRunning);
            Assert.IsFalse(timer.IsPaused);
        }

    #endregion

    #region T-1: 시간 진행·역행·완료

        [Test]
        public void TEST_Timer_Tick_진행_역행_경계_완료()
        {
            var timer = new Timer();
            timer.Start(3.0f);

            // 1초 경과
            timer.Tick(1.0f);
            Assert.AreEqual(1.0f, timer.ElapsedTime);
            Assert.AreEqual(2.0f, timer.RemainingTime);
            Assert.IsTrue(timer.IsRunning);

            // 2초 경과
            timer.Tick(1.0f);
            Assert.AreEqual(2.0f, timer.ElapsedTime);
            Assert.AreEqual(1.0f, timer.RemainingTime);
            Assert.IsTrue(timer.IsRunning);

            // 완료
            timer.Tick(1.0f);
            Assert.AreEqual(3.0f, timer.ElapsedTime);
            Assert.AreEqual(0.0f, timer.RemainingTime);
            Assert.AreEqual(TimerState.Ready, timer.Current);
            Assert.IsFalse(timer.IsRunning);

            // 오버플로우 (Duration 초과)
            timer.Start(2.0f);
            timer.Tick(5.0f);
            Assert.AreEqual(2.0f, timer.ElapsedTime,  "ElapsedTime은 Duration으로 clamp되어야 합니다");
            Assert.AreEqual(0.0f, timer.RemainingTime, "RemainingTime은 0이 되어야 합니다");
            Assert.AreEqual(TimerState.Ready, timer.Current, "오버플로우 시 타이머가 완료되어야 합니다");
            Assert.IsFalse(timer.IsRunning, "오버플로우 시 타이머가 중지되어야 합니다");

            // 음수 Tick은 진행 중인 타이머의 시간을 되돌린다.
            timer.Start(10.0f);
            timer.Tick(6.0f);
            timer.Tick(-2.0f);
            Assert.AreEqual(4.0f, timer.ElapsedTime);
            Assert.AreEqual(6.0f, timer.RemainingTime);
            Assert.IsTrue(timer.IsRunning);

            timer.Tick(-100.0f);
            Assert.AreEqual(0.0f, timer.ElapsedTime);
            timer.Tick(10.0f);
            Assert.AreEqual(TimerState.Ready, timer.Current);

            // 길이가 없는 타이머도 첫 Tick에서 완료하며 비율은 유효한 숫자다.
            timer.Start(0.0f);
            Assert.AreEqual(0.0f, timer.ElapsedTime01);
            Assert.AreEqual(0.0f, timer.RemainingTime01);
            timer.Tick(0.0f);
            Assert.AreEqual(TimerState.Ready, timer.Current);
        }

    #endregion

    #region R-1: Reset

        [Test]
        public void TEST_Timer_Reset_초기화()
        {
            var timer = new Timer();
            timer.Start(5.0f);
            timer.Tick(2.0f);
            timer.Stop();

            timer.Reset();

            Assert.AreEqual(0.0f, timer.Duration);
            Assert.AreEqual(0.0f, timer.ElapsedTime);
            Assert.AreEqual(0.0f, timer.RemainingTime);
            Assert.AreEqual(TimerState.Ready, timer.Current);
        }

    #endregion

    #region P-1: Setter 기본 동작

        [Test]
        public void TEST_Timer_Setter_Duration_ElapsedTime_RemainingTime_clamp()
        {
            var timer = new Timer();
            timer.Start(10.0f);

            // Duration setter
            timer.Duration = 15.0f;
            Assert.AreEqual(15.0f, timer.Duration);
            Assert.AreEqual(0.0f,  timer.ElapsedTime);
            Assert.AreEqual(15.0f, timer.RemainingTime);

            // ElapsedTime setter
            timer.ElapsedTime = 5.0f;
            Assert.AreEqual(5.0f,  timer.ElapsedTime);
            Assert.AreEqual(10.0f, timer.RemainingTime);
            Assert.AreEqual(5.0f / 15.0f, timer.ElapsedTime01);
            Assert.AreEqual(10.0f / 15.0f, timer.RemainingTime01);

            // ElapsedTime clamp (Duration 초과)
            timer.ElapsedTime = 20.0f;
            Assert.AreEqual(15.0f, timer.ElapsedTime);
            Assert.AreEqual(0.0f,  timer.RemainingTime);

            // RemainingTime setter
            timer.RemainingTime = 8.0f;
            Assert.AreEqual(8.0f, timer.RemainingTime);
            Assert.AreEqual(7.0f, timer.ElapsedTime);

            // RemainingTime clamp (Duration 초과)
            timer.RemainingTime = 20.0f;
            Assert.AreEqual(15.0f, timer.RemainingTime);
            Assert.AreEqual(0.0f,  timer.ElapsedTime);
        }

    #endregion

    #region X-1: Start·Reset·이벤트 예외

        [Test]
        public void TEST_Timer_Start_Reset_상태_제약과_이벤트_예외()
        {
            var timer = new Timer();

            Assert.Throws<Timer.InvalidTimeException>(() => timer.Start(-1.0f));
            Assert.AreEqual(TimerState.Ready, timer.Current);
            Assert.AreEqual(0.0f, timer.Duration);

            timer.Start(5.0f);
            Assert.Throws<Timer.AlreadyRunningException>(() => timer.Start(3.0f));
            Assert.Throws<Timer.FailedToResetException>(() => timer.Reset());

            timer.Stop();
            timer.Reset();

            // 상태 전이 이벤트에서 예외가 나더라도 새 주기의 시간은 완성되어 있다.
            var stateChanges = 0;
            timer.OnStateChange += (_, change) =>
            {
                if (change.Current == TimerState.Run)
                {
                    stateChanges++;
                    throw new InvalidOperationException("state callback failure");
                }
            };

            Assert.Throws<Timer.InvalidTimeException>(() => timer.Start(-1.0f));
            Assert.AreEqual(0, stateChanges);
            Assert.AreEqual(TimerState.Ready, timer.Current);

            Assert.Throws<InvalidOperationException>(() => timer.Start(8.0f));
            Assert.AreEqual(1, stateChanges);
            Assert.AreEqual(TimerState.Run, timer.Current);
            Assert.AreEqual(8.0f, timer.Duration);
            Assert.AreEqual(0.0f, timer.ElapsedTime);
            timer.Stop();
        }

    #endregion

    #region X-2: Setter 상태 검증

        [Test]
        public void TEST_Timer_Setter_Ready상태_예외_Pause상태_정상()
        {
            var timer = new Timer();

            // Ready 상태에서 setter 호출 시 예외
            Assert.Throws<InvalidOperationException>(() => timer.Duration      = 5.0f);
            Assert.Throws<InvalidOperationException>(() => timer.ElapsedTime   = 2.0f);
            Assert.Throws<InvalidOperationException>(() => timer.RemainingTime = 3.0f);

            // Pause 상태에서 setter 호출 시 정상 동작
            timer.Start(5.0f);
            timer.Pause();
            timer.Duration      = 10.0f;
            timer.ElapsedTime   = 2.0f;
            timer.RemainingTime = 3.0f;
            Assert.AreEqual(10.0f, timer.Duration);
            Assert.AreEqual(7.0f,  timer.ElapsedTime);
            Assert.AreEqual(3.0f,  timer.RemainingTime);
        }

    #endregion

    #region X-3: 시간 입력 검증과 상태 보존

        [Test]
        public void TEST_Timer_시간_입력_유효성_및_상태_보존()
        {
            var timer = new Timer();
            timer.Start(5.0f);
            timer.Tick(2.0f);

            Assert.Throws<Timer.InvalidTimeException>(() => timer.Duration      = -1.0f);
            Assert.Throws<Timer.InvalidTimeException>(() => timer.ElapsedTime   = -1.0f);
            Assert.Throws<Timer.InvalidTimeException>(() => timer.RemainingTime = -1.0f);

            var invalidTimes = new[]
            {
                float.NaN,
                float.PositiveInfinity,
                float.NegativeInfinity
            };

            foreach (var invalid in invalidTimes)
            {
                Assert.Throws<Timer.InvalidTimeException>(() => timer.Tick(invalid));
                Assert.Throws<Timer.InvalidTimeException>(() => timer.Duration = invalid);
                Assert.Throws<Timer.InvalidTimeException>(() => timer.ElapsedTime = invalid);
                Assert.Throws<Timer.InvalidTimeException>(() => timer.RemainingTime = invalid);

                Assert.AreEqual(5.0f, timer.Duration);
                Assert.AreEqual(2.0f, timer.ElapsedTime);
                Assert.IsTrue(timer.IsRunning);
            }

            timer.Stop();
            foreach (var invalid in invalidTimes)
            {
                Assert.Throws<Timer.InvalidTimeException>(() => timer.Start(invalid));
                Assert.AreEqual(TimerState.Ready, timer.Current);
                Assert.AreEqual(5.0f, timer.Duration);
                Assert.AreEqual(2.0f, timer.ElapsedTime);
            }
        }

    #endregion

    #region V-1: 완료 상태와 콜백

        [Test]
        public void TEST_Timer_OnEnd_완료_콜백_재진입과_예외()
        {
            var timer = new Timer();
            var endCount = 0;
            var readyCount = 0;
            var order = 0;

            timer.OnStateChange += (_, change) =>
            {
                if (change.Current == TimerState.Ready)
                {
                    readyCount++;
                    Assert.AreEqual(1, ++order);
                }
            };

            timer.OnEnd += (sender, _) =>
            {
                endCount++;
                Assert.AreEqual(timer, sender);
                Assert.AreEqual(TimerState.Ready, timer.Current);
                Assert.AreEqual(2.0f, timer.ElapsedTime);
                Assert.AreEqual(2, ++order);

                // 완료 이벤트 중 Tick을 다시 호출해도 완료가 중복되지 않는다.
                timer.Tick(1.0f);
            };

            timer.Start(2.0f);
            timer.Tick(1.0f);
            Assert.AreEqual(0, endCount);

            timer.Tick(1.0f);
            Assert.AreEqual(1, readyCount);
            Assert.AreEqual(1, endCount);
            timer.Tick(1.0f);
            Assert.AreEqual(1, endCount);

            // 콜백 예외가 발생해도 완료된 타이머는 이미 Ready이다.
            var failing = new Timer();
            failing.OnEnd += (_, _) => throw new InvalidOperationException("end callback failure");
            failing.Start(1.0f);
            Assert.Throws<InvalidOperationException>(() => failing.Tick(1.0f));
            Assert.AreEqual(TimerState.Ready, failing.Current);
            Assert.AreEqual(1.0f, failing.ElapsedTime);

            // 완료된 Timer는 별도 Stop 호출 없이 새 주기를 시작할 수 있다.
            var restarting = new Timer();
            var restartCount = 0;
            restarting.OnEnd += (_, _) =>
            {
                restartCount++;
                if (restartCount == 1)
                {
                    restarting.Start(4.0f);
                }
            };

            restarting.Start(1.0f);
            restarting.Tick(1.0f);
            Assert.AreEqual(TimerState.Run, restarting.Current);
            Assert.AreEqual(4.0f, restarting.Duration);
            Assert.AreEqual(0.0f, restarting.ElapsedTime);
            restarting.Tick(2.0f);
            Assert.AreEqual(2.0f, restarting.ElapsedTime);
            Assert.AreEqual(1, restartCount);
        }

    #endregion

    #region V-2: 상태 전이와 시간 관찰

        [Test]
        public void TEST_Timer_OnStateChange_상태_전이와_시간_관찰()
        {
            var timer = new Timer();
            bool stateChangeEventFired = false;
            Timer stateChangeSender    = null;
            ValueChangeEventArgs<TimerState> stateChangeEventArgs = default;
            float runDuration = -1.0f;
            float runElapsed = -1.0f;

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

                if (e.Current == TimerState.Run)
                {
                    runDuration = timer.Duration;
                    runElapsed = timer.ElapsedTime;
                }
            };

            // 시작
            timer.Start(5.0f);
            Assert.IsTrue(stateChangeEventFired, "시작 시 OnStateChange 이벤트가 발생해야 합니다");
            Assert.AreEqual(timer,             stateChangeSender,              "이벤트 발신자는 타이머 자신이어야 합니다");
            Assert.AreEqual(TimerState.Ready,  stateChangeEventArgs.Previous, "이전 상태는 Ready여야 합니다");
            Assert.AreEqual(TimerState.Run,    stateChangeEventArgs.Current,  "현재 상태는 Run이어야 합니다");
            Assert.AreEqual(5.0f, runDuration);
            Assert.AreEqual(0.0f, runElapsed);

            Reset();

            // 일시정지
            timer.Pause();
            Assert.IsTrue(stateChangeEventFired, "일시정지 시 OnStateChange 이벤트가 발생해야 합니다");
            Assert.AreEqual(TimerState.Run,   stateChangeEventArgs.Previous, "이전 상태는 Run이어야 합니다");
            Assert.AreEqual(TimerState.Pause, stateChangeEventArgs.Current,  "현재 상태는 Pause여야 합니다");

            Reset();

            // 재개
            timer.Resume();
            Assert.IsTrue(stateChangeEventFired, "재개 시 OnStateChange 이벤트가 발생해야 합니다");
            Assert.AreEqual(TimerState.Pause, stateChangeEventArgs.Previous, "이전 상태는 Pause여야 합니다");
            Assert.AreEqual(TimerState.Run,   stateChangeEventArgs.Current,  "현재 상태는 Run이어야 합니다");

            Reset();

            // 중지
            timer.Stop();
            Assert.IsTrue(stateChangeEventFired, "중지 시 OnStateChange 이벤트가 발생해야 합니다");
            Assert.AreEqual(TimerState.Run,   stateChangeEventArgs.Previous, "이전 상태는 Run이어야 합니다");
            Assert.AreEqual(TimerState.Ready, stateChangeEventArgs.Current,  "현재 상태는 Ready여야 합니다");

            timer.Start(10.0f);
            Assert.AreEqual(10.0f, runDuration);
            Assert.AreEqual(0.0f, runElapsed);
        }

    #endregion

    #region C-1: 복제

        // ------------------------------------------------------------
        /// <summary>
        /// Clone은 실행 중인 시간과 상태를 가진 독립된 새 인스턴스를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Timer_Clone_실행중시간과상태를독립사본으로복제()
        {
            var source = new Timer();
            source.Start(10.0f);
            source.Tick(3.0f);

            var clone = source.Clone();

            Assert.AreNotSame(source, clone);
            Assert.AreEqual(JsonUtility.ToJson(source), JsonUtility.ToJson(clone));
            Assert.IsTrue(clone.IsRunning);
            Assert.AreEqual(10.0f, clone.Duration);
            Assert.AreEqual(3.0f, clone.ElapsedTime);

            clone.Tick(2.0f);

            Assert.AreEqual(3.0f, source.ElapsedTime);
            Assert.AreEqual(5.0f, clone.ElapsedTime);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> CloneFrom은 상태 전이 이벤트 없이 일시정지 상태를 복제하고
        /// <br/> 대상의 기존 구독은 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Timer_CloneFrom_일시정지상태복제_이벤트없음_구독유지()
        {
            var source = new Timer();
            source.Start(8.0f);
            source.Tick(2.0f);
            source.Pause();
            var target = new Timer();
            var stateChanges = 0;
            var ends = 0;
            target.OnStateChange += (_, _) => stateChanges++;
            target.OnEnd += (_, _) => ends++;

            target.CloneFrom(source);

            Assert.AreEqual(0, stateChanges);
            Assert.IsTrue(target.IsPaused);
            Assert.AreEqual(8.0f, target.Duration);
            Assert.AreEqual(2.0f, target.ElapsedTime);

            target.Resume();
            target.Tick(6.0f);

            Assert.AreEqual(2, stateChanges);
            Assert.AreEqual(1, ends);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 중지 뒤 경과 시간이 남은 Ready 상태와 Start 값도 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Timer_CloneFrom_중지후남은경과시간과Start값복제()
        {
            var source = new Timer();
            source.Start(5.0f);
            source.Tick(2.0f);
            source.Stop();
            source.cached = 4.0f;
            var target = new Timer();
            target.Start(1.0f);

            target.CloneFrom(source);

            Assert.AreEqual(TimerState.Ready, target.Current);
            Assert.AreEqual(5.0f, target.Duration);
            Assert.AreEqual(2.0f, target.ElapsedTime);
            Assert.AreEqual(4.0f, target.cached);
            Assert.AreEqual(JsonUtility.ToJson(source), JsonUtility.ToJson(target));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 복제본은 원본의 구독을 가져가지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Timer_Clone_원본구독은복제하지않음()
        {
            var source = new Timer();
            var sourceEnds = 0;
            source.OnEnd += (_, _) => sourceEnds++;
            source.Start(1.0f);

            var clone = source.Clone();
            clone.Tick(1.0f);

            Assert.AreEqual(0, sourceEnds);
            Assert.IsTrue(source.IsRunning);
            Assert.AreEqual(TimerState.Ready, clone.Current);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// null source는 예외로 거부하고 기존 상태를 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Timer_CloneFrom_null거부()
        {
            var target = new Timer();
            target.Start(3.0f);

            Assert.Throws<ArgumentNullException>(() => target.CloneFrom(null));
            Assert.IsTrue(target.IsRunning);
            Assert.AreEqual(3.0f, target.Duration);
        }

    #endregion

    }

}
