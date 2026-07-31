/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CuePlaybackService.cs
수정일 : 2026-07-31

# 설명
명시적으로 조립된 Cue Player 중 지정 Cue를 지원하는 Player를 선택해 실행한다.

# 적용 범위
ICuePlayback은 실제 재생 자원과 상태를 소유하고 호출자는 개별 종료를 제어한다.
Service는 Playback을 추적하고 Tick에서 Released 추적을 제거하며 전체 종료 경계를 제공한다.
Service는 시작 시간, 반복, 재생 구간과 Payload별 Unity 세부 동작을 소유하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Cue Player를 조합하고 생성된 Cue Playback을 추적하는 Service.
    /// </summary>
    // ============================================================
    public sealed class CuePlaybackService
    {
    #region 필드

        private readonly List<ICuePlayer> players = new();
        private readonly List<ICuePlayback> playbacks = new();

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 순서의 Cue Player 구성으로 Service를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public CuePlaybackService(IEnumerable<ICuePlayer> players) : base()
        {
            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            foreach (var player in players)
            {
                if (player == null)
                {
                    throw new ArgumentException
                    (
                        "Cue Player 구성에는 null을 포함할 수 없습니다.",
                        nameof(players)
                    );
                }

                this.players.Add(player);
            }
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 첫 지원 Player로 Cue를 실행하고 생성된 Playback을 그대로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public ICuePlayback Play(IPlaybackCue cue)
        {
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            ICuePlayer selectedPlayer = null;

            for (var i = 0; i < players.Count; i++)
            {
                if (!players[i].CanPlay(cue)) continue;

                selectedPlayer = players[i];
                break;
            }

            if (selectedPlayer == null)
            {
                throw new InvalidOperationException
                (
                    $"{cue.GetType().Name} Cue를 처리할 Cue Player가 없습니다."
                );
            }

            var playback = selectedPlayer.Play(cue);
            if (playback == null)
            {
                throw new InvalidOperationException
                (
                    $"{selectedPlayer.GetType().Name}은(는) Cue Playback을 반환해야 합니다."
                );
            }

            playbacks.Add(playback);
            return playback;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Playback 상태를 진행하지 않고 Released가 된 Playback을 추적 목록에서 제거한다.
        /// <br/> Service를 소유한 Runtime이나 Session이 자신의 갱신 경계에서 호출한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void Tick()
        {
            for (var i = playbacks.Count - 1; i >= 0; i--)
            {
                if (playbacks[i].State != CuePlaybackState.Released) continue;

                playbacks.RemoveAt(i);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Service가 추적하는 모든 Cue Playback을 즉시 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        public void StopAll()
        {
            List<Exception> errors = null;

            for (var i = playbacks.Count - 1; i >= 0; i--)
            {
                try
                {
                    playbacks[i].Dispose();
                }
                catch (Exception exception)
                {
                    errors ??= new();
                    errors.Add(exception);
                }
            }

            playbacks.Clear();

            if (errors != null)
            {
                throw new AggregateException("전체 Cue Playback 종료 중 하나 이상의 정리가 실패했습니다.", errors);
            }
        }

    #endregion

    }
}
