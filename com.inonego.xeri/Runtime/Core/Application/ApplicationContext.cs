/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ApplicationContext.cs
수정일 : 2026-11-03

# 설명
초기화 모듈에 전달되는 Application 실행 문맥과 소유권 경계를 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace inonego.Xeri.Application
{
    // ============================================================
    /// <summary>
    /// Application 모듈이 공유하는 실행 세대의 문맥.
    /// </summary>
    // ============================================================
    public sealed class ApplicationContext
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 모듈을 실행하는 Application Runtime.
        /// </summary>
        // ------------------------------------------------------------
        public ApplicationRuntime Runtime { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 실행 세대에서 명시적으로 소유하는 최상위 Scope.
        /// </summary>
        // ------------------------------------------------------------
        public ApplicationScope Scope => Runtime.Scope;

        // ------------------------------------------------------------
        /// <summary>
        /// 종료 요청 시 전달되는 취소 토큰.
        /// </summary>
        // ------------------------------------------------------------
        public CancellationToken CancellationToken => Scope.CancellationToken;

    #endregion

    #region 생성자

        internal ApplicationContext(ApplicationRuntime runtime)
        {
            Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

    #endregion

    #region 진행 작업

        // ------------------------------------------------------------
        /// <summary>
        /// 실행 세대가 소유한 진행 작업을 종료 경계에 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public Task Track(Task task) => Scope.Track(task);

    #endregion

    }
}
