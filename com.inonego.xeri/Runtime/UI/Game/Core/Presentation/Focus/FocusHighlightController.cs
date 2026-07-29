/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : FocusHighlightController.cs
수정일 : 2026-07-29

# 설명
Driver별 중첩 Focus Highlight 요청을 최신 표시 우선으로 합성하고 해제 시 이전 요청을 복원한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Focus Highlight 요청과 backend 표시 수명을 소유한다.
    /// </summary>
    // ============================================================
    public sealed class FocusHighlightController : IDisposable
    {
    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// 한 Driver에 적용할 순서화된 표시 요청.
        /// </summary>
        // ============================================================
        private sealed class Request
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 표시 요청 식별 값.
            /// </summary>
            // ------------------------------------------------------------
            public long ID { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 표시 호출 인자.
            /// </summary>
            // ------------------------------------------------------------
            public FocusHighlightParams Params { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// Focus Highlight 요청을 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public Request
            (
                long id,
                FocusHighlightParams parameters
            ) : base()
            {
                ID = id;
                Params = parameters ?? throw new ArgumentNullException(nameof(parameters));
            }
        }

    #endregion

    #region 필드

        private readonly Dictionary<IFocusHighlightDriver, List<Request>> requests =
            new Dictionary<IFocusHighlightDriver, List<Request>>();
        private long nextRequestID = 1L;
        private bool isDisposed = false;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Driver에 Focus Highlight 요청을 표시하고 소유 Handle을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public FocusHighlightHandle Show
        (
            IFocusHighlightDriver driver,
            FocusHighlightParams parameters
        )
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(FocusHighlightController));
            }

            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            if (!driver.IsValid)
            {
                throw new InvalidOperationException("Focus Highlight Driver가 유효하지 않습니다.");
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            driver.Show(parameters);

            if (!requests.TryGetValue(driver, out var list))
            {
                list = new List<Request>();
                requests.Add(driver, list);
            }

            var request = new Request(nextRequestID++, parameters);
            list.Add(request);
            return new FocusHighlightHandle(this, driver, request.ID);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 요청을 제거하고 이전 최신 요청 또는 숨김 상태를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Release
        (
            IFocusHighlightDriver driver,
            long requestID
        )
        {
            if (isDisposed) return;
            if (!requests.TryGetValue(driver, out var list)) return;

            var index = FindRequest(list, requestID);

            if (index < 0) return;

            var wasTop = index == list.Count - 1;

            if (wasTop)
            {
                if (index > 0)
                {
                    driver.Show(list[index - 1].Params);
                }
                else
                {
                    driver.Hide();
                }
            }

            list.RemoveAt(index);

            if (list.Count == 0)
            {
                requests.Remove(driver);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 요청 ID에 해당하는 목록 인덱스를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private static int FindRequest
        (
            List<Request> list,
            long requestID
        )
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].ID == requestID)
                {
                    return i;
                }
            }

            return -1;
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 Driver의 Highlight를 숨기고 표시 요청을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            var errors = new List<Exception>();
            var drivers = new List<IFocusHighlightDriver>(requests.Keys);

            for (var i = drivers.Count - 1; i >= 0; i--)
            {
                try
                {
                    drivers[i].Hide();
                    requests.Remove(drivers[i]);
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            if (errors.Count > 0)
            {
                throw new AggregateException("Focus Highlight 해제가 실패했습니다.", errors);
            }

            isDisposed = true;
        }

    #endregion

    }
}
