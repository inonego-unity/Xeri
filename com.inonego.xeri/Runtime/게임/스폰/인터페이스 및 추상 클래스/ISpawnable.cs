/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ISpawnable.cs
수정일 : 2026-07-27

# 설명
스폰 시스템 기본 인터페이스 모음.

- ISpawnRegistryObject<TKey> : SpawnRegistry 등록 가능한 객체 (IKeyable + ISpawnable + IDespawnable)
- ISpawnable                 : Registry 전용 Spawning/Spawned Hook
- IDespawnable               : Registry 전용 Despawning/Despawned Hook + 디스폰 요청 콜백 슬롯
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// 스폰 등록 가능한 객체를 위한 인터페이스입니다.
    /// </summary>
    /// <typeparam name="TKey">스폰 등록 가능한 객체의 키 타입입니다.</typeparam>
    // ============================================================
    public interface ISpawnRegistryObject<TKey> : IKeyable<TKey>, ISpawnable, IDespawnable
    where TKey : IEquatable<TKey>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// Registry가 소유하는 현재 스폰 처리 상태.
        /// </summary>
        // ------------------------------------------------------------
        public SpawnState SpawnState
        {
            get; protected internal set;
        }
    }

    // ============================================================
    /// <summary>
    /// 스폰 가능한 객체를 위한 인터페이스입니다.
    /// </summary>
    // ============================================================
    public interface ISpawnable
    {
        // ------------------------------------------------------------
        /// <summary>
        /// Registry가 Spawning 상태에서 등록 전에 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        protected internal void OnSpawning();

        // ------------------------------------------------------------
        /// <summary>
        /// Registry 등록과 Spawned 상태 전환이 끝난 뒤 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        protected internal void OnSpawned();
    }

    // ============================================================
    /// <summary>
    /// 디스폰 가능한 객체를 위한 인터페이스입니다.
    /// </summary>
    // ============================================================
    public interface IDespawnable
    {
        // ------------------------------------------------------------
        /// <summary>
        /// Registry가 Despawning 상태에서 등록 해제 전에 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        protected internal void OnDespawning(DespawnReason reason);

        // ------------------------------------------------------------
        /// <summary>
        /// Registry 등록 해제와 Despawned 상태 전환이 끝난 뒤 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        protected internal void OnDespawned(DespawnReason reason);

        // ----------------------------------------------------------------------
        /// <summary>
        /// 레지스트리가 등록한 디스폰 콜백. 외부에서는 Reason을 받는 확장 메서드로 호출한다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected internal Action<DespawnReason> DespawnFromRegistry { get; set; }
    }
}
