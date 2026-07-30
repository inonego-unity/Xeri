/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Pool_PoolBase.cs
수정일 : 2026-07-30

# 설명
PoolBase 시스템의 핵심 기능 테스트. Edit Mode.

# 테스트 구성
 E: 기본 기능 (생성/Acquire/Release/재사용/ReleaseAll)
 X: 예외 처리 (미등록 아이템/이중 Release)
 C: 콜백 (AcquireInternal/ReleaseInternal)
 M: 풀 관리 (PushToReleased/PopFromReleased/Move*)
 L: Lease와 Generation
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST._Pool
{

    using inonego.Xeri.Pool;

// ============================================================
/// <summary>
/// PoolBase 시스템의 핵심 기능 테스트 클래스.
/// </summary>
// ============================================================
public class TEST_Pool_PoolBase
{

#region 헬퍼

    // ------------------------------------------------------------
    /// <summary>
    /// 테스트용 Pool 아이템 클래스.
    /// </summary>
    // ------------------------------------------------------------
    private class TestPoolItem
    {
        public int Value { get; set; }
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 값 동등성을 구현한 참조 동일성 검증용 항목.
    /// </summary>
    // ------------------------------------------------------------
    private class ValueEqualPoolItem
    {
        public int Value { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ValueEqualPoolItem other && Value == other.Value;
        }

        public override int GetHashCode() => Value;
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 테스트용 Pool 클래스.
    /// </summary>
    // ------------------------------------------------------------
    private class TestPool : PoolBase<TestPoolItem>
    {
        protected override TestPoolItem AcquireNew()
        {
            return new TestPoolItem();
        }

        protected override async Awaitable<TestPoolItem> AcquireNewAsync()
        {
            return await Task.FromResult(new TestPoolItem());
        }
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 값 동등성 항목을 관리하는 테스트용 Pool 클래스.
    /// </summary>
    // ------------------------------------------------------------
    private class ValueEqualPool : PoolBase<ValueEqualPoolItem>
    {
        protected override ValueEqualPoolItem AcquireNew()
        {
            return new ValueEqualPoolItem();
        }

        protected override async Awaitable<ValueEqualPoolItem> AcquireNewAsync()
        {
            return await Task.FromResult(new ValueEqualPoolItem());
        }
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 콜백 카운트 추적용 Pool 클래스.
    /// </summary>
    // ------------------------------------------------------------
    private class TestPoolWithCallbacks : PoolBase<TestPoolItem>
    {
        public int AcquireCallCount { get; private set; }
        public int ReleaseCallCount { get; private set; }
        public int PublicReleaseCallCount { get; private set; }

        protected override TestPoolItem AcquireNew()
        {
            return new TestPoolItem();
        }

        protected override async Awaitable<TestPoolItem> AcquireNewAsync()
        {
            return await Task.FromResult(new TestPoolItem());
        }

        protected override void AcquireInternal(TestPoolItem item)
        {
            base.AcquireInternal(item);
            AcquireCallCount++;
            item.Value = 100;
        }

        public override void Release(TestPoolItem item, bool pushToReleased = true)
        {
            PublicReleaseCallCount++;
            base.Release(item, pushToReleased);
        }

        protected override void ReleaseInternal(TestPoolItem item, bool removeFromAcquired = true, bool pushToReleased = true)
        {
            ReleaseCallCount++;
            item.Value = 0;
            base.ReleaseInternal(item, removeFromAcquired, pushToReleased);
        }
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 반환 실패와 Discard 횟수를 관찰하는 테스트용 Pool.
    /// </summary>
    // ------------------------------------------------------------
    private class FailingReleasePool : PoolBase<TestPoolItem>
    {
        public TestPoolItem FailureItem { get; set; }
        public int ReleaseAttemptCount { get; private set; }
        public int DiscardCount { get; private set; }

        protected override TestPoolItem AcquireNew()
        {
            return new TestPoolItem();
        }

        protected override async Awaitable<TestPoolItem> AcquireNewAsync()
        {
            return await Task.FromResult(new TestPoolItem());
        }

        protected override void ReleaseInternal
        (
            TestPoolItem item,
            bool removeFromAcquired = true,
            bool pushToReleased = true
        )
        {
            ReleaseAttemptCount++;

            if (ReferenceEquals(item, FailureItem))
            {
                throw new InvalidOperationException("release failure");
            }

            base.ReleaseInternal(item, removeFromAcquired, pushToReleased);
        }

        protected override void OnDiscard(TestPoolItem item)
        {
            DiscardCount++;
        }
    }

#endregion

#region E-1: 기본 생성

    [Test]
    public void TEST_Pool_PoolBase_기본_생성_초기값()
    {
        var pool = new TestPool();

        Assert.AreEqual(0, pool.Released.Count);
        Assert.AreEqual(0, pool.Acquired.Count);
    }

#endregion

#region E-2: Acquire

    [Test]
    public void TEST_Pool_PoolBase_Acquire_새로운_아이템_획득()
    {
        var pool = new TestPool();

        var item1 = pool.Acquire();
        var item2 = pool.Acquire();

        Assert.IsNotNull(item1);
        Assert.IsNotNull(item2);
        Assert.AreNotSame(item1, item2);
        Assert.AreEqual(2, pool.Acquired.Count);
        Assert.AreEqual(0, pool.Released.Count);
    }

#endregion

#region E-3: Release

    [Test]
    public void TEST_Pool_PoolBase_Release_아이템_반환_및_pushToReleased()
    {
        var pool = new TestPool();
        var item1 = pool.Acquire();
        var item2 = pool.Acquire();

        // Release - item1
        pool.Release(item1);

        Assert.AreEqual(1, pool.Acquired.Count);
        Assert.AreEqual(1, pool.Released.Count);

        // Release - item2
        pool.Release(item2);

        Assert.AreEqual(0, pool.Acquired.Count);
        Assert.AreEqual(2, pool.Released.Count);

        // Release - item3 (pushToReleased: false)
        var item3 = pool.Acquire();
        pool.Release(item3, pushToReleased: false);

        Assert.AreEqual(0, pool.Acquired.Count, "Acquired 목록에서 제거되어야 합니다.");
        Assert.AreEqual(1, pool.Released.Count, "Released 큐에 추가되지 않아야 합니다.");
    }

#endregion

#region E-4: 재사용

    [Test]
    public void TEST_Pool_PoolBase_Release_후_Acquire_재사용()
    {
        var pool = new TestPool();
        var item1 = pool.Acquire();
        pool.Release(item1);

        var item2 = pool.Acquire();

        Assert.AreSame(item1, item2, "Released된 오브젝트가 재사용되어야 합니다");
        Assert.AreEqual(1, pool.Acquired.Count);
        Assert.AreEqual(0, pool.Released.Count);
    }

#endregion

#region E-5: ReleaseAll

    [Test]
    public void TEST_Pool_PoolBase_ReleaseAll_모든_아이템_반환()
    {
        var pool = new TestPoolWithCallbacks();
        pool.Acquire();
        pool.Acquire();
        pool.Acquire();

        pool.ReleaseAll();

        Assert.AreEqual(0, pool.Acquired.Count);
        Assert.AreEqual(3, pool.Released.Count);
        Assert.AreEqual(3, pool.PublicReleaseCallCount);
        Assert.AreEqual(3, pool.ReleaseCallCount);
    }

#endregion

#region L-1: Lease 기본 반환

    [Test]
    public void TEST_Pool_AcquireLease_Release시_Released로_복귀()
    {
        var pool = new TestPoolWithCallbacks();
        var lease = pool.AcquireLease();

        lease.Dispose();

        Assert.IsTrue(lease.IsDisposed);
        Assert.AreEqual(0, pool.Acquired.Count);
        Assert.AreEqual(1, pool.Released.Count);
        Assert.AreEqual(1, pool.PublicReleaseCallCount);
        Assert.AreEqual(1, pool.ReleaseCallCount, "Lease 반환도 기존 virtual Release 경계를 통과해야 합니다.");
    }

    [Test]
    public async Task TEST_Pool_AcquireLeaseAsync_Release시_Released로_복귀()
    {
        var pool = new TestPool();
        var lease = await pool.AcquireLeaseAsync();

        lease.Dispose();

        Assert.AreEqual(0, pool.Acquired.Count);
        Assert.AreEqual(1, pool.Released.Count);
    }

#endregion

#region L-2: 오래된 Generation

    [Test]
    public void TEST_Pool_직접반환과_재획득후_이전Lease는_현재Item에_영향없음()
    {
        var pool = new TestPool();
        var lease = pool.AcquireLease();
        var item = lease.Value;

        pool.Release(item);
        var reacquired = pool.Acquire();
        lease.Dispose();

        Assert.AreSame(item, reacquired);
        Assert.IsTrue(pool.IsAcquired(reacquired));
        Assert.AreEqual(1, pool.Acquired.Count);
        Assert.AreEqual(0, pool.Released.Count);
    }

#endregion

#region L-3: Lease 반환 실패

    [Test]
    public void TEST_Pool_Lease반환실패_Item을_Released에_공개하지않고_Discard()
    {
        var pool = new FailingReleasePool();
        var lease = pool.AcquireLease();
        pool.FailureItem = lease.Value;

        Assert.Throws<InvalidOperationException>(() => lease.Dispose());
        lease.Dispose();

        Assert.AreEqual(1, pool.ReleaseAttemptCount);
        Assert.AreEqual(1, pool.DiscardCount);
        Assert.AreEqual(0, pool.Acquired.Count);
        Assert.AreEqual(0, pool.Released.Count);
    }

#endregion

#region L-4: ReleaseAll 실패 격리

    [Test]
    public void TEST_Pool_ReleaseAll_일부실패에도_나머지_초기대상을_처리()
    {
        var pool = new FailingReleasePool();
        var first = pool.Acquire();
        var failed = pool.Acquire();
        var third = pool.Acquire();
        pool.FailureItem = failed;

        var exception = Assert.Throws<AggregateException>(() => pool.ReleaseAll());

        Assert.AreEqual(1, exception.InnerExceptions.Count);
        Assert.AreEqual(3, pool.ReleaseAttemptCount);
        Assert.AreEqual(1, pool.DiscardCount);
        Assert.AreEqual(0, pool.Acquired.Count);
        Assert.AreEqual(2, pool.Released.Count);
        Assert.IsTrue(pool.IsReleased(first));
        Assert.IsTrue(pool.IsReleased(third));
        Assert.IsFalse(pool.IsReleased(failed));
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Bulk 반환도 개별 반환과 같은 public virtual Release 확장 경계를 사용하는지 검증합니다.
    /// </summary>
    // ----------------------------------------------------------------------
    [Test]
    public void TEST_Pool_ReleaseAll_PublicReleaseOverride를_호출()
    {
        var pool = new TestPoolWithCallbacks();
        var item = pool.Acquire();

        Assert.DoesNotThrow(() => pool.ReleaseAll());

        Assert.AreEqual(1, pool.PublicReleaseCallCount);
        Assert.AreEqual(0, pool.Acquired.Count);
        Assert.AreEqual(1, pool.Released.Count);
        Assert.IsTrue(pool.IsReleased(item));
    }

#endregion

#region E-6: 참조 동일성

    [Test]
    public void TEST_Pool_PoolBase_값이_같은_서로_다른_항목_별도_관리()
    {
        var pool   = new ValueEqualPool();
        var first  = new ValueEqualPoolItem { Value = 1 };
        var second = new ValueEqualPoolItem { Value = 1 };

        pool.PushToReleased(first);
        pool.PushToReleased(second);

        var acquiredFirst  = pool.Acquire();
        var acquiredSecond = pool.Acquire();

        Assert.AreSame(first, acquiredFirst);
        Assert.AreSame(second, acquiredSecond);
        Assert.AreEqual(2, pool.Acquired.Count);
    }

#endregion

#region X-1: 잘못된 Release 예외

    [Test]
    public void TEST_Pool_PoolBase_Release_미등록_및_이중_Release_예외()
    {
        var pool = new TestPool();

        // 풀에 없는 아이템 Release 시도
        var nonPooledItem = new TestPoolItem();
        Assert.Throws<InvalidOperationException>(() => pool.Release(nonPooledItem));

        // 이미 Released된 아이템 다시 Release 시도
        var item = pool.Acquire();
        pool.Release(item);
        Assert.Throws<InvalidOperationException>(() => pool.Release(item));
    }

#endregion

#region C-1: AcquireInternal / ReleaseInternal 콜백

    [Test]
    public void TEST_Pool_PoolBase_AcquireInternal_ReleaseInternal_콜백_호출()
    {
        var pool = new TestPoolWithCallbacks();
        var item = pool.Acquire();

        Assert.AreEqual(1, pool.AcquireCallCount);
        Assert.AreEqual(0, pool.ReleaseCallCount);

        pool.Release(item);

        Assert.AreEqual(1, pool.AcquireCallCount);
        Assert.AreEqual(1, pool.ReleaseCallCount);

        var item2 = pool.Acquire();

        Assert.AreEqual(2, pool.AcquireCallCount);
        Assert.AreEqual(1, pool.ReleaseCallCount);
        Assert.AreSame(item, item2);
    }

#endregion

#region M-1: PushToReleased

    [Test]
    public void TEST_Pool_PoolBase_PushToReleased_추가_및_중복_예외()
    {
        var pool = new TestPoolWithCallbacks();
        var item = new TestPoolItem();

        pool.PushToReleased(item);

        Assert.AreEqual(1, pool.Released.Count);
        Assert.AreEqual(0, pool.Acquired.Count);
        Assert.AreEqual(1, pool.ReleaseCallCount, "PushToReleased 시 ReleaseInternal이 호출되어야 합니다.");

        // 중복 추가 시도 시 예외
        Assert.Throws<InvalidOperationException>(() => pool.PushToReleased(item), "이미 풀에 있는 아이템을 추가하려고 하면 예외가 발생해야 합니다.");

        // 사용 중인 아이템 추가 시도 시 예외
        var acquiredItem = pool.Acquire();
        Assert.Throws<InvalidOperationException>(() => pool.PushToReleased(acquiredItem), "이미 사용 중인 아이템을 풀에 추가하려고 하면 예외가 발생해야 합니다.");
    }

#endregion

#region M-2: PopFromReleased

    [Test]
    public void TEST_Pool_PoolBase_PopFromReleased_재사용_및_새로_생성()
    {
        var pool = new TestPool();
        var item1 = new TestPoolItem();
        pool.PushToReleased(item1);

        // Released에 아이템이 있는 경우 — 재사용
        var poppedItem1 = pool.PopFromReleased();
        Assert.AreSame(item1, poppedItem1);
        Assert.AreEqual(0, pool.Released.Count);

        // Released가 비어있는 경우 — 새로 생성
        var poppedItem2 = pool.PopFromReleased();
        Assert.IsNotNull(poppedItem2);
        Assert.AreNotSame(item1, poppedItem2);
    }

#endregion

#region M-3: MoveAcquiredOneTo

    [Test]
    public void TEST_Pool_PoolBase_MoveAcquiredOneTo_원본_제거_대상_추가()
    {
        var pool1 = new TestPoolWithCallbacks();
        var pool2 = new TestPoolWithCallbacks();
        var item = pool1.Acquire();

        pool1.MoveAcquiredOneTo(pool2, item);

        Assert.AreEqual(0, pool1.Acquired.Count, "원본 풀에서 제거되어야 합니다.");
        Assert.AreEqual(0, pool1.ReleaseCallCount, "소유권 이동은 원본 풀의 ReleaseInternal을 호출하지 않아야 합니다.");
        Assert.AreEqual(1, pool2.Acquired.Count, "대상 풀에 추가되어야 합니다.");
        Assert.AreEqual(1, pool2.AcquireCallCount, "대상 풀의 AcquireInternal이 호출되어야 합니다.");
    }

#endregion

#region M-4: MoveReleasedOneTo

    [Test]
    public void TEST_Pool_PoolBase_MoveReleasedOneTo_원본_감소_대상_증가()
    {
        var pool1 = new TestPoolWithCallbacks();
        var pool2 = new TestPoolWithCallbacks();

        pool1.PushToReleased(new TestPoolItem());
        pool1.PushToReleased(new TestPoolItem());

        Assert.AreEqual(2, pool1.Released.Count);
        Assert.AreEqual(0, pool2.Released.Count);

        pool1.MoveReleasedOneTo(pool2);

        Assert.AreEqual(1, pool1.Released.Count, "원본 풀에 1개가 남아있어야 합니다.");
        Assert.AreEqual(1, pool2.Released.Count, "대상 풀로 1개가 이동되어야 합니다.");
        Assert.AreEqual(1, pool2.ReleaseCallCount, "대상 풀의 ReleaseInternal이 1번 호출되어야 합니다.");
    }

#endregion

}

}
