/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EntityViewRegistry.cs
수정일 : 2026-08-14

# 설명
Entity Key와 EntityViewBase View의 대응 관계만 관리하는 View 매핑.
GameObject 생성/회수와 EntitySpawnRegistry 이벤트 연결은 담당하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// Entity Key에 대응하는 Entity View를 저장하고 조회한다.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class EntityViewRegistry<TEntityView, TEntity>
    where TEntityView : EntityViewBase<TEntity>
    where TEntity : class, IEntity
    {

    #region 필드

        private readonly Dictionary<ulong, TEntityView> views = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 등록된 View 매핑.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyDictionary<ulong, TEntityView> Views => views;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 등록된 View 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => views.Count;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Key에 View를 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Register(ulong key, TEntityView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (views.ContainsKey(key))
            {
                throw new InvalidOperationException($"이미 동일 key({key})에 대응하는 Entity view가 등록되어 있습니다.");
            }

            views.Add(key, view);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Key에 대응하는 View 등록을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Unregister(ulong key)
        {
            return views.Remove(key);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Key에 대응하는 View를 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public TEntityView Find(ulong key)
        {
            return views.TryGetValue(key, out var view) ? view : null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Key에 대응하는 View를 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryFind(ulong key, out TEntityView view)
        {
            return views.TryGetValue(key, out view);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Key 등록 여부를 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Contains(ulong key)
        {
            return views.ContainsKey(key);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 등록을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear()
        {
            views.Clear();
        }

    #endregion

    }
}
