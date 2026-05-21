/* BLOCK_HEADER_BEGIN =======================================================================
파일명: BootstrapperRunner.cs
수정일: 2026-05-20

# 설명
BootStrapper 모듈 목록을 순차 실행하는 런타임 헬퍼.
실제 MonoBehaviour 생명주기와 분리하여 모듈 실행 규칙을 단위 테스트할 수 있게 한다.
========================================================================= BLOCK_HEADER_END */

using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Bootstrapper
{
    // ============================================================
    /// <summary>
    /// BootStrapper 모듈 실행 헬퍼.
    /// </summary>
    // ============================================================
    public static class BootstrapperRunner
    {

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 모듈 목록을 순서대로 초기화한다. null 모듈은 건너뛴다.
        /// </summary>
        // ------------------------------------------------------------
        public static async Awaitable Init(IReadOnlyList<BootstrapperModuleAsset> modules)
        {
            if (modules == null) return;

            foreach (var module in modules)
            {
                if (module == null) continue;

                await module.Init();
            }
        }

    #endregion

    }
}
