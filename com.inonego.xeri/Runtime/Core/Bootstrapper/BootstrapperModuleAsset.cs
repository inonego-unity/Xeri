/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : BootstrapperModuleAsset.cs
수정일 : 2026-11-03

# 설명
Unity Bootstrapper의 Initial Scene 전후 초기화 계약을 정의한다.
Application의 일반 초기화 모듈과 달리 Scene phase를 필수로 갖는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Application;

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
    /// Scene phase에 속한 Bootstrapper Module Asset.
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
        /// Scene phase에서 Application 소유 자원을 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract Task StartAsync(ApplicationContext context, CancellationToken cancellationToken);

    #endregion

    }
}
