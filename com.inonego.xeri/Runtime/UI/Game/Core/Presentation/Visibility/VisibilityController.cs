/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : VisibilityController.cs
수정일 : 2026-07-31

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
        private bool isDisposed = false;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Target에 새 Visibility 요청을 적용하고 Lease를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Lease Set
        (
            IVisibilityTarget target,
            bool visible
        )
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(VisibilityController));
            }

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
                Visible = visible,
            };

            target.SetVisible(visible);

            if (isNewEntry)
            {
                entries.Add(target, entry);
            }

            entry.Requests.Add(request);
            return new Lease(() => Release(target, request));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 요청만 제거하고 다음 유효 요청 또는 기준 상태를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Release
        (
            IVisibilityTarget target,
            Request request
        )
        {
            if (isDisposed) return;
            if (!entries.TryGetValue(target, out var entry)) return;

            var index = entry.Requests.IndexOf(request);

            if (index < 0) return;

            var wasTop = index == entry.Requests.Count - 1;

            entry.Requests.RemoveAt(index);

            if (entry.Requests.Count == 0)
            {
                ReleaseTarget(target, entry);
                return;
            }

            if (wasTop)
            {
                target.SetVisible(entry.Requests[entry.Requests.Count - 1].Visible);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Target의 마지막 Visibility 요청을 종료하고 기준 상태를 복원한다.
        /// <br/> Controller 전체 종료에서는 원본 목록을 순회한 뒤 한 번에 비우도록 등록 제거를 생략한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ReleaseTarget
        (
            IVisibilityTarget target,
            Entry entry,
            bool removeFromEntries = true
        )
        {
            if (removeFromEntries)
            {
                entries.Remove(target);
            }

            target.SetVisible(entry.Baseline);
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

            isDisposed = true;
            var errors = new List<Exception>();

            try
            {
                // 종료 상태에서는 Handle 해제가 목록을 변경하지 않으므로 원본 Target 목록을 직접 순회한다.
                foreach (var pair in entries)
                {
                    try
                    {
                        ReleaseTarget
                        (
                            pair.Key,
                            pair.Value,
                            removeFromEntries: false
                        );
                    }
                    catch (Exception exception)
                    {
                        errors.Add(exception);
                    }
                }
            }
            finally
            {
                entries.Clear();
            }

            if (errors.Count > 0)
            {
                throw new AggregateException("Visibility Controller 해제가 실패했습니다.", errors);
            }

        }

    #endregion

    }
}
