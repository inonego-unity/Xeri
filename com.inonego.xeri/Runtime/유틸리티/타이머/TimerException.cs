/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TimerException.cs
수정일 : 2026-04-28

# 설명
Timer 전용 예외 클래스 정의 (partial).
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Utility
{
    public partial class Timer
    {
        // ============================================================
        /// <summary>
        /// Timer 관련 예외의 기본 클래스.
        /// </summary>
        // ============================================================
        [Serializable]
        public class Exception : System.Exception
        {
            public Exception() : base() { }
            public Exception(string message) : base(message) { }
            public Exception(string message, System.Exception innerException) : base(message, innerException) { }
        }

        // ============================================================
        /// <summary>
        /// 이미 작동 중인 타이머에 Start를 호출할 때 발생하는 예외.
        /// </summary>
        // ============================================================
        [Serializable]
        public class AlreadyRunningException : Exception
        {
            public AlreadyRunningException() : base("타이머가 이미 작동 중입니다. 중지 후 시작해주세요.") { }
            public AlreadyRunningException(string message) : base(message) { }
        }

        // ============================================================
        /// <summary>
        /// 작동 중이거나 일시정지 중인 타이머에 Reset을 호출할 때 발생하는 예외.
        /// </summary>
        // ============================================================
        [Serializable]
        public class FailedToResetException : Exception
        {
            public FailedToResetException() : base("타이머가 작동 중이거나 일시정지 중입니다. 정지 후 리셋해주세요.") { }
            public FailedToResetException(string message) : base(message) { }
        }

        // ============================================================
        /// <summary>
        /// 유효하지 않은 시간 값을 설정할 때 발생하는 예외.
        /// </summary>
        // ============================================================
        [Serializable]
        public class InvalidTimeException : Exception
        {
            public InvalidTimeException() : base("시간 값은 0 이상이어야 합니다.") { }
            public InvalidTimeException(string message) : base(message) { }
        }
    }
}
