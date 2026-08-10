/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PlaybackClock.cs
수정일 : 2026-07-31

# 설명
PlaybackClock의 상태 전이, 시간 진행, Duration 변경, 자연 완료와 Loop 공개 계약을 검증한다.

# 테스트 구성
 S: 상태 명령
 T: 시간, Seek와 Speed
 D: Duration 변경
 C: 자연 완료
 L: Loop
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace inonego.Xeri.TEST._Playback
{
    using inonego.Xeri.Playback;

    // ============================================================
    /// <summary>
    /// PlaybackClock 공개 계약 테스트.
    /// </summary>
    // ============================================================
    public class TEST_PlaybackClock
    {

    #region S-1: 상태 명령

        // ------------------------------------------------------------
        /// <summary>
        /// 재생, 일시정지, 재개와 정지가 상태 및 시간 계약을 따른다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PlaybackClock_상태명령은_현재위치를_보존하거나_초기화()
        {
            var stateChanges = new List<ValueChangeEventArgs<PlaybackState>>();
            var completedCount = 0;
            var clock = new PlaybackClock();
            clock.SetDuration(5.0f);
            clock.OnStateChange += (_, e) => stateChanges.Add(e);
            clock.OnCompleted += () => completedCount++;

            clock.Play();
            clock.Tick(2.0f);
            clock.Pause();
            clock.Tick(1.0f);
            clock.Pause();
            clock.Play();
            clock.Play();
            clock.Stop();
            clock.Stop();

            Assert.AreEqual(PlaybackState.Stopped, clock.State);
            Assert.IsFalse(clock.IsPlaying);
            Assert.IsFalse(clock.IsPaused);
            Assert.AreEqual(0.0f, clock.Time);
            Assert.AreEqual(0, completedCount);
            Assert.AreEqual(4, stateChanges.Count);
            Assert.AreEqual(PlaybackState.Stopped, stateChanges[0].Previous);
            Assert.AreEqual(PlaybackState.Playing, stateChanges[0].Current);
            Assert.AreEqual(PlaybackState.Playing, stateChanges[1].Previous);
            Assert.AreEqual(PlaybackState.Paused, stateChanges[1].Current);
            Assert.AreEqual(PlaybackState.Paused, stateChanges[2].Previous);
            Assert.AreEqual(PlaybackState.Playing, stateChanges[2].Current);
            Assert.AreEqual(PlaybackState.Playing, stateChanges[3].Previous);
            Assert.AreEqual(PlaybackState.Stopped, stateChanges[3].Current);
        }

    #endregion

    #region T-1: 시간, Seek와 Speed

        // ------------------------------------------------------------
        /// <summary>
        /// Tick은 유효한 delta에서만 Speed를 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PlaybackClock_Tick과_SetTime은_재생상태와_범위를_따름()
        {
            var clock = new PlaybackClock();
            clock.SetDuration(10.0f);
            clock.Speed = 2.0f;
            clock.Play();

            clock.Tick(1.0f);
            Assert.AreEqual(2.0f, clock.Time);

            clock.Pause();
            clock.Tick(1.0f);
            Assert.AreEqual(2.0f, clock.Time);

            clock.Play();
            clock.Tick(0.0f);
            clock.Tick(-1.0f);
            clock.Tick(float.NaN);
            clock.Tick(float.PositiveInfinity);
            Assert.AreEqual(2.0f, clock.Time);

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetTime(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetTime(float.PositiveInfinity));
            Assert.AreEqual(2.0f, clock.Time);

            clock.SetTime(12.0f);
            Assert.AreEqual(10.0f, clock.Time);
            Assert.IsTrue(clock.IsPlaying);

            clock.SetTime(-1.0f);
            Assert.AreEqual(0.0f, clock.Time);
            Assert.IsTrue(clock.IsPlaying);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 유효하지 않은 Speed는 거부하고 기존 값을 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PlaybackClock_유효하지않은_Speed는_거부하고_기존값을_유지()
        {
            var clock = new PlaybackClock();
            clock.Speed = 2.0f;

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Speed = float.NaN);
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Speed = float.PositiveInfinity);
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Speed = 0.0f);
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Speed = -1.0f);
            Assert.AreEqual(2.0f, clock.Speed);
        }

    #endregion

    #region D-1: Duration 보존과 Clamp

        // ------------------------------------------------------------
        /// <summary>
        /// Duration 변경은 가능한 범위에서 현재 상태와 재생 위치를 보존한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PlaybackClock_Duration변경은_상태와_재생위치를_가능한범위에서_보존()
        {
            var completedCount = 0;
            var clock = new PlaybackClock();
            clock.SetDuration(10.0f);
            clock.SetTime(6.0f);
            clock.Play();
            clock.OnCompleted += () => completedCount++;

            clock.SetDuration(12.0f);
            Assert.AreEqual(PlaybackState.Playing, clock.State);
            Assert.AreEqual(6.0f, clock.Time);

            clock.SetDuration(8.0f);
            Assert.AreEqual(PlaybackState.Playing, clock.State);
            Assert.AreEqual(6.0f, clock.Time);

            clock.Pause();
            clock.SetDuration(4.0f);

            Assert.AreEqual(PlaybackState.Paused, clock.State);
            Assert.AreEqual(4.0f, clock.Time);
            Assert.AreEqual(0, completedCount);
        }

    #endregion

    #region D-2: Duration 자연 완료

        // ------------------------------------------------------------
        /// <summary>
        /// 재생 중 Duration이 현재 위치 이하로 줄면 최종 위치를 알리고 자연 완료한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PlaybackClock_재생중_Duration축소는_최종위치에서_자연완료()
        {
            var events = new List<string>();
            var clock = new PlaybackClock();
            clock.SetDuration(10.0f);
            clock.SetTime(6.0f);
            clock.Play();
            clock.OnStateChange += (_, _) => events.Add("State");
            clock.OnTimeChange += (_, e) =>
            {
                events.Add("Time");
                Assert.AreEqual(6.0f, e.Previous);
                Assert.AreEqual(5.0f, e.Current);
            };
            clock.OnCompleted += () => events.Add("Completed");

            clock.SetDuration(5.0f);

            CollectionAssert.AreEqual(new[] { "State", "Time", "Completed" }, events);
            Assert.AreEqual(PlaybackState.Stopped, clock.State);
            Assert.AreEqual(5.0f, clock.Time);
        }

    #endregion

    #region D-3: Loop Duration 보존

        // ------------------------------------------------------------
        /// <summary>
        /// Loop 재생 중 Duration 축소는 완료하지 않고 새 범위에서 재생을 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PlaybackClock_Loop재생중_Duration축소는_재생상태를_유지()
        {
            var completedCount = 0;
            var clock = new PlaybackClock();
            clock.SetDuration(10.0f);
            clock.SetTime(6.0f);
            clock.IsLooping = true;
            clock.Play();
            clock.OnCompleted += () => completedCount++;

            clock.SetDuration(5.0f);

            Assert.AreEqual(PlaybackState.Playing, clock.State);
            Assert.AreEqual(5.0f, clock.Time);
            Assert.AreEqual(0, completedCount);
        }

    #endregion

    #region D-4: Duration 경계

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 유효하지 않은 Duration은 기존 상태를 보존한다.
        /// <br/> Duration 0은 완료 없이 정지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PlaybackClock_Duration경계는_기존상태보존과_명시적정지를_구분()
        {
            var completedCount = 0;
            var clock = new PlaybackClock();
            clock.SetDuration(10.0f);
            clock.SetTime(3.0f);
            clock.Play();
            clock.OnCompleted += () => completedCount++;

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetDuration(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetDuration(float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetDuration(-1.0f));
            Assert.AreEqual(10.0f, clock.Duration);
            Assert.AreEqual(3.0f, clock.Time);
            Assert.AreEqual(PlaybackState.Playing, clock.State);

            clock.SetDuration(0.0f);

            Assert.AreEqual(0.0f, clock.Duration);
            Assert.AreEqual(0.0f, clock.Time);
            Assert.AreEqual(PlaybackState.Stopped, clock.State);
            Assert.AreEqual(0, completedCount);
        }

    #endregion

    #region C-1: 자연 완료

        // ------------------------------------------------------------
        /// <summary>
        /// 자연 완료는 최종 상태와 시간을 확정한 뒤 완료를 알리고 다시 재생할 수 있다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PlaybackClock_자연완료는_상태_시간_완료순으로_알림()
        {
            var events = new List<string>();
            var timeChanges = new List<ValueChangeEventArgs<float>>();
            var clock = new PlaybackClock();
            clock.SetDuration(5.0f);
            clock.Play();
            clock.Tick(2.0f);
            clock.OnStateChange += (_, _) => events.Add("State");
            clock.OnTimeChange += (_, e) =>
            {
                events.Add("Time");
                timeChanges.Add(e);
            };
            clock.OnCompleted += () => events.Add("Completed");

            clock.Tick(4.0f);

            CollectionAssert.AreEqual(new[] { "State", "Time", "Completed" }, events);
            Assert.AreEqual(2.0f, timeChanges[0].Previous);
            Assert.AreEqual(5.0f, timeChanges[0].Current);
            Assert.AreEqual(PlaybackState.Stopped, clock.State);
            Assert.AreEqual(5.0f, clock.Time);

            events.Clear();
            timeChanges.Clear();
            clock.Play();

            CollectionAssert.AreEqual(new[] { "State", "Time" }, events);
            Assert.AreEqual(5.0f, timeChanges[0].Previous);
            Assert.AreEqual(0.0f, timeChanges[0].Current);
            Assert.AreEqual(PlaybackState.Playing, clock.State);
            Assert.AreEqual(0.0f, clock.Time);
        }

    #endregion

    #region L-1: Loop

        // ------------------------------------------------------------
        /// <summary>
        /// Loop는 초과 시간을 보존하고 Loop 경계를 시간 변경보다 먼저 알린다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PlaybackClock_Loop는_초과시간을_보존하고_경계후_시간을_알림()
        {
            var events = new List<string>();
            var timeChanges = new List<ValueChangeEventArgs<float>>();
            var completedCount = 0;
            var clock = new PlaybackClock();
            clock.SetDuration(4.0f);
            clock.SetTime(3.0f);
            clock.IsLooping = true;
            clock.Play();
            clock.OnLooped += () => events.Add("Looped");
            clock.OnTimeChange += (_, e) =>
            {
                events.Add("Time");
                timeChanges.Add(e);
            };
            clock.OnCompleted += () => completedCount++;

            clock.Tick(2.0f);

            CollectionAssert.AreEqual(new[] { "Looped", "Time" }, events);
            Assert.AreEqual(3.0f, timeChanges[0].Previous);
            Assert.AreEqual(1.0f, timeChanges[0].Current);
            Assert.AreEqual(PlaybackState.Playing, clock.State);
            Assert.AreEqual(1.0f, clock.Time);
            Assert.AreEqual(0, completedCount);

            events.Clear();
            timeChanges.Clear();
            clock.Tick(4.0f);

            CollectionAssert.AreEqual(new[] { "Looped", "Time" }, events);
            Assert.AreEqual(1.0f, timeChanges[0].Previous);
            Assert.AreEqual(1.0f, timeChanges[0].Current);
            Assert.AreEqual(PlaybackState.Playing, clock.State);
            Assert.AreEqual(1.0f, clock.Time);
            Assert.AreEqual(0, completedCount);

            clock.Speed = float.MaxValue;
            clock.Tick(2.0f);

            Assert.IsFalse(float.IsNaN(clock.Time));
            Assert.IsFalse(float.IsInfinity(clock.Time));
            Assert.GreaterOrEqual(clock.Time, 0.0f);
            Assert.Less(clock.Time, clock.Duration);
            Assert.AreEqual(PlaybackState.Playing, clock.State);
            Assert.AreEqual(0, completedCount);

            clock.Stop();
            clock.SetDuration(1.0f);
            clock.Speed = 0.9999999f;
            clock.Play();
            clock.Tick(2.00000024f);

            Assert.Greater(clock.Time, 0.99f);
            Assert.Less(clock.Time, clock.Duration);
            Assert.AreEqual(PlaybackState.Playing, clock.State);
            Assert.AreEqual(0, completedCount);
        }

    #endregion

    }
}
