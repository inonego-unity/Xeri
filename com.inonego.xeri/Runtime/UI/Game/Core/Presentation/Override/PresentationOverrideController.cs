/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationOverrideController.cs
수정일 : 2026-07-30

# 설명
한 속성의 기준 값과 중첩 Override를 획득 순서로 합성하는 범용 Controller를 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// 한 Presentation 속성의 중첩 Override를 소유한다.
    /// </summary>
    // ============================================================
    public sealed class PresentationOverrideController<TValue> : IDisposable
    {
    #region 내부 데이터

        private sealed class Request
        {
            public TValue Value = default;
        }

    #endregion

    #region 필드

        private readonly Action<TValue> apply = null;
        private readonly TValue baseline = default;
        private readonly List<Request> requests = new List<Request>();
        private bool isDisposed = false;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 기준 값 조회와 값 적용 동작으로 Override Controller를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationOverrideController
        (
            Func<TValue> readBaseline,
            Action<TValue> apply
        ) : base()
        {
            if (readBaseline == null)
            {
                throw new ArgumentNullException(nameof(readBaseline));
            }

            this.apply = apply ?? throw new ArgumentNullException(nameof(apply));
            baseline = readBaseline();
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 새 Override 값을 적용하고 요청 Lease를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Lease Set(TValue value)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(PresentationOverrideController<TValue>));
            }

            var request = new Request
            {
                Value = value,
            };

            apply(value);
            requests.Add(request);

            return new Lease(() => Release(request));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Override를 제거하고 다음 유효 값 또는 기준 값을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Release(Request request)
        {
            if (isDisposed) return;

            var index = requests.IndexOf(request);

            if (index < 0) return;

            var value = index == requests.Count - 1
                ? index > 0 ? requests[index - 1].Value : baseline
                : requests[requests.Count - 1].Value;

            requests.RemoveAt(index);
            apply(value);
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 Override를 제거하고 기준 값을 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            isDisposed = true;

            try
            {
                apply(baseline);
            }
            finally
            {
                // 기준 값 복원 결과와 관계없이 Terminal Controller는 요청 참조를 남기지 않는다.
                requests.Clear();
            }
        }

    #endregion

    }
}
