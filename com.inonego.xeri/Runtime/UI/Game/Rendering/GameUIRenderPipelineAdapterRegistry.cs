/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GameUIRenderPipelineAdapterRegistry.cs
수정일 : 2026-08-24

# 설명
Game UI Bootstrapper가 현재 Render Pipeline용 출력 Adapter를 획득하는 선택형 Resolver Registry다.
Render Pipeline 전용 Assembly는 Core에 직접 의존성을 만들지 않고 Resolver만 등록한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ================================================================================
    /// <summary>
    /// Render Pipeline별 Adapter Resolver를 등록하고 현재 Pipeline용 Adapter를 선택한다.
    /// </summary>
    // ================================================================================
    internal static class GameUIRenderPipelineAdapterRegistry
    {

    #region 필드

        private static Func<IDisposable> acquireAdapter = null;

    #endregion

    #region Resolver 수명

        // ------------------------------------------------------------
        /// <summary>
        /// Play Mode 재진입에서 이전 Domain의 Resolver 연결을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetResolvers()
        {
            acquireAdapter = null;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 Pipeline을 처리할 수 있을 때 Adapter를 반환하는 Resolver를 등록한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal static void Register(Func<IDisposable> resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            var previous = acquireAdapter;
            acquireAdapter = previous == null
                ? resolver
                : () => resolver.Invoke() ?? previous.Invoke();
        }

    #endregion

    #region Adapter 획득

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Render Pipeline용 Adapter를 획득하고 없으면 기존 Overlay 출력을 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static IDisposable Acquire()
        {
            // 각 Resolver가 자기 Pipeline 여부를 판별하므로 Registry는 첫 유효 Adapter만 전달한다.
            return acquireAdapter?.Invoke();
        }

    #endregion

    }
}
