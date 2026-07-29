/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PresentationLayerRegistry.cs
수정일 : 2026-07-29

# 설명
동적 Profile 경계에서 공유 Presentation Layer의 전역 순서와 충돌 계약을 검증한다.

# 테스트 구성
 O: 공유 Layer Order 정렬과 재정렬
 X: Order 충돌 거부
 R: Layer 해제 실패 재시도
 U: Layer 소비자 독립 수명
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using NUnit.Framework;

namespace inonego.Xeri.TEST.UI._Game
{
    using inonego.Xeri.UI.Game;

    // ============================================================
    /// <summary>
    /// PresentationLayerRegistry의 동적 공유 Layer 순서 계약 테스트.
    /// </summary>
    // ============================================================
    public sealed class TEST_PresentationLayerRegistry
    {
    #region 헬퍼

        // ============================================================
        /// <summary>
        /// 테스트 Transform을 사용하는 Presentation Layer backend.
        /// </summary>
        // ============================================================
        private sealed class TestLayerDriver : IPresentationLayerDriver
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 테스트 Layer Root.
            /// </summary>
            // ------------------------------------------------------------
            public Transform Root { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 테스트 backend의 활성 상태.
            /// </summary>
            // ------------------------------------------------------------
            public bool IsActive { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 다음 비활성 적용을 실패시킬지 여부.
            /// </summary>
            // ------------------------------------------------------------
            public bool FailNextDeactivation { get; set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 지정 Root를 사용하는 테스트 backend를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public TestLayerDriver(Transform root) : base()
            {
                Root = root;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 테스트 Root와 Asset이 존재하는지 검증한다.
            /// </summary>
            // ------------------------------------------------------------
            public bool Validate
            (
                PresentationLayerAsset asset,
                out string error
            )
            {
                error = asset == null || Root == null ? "invalid" : "";
                return string.IsNullOrEmpty(error);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 테스트 backend의 활성 상태를 기록한다.
            /// </summary>
            // ------------------------------------------------------------
            public void SetActive(bool active)
            {
                if (!active && FailNextDeactivation)
                {
                    FailNextDeactivation = false;
                    throw new InvalidOperationException("injected layer deactivation failure");
                }

                IsActive = active;
            }
        }

        private readonly List<UnityEngine.Object> ownedObjects = new List<UnityEngine.Object>();

        // ------------------------------------------------------------
        /// <summary>
        /// private 직렬화 값을 지정한 테스트 Layer Asset을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private PresentationLayerAsset CreateAsset
        (
            string id,
            int order
        )
        {
            var asset = ScriptableObject.CreateInstance<PresentationLayerAsset>();
            SetField(asset, "id", id);
            SetField(asset, "mode", PresentationLayerMode.Shared);
            SetField(asset, "order", order);
            ownedObjects.Add(asset);
            return asset;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 Layer Root를 공통 부모 아래에 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private Transform CreateRoot
        (
            string name,
            Transform parent
        )
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            ownedObjects.Add(gameObject);
            return gameObject.transform;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 Asset의 private 직렬화 필드를 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void SetField
        (
            object target,
            string name,
            object value
        )
        {
            var field = target.GetType().GetField
            (
                name,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.IsNotNull(field, $"테스트 설정 필드 '{name}'을 찾지 못했습니다.");
            field.SetValue(target, value);
        }

    #endregion

    #region 픽스처

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트에서 만든 Unity Object를 역순 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            for (var i = ownedObjects.Count - 1; i >= 0; i--)
            {
                if (ownedObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(ownedObjects[i]);
                }
            }

            ownedObjects.Clear();
        }

    #endregion

    #region O-1: 공유 Layer 정렬

        // ------------------------------------------------------------
        /// <summary>
        /// 등록·해제마다 전체 공유 Layer가 Order 기준으로 다시 정렬되는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PresentationLayerRegistry_동적등록해제_전체공유Layer순서재계산()
        {
            var parentObject = new GameObject("Layer Parent");
            ownedObjects.Add(parentObject);
            var highRoot = CreateRoot("High", parentObject.transform);
            var lowRoot = CreateRoot("Low", parentObject.transform);
            var registry = new PresentationLayerRegistry();
            var highHandle = registry.Register(CreateAsset("High", 20), new TestLayerDriver(highRoot));
            var lowHandle = registry.Register(CreateAsset("Low", 10), new TestLayerDriver(lowRoot));

            Assert.AreEqual(0, lowRoot.GetSiblingIndex());
            Assert.AreEqual(1, highRoot.GetSiblingIndex());

            lowHandle.Dispose();

            Assert.AreEqual(0, highRoot.GetSiblingIndex());

            highHandle.Dispose();
            registry.Dispose();
        }

    #endregion

    #region U-1: Layer 소비자 수명

        // ------------------------------------------------------------
        /// <summary>
        /// Layer 소비자 하나를 해제해도 다른 소비자와 등록이 유지되는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PresentationLayerHandle_소비자하나해제_다른소비자와등록유지()
        {
            var parentObject = new GameObject("Layer Parent");
            ownedObjects.Add(parentObject);
            var root = CreateRoot("Shared", parentObject.transform);
            var registry = new PresentationLayerRegistry();
            var handle = registry.Register(CreateAsset("Shared", 0), new TestLayerDriver(root));
            var first = handle.AcquireUsage();
            var second = handle.AcquireUsage();

            first.Dispose();

            Assert.IsTrue(handle.HasConsumers);
            Assert.IsTrue(registry.Contains("Shared"));
            Assert.Throws<InvalidOperationException>(handle.Dispose);

            second.Dispose();
            handle.Dispose();

            Assert.IsFalse(handle.HasConsumers);
            Assert.IsTrue(handle.IsDisposed);
            Assert.IsFalse(registry.Contains("Shared"));
            registry.Dispose();
        }

    #endregion

    #region X-1: Order 충돌

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 ID의 공유 Layer가 같은 Order를 등록하지 못하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PresentationLayerRegistry_동일Order_등록전거부()
        {
            var parentObject = new GameObject("Layer Parent");
            ownedObjects.Add(parentObject);
            var firstRoot = CreateRoot("First", parentObject.transform);
            var secondRoot = CreateRoot("Second", parentObject.transform);
            var registry = new PresentationLayerRegistry();
            var firstHandle = registry.Register(CreateAsset("First", 10), new TestLayerDriver(firstRoot));

            var exception = Assert.Throws<InvalidOperationException>
            (
                () => registry.Register(CreateAsset("Second", 10), new TestLayerDriver(secondRoot))
            );

            StringAssert.Contains("Order(10)", exception.Message);
            Assert.AreEqual(0, firstRoot.GetSiblingIndex());

            firstHandle.Dispose();
            registry.Dispose();
        }

    #endregion

    #region R-1: Layer 해제 실패

        // ------------------------------------------------------------
        /// <summary>
        /// backend 비활성화 실패 전에 Registry 소유권을 제거하지 않고 재시도하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PresentationLayerRegistry_비활성화실패_등록유지후재시도()
        {
            var parentObject = new GameObject("Layer Parent");
            ownedObjects.Add(parentObject);
            var root = CreateRoot("Retry", parentObject.transform);
            var registry = new PresentationLayerRegistry();
            var driver = new TestLayerDriver(root);
            var handle = registry.Register(CreateAsset("Retry", 10), driver);
            driver.FailNextDeactivation = true;

            Assert.Throws<InvalidOperationException>(handle.Dispose);
            Assert.IsTrue(registry.Contains("Retry"));
            Assert.IsTrue(driver.IsActive);
            Assert.IsFalse(handle.IsDisposed);

            handle.Dispose();

            Assert.IsFalse(registry.Contains("Retry"));
            Assert.IsFalse(driver.IsActive);
            Assert.IsTrue(handle.IsDisposed);
            registry.Dispose();
        }

    #endregion

    }
}
