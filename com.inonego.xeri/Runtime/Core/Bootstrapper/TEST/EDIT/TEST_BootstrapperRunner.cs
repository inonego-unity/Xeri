/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_BootstrapperRunner.cs
수정일 : 2026-11-03

# 설명
Bootstrapper phase의 순차 실행과 실패 시 Application 수명 정리를 검증한다.

# 테스트 구성
 E: Scene phase 필터링과 실행 순서
 X: 초기화 실패와 소유권 정리
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

    }
}
