/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_BootstrapperRunner.cs
수정일 : 2026-11-18

# 설명
Bootstrapper phase의 순차 실행, 실패 시 Application 수명 정리, 에디터 변경 이벤트를 검증한다.

# 테스트 구성
 E: Scene phase 필터링과 실행 순서
 X: 초기화 실패와 소유권 정리
 C: 모듈 목록·모듈 에셋 변경 이벤트
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEditor;
using UnityEngine;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Application;
using inonego.Xeri.Bootstrapper;

namespace inonego.Xeri.TEST.Core._Bootstrapper
{
    // ============================================================
    /// <summary>
    /// BootstrapperRunner의 phase·실패 전파 계약을 검증한다.
    /// </summary>
    // ============================================================
    public sealed class TEST_BootstrapperRunner
    {

    #region 헬퍼

        public sealed class ProbeModule : BootstrapperModuleAsset
        {
            public BootstrapperModulePhase TestPhase { get; set; }
            public Func<ApplicationContext, CancellationToken, Task> Run { get; set; }

            public override BootstrapperModulePhase Phase => TestPhase;

            public override Task StartAsync(ApplicationContext context, CancellationToken cancellationToken)
            {
                return Run(context, cancellationToken);
            }
        }

        private sealed class Startup : IApplicationModule
        {
            private readonly IReadOnlyList<BootstrapperModuleAsset> modules;

            public Startup(IReadOnlyList<BootstrapperModuleAsset> modules)
            {
                this.modules = modules;
            }

            public async Task StartAsync(ApplicationContext context, CancellationToken cancellationToken)
            {
                await BootstrapperRunner.RunPhase
                (
                    modules, BootstrapperModulePhase.BeforeInitialScene,
                    context, cancellationToken
                );
                await BootstrapperRunner.RunPhase
                (
                    modules, BootstrapperModulePhase.AfterInitialScene,
                    context, cancellationToken
                );
            }
        }

    #endregion

    #region E-1: Phase 필터링과 순서

        // ------------------------------------------------------------
        /// <summary>
        /// Asset 목록이 섞여 있어도 phase별 원래 순서를 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public async Task TEST_BootstrapperRunner_Phase순서와_Application독립종료()
        {
            var order = new List<int>();
            var assets = new List<BootstrapperModuleAsset>();
            var phases = new[]
            {
                BootstrapperModulePhase.AfterInitialScene,
                BootstrapperModulePhase.BeforeInitialScene,
                BootstrapperModulePhase.AfterInitialScene,
            };

            for (int index = 0; index < phases.Length; index++)
            {
                var asset = ScriptableObject.CreateInstance<ProbeModule>();
                int number = index;
                asset.TestPhase = phases[index];
                asset.Run = (_, _) =>
                {
                    order.Add(number);
                    return Task.CompletedTask;
                };
                assets.Add(asset);
            }

            var runtime = new ApplicationRuntime();
            try
            {
                IApplicationModule[] modules =
                {
                    new Startup(assets)
                };
                await runtime.StartAsync(modules);
                int[] expected =
                {
                    1, 0, 2
                };
                CollectionAssert.AreEqual(expected, order);
                Assert.AreEqual(ApplicationState.Running, runtime.State);
            }
            finally
            {
                await runtime.StopAsync();
                foreach (var asset in assets)
                {
                    UnityEngine.Object.DestroyImmediate(asset);
                }
            }

            Assert.AreEqual(ApplicationState.Stopped, runtime.State);
        }

    #endregion

    #region X-1: 모듈 실패 중단과 자원 정리

        // ------------------------------------------------------------
        /// <summary>
        /// 중간 모듈 실패 후 후속 모듈을 실행하지 않고 소유 자원을 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public async Task TEST_BootstrapperRunner_실패시_후속중단과Scope정리()
        {
            bool released = false;
            bool laterStarted = false;
            var first = ScriptableObject.CreateInstance<ProbeModule>();
            var failed = ScriptableObject.CreateInstance<ProbeModule>();
            var later = ScriptableObject.CreateInstance<ProbeModule>();
            first.TestPhase = BootstrapperModulePhase.BeforeInitialScene;
            failed.TestPhase = BootstrapperModulePhase.BeforeInitialScene;
            later.TestPhase = BootstrapperModulePhase.AfterInitialScene;
            first.Run = (context, _) =>
            {
                context.Scope.Own(new Lease(() => released = true));
                return Task.CompletedTask;
            };
            failed.Run = (_, _) => Task.FromException(new InvalidOperationException("failure"));
            later.Run = (_, _) =>
            {
                laterStarted = true;
                return Task.CompletedTask;
            };

            var runtime = new ApplicationRuntime();
            try
            {
                BootstrapperModuleAsset[] modules =
                {
                    first, failed, later
                };
                IApplicationModule[] startup =
                {
                    new Startup(modules)
                };
                var exception = Assert.ThrowsAsync<InvalidOperationException>
                (
                    async () => await runtime.StartAsync(startup)
                );
                StringAssert.Contains("failure", exception.Message);
                Assert.AreEqual(ApplicationState.Failed, runtime.State);
                Assert.IsTrue(released);
                Assert.IsFalse(laterStarted);
            }
            finally
            {
                await runtime.StopAsync();
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(failed);
                UnityEngine.Object.DestroyImmediate(later);
            }
        }

    #endregion

    #region C-1: 변경 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 모듈을 목록에 추가하고 다시 빼면 모듈 목록 변경 이벤트가 각각 한 번씩 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_BootstrapperSettings_모듈목록변경_이벤트발생()
        {
            var settings = ScriptableObject.CreateInstance<BootstrapperSettings>();
            var module = ScriptableObject.CreateInstance<ProbeModule>();
            var changes = 0;
            Action<BootstrapperSettings> handler = changed =>
            {
                if (changed == settings)
                {
                    changes++;
                }
            };

            BootstrapperSettings.OnModulesChange += handler;
            try
            {
                using var serialized = new SerializedObject(settings);
                var modules = serialized.FindProperty("modules");

                modules.InsertArrayElementAtIndex(0);
                modules.GetArrayElementAtIndex(0).objectReferenceValue = module;
                serialized.ApplyModifiedProperties();
                Assert.AreEqual(1, changes);

                modules.DeleteArrayElementAtIndex(0);
                serialized.ApplyModifiedProperties();
                Assert.AreEqual(2, changes);
            }
            finally
            {
                BootstrapperSettings.OnModulesChange -= handler;
                UnityEngine.Object.DestroyImmediate(settings);
                UnityEngine.Object.DestroyImmediate(module);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모듈 에셋의 값이 검증되면 모듈 변경 이벤트가 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_BootstrapperModuleAsset_값검증_이벤트발생()
        {
            var module = ScriptableObject.CreateInstance<ProbeModule>();
            var changes = 0;
            Action<BootstrapperModuleAsset> handler = changed =>
            {
                if (changed == module)
                {
                    changes++;
                }
            };

            BootstrapperModuleAsset.OnChange += handler;
            try
            {
                using var serialized = new SerializedObject(module);
                serialized.FindProperty("m_Name").stringValue = "Changed";
                serialized.ApplyModifiedProperties();
                Assert.GreaterOrEqual(changes, 1);
            }
            finally
            {
                BootstrapperModuleAsset.OnChange -= handler;
                UnityEngine.Object.DestroyImmediate(module);
            }
        }

    #endregion

    }
}
