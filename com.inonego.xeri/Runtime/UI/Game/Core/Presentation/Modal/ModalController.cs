/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ModalController.cs
수정일 : 2026-07-31

# 설명
Modal 표시 순서를 소유하고 Stack 상단만 상호작용 가능하도록 backend 상태를 갱신한다.
개별 종료는 외부 정리 전에 Stack에서 제거하고, 전체 종료는 Stack을 고정 순회한 뒤 한 번에 비운다.
실패한 정리를 같은 Handle로 다시 시도하지 않는다.
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

                var handle = new ModalHandle(this, driver, ownedHandles);
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

                if (errors.Count == 1)
                {
                    throw;
                }

                throw new AggregateException("Modal 표시 시작과 롤백이 실패했습니다.", errors);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Modal만 제거하고 남은 Stack top 상태를 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Release
        (
            ModalHandle handle,
            bool removeFromStack = true
        )
        {
            if (isDisposed && removeFromStack) return;

            var index = stack.IndexOf(handle);

            var wasTop = index == stack.Count - 1;
            var previous = wasTop && index > 0
                ? stack[index - 1]
                : null;

            // 개별 종료는 대상을 공개 Stack에서 먼저 제거하고, 전체 종료는 순회 완료 뒤 Stack을 한 번에 비운다.
            if (removeFromStack)
            {
                stack.RemoveAt(index);
            }

            handle.MarkStackReleased();

            var errors = new List<Exception>();

            try
            {
                handle.Driver.SetTop(false);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }

            try
            {
                handle.ReleaseOwnedHandles();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }

            // 현재 Modal의 필수 정리가 모두 성공한 경우에만 이전 화면을 다시 공개한다.
            if
            (
                errors.Count == 0 &&
                previous != null &&
                !isDisposed &&
                stack.Count > 0 &&
                ReferenceEquals(stack[stack.Count - 1], previous)
            )
            {
                try
                {
                    previous.Driver.SetTop(true);
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            if (errors.Count > 0)
            {
                throw new AggregateException("Modal 해제가 실패했습니다.", errors);
            }
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 모든 Modal을 top부터 각각 한 번 해제한다.
        /// <br/> Stack과 Handle을 먼저 Terminal화하고 실패 항목을 재시도 대상으로 보관하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            isDisposed = true;
            var errors = new List<Exception>();

            try
            {
                // 개별 Modal이 Stack을 변경하지 않게 한 뒤 원본 Stack을 상단부터 정리한다.
                for (var i = stack.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        Release(stack[i], removeFromStack: false);
                    }
                    catch (AggregateException exception)
                    {
                        errors.AddRange(exception.InnerExceptions);
                    }
                    catch (Exception exception)
                    {
                        errors.Add(exception);
                    }
                }
            }
            finally
            {
                stack.Clear();
            }

            if (errors.Count > 0)
            {
                throw new AggregateException("Modal Controller 해제가 실패했습니다.", errors);
            }
        }

    #endregion

    }
}
