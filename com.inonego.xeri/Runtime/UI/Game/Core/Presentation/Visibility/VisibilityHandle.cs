/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : VisibilityHandle.cs
수정일 : 2026-07-29

# 설명
한 Visibility Override 요청의 소유권을 정확히 한 번 해제한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Visibility 요청 소유권 Handle.
    /// </summary>
    // ============================================================
    public sealed class VisibilityHandle : IDisposable
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 요청이 해제됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDisposed => owner == null;

        private VisibilityController owner = null;
        private readonly IVisibilityTarget target = null;
        private readonly long requestID = 0L;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Visibility 요청 Handle을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        internal VisibilityHandle
        (
            VisibilityController owner,
            IVisibilityTarget target,
            long requestID
        ) : base()
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.requestID = requestID;
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Handle의 Visibility 요청만 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (owner == null) return;

            var current = owner;
            current.Release(target, requestID);
            owner = null;
        }

    #endregion

    }
}
