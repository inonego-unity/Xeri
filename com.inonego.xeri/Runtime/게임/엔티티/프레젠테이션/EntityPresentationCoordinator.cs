/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EntityPresentationCoordinator.cs
수정일 : 2026-08-14

# 설명
여러 Entity Presentation Provider의 정순 생성과 역순 rollback·release를 조정한다.
concrete Presentation 인스턴스와 Registry, Pool, 독립 Despawn 정책은 소유하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Game
{
    // ================================================================================
    /// <summary>
    /// 여러 Presentation Provider의 View 수명 transaction을 조정한다.
    /// </summary>
    // ================================================================================
    [Serializable]
    public sealed class EntityPresentationCoordinator<TEntityView, TEntity>
    where TEntityView : EntityViewBase<TEntity>
    where TEntity : class, IEntity
    {

    #region 필드

        private readonly List<IEntityPresentationProvider<TEntityView, TEntity>> providers = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 조립된 Presentation Provider 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<IEntityPresentationProvider<TEntityView, TEntity>> Providers => providers;

    #endregion

    #region Provider 조립

        // ------------------------------------------------------------
        /// <summary>
        /// Presentation Provider를 실행 순서의 마지막에 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Register(IEntityPresentationProvider<TEntityView, TEntity> provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            for (var index = 0; index < providers.Count; index++)
            {
                if (ReferenceEquals(providers[index], provider))
                {
                    throw new InvalidOperationException("동일 Presentation Provider를 중복 등록할 수 없습니다.");
                }
            }

            providers.Add(provider);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 동일 Presentation Provider 등록을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Unregister(IEntityPresentationProvider<TEntityView, TEntity> provider)
        {
            for (var index = providers.Count - 1; index >= 0; index--)
            {
                if (!ReferenceEquals(providers[index], provider))
                {
                    continue;
                }

                providers.RemoveAt(index);
                return true;
            }

            return false;
        }

    #endregion

    #region View 수명

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 모든 Provider를 정순 실행한다.
        /// <br/> 실패 시 완료된 Provider만 역순 rollback하고 모든 오류를 전달한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Spawn(TEntity entity, TEntityView view)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            var completed = 0;

            try
            {
                for (var index = 0; index < providers.Count; index++)
                {
                    providers[index].OnViewSpawned(entity, view);
                    completed++;
                }
            }
            catch (Exception spawnException)
            {
                List<Exception> failures = null;

                for (var index = completed - 1; index >= 0; index--)
                {
                    try
                    {
                        providers[index].OnViewReleasing
                        (
                            entity.Key,
                            view,
                            DespawnReason.SpawnRollback
                        );
                    }
                    catch (Exception rollbackException)
                    {
                        failures ??= new List<Exception>();
                        failures.Add(rollbackException);
                    }
                }

                if (failures == null)
                {
                    throw;
                }

                failures.Insert(0, spawnException);
                throw new AggregateException("Presentation 생성과 rollback 중 오류가 발생했습니다.", failures);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 Provider의 View 분리 처리를 역순으로 끝까지 시도한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Release
        (
            ulong entityKey,
            TEntityView view,
            DespawnReason reason
        )
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            List<Exception> failures = null;

            for (var index = providers.Count - 1; index >= 0; index--)
            {
                try
                {
                    providers[index].OnViewReleasing(entityKey, view, reason);
                }
                catch (Exception exception)
                {
                    failures ??= new List<Exception>();
                    failures.Add(exception);
                }
            }

            if (failures == null) return;

            throw failures.Count == 1
                ? failures[0]
                : new AggregateException("일부 Presentation Provider 반환에 실패했습니다.", failures);
        }

    #endregion

    }
}
