/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityVFXGraphCuePlayer.cs
수정일 : 2026-09-05

# 설명
UnityVFXGraphCue를 Pool에서 획득한 VisualEffect로 실행한다.

# 특이사항, 제약사항
TransformBinding_Fixed, TransformBinding_Tracked과 최초 Play 전 입력 준비가 필요한 VFXGraphBinding을 지원한다.
Binding 없는 임의 위치 fallback은 제공하지 않고 Prefab 내부 Graph와 Render Pipeline 자산은 변경하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.VFX;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity VFX Graph Cue Player.
    /// </summary>
    // ============================================================
    [DisallowMultipleComponent]
    public sealed class UnityVFXGraphCuePlayer : MonoBehaviour,
        ICuePlayer<TransformBinding_Fixed>,
        ICuePlayer<TransformBinding_Tracked>,
        ICuePlayer<VFXGraphBinding>
    {

    #region 구성

        [SerializeField]
        private Transform poolRoot = null;

    #endregion

    #region 재생 상태

        private readonly List<UnityVFXGraphPlayback> playbacks = new();

    #endregion

    #region 인터페이스 구현

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Cue를 고정 World Transform에서 재생할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool ICuePlayer<TransformBinding_Fixed>.CanPlay
        (
            IPlaybackCue cue,
            in TransformBinding_Fixed binding
        )
        {
            return cue is UnityVFXGraphCue && binding.IsValid;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Cue를 지정 World Transform에서 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        ICuePlayback ICuePlayer<TransformBinding_Fixed>.Play
        (
            IPlaybackCue cue,
            in TransformBinding_Fixed binding
        )
        {
            if (!binding.IsValid)
            {
                throw new ArgumentException
                (
                    "Fixed Transform Binding의 World TRS가 유효하지 않습니다.",
                    nameof(binding)
                );
            }

            var world = binding.World;
            return PlayVFX
            (
                RequireCue(cue),
                transformBinding: null,
                world.Position,
                world.Rotation,
                world.Scale,
                prepareVFX: null
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Cue를 Transform 추적 재생할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool ICuePlayer<TransformBinding_Tracked>.CanPlay
        (
            IPlaybackCue cue,
            in TransformBinding_Tracked binding
        )
        {
            return cue is UnityVFXGraphCue && binding.IsValid;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Cue를 지정 Transform을 따라가도록 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        ICuePlayback ICuePlayer<TransformBinding_Tracked>.Play
        (
            IPlaybackCue cue,
            in TransformBinding_Tracked binding
        )
        {
            if (!binding.IsValid)
            {
                throw new ArgumentException
                (
                    "Tracked Transform Binding의 Target과 Local TRS가 유효하지 않습니다.",
                    nameof(binding)
                );
            }

            var world = binding.World;
            return PlayVFX
            (
                RequireCue(cue),
                binding,
                world.Position,
                world.Rotation,
                world.Scale,
                prepareVFX: null
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 최초 Play 전 입력 준비가 필요한 VFX Graph Cue를 처리할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool ICuePlayer<VFXGraphBinding>.CanPlay
        (
            IPlaybackCue cue,
            in VFXGraphBinding binding
        )
        {
            return cue is UnityVFXGraphCue && binding.IsValid;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> VFX Graph를 추적 Transform에 배치하고 노출 입력을 준비한 뒤
        /// <br/> Reinit과 Play를 각각 한 번만 실행한다.
        /// </summary>
        // ----------------------------------------------------------------------
        ICuePlayback ICuePlayer<VFXGraphBinding>.Play
        (
            IPlaybackCue cue,
            in VFXGraphBinding binding
        )
        {
            if (!binding.IsValid)
            {
                throw new ArgumentException
                (
                    "VFX Graph Binding의 추적 Transform과 준비 동작이 유효해야 합니다.",
                    nameof(binding)
                );
            }

            var transformBinding = binding.Tracking;
            var world = transformBinding.World;
            return PlayVFX
            (
                RequireCue(cue),
                transformBinding,
                world.Position,
                world.Rotation,
                world.Scale,
                binding.PrepareVFX
            );
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> VFX Graph Playback을 획득하고 Transform·노출 입력을 모두 준비한 뒤 시작한다.
        /// <br/> 시작이 실패하면 공개되지 않은 Playback을 같은 호출 경계에서 Pool로 반환한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private ICuePlayback PlayVFX
        (
            UnityVFXGraphCue cue,
            TransformBinding_Tracked? transformBinding,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Action<VisualEffect> prepareVFX
        )
        {
            var playback = AcquirePlayback(cue, transformBinding);

            try
            {
                playback.Effect.transform.SetPositionAndRotation
                (
                    position,
                    rotation
                );
                playback.Effect.transform.localScale = scale;

                // 최초 Spawn이 완성된 입력을 읽도록 Graph 초기화 전에 이번 실행 값을 모두 기록한다.
                prepareVFX?.Invoke(playback.Effect);
                playback.Effect.pause = false;
                playback.Effect.Reinit();

                // 준비된 Graph에 시작 이벤트를 한 번만 보내 실제 출력 상태로 진입시킨다.
                playback.Effect.Play();
                playbacks.Add(playback);
                return playback;
            }
            catch (Exception startException)
            {
                try
                {
                    // 시작되지 않은 Playback도 획득한 Pool 소유권을 이 호출 경계에서 끝낸다.
                    playback.Dispose();
                }
                catch (Exception releaseException)
                {
                    // 반환 실패가 최초 시작 오류를 덮지 않도록 두 실패를 함께 전달한다.
                    throw new AggregateException
                    (
                        "VFX Graph 시작과 실패 Playback 반환이 모두 실패했습니다.",
                        startException,
                        releaseException
                    );
                }

                throw;
            }
        }

    #endregion

    #region 재생 수명

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 활성 VFX Graph Playback을 즉시 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        public void StopAll()
        {
            List<Exception> errors = null;

            // 역순 종료로 각 Playback이 자기 자신을 추적 목록에서 제거해도 순회를 보존한다.
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
                    "VFX Graph Playback 종료 중 하나 이상의 정리가 실패했습니다.",
                    errors
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Terminal Playback을 활성 추적 목록에서 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void ReleasePlayback(UnityVFXGraphPlayback playback)
        {
            playbacks.Remove(playback);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime Cue가 선택한 Variant Pool에서 Lease를 획득해 VFX Graph Playback을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private UnityVFXGraphPlayback AcquirePlayback
        (
            UnityVFXGraphCue cue,
            TransformBinding_Tracked? transformBinding
        )
        {
            // Variant 선택과 Pool 획득은 runtime Cue에 위임해 Player가 Pool 상태를 소유하지 않게 한다.
            var lease = cue.AcquireLease
            (
                transform,
                poolRoot,
                out _
            );

            try
            {
                return new UnityVFXGraphPlayback
                (
                    this,
                    lease,
                    transformBinding
                );
            }
            catch
            {
                // 공개되지 못한 Playback의 Pool 소유권을 같은 실패 경계에서 즉시 반환한다.
                lease.Dispose();
                throw;
            }
        }

    #endregion

    #region 검증

        // ------------------------------------------------------------
        /// <summary>
        /// 범용 Cue를 UnityVFXGraphCue로 검증해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static UnityVFXGraphCue RequireCue(IPlaybackCue cue)
        {
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            return cue as UnityVFXGraphCue ?? throw new ArgumentException
            (
                "UnityVFXGraphCuePlayer는 UnityVFXGraphCue만 재생할 수 있습니다.",
                nameof(cue)
            );
        }

    #endregion

    #region Unity 생명주기

        // ------------------------------------------------------------
        /// <summary>
        /// 활성 VFX Graph Playback의 Transform 추적과 자연 종료를 진행한다.
        /// </summary>
        // ------------------------------------------------------------
        private void LateUpdate()
        {
            for (var index = playbacks.Count - 1; index >= 0; index--)
            {
                playbacks[index].Tick();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비활성화에서 활성 VFX Graph Playback을 모두 즉시 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            StopAll();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 파괴 시 남은 활성 VFX Graph Playback을 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDestroy()
        {
            StopAll();
        }

    #endregion

    }
}
