/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_CuePlaybackService.cs
수정일 : 2026-07-31

# 설명
CuePlaybackService의 Player 선택, Playback 반환과 추적 중인 Playback 정리 계약을 검증한다.

# 테스트 구성
 D: Cue Player 선택과 Playback 반환
 L: Playback 추적과 전체 종료
========================================================================= BLOCK_HEADER_END */

using NUnit.Framework;

namespace inonego.Xeri.TEST._Playback
{
    using inonego.Xeri.Playback;

    // ============================================================
    /// <summary>
    /// CuePlaybackService 공개 계약 테스트.
    /// </summary>
    // ============================================================
    public sealed class TEST_CuePlaybackService
    {
    #region 테스트 데이터

        // ============================================================
        /// <summary>
        /// Cue Player 선택에 사용하는 테스트 Cue.
        /// </summary>
        // ============================================================
        private sealed class TestCue : IPlaybackCue
        {
        }

        // ============================================================
        /// <summary>
        /// 종료 요청과 완료 상태를 기록하는 테스트 Playback.
        /// </summary>
        // ============================================================
        private sealed class TestPlayback : ICuePlayback
        {
        #region 필드

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 Playback 수명 상태.
            /// </summary>
            // ------------------------------------------------------------
            public CuePlaybackState State { get; private set; } = CuePlaybackState.Playing;

            // ------------------------------------------------------------
            /// <summary>
            /// Dispose가 호출된 횟수.
            /// </summary>
            // ------------------------------------------------------------
            public int DisposeInvocationCount { get; private set; } = 0;

        #endregion

        #region 메서드

            // ------------------------------------------------------------
            /// <summary>
            /// 지정 방식으로 Playback을 종료한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Stop(CueStopMode mode = CueStopMode.Immediate)
            {
                if (State == CuePlaybackState.Released) return;

                State = CuePlaybackState.Released;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Playback이 자체적으로 자연 완료된 상태를 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public void Complete()
            {
                State = CuePlaybackState.Released;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 즉시 종료 요청을 전달한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Dispose()
            {
                DisposeInvocationCount++;
                Stop();
            }

        #endregion

        }

        // ============================================================
        /// <summary>
        /// Cue 지원 여부에 따라 지정 Playback을 반환하는 테스트 Player.
        /// </summary>
        // ============================================================
        private sealed class TestCuePlayer : ICuePlayer
        {
        #region 필드

            private readonly bool canPlay = false;
            private ICuePlayback playback = null;

            // ------------------------------------------------------------
            /// <summary>
            /// Cue를 실행한 횟수.
            /// </summary>
            // ------------------------------------------------------------
            public int PlayCount { get; private set; } = 0;

        #endregion

        #region 생성자

            // ------------------------------------------------------------
            /// <summary>
            /// Cue 지원 여부와 반환할 Playback으로 Player를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public TestCuePlayer
            (
                bool canPlay,
                ICuePlayback playback
            ) : base()
            {
                this.canPlay = canPlay;
                this.playback = playback;
            }

        #endregion

        #region 메서드

            // ------------------------------------------------------------
            /// <summary>
            /// 다음 Cue 실행에서 반환할 Playback을 변경한다.
            /// </summary>
            // ------------------------------------------------------------
            public void SetPlayback(ICuePlayback playback)
            {
                this.playback = playback;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 설정된 Cue 지원 여부를 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public bool CanPlay(IPlaybackCue cue)
            {
                return canPlay && cue is TestCue;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 설정된 Playback을 반환하고 실행 횟수를 기록한다.
            /// </summary>
            // ------------------------------------------------------------
            public ICuePlayback Play(IPlaybackCue cue)
            {
                PlayCount++;
                return playback;
            }

        #endregion

        }

    #endregion

    #region D-1: Cue Player 선택과 Playback 반환

        // ------------------------------------------------------------
        /// <summary>
        /// Service는 첫 지원 Player가 만든 Playback 자체를 그대로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CuePlaybackService_첫지원Player의_Playback자체를_반환()
        {
            var playback = new TestPlayback();
            var unsupportedPlayer = new TestCuePlayer(canPlay: false, playback);
            var player = new TestCuePlayer(canPlay: true, playback);
            var service = new CuePlaybackService
            (
                new ICuePlayer[]
                {
                    unsupportedPlayer,
                    player,
                }
            );

            var result = service.Play(new TestCue());

            Assert.AreSame(playback, result);
            Assert.AreEqual(0, unsupportedPlayer.PlayCount);
            Assert.AreEqual(1, player.PlayCount);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Cue를 지원하는 Player가 없으면 실행 요청을 명시적으로 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CuePlaybackService_지원Player가_없으면_실행을_거부()
        {
            var service = new CuePlaybackService
            (
                new[]
                {
                    new TestCuePlayer
                    (
                        canPlay: false,
                        new TestPlayback()
                    ),
                }
            );

            Assert.Throws<System.InvalidOperationException>
            (
                () => service.Play(new TestCue())
            );
        }

    #endregion

    #region L-1: Playback 추적과 전체 종료

        // ------------------------------------------------------------
        /// <summary>
        /// 전체 종료는 추적 중인 Playback만 한 번 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CuePlaybackService_전체종료는_추적중Playback만_한번정리()
        {
            var completedPlayback = new TestPlayback();
            var activePlayback = new TestPlayback();
            var player = new TestCuePlayer(canPlay: true, completedPlayback);
            var service = new CuePlaybackService(new[] { player });

            service.Play(new TestCue());
            completedPlayback.Complete();
            service.Tick();

            player.SetPlayback(activePlayback);
            service.Play(new TestCue());

            service.StopAll();
            service.StopAll();

            Assert.AreEqual(0, completedPlayback.DisposeInvocationCount);
            Assert.AreEqual(1, activePlayback.DisposeInvocationCount);
        }

    #endregion

    }
}
