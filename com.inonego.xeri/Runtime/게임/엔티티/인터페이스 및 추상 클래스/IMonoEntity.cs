/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IMonoEntity.cs
수정일 : 2026-05-07

# 설명
MonoBehaviour 와 엔티티를 연결하는 인터페이스 모음.

- IReadOnlyMonoEntity : 외부 노출용. 엔티티(IReadOnlyEntity)·IsSpawned·Key 를 읽기 전용으로 노출.
- IMonoEntity        : 시스템 내부용. SpawnRegistry 등록·IEntity 노출.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// MonoEntity 읽기 전용 뷰 인터페이스.
    /// </summary>
    // ============================================================
    public interface IReadOnlyMonoEntity : IKeyable<ulong>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 대응되는 논리 엔티티(읽기 전용).
        /// </summary>
        // ------------------------------------------------------------
        IReadOnlyEntity Entity { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 스폰되어 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        bool IsSpawned { get; }
    }

    // ==========================================================================
    /// <summary>
    /// 시스템 내부 MonoEntity 인터페이스. SpawnRegistry 등록 + IEntity 노출.
    /// </summary>
    // ==========================================================================
    public interface IMonoEntity : IReadOnlyMonoEntity, ISpawnRegistryObject<ulong>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 대응되는 논리 엔티티(시스템 내부 mutable 노출).
        /// </summary>
        // ------------------------------------------------------------
        new IEntity Entity { get; }
    }
}
