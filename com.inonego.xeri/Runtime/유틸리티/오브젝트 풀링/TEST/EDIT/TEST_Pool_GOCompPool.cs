/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Pool_GOCompPool.cs
수정일 : 2026-07-30

# 설명
GOCompPool 시스템의 핵심 기능 테스트. Edit Mode.

# 테스트 구성
 E: 기본 기능 (생성/Acquire/Release/재사용)
 V: Provider 결과 검증과 반환 실패
 M: 풀 이동 (대상 인수 실패 시 원본 상태 복원)
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.TestTools;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST._Pool
{

    using inonego.Xeri.Pool;

// ============================================================
/// <summary>
/// GOCompPool 시스템의 핵심 기능 테스트 클래스.
/// </summary>
// ============================================================
public class TEST_Pool_GOCompPool
{

#region 헬퍼

    // ------------------------------------------------------------
    /// <summary>
    /// 테스트용 컴포넌트 클래스.
    /// </summary>
    // ------------------------------------------------------------
    private class TestComponent : MonoBehaviour
    {
        public int Value { get; set; }
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Component 상태를 변경한 뒤 인수를 거절하는 테스트용 Pool.
    /// </summary>
    // ------------------------------------------------------------
    private class RejectingPool : PoolBase<TestComponent>
    {
        private readonly Transform rejectedParent;

        public RejectingPool(Transform rejectedParent) : base()
        {
            this.rejectedParent = rejectedParent;
        }

        protected override TestComponent AcquireNew()
        {
            throw new NotSupportedException();
        }

        protected override Awaitable<TestComponent> AcquireNewAsync()
        {
            throw new NotSupportedException();
        }

        protected override void AcquireInternal(TestComponent item)
        {
            // 대상 인수 도중 변경될 수 있는 Unity 상태를 재현한 뒤 요청을 거절한다.
            item.transform.SetParent(rejectedParent);
            item.gameObject.SetActive(false);

            throw new InvalidOperationException("테스트 대상 Pool이 Component 인수를 거절했습니다.");
        }
    }

    // ============================================================
    /// <summary>
    /// 지정 GameObject의 획득과 반환을 기록하고 반환 실패를 주입하는 Provider.
    /// </summary>
    // ============================================================
    private sealed class TrackingProvider : IGameObjectProvider
    {
        public Transform Parent { get; set; }
        public GameObject Item { get; set; }
        public bool FailRelease { get; set; }
        public int ReleaseCount { get; private set; }

        public GameObject Acquire(bool worldPositionStays = true)
        {
            return Item;
        }

        public Awaitable<GameObject> AcquireAsync(bool worldPositionStays = true)
        {
            throw new NotSupportedException();
        }

        public void Release
        (
            GameObject gameObject,
            bool worldPositionStays = true
        )
        {
            ReleaseCount++;

            if (FailRelease)
            {
                throw new InvalidOperationException("injected provider release failure");
            }
        }
    }

    // ============================================================
    /// <summary>
    /// Lease 반환 경계에서 Component 반환 실패를 주입하는 Pool.
    /// </summary>
    // ============================================================
    private sealed class FailingReleaseGOCompPool : GOCompPool<TestComponent>
    {
        public int ReleaseAttemptCount { get; private set; }

        public FailingReleaseGOCompPool(IGameObjectProvider provider) : base(provider)
        {
        }

        protected override void ReleaseInternal
        (
            TestComponent item,
            bool removeFromAcquired = true,
            bool pushToReleased = true
        )
        {
            ReleaseAttemptCount++;
            throw new InvalidOperationException("injected component release failure");
        }
    }

#endregion

#region E-1: 기본 생성

    [UnityTest]
    public IEnumerator TEST_Pool_GOCompPool_기본_생성_초기값()
    {
        var pool = new GOCompPool<TestComponent>();

        Assert.IsNotNull(pool);
        Assert.IsNotNull(pool.GameObjectProvider);
        Assert.AreEqual(0, pool.Released.Count);
        Assert.AreEqual(0, pool.Acquired.Count);

        yield return null;
    }

#endregion

#region E-2: Acquire

    [UnityTest]
    public IEnumerator TEST_Pool_GOCompPool_Acquire_컴포넌트_획득()
    {
        var prefab = new GameObject("TestPrefab");
        prefab.AddComponent<TestComponent>();
        var provider = new PrefabGameObjectProvider { Prefab = prefab };
        var pool = new GOCompPool<TestComponent>(provider);

        TestComponent comp1 = null;
        TestComponent comp2 = null;

        try
        {
            comp1 = pool.Acquire();
            comp2 = pool.Acquire();

            yield return null;

            Assert.IsNotNull(comp1);
            Assert.IsNotNull(comp2);
            Assert.AreNotSame(comp1, comp2);
            Assert.AreEqual(2, pool.Acquired.Count);
            Assert.AreEqual(0, pool.Released.Count);
            Assert.IsTrue(comp1.gameObject.activeSelf, "획득한 컴포넌트의 GameObject는 활성화되어야 합니다");
            Assert.IsTrue(comp2.gameObject.activeSelf, "획득한 컴포넌트의 GameObject는 활성화되어야 합니다");
        }
        finally
        {
            if (comp1 != null) GameObject.DestroyImmediate(comp1.gameObject);
            if (comp2 != null) GameObject.DestroyImmediate(comp2.gameObject);
            GameObject.DestroyImmediate(prefab);
        }
    }

#endregion

#region E-3: Release

    [UnityTest]
    public IEnumerator TEST_Pool_GOCompPool_Release_컴포넌트_반환()
    {
        var prefab = new GameObject("TestPrefab");
        prefab.AddComponent<TestComponent>();
        var provider = new PrefabGameObjectProvider { Prefab = prefab };
        var poolParent = new GameObject("PoolParent");
        var pool = new GOCompPool<TestComponent>(provider) { Pool = poolParent.transform };

        TestComponent comp = null;

        try
        {
            comp = pool.Acquire();

            yield return null;

            pool.Release(comp);

            yield return null;

            Assert.AreEqual(0, pool.Acquired.Count);
            Assert.AreEqual(1, pool.Released.Count);
            Assert.IsFalse(comp.gameObject.activeSelf, "반환된 컴포넌트의 GameObject는 비활성화되어야 합니다");
            Assert.AreEqual(poolParent.transform, comp.transform.parent, "반환된 컴포넌트는 Pool 부모로 이동해야 합니다");
        }
        finally
        {
            if (comp != null) GameObject.DestroyImmediate(comp.gameObject);
            GameObject.DestroyImmediate(prefab);
            GameObject.DestroyImmediate(poolParent);
        }
    }

#endregion

#region E-4: 재사용

    [UnityTest]
    public IEnumerator TEST_Pool_GOCompPool_Release_후_Acquire_재사용()
    {
        var prefab = new GameObject("TestPrefab");
        prefab.AddComponent<TestComponent>();
        var provider = new PrefabGameObjectProvider { Prefab = prefab };
        var pool = new GOCompPool<TestComponent>(provider);

        TestComponent comp1 = null;
        TestComponent comp2 = null;

        try
        {
            comp1 = pool.Acquire();
            comp1.Value = 42;
            var instanceId = comp1.GetEntityId();

            yield return null;

            pool.Release(comp1);

            yield return null;

            comp2 = pool.Acquire();

            yield return null;

            Assert.AreEqual(instanceId, comp2.GetEntityId(), "같은 컴포넌트가 재사용되어야 합니다");
            Assert.AreEqual(42, comp2.Value, "재사용 시 컴포넌트의 데이터가 유지되어야 합니다");
            Assert.IsTrue(comp2.gameObject.activeSelf, "재사용된 컴포넌트의 GameObject는 활성화되어야 합니다");
            Assert.AreEqual(1, pool.Acquired.Count);
            Assert.AreEqual(0, pool.Released.Count);
        }
        finally
        {
            if (comp2 != null) GameObject.DestroyImmediate(comp2.gameObject);
            GameObject.DestroyImmediate(prefab);
        }
    }

#endregion

#region V-1: Provider 결과 검증

    // ----------------------------------------------------------------------
    /// <summary>
    /// Provider 결과에 필수 Component가 없으면 GameObject를 Provider에 반환하는지 검증한다.
    /// </summary>
    // ----------------------------------------------------------------------
    [Test]
    public void TEST_GOCompPool_Component누락_Provider로_반환()
    {
        var gameObject = new GameObject("Missing Component");
        var provider = new TrackingProvider { Item = gameObject };
        var pool = new GOCompPool<TestComponent>(provider);

        try
        {
            Assert.Throws<InvalidOperationException>(() => pool.Acquire());
            Assert.AreEqual(1, provider.ReleaseCount);
            Assert.AreEqual(0, pool.Acquired.Count);
            Assert.AreEqual(0, pool.Released.Count);
        }
        finally
        {
            GameObject.DestroyImmediate(gameObject);
        }
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Component 누락과 Provider 반환 실패가 함께 발생하면 두 오류를 모두 전달하는지 검증한다.
    /// </summary>
    // ----------------------------------------------------------------------
    [Test]
    public void TEST_GOCompPool_Component누락과Provider반환실패_두오류를함께전달()
    {
        var gameObject = new GameObject("Missing Component");
        var provider = new TrackingProvider
        {
            Item = gameObject,
            FailRelease = true,
        };
        var pool = new GOCompPool<TestComponent>(provider);

        try
        {
            var exception = Assert.Throws<AggregateException>(() => pool.Acquire());

            Assert.AreEqual(2, exception.InnerExceptions.Count);
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerExceptions[0]);
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerExceptions[1]);
            StringAssert.Contains
            (
                nameof(TestComponent),
                exception.InnerExceptions[0].Message
            );
            StringAssert.Contains
            (
                "injected provider release failure",
                exception.InnerExceptions[1].Message
            );
            Assert.AreEqual(1, provider.ReleaseCount);
            Assert.AreEqual(0, pool.Acquired.Count);
            Assert.AreEqual(0, pool.Released.Count);
        }
        finally
        {
            GameObject.DestroyImmediate(gameObject);
        }
    }

#endregion

#region V-2: Lease 반환 실패

    // ----------------------------------------------------------------------
    /// <summary>
    /// Lease 반환 실패 시 Provider 정리를 한 번 수행하고 Pool 소유권을 종결하는지 검증한다.
    /// </summary>
    // ----------------------------------------------------------------------
    [Test]
    public void TEST_GOCompPool_Lease반환실패_Provider정리후Terminal()
    {
        var gameObject = new GameObject("Lease Release Failure");
        var component = gameObject.AddComponent<TestComponent>();
        var provider = new TrackingProvider { Item = gameObject };
        var pool = new FailingReleaseGOCompPool(provider);

        try
        {
            var lease = pool.AcquireLease();

            var exception = Assert.Throws<InvalidOperationException>(() => lease.Dispose());
            StringAssert.Contains("injected component release failure", exception.Message);

            Assert.IsTrue(lease.IsDisposed);
            Assert.AreEqual(1, pool.ReleaseAttemptCount);
            Assert.AreEqual(1, provider.ReleaseCount);
            Assert.IsFalse(pool.IsAcquired(component));
            Assert.IsFalse(pool.IsReleased(component));

            Assert.DoesNotThrow(lease.Dispose);
            Assert.AreEqual(1, pool.ReleaseAttemptCount);
            Assert.AreEqual(1, provider.ReleaseCount);
        }
        finally
        {
            GameObject.DestroyImmediate(gameObject);
        }
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Lease 반환과 Provider 정리가 모두 실패하면 최초 오류 순서를 보존하고 다시 시도하지 않는지 검증한다.
    /// </summary>
    // ----------------------------------------------------------------------
    [Test]
    public void TEST_GOCompPool_Lease반환과Provider정리실패_두오류를한번만전달()
    {
        var gameObject = new GameObject("Lease Cleanup Failure");
        var component = gameObject.AddComponent<TestComponent>();
        var provider = new TrackingProvider
        {
            Item = gameObject,
            FailRelease = true,
        };
        var pool = new FailingReleaseGOCompPool(provider);

        try
        {
            var lease = pool.AcquireLease();
            var exception = Assert.Throws<AggregateException>(() => lease.Dispose());

            Assert.AreEqual(2, exception.InnerExceptions.Count);
            StringAssert.Contains
            (
                "injected component release failure",
                exception.InnerExceptions[0].Message
            );
            StringAssert.Contains
            (
                "injected provider release failure",
                exception.InnerExceptions[1].Message
            );
            Assert.IsTrue(lease.IsDisposed);
            Assert.AreEqual(1, pool.ReleaseAttemptCount);
            Assert.AreEqual(1, provider.ReleaseCount);
            Assert.IsFalse(pool.IsAcquired(component));
            Assert.IsFalse(pool.IsReleased(component));

            Assert.DoesNotThrow(lease.Dispose);
            Assert.AreEqual(1, pool.ReleaseAttemptCount);
            Assert.AreEqual(1, provider.ReleaseCount);
        }
        finally
        {
            GameObject.DestroyImmediate(gameObject);
        }
    }

#endregion

#region M-1: Acquired 이동 실패

    [UnityTest]
    public IEnumerator TEST_Pool_GOCompPool_MoveAcquiredOneTo_대상_인수_실패_원본_상태_유지()
    {
        var prefab = new GameObject("TestPrefab");
        prefab.AddComponent<TestComponent>();
        var sourceParent = new GameObject("SourceParent");
        var rejectedParent = new GameObject("RejectedParent");
        var provider = new PrefabGameObjectProvider
        {
            Prefab = prefab,
            Parent = sourceParent.transform,
        };
        var source = new GOCompPool<TestComponent>(provider);
        var target = new RejectingPool(rejectedParent.transform);

        TestComponent comp = null;

        try
        {
            comp = source.Acquire();

            Assert.Throws<InvalidOperationException>(() => source.MoveAcquiredOneTo(target, comp));

            Assert.IsTrue(source.IsAcquired(comp));
            Assert.IsFalse(target.IsAcquired(comp));
            Assert.IsFalse(target.IsReleased(comp));
            Assert.AreSame(sourceParent.transform, comp.transform.parent);
            Assert.IsTrue(comp.gameObject.activeSelf);

            yield return null;
        }
        finally
        {
            if (comp != null) GameObject.DestroyImmediate(comp.gameObject);
            GameObject.DestroyImmediate(prefab);
            GameObject.DestroyImmediate(sourceParent);
            GameObject.DestroyImmediate(rejectedParent);
        }
    }

#endregion

}

}
