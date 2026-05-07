/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ISpawnRegistry.cs
수정일 : 2026-05-07

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
        /// 객체가 스폰될 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TKey, T> OnSpawn;

        // ------------------------------------------------------------
        /// <summary>
        /// 객체가 디스폰될 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TKey, T> OnDespawn;
    }
}
