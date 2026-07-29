/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : FocusHighlightHandle.cs
수정일 : 2026-07-29

# 설명
한 Focus Highlight 표시 요청을 정확히 한 번 해제하는 소유권 Handle을 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Focus Highlight 표시 수명 Handle.
    /// </summary>
    // ============================================================
    public sealed class FocusHighlightHandle : IDisposable
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 요청이 해제됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDisposed => owner == null;

        private FocusHighlightController owner = null;
        private readonly IFocusHighlightDriver driver = null;
        private readonly long requestID = 0L;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Controller의 한 표시 요청을 소유하는 Handle을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        internal FocusHighlightHandle
        (
            FocusHighlightController owner,
            IFocusHighlightDriver driver,
            long requestID
        ) : base()
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.driver = driver ?? throw new ArgumentNullException(nameof(driver));
            this.requestID = requestID;
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Handle의 표시 요청만 해제하고 이전 요청을 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (owner == null) return;

            var current = owner;
            current.Release(driver, requestID);
            owner = null;
        }

    #endregion

    }
}
