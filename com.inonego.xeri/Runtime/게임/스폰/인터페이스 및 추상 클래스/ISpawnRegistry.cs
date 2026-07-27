/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ISpawnRegistry.cs
수정일 : 2026-07-27

# 설명
스폰 레지스트리 인터페이스 및 스폰된 객체를 보관하는 사전 인터페이스.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// 스폰된 객체를 키로 보관하는 사전 인터페이스.
    /// </summary>
    // ============================================================
    public interface ISpawnedDictionary<TKey, T> : IReadOnlyDictionary<TKey, T>
    where TKey : IEquatable<TKey>
    where T : class, ISpawnRegistryObject<TKey> {}

    // ============================================================
    /// <summary>
    /// 스폰 레지스트리 인터페이스.
    /// </summary>
    /// <typeparam name="TKey">스폰된 객체의 키 타입.</typeparam>
    /// <typeparam name="T">스폰 가능한 객체의 타입.</typeparam>
    // ============================================================
    public interface ISpawnRegistry<TKey, T>
    where TKey : IEquatable<TKey>
    where T : class, ISpawnRegistryObject<TKey>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 현재 스폰된 객체들의 사전.
        /// </summary>
        // ------------------------------------------------------------
        public ISpawnedDictionary<TKey, T> Spawned { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 해당 키를 가지는 객체를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        public T Find(TKey key);

        // ------------------------------------------------------------
        /// <summary>
        /// 동일 키를 가지는 객체를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        public T Find(IKeyable<TKey> keyable);

        // ------------------------------------------------------------
        /// <summary>
        /// 객체가 Spawning 상태로 진입한 뒤 Registry 등록 전에 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TKey, T> OnSpawning;

        // ------------------------------------------------------------
        /// <summary>
        /// 객체의 Registry 등록과 Spawned 상태 전환이 완료된 뒤 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TKey, T> OnSpawned;

        // ------------------------------------------------------------
        /// <summary>
        /// 객체가 등록 해제되기 직전에 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TKey, T, DespawnReason> OnDespawning;

        // ------------------------------------------------------------
        /// <summary>
        /// 객체의 등록 해제와 상태 전환이 완료된 뒤 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TKey, T, DespawnReason> OnDespawned;
    }
}
