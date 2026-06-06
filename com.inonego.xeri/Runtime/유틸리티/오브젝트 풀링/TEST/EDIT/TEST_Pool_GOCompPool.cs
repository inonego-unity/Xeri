/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Pool_GOCompPool.cs
수정일 : 2026-05-08

# 설명
GOCompPool 시스템의 핵심 기능 테스트. Edit Mode.

# 테스트 구성
 E: 기본 기능 (생성/Acquire/Release/재사용)
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
            var instanceId = comp1.GetInstanceID();

            yield return null;

            pool.Release(comp1);

            yield return null;

            comp2 = pool.Acquire();

            yield return null;

            Assert.AreEqual(instanceId, comp2.GetInstanceID(), "같은 컴포넌트가 재사용되어야 합니다");
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

}

}
