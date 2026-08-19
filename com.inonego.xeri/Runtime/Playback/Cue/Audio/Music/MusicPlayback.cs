/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MusicPlayback.cs
수정일 : 2026-08-19

# 설명
같은 Timeline에 예약된 Music Layer Playback들을 하나의 집합 수명으로 제어한다.

# 종료 계약
실제 AudioSource 자원은 각 IAudioPlayback이 소유하며 이 Handle은 집합 제어만 담당한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// 동기 Music Layer Group의 Aggregate Playback Handle.
    /// </summary>
    // ============================================================
    internal sealed class MusicPlayback : IMusicPlayback
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Aggregate Playback 수명 상태.
        /// </summary>
        // ------------------------------------------------------------
        public CuePlaybackState State
        {
            get
            {
                var hasActive = false;
                var hasDraining = false;

                for (var i = 0; i < playbacks.Length; i++)
                {
                    var state = playbacks[i].State;

                    if (state == CuePlaybackState.Released) continue;

                    hasActive = true;
                    hasDraining |= state == CuePlaybackState.Draining;
                }

                if (!hasActive) return CuePlaybackState.Released;

                return hasDraining
                    ? CuePlaybackState.Draining
                    : CuePlaybackState.Playing;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 동기 Layer Group의 대표 재생 Clock.
        /// </summary>
        // ------------------------------------------------------------
        public IPlaybackClock Clock => playbacks[0].Clock;

        // ------------------------------------------------------------
        /// <summary>
        /// Group에 포함된 Layer 수.
        /// </summary>
        // ------------------------------------------------------------
        public int LayerCount => playbacks.Length;

        private readonly IAudioPlayback[] playbacks;

    #endregion

    #region 생성자

        // ----------------------------------------------------------------------
        /// <summary>
        /// 동기 시작된 Layer Playback 집합으로 Aggregate Handle을 생성한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal MusicPlayback(IAudioPlayback[] playbacks) : base()
        {
            if (playbacks == null)
            {
                throw new ArgumentNullException(nameof(playbacks));
            }

            if (playbacks.Length == 0)
            {
                throw new ArgumentException("Music Playback에는 하나 이상의 Layer가 필요합니다.", nameof(playbacks));
            }

            for (var i = 0; i < playbacks.Length; i++)
            {
                if (playbacks[i] == null)
                {
                    throw new ArgumentException("Music Playback Layer에 null Playback을 포함할 수 없습니다.", nameof(playbacks));
                }
            }

            this.playbacks = playbacks;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Layer의 현재 개별 Volume을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public float GetLayerVolume(int index)
        {
            ValidateLayerIndex(index);
            return playbacks[index].Volume;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Layer의 개별 Volume을 변경한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetLayerVolume(int index, float volume)
        {
            ValidateLayerIndex(index);
            playbacks[index].Volume = volume;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 Layer의 현재 위치를 보존하고 재생을 일시정지한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Pause()
        {
            for (var i = 0; i < playbacks.Length; i++)
            {
                playbacks[i].Pause();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 일시정지된 모든 Layer를 같은 Timeline에서 재개한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Resume()
        {
            for (var i = 0; i < playbacks.Length; i++)
            {
                playbacks[i].Resume();
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 지정 종료 방식으로 모든 Layer Playback의 수명을 함께 종료한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Stop(CueStopMode mode = CueStopMode.Immediate)
        {
            List<Exception> errors = null;
            // 한 Layer 정리 실패가 나머지 Layer 수명 종료를 막지 않도록 끝까지 시도한다.
            for (var i = 0; i < playbacks.Length; i++)
            {
                try
                {
                    playbacks[i].Stop(mode);
                }
                catch (Exception exception)
                {
                    errors ??= new();
                    errors.Add(exception);
                }
            }

            if (errors != null)
            {
                throw new AggregateException
                (
                    "Music Layer Playback 종료 중 하나 이상의 정리가 실패했습니다.",
                    errors
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 Layer Playback을 즉시 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            Stop(CueStopMode.Immediate);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Layer 인덱스가 현재 Group 범위 안인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ValidateLayerIndex(int index)
        {
            if (index < 0 || index >= playbacks.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

    #endregion

    }
}
