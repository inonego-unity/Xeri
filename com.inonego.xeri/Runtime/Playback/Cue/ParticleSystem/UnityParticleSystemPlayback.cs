/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityParticleSystemPlayback.cs
수정일 : 2026-08-19

# 설명
Pool에서 실행 중인 Unity ParticleSystem Cue의 수명과 Transform 추적을 관리한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// 실행 중인 Unity ParticleSystem Cue Playback.
    /// </summary>
    // ============================================================
    public sealed class UnityParticleSystemPlayback : ICuePlayback
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
        /// Player 내부에서 배치·재생에 사용할 현재 ParticleSystem.
        /// </summary>
        // ------------------------------------------------------------
        internal ParticleSystem Particle => particle;

        private UnityParticleSystemCuePlayer owner = null;
        private UnityParticleSystemCue cue = null;
        private ParticleSystem particle = null;
        private TransformBinding? transformBinding = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Player가 획득한 Particle과 선택적 추적 Transform으로 Playback을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        internal UnityParticleSystemPlayback
        (
            UnityParticleSystemCuePlayer owner,
            UnityParticleSystemCue cue,
            ParticleSystem particle,
            TransformBinding? transformBinding
        )
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.cue = cue ?? throw new ArgumentNullException(nameof(cue));
            this.particle = particle ?? throw new ArgumentNullException(nameof(particle));
            this.transformBinding = transformBinding;
        }

    #endregion

    #region 메서드

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Immediate는 Particle을 즉시 정리한다.
        /// <br/> Natural은 새 방출을 멈추고 기존 Particle이 사라질 때까지 Draining한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void Stop(CueStopMode mode = CueStopMode.Immediate)
        {
            if (State == CuePlaybackState.Released) return;

            if (mode == CueStopMode.Natural)
            {
                if (State == CuePlaybackState.Draining) return;

                State = CuePlaybackState.Draining;
                particle.Stop
                (
                    withChildren: true,
                    ParticleSystemStopBehavior.StopEmitting
                );
                return;
            }

            Release();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Playback을 즉시 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            Stop();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Transform 추적과 Particle 자연 종료를 한 Frame 진행한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Tick()
        {
            if (State == CuePlaybackState.Released) return;

            if (transformBinding.HasValue)
            {
                var binding = transformBinding.Value;

                if (!binding.IsValid)
                {
                    Release();
                    return;
                }

                particle.transform.SetPositionAndRotation
                (
                    binding.Position,
                    binding.Rotation
                );
                particle.transform.localScale = binding.Scale;
            }

            if (particle.IsAlive(withChildren: true)) return;

            Release();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Terminal 상태를 먼저 확정한 뒤 Particle을 Player Pool에 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Release()
        {
            if (State == CuePlaybackState.Released) return;

            var releaseOwner = owner;
            var releaseCue = cue;
            var releaseParticle = particle;

            State = CuePlaybackState.Released;
            owner = null;
            cue = null;
            particle = null;
            transformBinding = null;

            releaseOwner?.ReleasePlayback
            (
                this,
                releaseCue,
                releaseParticle
            );
        }

    #endregion

    }
}
