/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : InteractionBlockerHandle.cs
수정일 : 2026-07-29

# 설명
UGUI Interaction Blocker의 한 점유 요청을 정확히 한 번 해제한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UGUI Interaction Blocker 점유 Handle.
    /// </summary>
    // ============================================================
    public sealed class InteractionBlockerHandle : IDisposable
    {
    #region 필드

        private Action release = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Blocker 해제 동작을 소유하는 Handle을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        internal InteractionBlockerHandle(Action release) : base()
        {
            this.release = release ?? throw new ArgumentNullException(nameof(release));
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Handle의 Blocker 점유만 해제한다.
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
