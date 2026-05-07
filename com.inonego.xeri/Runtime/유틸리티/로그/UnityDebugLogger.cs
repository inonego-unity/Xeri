/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityDebugLogger.cs
수정일 : 2026-05-07

# 설명
UnityEngine.Debug를 통해 로그를 출력하는 Logger 구현체.
UNITY_EDITOR 조건부 컴파일로 빌드 시에는 로그가 출력되지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Utility
{
    // ============================================================
    /// <summary>
    /// <br/> Unity 에디터 환경에서 로그를 출력하는 Logger.
    /// <br/> UNITY_EDITOR 조건부 컴파일로 빌드 시에는 출력되지 않는다.
    /// </summary>
    // ============================================================
    [Serializable]
    public class UnityDebugLogger : LoggerBase
    {

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 일반 로그를 출력한다.
        /// </summary>
        // ------------------------------------------------------------
        public override void Log(string message)
        {
        #if UNITY_EDITOR
            if (log) Debug.Log(message);
        #endif
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 경고 로그를 출력한다.
        /// </summary>
        // ------------------------------------------------------------
        public override void LogWarning(string message)
        {
        #if UNITY_EDITOR
            if (log) Debug.LogWarning(message);
        #endif
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 오류 로그를 출력한다.
        /// </summary>
        // ------------------------------------------------------------
        public override void LogError(string message)
        {
        #if UNITY_EDITOR
            if (log) Debug.LogError(message);
        #endif
        }

    #endregion

    }
}
