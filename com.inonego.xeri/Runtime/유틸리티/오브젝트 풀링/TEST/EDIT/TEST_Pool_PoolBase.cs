/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Pool_PoolBase.cs
수정일 : 2026-05-01

# 설명
PoolBase 시스템의 핵심 기능 테스트. Edit Mode.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using UnityEngine;

using inonego.Xeri.Pool;

// ============================================================================
/// <summary>
/// PoolBase 시스템의 핵심 기능 테스트 클래스입니다.
/// </summary>
// ============================================================================
public class TEST_Pool_PoolBase
{

#region PoolBase 기본 기능 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// Pool의 기본 생성 및 초기값을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void PoolBase_01_기본_생성_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var pool = new TestPool();

        // ------------------------------------------------------------
        // 초기 상태 확인
        // ------------------------------------------------------------
        Assert.AreEqual(0, pool.Released.Count);
        Assert.AreEqual(0, pool.Acquired.Count);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Pool의 Acquire 기능을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void PoolBase_02_Acquire_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var pool = new TestPool();

        // ------------------------------------------------------------
        // Acquire
        // ------------------------------------------------------------
        var item1 = pool.Acquire();
        var item2 = pool.Acquire();

        Assert.IsNotNull(item1);
        Assert.IsNotNull(item2);
        Assert.AreNotSame(item1, item2);
        Assert.AreEqual(2, pool.Acquired.Count);
        Assert.AreEqual(0, pool.Released.Count);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Pool의 Release 기능을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void PoolBase_03_Release_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var pool = new TestPool();
        var item1 = pool.Acquire();
        var item2 = pool.Acquire();

        // ------------------------------------------------------------
        // Release - item1
        // ------------------------------------------------------------
        pool.Release(item1);

        Assert.AreEqual(1, pool.Acquired.Count);
        Assert.AreEqual(1, pool.Released.Count);

        // ------------------------------------------------------------
        // Release - item2
        // ------------------------------------------------------------
        pool.Release(item2);

        Assert.AreEqual(0, pool.Acquired.Count);
        Assert.AreEqual(2, pool.Released.Count);

        // ------------------------------------------------------------
        // Release - item3 (pushToReleased: false)
        // ------------------------------------------------------------
        var item3 = pool.Acquire();
        pool.Release(item3, pushToReleased: false);

        Assert.AreEqual(0, pool.Acquired.Count, "Acquired 목록에서 제거되어야 합니다.");
        Assert.AreEqual(1, pool.Released.Count, "Released 큐에 추가되지 않아야 합니다.");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Pool의 재사용 기능을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void PoolBase_04_재사용_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var pool = new TestPool();
        var item1 = pool.Acquire();
        pool.Release(item1);

        // ------------------------------------------------------------
        // 재사용 확인
        // ------------------------------------------------------------
        var item2 = pool.Acquire();

        Assert.AreSame(item1, item2, "Released된 오브젝트가 재사용되어야 합니다");
        Assert.AreEqual(1, pool.Acquired.Count);
        Assert.AreEqual(0, pool.Released.Count);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Pool의 ReleaseAll 기능을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void PoolBase_05_ReleaseAll_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var pool = new TestPool();
        pool.Acquire();
        pool.Acquire();
        pool.Acquire();

        // ------------------------------------------------------------
        // ReleaseAll
        // ------------------------------------------------------------
        pool.ReleaseAll();

        Assert.AreEqual(0, pool.Acquired.Count);
        Assert.AreEqual(3, pool.Released.Count);
    }

#endregion

#region PoolBase 예외 처리 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// Pool의 예외 상황들을 통합 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void PoolBase_06_예외_처리_통합_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var pool = new TestPool();

        // ------------------------------------------------------------
        // 풀에 없는 아이템 Release 시도
        // ------------------------------------------------------------
        var nonPooledItem = new TestPoolItem();
        Assert.Throws<Exception>(() => pool.Release(nonPooledItem));

        // ------------------------------------------------------------
        // 이미 Released된 아이템 다시 Release 시도
        // ------------------------------------------------------------
        var item = pool.Acquire();
        pool.Release(item);
        Assert.Throws<Exception>(() => pool.Release(item));
    }

#endregion

#region PoolBase 콜백 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// Pool의 AcquireInternal/ReleaseInternal 오버라이드 콜백을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void PoolBase_07_콜백_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var pool = new TestPoolWithCallbacks();
        var item = pool.Acquire();

        Assert.AreEqual(1, pool.AcquireCallCount);
        Assert.AreEqual(0, pool.ReleaseCallCount);

        // ------------------------------------------------------------
        // Release
        // ------------------------------------------------------------
        pool.Release(item);

        Assert.AreEqual(1, pool.AcquireCallCount);
        Assert.AreEqual(1, pool.ReleaseCallCount);

        // ------------------------------------------------------------
        // 재사용
        // ------------------------------------------------------------
        var item2 = pool.Acquire();

        Assert.AreEqual(2, pool.AcquireCallCount);
        Assert.AreEqual(1, pool.ReleaseCallCount);
        Assert.AreSame(item, item2);
    }

#endregion

#region PoolBase 풀 관리 메서드 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// PushToReleased 메서드를 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void PoolBase_08_PushToReleased_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var pool = new TestPoolWithCallbacks();
        var item = new TestPoolItem();

        // ------------------------------------------------------------
        // PushToReleased
        // ------------------------------------------------------------
        pool.PushToReleased(item);

        Assert.AreEqual(1, pool.Released.Count);
        Assert.AreEqual(0, pool.Acquired.Count);
        Assert.AreEqual(1, pool.ReleaseCallCount, "PushToReleased 시 ReleaseInternal이 호출되어야 합니다.");

        // ------------------------------------------------------------
        // 중복 추가 시도 시 예외
        // ------------------------------------------------------------
        Assert.Throws<Exception>(() => pool.PushToReleased(item), "이미 풀에 있는 아이템을 추가하려고 하면 예외가 발생해야 합니다.");

        // ------------------------------------------------------------
        // 사용 중인 아이템 추가 시도 시 예외
        // ------------------------------------------------------------
        var acquiredItem = pool.Acquire();
        Assert.Throws<Exception>(() => pool.PushToReleased(acquiredItem), "이미 사용 중인 아이템을 풀에 추가하려고 하면 예외가 발생해야 합니다.");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// PopFromReleased 메서드를 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void PoolBase_09_PopFromReleased_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var pool = new TestPool();
        var item1 = new TestPoolItem();
        pool.PushToReleased(item1);

        // ------------------------------------------------------------
        // Released에 아이템이 있는 경우 — 재사용
        // ------------------------------------------------------------
        var poppedItem1 = pool.PopFromReleased();
        Assert.AreSame(item1, poppedItem1);
        Assert.AreEqual(0, pool.Released.Count);

        // ------------------------------------------------------------
        // Released가 비어있는 경우 — 새로 생성
        // ------------------------------------------------------------
        var poppedItem2 = pool.PopFromReleased();
        Assert.IsNotNull(poppedItem2);
        Assert.AreNotSame(item1, poppedItem2);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// MoveAcquiredOneTo 메서드를 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void PoolBase_10_MoveAcquiredTo_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var pool1 = new TestPoolWithCallbacks();
        var pool2 = new TestPoolWithCallbacks();
        var item = pool1.Acquire();

        // ------------------------------------------------------------
        // MoveAcquiredOneTo
        // ------------------------------------------------------------
        pool1.MoveAcquiredOneTo(pool2, item);

        Assert.AreEqual(0, pool1.Acquired.Count, "원본 풀에서 제거되어야 합니다.");
        Assert.AreEqual(1, pool1.ReleaseCallCount, "원본 풀의 ReleaseInternal이 호출되어야 합니다.");
        Assert.AreEqual(1, pool2.Acquired.Count, "대상 풀에 추가되어야 합니다.");
        Assert.AreEqual(1, pool2.AcquireCallCount, "대상 풀의 AcquireInternal이 호출되어야 합니다.");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// MoveReleasedOneTo 메서드를 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void PoolBase_11_MoveReleasedTo_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var pool1 = new TestPoolWithCallbacks();
        var pool2 = new TestPoolWithCallbacks();

        pool1.PushToReleased(new TestPoolItem());
        pool1.PushToReleased(new TestPoolItem());

        Assert.AreEqual(2, pool1.Released.Count);
        Assert.AreEqual(0, pool2.Released.Count);

        // ------------------------------------------------------------
        // MoveReleasedOneTo
        // ------------------------------------------------------------
        pool1.MoveReleasedOneTo(pool2);

        Assert.AreEqual(1, pool1.Released.Count, "원본 풀에 1개가 남아있어야 합니다.");
        Assert.AreEqual(1, pool2.Released.Count, "대상 풀로 1개가 이동되어야 합니다.");
        Assert.AreEqual(1, pool2.ReleaseCallCount, "대상 풀의 ReleaseInternal이 1번 호출되어야 합니다.");
    }

#endregion

#region 테스트용 Pool 클래스

    // ------------------------------------------------------------
    /// <summary>
    /// 테스트용 Pool 아이템 클래스입니다.
    /// </summary>
    // ------------------------------------------------------------
    private class TestPoolItem
    {
        public int Value { get; set; }
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 테스트용 Pool 클래스입니다.
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
    /// 콜백 카운트 추적용 Pool 클래스입니다.
    /// </summary>
    // ------------------------------------------------------------
    private class TestPoolWithCallbacks : PoolBase<TestPoolItem>
    {
        public int AcquireCallCount { get; private set; }
        public int ReleaseCallCount { get; private set; }

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

        protected override void ReleaseInternal(TestPoolItem item, bool removeFromAcquired = true, bool pushToReleased = true)
        {
            base.ReleaseInternal(item, removeFromAcquired, pushToReleased);
            ReleaseCallCount++;
            item.Value = 0;
        }
    }

#endregion

}
