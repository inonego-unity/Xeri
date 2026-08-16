/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EntityPresentationRegistry.cs
수정일 : 2026-08-16

# 설명
Entity Key와 0..N Presentation 인스턴스의 조회 관계와 등록 수명 알림을 관리한다.
구체 Presentation의 생성·Bind·Despawn·Pool 수명은 소유하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Game
{
    // ==========================================================================================
    /// <summary>
    /// Entity Presentation Registry의 단일 등록·해제 사실을 전달하는 이벤트 인자.
    /// </summary>
    /// <typeparam name="TPresentation">Registry가 관리하는 Presentation 기준 타입.</typeparam>
    // ==========================================================================================
    public sealed class EntityPresentationRegistryEventArgs<TPresentation> : EventArgs
        where TPresentation : class
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Presentation이 연결된 Entity Key.
        /// </summary>
        // ------------------------------------------------------------
        public ulong EntityKey { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 등록 또는 해제된 Presentation 인스턴스.
        /// </summary>
        // ------------------------------------------------------------
        public TPresentation Presentation { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Entity Key와 Presentation으로 이벤트 인자를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public EntityPresentationRegistryEventArgs
        (
            ulong entityKey,
            TPresentation presentation
        )
        {
            EntityKey = entityKey;
            Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        }

    #endregion

    }

    // ================================================================================
    /// <summary>
    /// Entity Key 기준으로 Presentation 인스턴스를 등록하고 조회하는 공용 Registry.
    /// </summary>
    // ================================================================================
    [Serializable]
    public sealed class EntityPresentationRegistry<TPresentation>
    where TPresentation : class
    {

    #region 필드

        private readonly Dictionary<ulong, List<TPresentation>> presentations = new();
        private readonly Dictionary<TPresentation, ulong> keys = new
        (
            ReferenceEqualityComparer<TPresentation>.Instance
        );

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 등록된 Presentation 인스턴스 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => keys.Count;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Registry에 등록된 모든 Presentation 인스턴스를 열거한다.
        /// </summary>
        // ------------------------------------------------------------
        public IEnumerable<TPresentation> Presentations => keys.Keys;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Presentation 등록이 완료된 뒤 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<EntityPresentationRegistryEventArgs<TPresentation>> OnRegister = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Presentation 등록 해제가 완료된 뒤 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<EntityPresentationRegistryEventArgs<TPresentation>> OnUnregister = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 하나 이상의 등록 항목을 전체 정리한 뒤 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler OnClear = null;

    #endregion

    #region 등록

        // ------------------------------------------------------------
        /// <summary>
        /// Entity Key에 Presentation 인스턴스를 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Register(ulong entityKey, TPresentation presentation)
        {
            if (presentation == null)
            {
                throw new ArgumentNullException(nameof(presentation));
            }

            if (keys.ContainsKey(presentation))
            {
                throw new InvalidOperationException("이미 Registry에 등록된 Presentation입니다.");
            }

            if (!presentations.TryGetValue(entityKey, out var values))
            {
                values = new List<TPresentation>();
                presentations.Add(entityKey, values);
            }

            values.Add(presentation);
            keys.Add(presentation, entityKey);

            OnRegister?.Invoke
            (
                this,
                new EntityPresentationRegistryEventArgs<TPresentation>
                (
                    entityKey,
                    presentation
                )
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 동일 Presentation 인스턴스의 Registry 등록을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Unregister(TPresentation presentation)
        {
            if (presentation == null || !keys.TryGetValue(presentation, out var entityKey))
            {
                return false;
            }

            keys.Remove(presentation);

            if (presentations.TryGetValue(entityKey, out var values))
            {
                for (var index = values.Count - 1; index >= 0; index--)
                {
                    if (!ReferenceEquals(values[index], presentation)) continue;

                    values.RemoveAt(index);
                    break;
                }

                if (values.Count == 0)
                {
                    presentations.Remove(entityKey);
                }
            }

            OnUnregister?.Invoke
            (
                this,
                new EntityPresentationRegistryEventArgs<TPresentation>
                (
                    entityKey,
                    presentation
                )
            );
            return true;
        }

    #endregion

    #region 검색

        // ------------------------------------------------------------
        /// <summary>
        /// Entity Key에 대응하는 모든 Presentation을 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<TPresentation> FindAll(ulong entityKey)
        {
            return presentations.TryGetValue(entityKey, out var values)
                ? values
                : Array.Empty<TPresentation>();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Entity Key에 대응하는 지정 Presentation 타입 하나를 조회한다.
        /// <br/> 같은 타입이 둘 이상이면 cardinality 오류를 전달한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool TryFindSingle<TConcrete>
        (
            ulong entityKey,
            out TConcrete presentation
        )
        where TConcrete : class
        {
            presentation = null;

            if (!presentations.TryGetValue(entityKey, out var values))
            {
                return false;
            }

            for (var index = 0; index < values.Count; index++)
            {
                if (values[index] is not TConcrete candidate)
                {
                    continue;
                }

                if (presentation != null)
                {
                    throw new InvalidOperationException
                    (
                        $"Entity Key {entityKey}에 {typeof(TConcrete).Name} Presentation이 둘 이상 등록되어 있습니다."
                    );
                }

                presentation = candidate;
            }

            return presentation != null;
        }

    #endregion

    #region 정리

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 조회 매핑을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear()
        {
            if (keys.Count == 0) return;

            presentations.Clear();
            keys.Clear();
            OnClear?.Invoke(this, EventArgs.Empty);
        }

    #endregion

    }
}
