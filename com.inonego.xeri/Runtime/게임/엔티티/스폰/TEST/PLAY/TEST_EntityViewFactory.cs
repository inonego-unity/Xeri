/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_EntityViewFactory.cs
수정일 : 2026-07-29

# 설명
EntityViewFactory의 View 생성 실패 정리 계약을 검증한다.
Unity Test Runner (Play Mode) 에서 실행한다.

# 테스트 구성
 X: 예외 처리 (View 생성 실패 시 Provider 반환)
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.TestTools;

using NUnit;
using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.TEST.Game._EntitySpawn
{

    using inonego.Xeri.Game;

    // ============================================================
    /// <summary>
    /// EntityViewFactory 생성 실패 정리 테스트.
    /// </summary>
    // ============================================================
    public class TEST_EntityViewFactory
    {

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// Factory 테스트용 Entity.
        /// </summary>
        // ------------------------------------------------------------
        private class TestEntity : EntityBase
        {
            private readonly HP_I hp = new HP_I { MaxValue = 100 };
            private readonly Value<int> group = new Value<int>();

            public override IHP HP => hp;
            public override IValue<int> Group => group;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Spawning 단계에서 생성을 실패시키는 View.
        /// </summary>
        // ------------------------------------------------------------
        private class FailingEntityView : EntityViewBase<TestEntity>
        {
            protected override void OnSpawningView(TestEntity entity)
            {
                throw new InvalidOperationException("테스트 View 생성 실패");
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 획득 및 반환 GameObject를 기록하는 Provider.
        /// </summary>
        // ------------------------------------------------------------
        private class TrackingProvider : IGameObjectProvider
        {
            public Transform Parent { get; set; }
            public GameObject GameObject { get; set; }
            public GameObject Released { get; private set; }
            public int ReleaseCount { get; private set; }

            public GameObject Acquire(bool worldPositionStays = true)
            {
                return GameObject;
            }

            public async Awaitable<GameObject> AcquireAsync(bool worldPositionStays = true)
            {
                return await Task.FromResult(GameObject);
            }

            public void Release(GameObject go, bool worldPositionStays = true)
            {
                Released = go;
                ReleaseCount++;
            }
        }

    #endregion

    #region X-1: View 생성 실패

        [UnityTest]
        public IEnumerator TEST_EntityViewFactory_Create_View_생성_실패_Provider_반환()
        {
            var gameObject = new GameObject("FailingView");
            var view = gameObject.AddComponent<FailingEntityView>();
            var provider = new TrackingProvider { GameObject = gameObject };
            var factory = new EntityViewFactory<FailingEntityView, TestEntity>(provider);
            var entity = new TestEntity();

            try
            {
                Assert.Throws<InvalidOperationException>(() => factory.Create(entity));
                Assert.AreEqual(1, provider.ReleaseCount);
                Assert.AreSame(gameObject, provider.Released);
                Assert.IsNull(view.Entity);

                yield return null;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

    #endregion

    }
}
