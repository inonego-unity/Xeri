/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityVFXGraphPlayback.cs
수정일 : 2026-08-17

# 설명
Pool에서 실행 중인 Unity VFX Graph Cue의 수명과 Transform 추적을 관리한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.VFX;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// 실행 중인 Unity VFX Graph Cue Playback.
    /// </summary>
    // ============================================================
    public sealed class UnityVFXGraphPlayback : ICuePlayback
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
        /// Player 내부에서 초기화·배치에 사용할 현재 VisualEffect.
        /// </summary>
        // ------------------------------------------------------------
        internal VisualEffect Effect => effect;

        private UnityVFXGraphCuePlayer owner = null;
        private UnityVFXGraphCue cue = null;
        private VisualEffect effect = null;
        private Transform emitter = null;
        private bool isAwaitingInitSimulation = true;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Player가 획득한 VisualEffect와 선택적 추적 Transform으로 Playback을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        internal UnityVFXGraphPlayback
        (
            UnityVFXGraphCuePlayer owner,
            UnityVFXGraphCue cue,
            VisualEffect effect,
            Transform emitter
        )
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.cue = cue ?? throw new ArgumentNullException(nameof(cue));
            this.effect = effect ?? throw new ArgumentNullException(nameof(effect));
            this.emitter = emitter;
        }

    #endregion

    #region 메서드

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Immediate는 VisualEffect 출력을 즉시 종료하고 Pool에 반환한다.
        /// <br/> Natural은 Spawn 정지를 요청하고 VFX Graph System이 모두 Sleep할 때까지 Draining한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void Stop(CueStopMode mode = CueStopMode.Immediate)
        {
            if (State == CuePlaybackState.Released) return;

            if (mode == CueStopMode.Natural)
            {
                if (State == CuePlaybackState.Draining) return;

                State = CuePlaybackState.Draining;
                effect.Stop();
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
        /// Transform 추적과 VFX Graph 자연 종료를 한 Frame 진행한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Tick()
        {
            if (State == CuePlaybackState.Released) return;

            if (!ReferenceEquals(emitter, null))
            {
                if (emitter == null)
                {
                    Release();
                    return;
                }

                effect.transform.SetPositionAndRotation
                (
                    emitter.position,
                    emitter.rotation
                );
            }

            var hasActiveSystem = effect.HasAnySystemAwake();

            // Play 직후 Graph가 실제 활성 상태를 보고할 때까지는 자연 종료를 판정하지 않는다.
            if (isAwaitingInitSimulation)
            {
                if (!hasActiveSystem) return;

                isAwaitingInitSimulation = false;
            }

            if (hasActiveSystem) return;

            Release();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Terminal 상태를 먼저 확정한 뒤 VisualEffect를 Player Pool에 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Release()
        {
            if (State == CuePlaybackState.Released) return;

            var releaseOwner = owner;
            var releaseCue = cue;
            var releaseEffect = effect;

            State = CuePlaybackState.Released;
            owner = null;
            cue = null;
            effect = null;
            emitter = null;

            releaseOwner?.ReleasePlayback
            (
                this,
                releaseCue,
                releaseEffect
            );
        }

    #endregion

    }
}
