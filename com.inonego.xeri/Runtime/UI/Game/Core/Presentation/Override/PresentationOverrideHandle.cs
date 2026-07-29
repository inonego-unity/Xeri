/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationOverrideHandle.cs
수정일 : 2026-07-29

# 설명
한 Presentation 속성 Override 요청의 소유권을 정확히 한 번 해제한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Presentation Override 요청 Handle.
    /// </summary>
    // ============================================================
    public sealed class PresentationOverrideHandle : IDisposable
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Override 요청이 해제됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDisposed => release == null;

        private Action release = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Override 해제 동작을 소유하는 Handle을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        internal PresentationOverrideHandle(Action release) : base()
        {
            this.release = release ?? throw new ArgumentNullException(nameof(release));
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Handle의 Override 요청만 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (release == null) return;

            var current = release;
            current();
            release = null;
        }

    #endregion

    }
}
