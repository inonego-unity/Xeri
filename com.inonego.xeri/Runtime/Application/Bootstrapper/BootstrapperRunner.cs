/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : BootstrapperRunner.cs
수정일 : 2026-11-03

# 설명
Bootstrapper Module 목록에서 지정 phase만 원래 목록 순서대로 실행한다.
Bootstrapper 고유의 Scene phase와 Application 초기화 계약을 연결한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
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
    /// Bootstrapper Module phase 실행 헬퍼.
    /// </summary>
    // ============================================================
    public static class BootstrapperRunner
    {

    #region 단계 실행

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 지정 phase의 Module을 순서대로 실행한다.
        /// <br/> Application Context로 Asset을 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public static async Task RunPhase
        (
            IReadOnlyList<BootstrapperModuleAsset> modules,
            BootstrapperModulePhase phase,
            ApplicationContext context,
            CancellationToken cancellationToken
        )
        {
            if (modules == null)
            {
                throw new ArgumentNullException(nameof(modules));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            foreach (var module in modules)
            {
                if (module == null)
                {
                    throw new InvalidOperationException("Bootstrapper Module 목록에 null이 포함되어 있습니다.");
                }

                if (module.Phase != phase) continue;

                cancellationToken.ThrowIfCancellationRequested();

                var task = module.StartAsync(context, cancellationToken) ??
                    throw new InvalidOperationException("Bootstrapper Module이 null Task를 반환했습니다.");
                await task;
            }
        }

    #endregion

    }
}
