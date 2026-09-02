/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : BootstrapperModuleAsset.cs
수정일 : 2026-09-02

# 설명
Bootstrapper의 Initial Scene 전후 실행 phase와 Module 초기화 계약을 정의한다.
각 Module Asset은 실행 phase를 명시하고 해당 phase에서 초기화 로직을 수행한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Bootstrapper
{
    // ============================================================
    /// <summary>
    /// Bootstrapper Module이 실행되는 Initial Scene 기준 phase.
    /// </summary>
    // ============================================================
    public enum BootstrapperModulePhase
    {
        BeforeInitialScene = 0,
        AfterInitialScene = 1,
    }

    // ============================================================
    /// <summary>
    /// Bootstrapper에서 phase별로 순차 실행되는 초기화 Module Asset.
    /// </summary>
    // ============================================================
    public abstract class BootstrapperModuleAsset : ScriptableObject
    {

    #region 실행 계약

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Module이 실행될 Bootstrap phase를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract BootstrapperModulePhase Phase { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Bootstrap phase에서 Module 초기화 로직을 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract Awaitable Init();

    #endregion

    }
}
