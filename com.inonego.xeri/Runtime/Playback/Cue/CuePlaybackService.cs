/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CuePlaybackService.cs
수정일 : 2026-08-10

# 설명
명시적으로 조립된 Cue Player 중 Cue와 Runtime Binding 계약을 지원하는 Player를 선택해 실행한다.

# 적용 범위
ICuePlayback은 실제 재생 자원과 상태를 소유하고 호출자는 개별 종료를 제어한다.
Service는 Playback을 추적하고 Tick에서 Released 추적을 제거하며 전체 종료 경계를 제공한다.
Service는 Binding 의미와 Payload별 Unity 세부 동작을 소유하지 않는다.
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

                if (ContainsPlayerReference(player))
                {
                    throw new ArgumentException
                    (
                        "동일 Cue Player 인스턴스를 중복 등록할 수 없습니다.",
                        nameof(players)
                    );
                }

                this.players.Add(player);
            }
        }

    #endregion

    #region 재생

        // ------------------------------------------------------------
        /// <summary>
        /// 별도 Runtime Binding 없이 Cue를 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        public ICuePlayback Play(IPlaybackCue cue)
        {
            return Play
            (
                cue,
                NoCueBinding.Default
            );
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정 Runtime Binding을 지원하는 유일한 Player로 Cue를 실행한다.
        /// <br/> 같은 Cue와 Binding을 둘 이상의 Player가 지원하면 조립 오류로 거부한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public ICuePlayback Play<TBinding>
        (
            IPlaybackCue cue,
            in TBinding binding
        )
            where TBinding : ICueBinding
        {
            ValidateCue(cue);

            ICuePlayer<TBinding> selectedPlayer = null;

            for (var index = 0; index < players.Count; index++)
            {
                if (players[index] is not ICuePlayer<TBinding> candidate) continue;
                if (!candidate.CanPlay(cue, in binding)) continue;

                if (selectedPlayer != null)
                {
                    throw new InvalidOperationException
                    (
                        $"{cue.GetType().Name} Cue와 {typeof(TBinding).Name} Binding을 둘 이상의 Player가 처리합니다."
                    );
                }

                selectedPlayer = candidate;
            }

            if (selectedPlayer == null)
            {
                throw new InvalidOperationException
                (
                    $"{cue.GetType().Name} Cue와 {typeof(TBinding).Name} Binding을 처리할 Player가 없습니다."
                );
            }

            return TrackPlayback
            (
                selectedPlayer.Play(cue, in binding),
                selectedPlayer.GetType().Name
            );
        }

    #endregion

    #region 추적

        // ------------------------------------------------------------
        /// <summary>
        /// Player가 반환한 Playback을 검증하고 Service 추적 목록에 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        private ICuePlayback TrackPlayback
        (
            ICuePlayback playback,
            string playerName
        )
        {
            if (playback == null)
            {
                throw new InvalidOperationException
                (
                    $"{playerName}은(는) Cue Playback을 반환해야 합니다."
                );
            }

            playbacks.Add(playback);
            return playback;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Service가 추적하는 Playback 중 Released 항목을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Tick()
        {
            for (var index = playbacks.Count - 1; index >= 0; index--)
            {
                if (playbacks[index].State != CuePlaybackState.Released) continue;

                playbacks.RemoveAt(index);
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

            for (var index = playbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    playbacks[index].Dispose();
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
                throw new AggregateException
                (
                    "전체 Cue Playback 종료 중 하나 이상의 정리가 실패했습니다.",
                    errors
                );
            }
        }

    #endregion

    #region 검증

        // ------------------------------------------------------------
        /// <summary>
        /// 동일 Player 인스턴스가 이미 구성됐는지 참조 동일성으로 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool ContainsPlayerReference(ICuePlayer player)
        {
            for (var index = 0; index < players.Count; index++)
            {
                if (ReferenceEquals(players[index], player)) return true;
            }

            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Cue 인자를 공통 실행 전에 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ValidateCue(IPlaybackCue cue)
        {
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }
        }

    #endregion

    }
}
