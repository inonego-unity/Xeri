/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EntityViewFactory.cs
수정일 : 2026-05-28

# 설명
Entity view GameObject 획득/회수와 EntityViewBase 컴포넌트 초기화를 담당한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// Entity에 대응하는 Unity view를 생성하고 회수한다.
    /// </summary>
    // ============================================================
    [Serializable]
    public class EntityViewFactory<TEntityView, TEntity>
    where TEntityView : EntityViewBase<TEntity>
    where TEntity : class, IEntity
    {

    #region 필드

        private readonly IGameObjectProvider provider = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// GameObject provider를 주입받는다.
        /// </summary>
        // ------------------------------------------------------------
        public EntityViewFactory(IGameObjectProvider provider) : base()
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Entity에 대응하는 view를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual TEntityView Create(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var go = provider.Acquire(worldPositionStays: false);

            if (go == null)
            {
                throw new InvalidOperationException("Entity view GameObject를 가져올 수 없습니다.");
            }

            if (!go.TryGetComponent(out TEntityView view))
            {
                provider.Release(go, worldPositionStays: false);

                throw new NullReferenceException($"게임 오브젝트에서 Entity view 컴포넌트({typeof(TEntityView).Name})를 찾을 수 없습니다.");
            }

            view.Init(entity);
            view.OnPreSpawn();

            go.SetActive(true);

            view.OnSpawn();

            return view;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Entity view를 회수한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void Release(TEntityView view)
        {
            if (view == null)
            {
                return;
            }

            view.OnPreDespawn();

            var go = view.gameObject;
            go.SetActive(false);

            provider.Release(go, worldPositionStays: false);

            view.OnDespawn();
        }

    #endregion

    }
}
