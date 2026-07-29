/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ModalController.cs
수정일 : 2026-07-29

# 설명
Modal 표시 순서를 소유하고 Stack 상단만 상호작용 가능하도록 backend 상태를 갱신한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Modal Stack과 상단 상호작용 상태를 소유한다.
    /// </summary>
    // ============================================================
    public sealed class ModalController : IDisposable
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Modal Stack 항목 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => stack.Count;

        private readonly List<ModalHandle> stack = new List<ModalHandle>();
        private bool isDisposed = false;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Modal Driver를 Stack top에 추가하고 성공 시 표시 Handle 소유권을 이전받는다.
        /// </summary>
        // ------------------------------------------------------------
        public ModalHandle Open
        (
            IModalDriver driver,
            params IDisposable[] ownedHandles
        )
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(ModalController));
            }

            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            var handle = new ModalHandle(this, driver, ownedHandles);
            var previous = stack.Count > 0
                ? stack[stack.Count - 1]
                : null;

            try
            {
                if (previous != null)
                {
                    previous.Driver.SetTop(false);
                }

                driver.SetTop(true);
                stack.Add(handle);
                return handle;
            }
            catch (Exception exception)
            {
                var errors = new List<Exception> { exception };

                try
                {
                    driver.SetTop(false);
                }
                catch (Exception cleanupException)
                {
                    errors.Add(cleanupException);
                }

                if (previous != null)
                {
                    try
                    {
                        previous.Driver.SetTop(true);
                    }
                    catch (Exception cleanupException)
                    {
                        errors.Add(cleanupException);
                    }
                }

                throw new AggregateException("Modal 표시 시작과 롤백이 실패했습니다.", errors);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Modal만 제거하고 남은 Stack top 상태를 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Release(ModalHandle handle)
        {
            if (handle == null || isDisposed) return;

            // Stack 제거가 끝난 Handle은 실패했던 하위 표시 정리만 재시도한다.
            if (handle.IsStackReleased)
            {
                handle.ReleaseOwnedHandles();
                return;
            }

            var index = stack.IndexOf(handle);

            if (index < 0)
            {
                handle.MarkStackReleased();
                handle.ReleaseOwnedHandles();
                return;
            }

            var wasTop = index == stack.Count - 1;

            if (wasTop)
            {
                handle.Driver.SetTop(false);

                // 이전 top 전환이 실패하면 현재 top을 되돌려 Stack의 공개 상태를 보존한다.
                if (stack.Count > 1)
                {
                    try
                    {
                        stack[stack.Count - 2].Driver.SetTop(true);
                    }
                    catch (Exception exception)
                    {
                        try
                        {
                            handle.Driver.SetTop(true);
                        }
                        catch (Exception rollbackException)
                        {
                            throw new AggregateException
                            (
                                "Modal top 복원과 현재 top 롤백이 실패했습니다.",
                                exception,
                                rollbackException
                            );
                        }

                        throw;
                    }
                }
            }
            else
            {
                handle.Driver.SetTop(false);
            }

            stack.RemoveAt(index);
            handle.MarkStackReleased();

            // 공개 Stack 제거 뒤에는 실패한 소유 Handle만 같은 ModalHandle에서 재시도한다.
            handle.ReleaseOwnedHandles();
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 Modal을 top부터 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            var errors = new List<Exception>();

            for (var i = stack.Count - 1; i >= 0; i--)
            {
                try
                {
                    stack[i].Driver.SetTop(false);
                    stack[i].ReleaseOwnedHandles();
                    stack.RemoveAt(i);
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            if (errors.Count > 0)
            {
                throw new AggregateException("Modal Controller 해제가 실패했습니다.", errors);
            }

            isDisposed = true;
        }

    #endregion

    }
}
