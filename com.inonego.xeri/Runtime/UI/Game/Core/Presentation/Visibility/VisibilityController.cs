/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : VisibilityController.cs
수정일 : 2026-07-29

# 설명
Target별 중첩 Visibility 요청을 획득 순서로 합성하고 마지막 해제 시 기준 상태를 복원한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// 중첩 Visibility 요청을 소유하는 Controller.
    /// </summary>
    // ============================================================
    public sealed class VisibilityController : IDisposable
    {
    #region 내부 데이터

        private sealed class Request
        {
            public long ID = 0L;
            public bool Visible = true;
        }

        private sealed class Entry
        {
            public bool Baseline = true;
            public readonly List<Request> Requests = new List<Request>();
        }

    #endregion

    #region 필드

        private readonly Dictionary<IVisibilityTarget, Entry> entries =
            new Dictionary<IVisibilityTarget, Entry>();
        private long nextRequestID = 1L;
        private bool isDisposed = false;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Target에 새 Visibility 요청을 적용하고 Handle을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisibilityHandle Set
        (
            IVisibilityTarget target,
            bool visible
        )
        {
            ThrowIfDisposed();

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var isNewEntry = !entries.TryGetValue(target, out var entry);

            if (isNewEntry)
            {
                entry = new Entry { Baseline = target.IsVisible };
            }

            var request = new Request
            {
                ID = nextRequestID++,
                Visible = visible,
            };

            target.SetVisible(visible);

            if (isNewEntry)
            {
                entries.Add(target, entry);
            }

            entry.Requests.Add(request);
            return new VisibilityHandle(this, target, request.ID);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 요청만 제거하고 다음 유효 요청 또는 기준 상태를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Release
        (
            IVisibilityTarget target,
            long requestID
        )
        {
            if (isDisposed) return;
            if (!entries.TryGetValue(target, out var entry)) return;

            var index = -1;

            for (var i = entry.Requests.Count - 1; i >= 0; i--)
            {
                if (entry.Requests[i].ID == requestID)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0) return;

            var nextVisible = index == entry.Requests.Count - 1
                ? index > 0 ? entry.Requests[index - 1].Visible : entry.Baseline
                : entry.Requests[entry.Requests.Count - 1].Visible;

            target.SetVisible(nextVisible);
            entry.Requests.RemoveAt(index);

            if (entry.Requests.Count == 0)
            {
                entries.Remove(target);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 해제된 Controller 사용을 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(VisibilityController));
            }
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 Target을 기준 Visibility로 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            var errors = new List<Exception>();
            var targets = new List<IVisibilityTarget>(entries.Keys);

            for (var i = targets.Count - 1; i >= 0; i--)
            {
                var target = targets[i];

                try
                {
                    target.SetVisible(entries[target].Baseline);
                    entries.Remove(target);
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            if (errors.Count > 0)
            {
                throw new AggregateException("Visibility Controller 해제가 실패했습니다.", errors);
            }

            isDisposed = true;
        }

    #endregion

    }
}
