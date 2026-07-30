/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ModalHandle.cs
수정일 : 2026-07-30

# 설명
Modal Stack 등록과 Modal이 소유한 표시 Handle의 대칭 해제를 묶는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// 한 Modal 표시 수명 Handle.
    /// </summary>
    // ============================================================
    public sealed class ModalHandle : IDisposable
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Modal 표시 수명이 해제됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDisposed => owner == null;

        internal IModalDriver Driver { get; }

        private ModalController owner = null;
        private readonly List<IDisposable> ownedHandles = new List<IDisposable>();

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Modal Driver와 소유 표시 Handle을 묶는다.
        /// </summary>
        // ------------------------------------------------------------
        internal ModalHandle
        (
            ModalController owner,
            IModalDriver driver,
            IEnumerable<IDisposable> ownedHandles
        ) : base()
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Driver = driver ?? throw new ArgumentNullException(nameof(driver));

            if (ownedHandles == null) return;

            foreach (var handle in ownedHandles)
            {
                if (handle != null)
                {
                    this.ownedHandles.Add(handle);
                }
            }
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Modal이 소유한 표시 Handle을 생성 역순으로 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void ReleaseOwnedHandles()
        {
            var errors = new List<Exception>();

            for (var i = ownedHandles.Count - 1; i >= 0; i--)
            {
                var handle = ownedHandles[i];
                ownedHandles.RemoveAt(i);

                try
                {
                    handle.Dispose();
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            if (errors.Count > 0)
            {
                throw new AggregateException("Modal 표시 Handle 해제가 실패했습니다.", errors);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Modal의 공개 Stack 소유권이 종료됐음을 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void MarkStackReleased()
        {
            owner = null;
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// Modal Stack 등록과 소유 표시 Handle을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (owner == null) return;

            var current = owner;
            owner = null;
            current.Release(this);
        }

    #endregion

    }
}
