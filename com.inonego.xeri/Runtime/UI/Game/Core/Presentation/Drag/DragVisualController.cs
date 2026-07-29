/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DragVisualController.cs
수정일 : 2026-07-29

# 설명
드래그 중 RectTransform 시각물을 명시적 Layer Root로 재배치하고 복원 Handle을 반환한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Drag Visual의 일시적 계층 재배치를 시작한다.
    /// </summary>
    // ============================================================
    public sealed class DragVisualController : IDisposable
    {
    #region 필드

        private readonly List<DragVisualHandle> handles = new List<DragVisualHandle>();
        private bool isDisposed = false;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Drag Visual을 지정 Root의 마지막 sibling으로 옮긴다.
        /// </summary>
        // ------------------------------------------------------------
        public DragVisualHandle Begin
        (
            RectTransform target,
            RectTransform dragRoot
        )
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(DragVisualController));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (dragRoot == null)
            {
                throw new ArgumentNullException(nameof(dragRoot));
            }

            var handle = new DragVisualHandle(this, target);

            try
            {
                target.SetParent(dragRoot, true);
                target.SetAsLastSibling();
                handles.Add(handle);
                return handle;
            }
            catch (Exception exception)
            {
                try
                {
                    handle.Dispose();
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException
                    (
                        "Drag Visual 시작과 원래 위치 복원이 실패했습니다.",
                        exception,
                        cleanupException
                    );
                }

                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 복원된 Drag Visual Handle을 활성 목록에서 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Release(DragVisualHandle handle)
        {
            handles.Remove(handle);
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 남은 Drag Visual을 최신 시작부터 원래 위치로 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            var errors = new List<Exception>();
            var snapshot = handles.ToArray();

            for (var i = snapshot.Length - 1; i >= 0; i--)
            {
                try
                {
                    snapshot[i].Dispose();
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            if (errors.Count > 0)
            {
                throw new AggregateException("Drag Visual Controller 해제가 실패했습니다.", errors);
            }

            isDisposed = true;
        }

    #endregion

    }
}
