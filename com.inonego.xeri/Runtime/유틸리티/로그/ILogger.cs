/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ILogger.cs
수정일 : 2026-05-07

# 설명
로그 출력 인터페이스. UnityEngine.Debug를 대체하여 의존성 주입으로 Logger를 교체할 수 있게 한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.Utility
{
    // ==========================================================================================
    /// <summary>
    /// <br/> UnityEngine.Debug를 대체하여 로그를 남길 수 있게 하는 인터페이스.
    /// <br/> 의존성 주입으로 Logger를 교체하면 에디터 출력·파일 저장 등 상황에 맞는 로깅을 선택할 수 있다.
    /// <br/>
    /// <br/> 인스펙터에 노출하려면 [SerializeReference]를 사용한다.
    /// <br/> 예시: [SerializeReference] private ILogger logger = new UnityDebugLogger();
    /// </summary>
    // ==========================================================================================
    public interface ILogger
    {
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
    }
}
