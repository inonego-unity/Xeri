/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityParticleSystemPlayback.cs
수정일 : 2026-09-05

# 설명
Pool Lease로 실행 중인 Unity ParticleSystem Cue의 수명과 Transform 추적을 관리한다.
자연 종료나 명시적 종료에서 Lease를 Dispose해 획득 원본 Pool로 자동 반환한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// 한 번 획득한 ParticleSystem Cue 재생의 실행 수명.
    /// </summary>
    // ============================================================
    public sealed class UnityParticleSystemPlayback : ICuePlayback
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Cue Playback 상태.
        /// </summary>
        // ------------------------------------------------------------
        public CuePlaybackState State { get; private set; } = CuePlaybackState.Playing;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 재생 중인 ParticleSystem 인스턴스.
        /// </summary>
        // ------------------------------------------------------------
        internal ParticleSystem Particle => particle;

        private ParticleSystem particle = null;
        private UnityParticleSystemCuePlayer owner = null;
        private Lease<ParticleSystem> lease = null;
        private TransformBinding_Tracked? transformBinding = null;

    #endregion

    #region 생성자

        // ----------------------------------------------------------------------
        /// <summary>
        /// Pool Lease와 선택적 Transform 추적 Binding으로 Playback을 생성한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal UnityParticleSystemPlayback
        (
            UnityParticleSystemCuePlayer owner,
            Lease<ParticleSystem> lease,
            TransformBinding_Tracked? transformBinding
        )
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.lease = lease ?? throw new ArgumentNullException(nameof(lease));
            particle = lease.Value ?? throw new ArgumentException
            (
                "ParticleSystem Lease에는 유효한 ParticleSystem이 필요합니다.",
                nameof(lease)
            );
            this.transformBinding = transformBinding;
        }

    #endregion

    #region 종료

        // ----------------------------------------------------------------------
        /// <summary>
        /// 지정 종료 방식으로 ParticleSystem 재생 종료를 시작하거나 즉시 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Stop(CueStopMode mode = CueStopMode.Immediate)
        {
            if (State == CuePlaybackState.Released) return;

            // Natural 종료는 새 방출만 막고 기존 Particle이 사라질 때까지 Playback을 유지한다.
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
        /// Playback을 즉시 종료하고 획득 Lease를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            Stop();
        }

    #endregion

    #region 진행

        // ----------------------------------------------------------------------
        /// <summary>
        /// Transform 추적을 갱신하고 Particle이 모두 끝나면 Playback을 자동 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal void Tick()
        {
            if (State == CuePlaybackState.Released) return;

            // Tracked Binding이 있으면 매 Frame 현재 World TRS를 재생 인스턴스에 반영한다.
            if (transformBinding.HasValue)
            {
                var binding = transformBinding.Value;

                if (!binding.IsValid)
                {
                    Release();
                    return;
                }

                var world = binding.World;
                particle.transform.SetPositionAndRotation
                (
                    world.Position,
                    world.Rotation
                );
                particle.transform.localScale = world.Scale;
            }

            // Root와 Child Particle이 모두 끝난 시점이 Pool 반환 경계다.
            if (particle.IsAlive(withChildren: true)) return;

            Release();
        }

    #endregion

    #region 반환

        // ----------------------------------------------------------------------
        /// <summary>
        /// 재생 인스턴스를 정지하고 Lease 반환과 Player 추적 해제를 한 번만 수행한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void Release()
        {
            if (State == CuePlaybackState.Released) return;

            var releaseOwner = owner;
            var releaseLease = lease;
            var releaseParticle = particle;

            // 외부 정리 중 예외가 발생해도 재진입하지 않도록 terminal 상태를 먼저 확정한다.
            State = CuePlaybackState.Released;
            owner = null;
            lease = null;
            particle = null;
            transformBinding = null;

            List<Exception> failures = null;

            try
            {
                releaseParticle?.Stop
                (
                    withChildren: true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }
            catch (Exception exception)
            {
                failures ??= new();
                failures.Add(exception);
            }

            // Lease가 기억한 원본 Pool로 인스턴스를 반환한다.
            try
            {
                releaseLease?.Dispose();
            }
            catch (Exception exception)
            {
                failures ??= new();
                failures.Add(exception);
            }
            finally
            {
                // Pool 반환 성공 여부와 무관하게 Player의 활성 추적에서는 terminal Playback을 제거한다.
                releaseOwner?.ReleasePlayback(this);
            }

            if (failures != null)
            {
                throw new AggregateException
                (
                    "ParticleSystem Playback 반환 중 하나 이상의 정리가 실패했습니다.",
                    failures
                );
            }
        }

    #endregion

    }
}
