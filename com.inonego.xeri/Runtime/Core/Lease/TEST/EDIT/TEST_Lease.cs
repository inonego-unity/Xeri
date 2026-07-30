/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Lease.cs
수정일 : 2026-07-30

# 설명
공통 Lease의 일회 종료, 재진입, 예외 후 Terminal 상태 계약을 검증한다.

# 테스트 구성
 E: 일회 종료
 X: Callback 재진입과 예외
========================================================================= BLOCK_HEADER_END */

using System;

using NUnit.Framework;

namespace inonego.Xeri.TEST.Core._Lease
{
    // ============================================================
    /// <summary>
    /// 공통 Lease 종료 계약 테스트.
    /// </summary>
    // ============================================================
    public class TEST_Lease
    {
    #region E-1: 일회 종료

        [Test]
        public void TEST_Lease_Release와_Dispose는_Callback을_한번만_실행()
        {
            var releaseCount = 0;
            var lease = new Lease(() => releaseCount++);

            lease.Release();
            lease.Dispose();

            Assert.AreEqual(1, releaseCount);
            Assert.IsTrue(lease.IsDisposed);
        }

    #endregion

    #region X-1: Callback 재진입

        [Test]
        public void TEST_Lease_Callback재진입에도_한번만_실행()
        {
            var releaseCount = 0;
            Lease lease = null;
            lease = new Lease
            (
                () =>
                {
                    releaseCount++;
                    lease.Release();
                }
            );

            lease.Release();

            Assert.AreEqual(1, releaseCount);
            Assert.IsTrue(lease.IsDisposed);
        }

    #endregion

    #region X-2: Callback 예외

        [Test]
        public void TEST_Lease_Callback예외후에도_IsDisposed를_유지()
        {
            var releaseCount = 0;
            var lease = new Lease
            (
                () =>
                {
                    releaseCount++;
                    throw new InvalidOperationException("release failure");
                }
            );

            Assert.Throws<InvalidOperationException>(() => lease.Release());
            lease.Dispose();

            Assert.AreEqual(1, releaseCount);
            Assert.IsTrue(lease.IsDisposed);
        }

    #endregion

    }
}
