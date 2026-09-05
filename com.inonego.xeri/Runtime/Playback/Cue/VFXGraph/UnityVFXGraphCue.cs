/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityVFXGraphCue.cs
수정일 : 2026-09-05

# 설명
UnityVFXGraphCueAsset의 Variant 선택 상태와 Variant별 VisualEffect Pool을 소유하는 runtime Cue를 제공한다.
Pool은 최초 Player 사용 시 Host Transform에 bind되고 Playback은 획득 Lease를 직접 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.VFX;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Pool;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity VFX Graph 기반 runtime Visual Cue.
    /// </summary>
    // ============================================================
    public sealed class UnityVFXGraphCue : VisualCue
    {

    #region 필드

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 이 runtime Cue를 생성한 authoring Asset.
        /// <br/> 단일 Variant 직접 구성에서는 null이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public UnityVFXGraphCueAsset Asset => asset;

        private readonly UnityVFXGraphCueAsset asset = null;
        private readonly UnityVFXGraphCueVariant standaloneVariant = null;
        private GOCompPool<VisualEffect>[] pools = null;
        private Transform poolParent = null;
        private Transform poolRoot = null;
        private int activeLeaseCount = 0;
        private bool isDisposeRequested = false;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 프로그램에서 직접 구성한 단일 Variant runtime Cue를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public UnityVFXGraphCue(UnityVFXGraphCueVariant variant)
            : base(excludePrevious: false)
        {
            standaloneVariant = variant ?? throw new ArgumentNullException(nameof(variant));
            pools = new GOCompPool<VisualEffect>[1];
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Asset의 Variant와 선택 정책을 사용하는 runtime Cue를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        internal UnityVFXGraphCue(UnityVFXGraphCueAsset asset)
            : base(asset != null ? asset.ExcludePrevious : false)
        {
            this.asset = asset ?? throw new ArgumentNullException(nameof(asset));
            pools = new GOCompPool<VisualEffect>[asset.VariantCount];
        }

    #endregion

    #region Variant 획득

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 이번 재생 Variant를 선택하고 대응 Pool에서 VisualEffect Lease를 획득한다.
        /// <br/> 반환 Lease는 Cue의 활성 Lease 수명과 원본 Pool 반환을 함께 끝낸다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal Lease<VisualEffect> AcquireLease
        (
            Transform parent,
            Transform releasedRoot,
            out UnityVFXGraphCueVariant variant
        )
        {
            ThrowIfDisposed();

            // Variant와 Player Host를 먼저 확정해 같은 index의 Pool을 일관되게 사용한다.
            var variantIndex = SelectVariant(out variant);
            EnsurePoolHost(parent, releasedRoot);
            var pool = GetOrCreatePool(variantIndex, variant);

            // Playback에는 원본 Pool Lease를 감싼 일회 반환 책임만 노출한다.
            var poolLease = pool.AcquireLease(worldPositionStays: false);
            activeLeaseCount++;

            return new Lease<VisualEffect>
            (
                poolLease.Value,
                () => ReleaseLease(poolLease)
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이번 재생에 사용할 VFX Graph Variant와 그 인덱스를 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        private int SelectVariant(out UnityVFXGraphCueVariant variant)
        {
            // 직접 구성된 Cue는 선택 대상이 하나뿐이므로 index 0을 사용한다.
            if (asset == null)
            {
                variant = standaloneVariant;
                return 0;
            }

            // Asset 기반 Cue는 최소 한 개 Variant를 authoring해야 선택을 진행할 수 있다.
            if (asset.VariantCount <= 0)
            {
                throw new InvalidOperationException
                (
                    $"Unity VFX Graph Cue Asset '{asset.name}'에 하나 이상의 Variant가 필요합니다."
                );
            }

            var index = SelectVariantIndex(asset.VariantCount);
            variant = asset.GetVariant(index) ?? throw new InvalidOperationException
            (
                $"Unity VFX Graph Cue Asset '{asset.name}'의 Variant {index}가 비어 있습니다."
            );
            return index;
        }

    #endregion

    #region 풀 연결과 생성

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Pool이 사용할 활성 Parent와 released Root를 최초 Player Host에 연결한다.
        /// <br/> 살아 있는 다른 Host로 같은 runtime Cue를 공유하는 호출은 거부한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void EnsurePoolHost(Transform parent, Transform releasedRoot)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var resolvedRoot = releasedRoot != null ? releasedRoot : parent;

            // 이미 연결된 살아 있는 Host는 동일한 Parent와 Root에서만 재사용할 수 있다.
            if (poolParent != null)
            {
                if (poolParent != parent || poolRoot != resolvedRoot)
                {
                    throw new InvalidOperationException
                    (
                        "하나의 UnityVFXGraphCue runtime 인스턴스를 서로 다른 살아있는 Player Host에 공유할 수 없습니다."
                    );
                }

                return;
            }

            // 이전 Host가 파괴된 뒤 다시 사용되는 경우 stale Pool 참조를 버리고 새 Host에 연결한다.
            ReleasePools();
            poolParent = parent;
            poolRoot = resolvedRoot;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 지정 Variant 인덱스에 대응하는 VisualEffect Pool을 조회하거나 최초 생성한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private GOCompPool<VisualEffect> GetOrCreatePool
        (
            int variantIndex,
            UnityVFXGraphCueVariant variant
        )
        {
            var existing = pools[variantIndex];
            if (existing != null) return existing;

            if (variant?.Prefab == null)
            {
                throw new MissingReferenceException
                (
                    $"Unity VFX Graph Cue Variant {variantIndex}에 VisualEffect Prefab이 필요합니다."
                );
            }

            // Released 인스턴스는 Variant별 Container에 모아 Pool 소유 범위를 분리한다.
            var releasedContainer = new GameObject
            (
                $"{GetCueName()}_Variant{variantIndex}_Pool"
            ).transform;
            releasedContainer.SetParent(poolRoot, false);

            // Acquired 인스턴스는 Player Host를 Parent로 사용하고 released 상태만 별도 Container로 보낸다.
            var provider = new PrefabGameObjectProvider
            (
                variant.Prefab.gameObject,
                poolParent
            );
            var created = new GOCompPool<VisualEffect>(provider)
            {
                Parent = poolParent,
                Pool = releasedContainer,
            };
            pools[variantIndex] = created;
            return created;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Pool Container 이름에 사용할 Cue 식별 문자열을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private string GetCueName() =>
            asset != null ? asset.name : nameof(UnityVFXGraphCue);

    #endregion

    #region 정리

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 이 runtime Cue의 추가 획득을 막고 소유 Pool 정리를 요청한다.
        /// <br/> 활성 Lease가 남아 있으면 마지막 Lease 반환 뒤 Pool을 정리한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public override void Dispose()
        {
            if (isDisposeRequested) return;

            isDisposeRequested = true;

            if (activeLeaseCount == 0)
            {
                ReleasePools();
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 원본 Pool Lease를 반환하고 Cue의 활성 Lease 수명을 갱신한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ReleaseLease(Lease<VisualEffect> poolLease)
        {
            try
            {
                poolLease.Dispose();
            }
            finally
            {
                activeLeaseCount--;

                // Dispose 요청 뒤 마지막 Playback이 반환되면 남은 Pool 자원을 함께 정리한다.
                if (isDisposeRequested && activeLeaseCount == 0)
                {
                    ReleasePools();
                }
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 생성된 Variant Pool Container와 Host 연결 상태를 정리한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ReleasePools()
        {
            if (pools == null) return;

            for (var index = 0; index < pools.Length; index++)
            {
                var releasedContainer = pools[index]?.Pool;

                if (releasedContainer != null)
                {
                    DestroyGameObject(releasedContainer.gameObject);
                }

                pools[index] = null;
            }

            poolParent = null;
            poolRoot = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 실행 환경에 맞는 Unity 파괴 API로 Pool Container를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void DestroyGameObject(GameObject gameObject)
        {
            if (gameObject == null) return;

            // Play Mode에서는 Frame 종료 파괴를 사용해 Unity 생명주기 규칙을 따른다.
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(gameObject);
                return;
            }

            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Dispose 요청 이후 새 Playback 획득을 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ThrowIfDisposed()
        {
            if (!isDisposeRequested) return;

            throw new ObjectDisposedException(nameof(UnityVFXGraphCue));
        }

    #endregion

    }
}
