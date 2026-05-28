/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SpawnRegistry.cs
수정일 : 2026-05-28

# 설명
스폰 레지스트리 베이스·구현체와 스폰된 객체 사전, 디스폰 확장 메서드 정의.

- SpawnRegistryUtility       : IDespawnable.Despawn() 확장 메서드
- SpawnedDictionary<,>        : XDictionary_VR 기반 스폰 사전
- SpawnRegistryBase<,>        : 핵심 스폰/디스폰/검색/이벤트/복제 로직
- SpawnRegistry<,>            : 파라미터 없는 Acquire/Spawn
- SpawnRegistry<,,>           : 파라미터 받는 Acquire/Spawn (INeedToInit<TParam> 연동)
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Game
{
    using Xeri.Serializable;

    // ============================================================
    /// <summary>
    /// IDespawnable 디스폰 확장 메서드.
    /// </summary>
    // ============================================================
    public static class SpawnRegistryUtility
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 객체를 디스폰한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Despawn(this IDespawnable despawnable)
        {
            if (despawnable != null)
            {
                if (despawnable.DespawnFromRegistry != null)
                {
                    despawnable.DespawnFromRegistry.Invoke();
                }
            }
        }
    }

    // ============================================================
    /// <summary>
    /// 스폰된 객체를 보관하는 직렬화 가능한 사전.
    /// </summary>
    // ============================================================
    [Serializable]
    public class SpawnedDictionary<TKey, T> : XDictionary_VR<TKey, T>, ISpawnedDictionary<TKey, T>
    where TKey : IEquatable<TKey>
    where T : class, ISpawnRegistryObject<TKey> {}

    // ============================================================
    /// <summary>
    /// 스폰을 관리하기 위한 베이스 클래스.
    /// </summary>
    // ============================================================
    [Serializable]
    public abstract class SpawnRegistryBase<TKey, T>
        : ISpawnRegistry<TKey, T>,
          IDeepCloneableFrom<SpawnRegistryBase<TKey, T>>
    where TKey : IEquatable<TKey>
    where T : class, ISpawnRegistryObject<TKey>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 스폰된 객체들의 사전(읽기 전용 노출).
        /// </summary>
        // ------------------------------------------------------------
        public ISpawnedDictionary<TKey, T> Spawned => spawned;

        [SerializeField, HideInInspector]
        private SpawnedDictionary<TKey, T> spawned = new();

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 객체가 스폰될 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TKey, T> OnSpawn = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 객체가 디스폰될 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TKey, T> OnDespawn = null;

    #endregion

    #region 깊은 복사

        // ------------------------------------------------------------
        /// <summary>
        /// source 의 스폰된 객체들을 깊은 복제하여 this 로 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloneFrom(SpawnRegistryBase<TKey, T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("SpawnRegistryBase<TKey, T>.CloneFrom()의 인자가 null입니다.");
            }

            spawned.Clear();

            foreach (var (key, entity) in source.spawned)
            {
                var cloned = entity is IDeepCloneable<T> cloneable ? cloneable.Clone() : entity;

                spawned.Add(key, cloned);
            }
        }

    #endregion

    #region 검색 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 해당 키를 가지는 객체를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        public T Find(TKey key)
        {
            return spawned.TryGetValue(key, out var value) ? value : null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 동일 키를 가지는 객체를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        public T Find(IKeyable<TKey> keyable)
        {
            if (keyable != null)
            {
                if (keyable.HasKey && spawned.TryGetValue(keyable.Key, out var value))
                {
                    return value;
                }
            }

            return null;
        }

    #endregion

    #region 자식 훅

        // ------------------------------------------------------------
        /// <summary>
        /// 자식 클래스에서 스폰 직전에 추가 작업을 수행하기 위한 훅.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnPreSpawnObject(T spawnable) {}

        // ------------------------------------------------------------
        /// <summary>
        /// 자식 클래스에서 스폰 직후에 추가 작업을 수행하기 위한 훅.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnSpawnObject(T spawnable) {}

        // ------------------------------------------------------------
        /// <summary>
        /// 자식 클래스에서 디스폰 직전에 추가 작업을 수행하기 위한 훅.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnPreDespawnObject(T despawnable) {}

        // ------------------------------------------------------------
        /// <summary>
        /// 자식 클래스에서 디스폰 직후에 추가 작업을 수행하기 위한 훅.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnDespawnObject(T despawnable) {}

    #endregion

    #region 스폰

        // ------------------------------------------------------------
        /// <summary>
        /// 공통 스폰 등록 로직을 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        protected T Spawn(T spawnable, Action<T> initAction = null)
        {
            if (spawnable == null)
            {
                throw new InvalidOperationException("스폰할 객체를 가져올 수 없습니다.");
            }

            if (spawnable.IsSpawned)
            {
                throw new InvalidOperationException("이미 스폰된 객체입니다.");
            }

            SpawnInternal(spawnable, initAction);

            OnSpawn?.Invoke(spawnable.Key, spawnable);

            return spawnable;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 스폰 내부 처리. 예외 발생 시 즉시 디스폰으로 롤백.
        /// </summary>
        // ------------------------------------------------------------
        private void SpawnInternal(T spawnable, Action<T> initAction = null)
        {
            try
            {
                OnPreSpawnObject(spawnable);
                spawnable.OnPreSpawn();

                if (initAction != null)
                {
                    initAction.Invoke(spawnable);
                }

                if (!spawnable.HasKey)
                {
                    throw new InvalidOperationException("스폰할 객체에 키가 설정되어 있지 않습니다.");
                }

                if (spawned.ContainsKey(spawnable.Key))
                {
                    throw new InvalidOperationException($"이미 동일 키({spawnable.Key})가 등록되어 있습니다.");
                }
            }
            catch (Exception)
            {
                // 스폰 중 예외가 발생하면 즉시 객체를 디스폰한다.
                DespawnInternal(spawnable);

                throw;
            }

            void DoDespawn() => Despawn(spawnable);

            spawnable.IsSpawned = true;
            spawnable.DespawnFromRegistry = DoDespawn;

            spawned.Add(spawnable.Key, spawnable);

            OnSpawnObject(spawnable);
            spawnable.OnSpawn();
        }

    #endregion

    #region 디스폰

        // ------------------------------------------------------------
        /// <summary>
        /// 공통 디스폰 로직을 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        protected void Despawn(T despawnable, bool removeFromDictionary = true)
        {
            if (despawnable == null)
            {
                throw new ArgumentNullException("디스폰할 객체를 설정해주세요.");
            }

            if (!despawnable.IsSpawned)
            {
                throw new InvalidOperationException("스폰되지 않은 객체를 디스폰할 수 없습니다.");
            }

            if (!despawnable.HasKey)
            {
                throw new InvalidOperationException("디스폰할 객체에 키가 설정되어 있지 않습니다.");
            }

            if (!spawned.ContainsKey(despawnable.Key))
            {
                throw new KeyNotFoundException($"등록되지 않은 키({despawnable.Key})에 해당하는 객체를 디스폰할 수 없습니다.");
            }

            OnDespawn?.Invoke(despawnable.Key, despawnable);

            DespawnInternal(despawnable, removeFromDictionary);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 디스폰 내부 처리. 사전 제거는 옵션으로 분리(DespawnAll 에서 일괄 처리용).
        /// </summary>
        // ----------------------------------------------------------------------
        private void DespawnInternal(T despawnable, bool removeFromDictionary = true)
        {
            despawnable.OnPreDespawn();
            OnPreDespawnObject(despawnable);

            if (removeFromDictionary)
            {
                if (despawnable.HasKey)
                {
                    spawned.Remove(despawnable.Key);
                }
            }

            despawnable.IsSpawned = false;
            despawnable.DespawnFromRegistry = null;

            despawnable.OnDespawn();
            OnDespawnObject(despawnable);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 객체를 디스폰한다.
        /// </summary>
        // ------------------------------------------------------------
        public void DespawnAll()
        {
            foreach (var (key, entity) in spawned)
            {
                try
                {
                    Despawn(entity, removeFromDictionary: false);
                }
                catch (Exception ex)
                {
                    // 일부 디스폰 실패해도 나머지는 계속 처리한다.
                #if UNITY_EDITOR
                    Debug.LogException(ex);
                #else
                    Debug.LogError(ex.Message);
                #endif
                }
            }

            spawned.Clear();
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 파라미터 없는 Acquire/Spawn 을 제공하는 스폰 레지스트리.
    /// </summary>
    /// <typeparam name="TKey">스폰된 객체의 키 타입.</typeparam>
    /// <typeparam name="T">스폰 가능한 객체의 타입.</typeparam>
    // ============================================================
    [Serializable]
    public abstract class SpawnRegistry<TKey, T> : SpawnRegistryBase<TKey, T>
    where TKey : IEquatable<TKey>
    where T : class, ISpawnRegistryObject<TKey>
    {

    #region 추상 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 새 객체를 풀·생성 등으로 가져온다. 자식이 구현.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract T Acquire();

    #endregion

    #region 스폰

        // ------------------------------------------------------------
        /// <summary>
        /// 객체를 스폰한다.
        /// </summary>
        // ------------------------------------------------------------
        protected T Spawn()
        {
            var spawnable = Acquire();

            Spawn(spawnable);

            return spawnable;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 객체를 스폰한다(외부 호출용).
        /// </summary>
        // ------------------------------------------------------------
        public virtual bool TrySpawn(out T spawned)
        {
            spawned = Spawn();

            return spawned != null;
        }

    #endregion

    }

    // =================================================================
    /// <summary>
    /// 파라미터를 받는 Acquire/Spawn 을 제공하는 스폰 레지스트리.
    /// </summary>
    /// <typeparam name="TKey">스폰된 객체의 키 타입.</typeparam>
    /// <typeparam name="T">스폰 가능한 객체의 타입.</typeparam>
    /// <typeparam name="TParam">스폰 시 전달할 매개변수 타입.</typeparam>
    // =================================================================
    [Serializable]
    public abstract class SpawnRegistry<TKey, T, TParam> : SpawnRegistryBase<TKey, T>
    where TKey : IEquatable<TKey>
    where T : class, ISpawnRegistryObject<TKey>, INeedToInit<TParam>
    {

    #region 자식 훅

        // ------------------------------------------------------------
        /// <summary>
        /// 자식 클래스의 Init 사전 작업 훅.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnInit(T spawnable, TParam param) {}

    #endregion

    #region 추상 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 새 객체를 가져온다. 자식이 구현.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract T Acquire(TParam param);

    #endregion

    #region 스폰

        // ------------------------------------------------------------
        /// <summary>
        /// 객체를 스폰한다.
        /// </summary>
        // ------------------------------------------------------------
        protected T Spawn(TParam param)
        {
            var spawnable = Acquire(param);

            void InitAction(T s)
            {
                OnInit(s, param);
                s.Init(param);
            }

            Spawn(spawnable, InitAction);

            return spawnable;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 객체를 스폰한다(외부 호출용).
        /// </summary>
        // ------------------------------------------------------------
        public virtual bool TrySpawn(TParam param, out T spawned)
        {
            spawned = Spawn(param);

            return spawned != null;
        }

    #endregion

    }
}
