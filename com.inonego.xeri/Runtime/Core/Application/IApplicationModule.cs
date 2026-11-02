/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IApplicationModule.cs
수정일 : 2026-11-03

# 설명
Application 초기화 단위의 순차 실행 계약을 정의한다.
모듈이 획득한 자원의 종료 책임은 ApplicationContext.Scope가 보유한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace inonego.Xeri.Application
{
    // ============================================================
    /// <summary>
    /// Application 실행 세대에서 한 번 초기화되는 모듈.
    /// </summary>
    // ============================================================
    public interface IApplicationModule
    {

    #region 초기화 계약

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 초기화를 실행하고 필수 준비 단계가 끝나면 반환한다.
        /// <br/> Application의 정상 종료 취소는 전달된 cancellationToken으로 전파한다.
        /// <br/> 모듈 자체의 다른 토큰에 의한 취소는 초기화 실패로 전달된다.
        /// <br/> 종료 요청이 필요하면 StopAsync를 시작할 수 있지만 여기서 완료를 기다리지 않는다.
        /// <br/> StopAsync는 이 초기화 Task의 완료를 기다린다.
        /// </summary>
        // --------------------------------------------------------------------------------
        Task StartAsync(ApplicationContext context, CancellationToken cancellationToken);

    #endregion

    }
}
