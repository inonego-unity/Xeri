/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : BootstrapperRunner.cs
수정일 : 2026-09-02

# 설명
Bootstrapper Module 목록에서 지정 phase만 원래 목록 순서대로 실행한다.
Module은 Bootstrapper가 보장한 phase ordering 안에서 독립적으로 초기화된다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Bootstrapper
{
    // ============================================================
    /// <summary>
    /// Bootstrapper Module phase 실행 헬퍼.
    /// </summary>
    // ============================================================
    public static class BootstrapperRunner
    {

    #region 단계 실행

        // ----------------------------------------------------------------------
        /// <summary>
        /// 지정 phase의 Module만 목록 순서대로 실행한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static async Awaitable RunPhase
        (
            IReadOnlyList<BootstrapperModuleAsset> modules,
            BootstrapperModulePhase phase
        )
        {
            if (modules == null) return;

            foreach (var module in modules)
            {
                if (module == null || module.Phase != phase) continue;

                await module.Init();
            }
        }

    #endregion

    }
}
