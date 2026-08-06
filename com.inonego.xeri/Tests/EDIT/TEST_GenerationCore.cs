/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_GenerationCore.cs
수정일 : 2026-08-04

# 설명
도메인 없이 Generation Core가 보장해야 하는 Seed·Composite·Retry·검증 관문 계약을 검증한다.

# 테스트 구성
 D: 결정적 Seed와 계획된 Child Slot의 독립성
 R: 제한된 Retry의 실패 전달
 V: Validator와 Instantiator 경계
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using NUnit.Framework;

using inonego.Xeri.Generation;

namespace inonego.Xeri.TEST.EDIT._Generation
{
    // ============================================================
    /// <summary>
    /// 도메인 독립 Generation Core의 공개 계약을 검증한다.
    /// </summary>
    // ============================================================
    public sealed class TEST_GenerationCore
    {
    #region D-1: 계획된 Child Slot의 독립성

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 형제 Slot의 실행 순서가 달라도 같은 Identity가 받은 Seed와 Manifest가 유지되는지 검증한다.
        /// <br/> 부모가 모든 Slot을 먼저 예약하면 형제 순서가 기존 Subtree 결과를 바꾸지 않아야 한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_GenerationComposite_형제실행순서가달라도안정Slot의Seed를유지한다()
        {
            var parentSeed = new GenerationSeed(12345UL);
            var firstSlot = CreateSlot("first");
            var secondSlot = CreateSlot("second");
            var pipeline = new GenerationPipeline<int, TestManifest>(new SeedManifestNode(), new ValidManifestValidator());

            var forward = GenerationComposite.GenerateChildren
            (
                pipeline,
                parentSeed,
                new[] { firstSlot, secondSlot }
            );
            var reversed = GenerationComposite.GenerateChildren
            (
                pipeline,
                parentSeed,
                new[] { secondSlot, firstSlot }
            );

            // 각 Slot의 Seed는 부모 Seed와 자기 Identity만으로 파생되므로 실행 위치와 무관해야 한다.
            Assert.That(GetManifestSeed(forward, firstSlot.Identity), Is.EqualTo(GetManifestSeed(reversed, firstSlot.Identity)));
            Assert.That(GetManifestSeed(forward, secondSlot.Identity), Is.EqualTo(GetManifestSeed(reversed, secondSlot.Identity)));
        }

    #endregion

    #region R-1: 유한 Retry 실패 전달

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 계속 실패하는 Node가 Retry 상한을 넘지 않고 마지막 Failure를 반환하는지 검증한다.
        /// <br/> 생성 실패가 무한 실행으로 이어지지 않는다는 Retry Pipeline의 공개 계약이다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_GenerationRetryPipeline_계속실패하면상한뒤마지막Failure를반환한다()
        {
            var node = new AlwaysFailNode();
            var pipeline = new GenerationRetryPipeline<int, TestManifest>
            (
                node,
                new ValidManifestValidator(),
                new GenerationFixedRetryPolicy(3)
            );

            var execution = pipeline.Generate(CreateContext("retry"));

            Assert.That(execution.HasExecution, Is.False);
            Assert.That(execution.AttemptCount, Is.EqualTo(3));
            Assert.That(execution.Failure.AttemptIndex, Is.EqualTo(2));
            Assert.That(node.CallCount, Is.EqualTo(3));
        }

    #endregion

    #region V-1: Validator와 Instantiator 경계

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> Validator Error가 있는 Manifest가 Instantiator까지 전달되지 않는지 검증한다.
        /// <br/> 생성 결과의 Runtime 변환은 검증 통과 뒤에만 가능해야 한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_GenerationPipeline_검증오류Manifest의인스턴스화를차단한다()
        {
            var pipeline = new GenerationPipeline<int, TestManifest>(new SeedManifestNode(), new InvalidManifestValidator());
            var instantiator = new CountingInstantiator();
            var execution = pipeline.Generate(CreateContext("invalid"));

            Assert.That(execution.IsValid, Is.False);
            Assert.Throws<InvalidOperationException>(() => pipeline.Instantiate(execution, instantiator));
            Assert.That(instantiator.CallCount, Is.EqualTo(0));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 기본 생성된 Validation Result가 빈 진단 목록으로 안전하게 읽히는지 검증한다.
        /// <br/> Validator 구현이 기본값을 반환해도 소비자가 null 목록을 별도로 처리하지 않아야 한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_GenerationValidationResult_기본값에서도빈Issues를제공한다()
        {
            var result = default(GenerationValidationResult);

            Assert.That(result.Issues, Is.Not.Null);
            Assert.That(result.Issues, Is.Empty);
        }

    #endregion

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트마다 독립적인 Generation Context를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private static GenerationContext<int> CreateContext(string slotKey)
        {
            var identity = CreateIdentity(slotKey);
            return new GenerationContext<int>(identity, new GenerationSeed(98765UL), 0);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트용 Child Slot을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private static GenerationChildSlot<int> CreateSlot(string slotKey)
        {
            return new GenerationChildSlot<int>(CreateIdentity(slotKey), 0);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트용 안정 Generation Identity를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private static GenerationIdentity CreateIdentity(string slotKey)
        {
            return new GenerationIdentity
            (
                new GenerationKey("test.recipe"),
                new GenerationSlot(new GenerationKey(slotKey)),
                new GenerationKey("test.pass")
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실행 결과 목록에서 지정 Identity의 Manifest Seed를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private static ulong GetManifestSeed
        (
            IReadOnlyList<GenerationChildExecution<int, TestManifest>> executions,
            GenerationIdentity identity
        )
        {
            for (var index = 0; index < executions.Count; index++)
            {
                var execution = executions[index];

                if (execution.Slot.Identity == identity)
                {
                    return execution.Execution.Manifest.Seed.Value;
                }
            }

            throw new AssertionException("지정한 Generation Identity의 실행 결과를 찾지 못했습니다.");
        }

    #endregion

    #region 테스트 구현체

        // ============================================================
        /// <summary>
        /// Generation Core 계약을 관찰하기 위한 최소 순수 Manifest다.
        /// </summary>
        // ============================================================
        private sealed class TestManifest : IGenerationManifest
        {
        #region 필드

            // ------------------------------------------------------------
            /// <summary>
            /// Manifest를 만든 안정 Identity다.
            /// </summary>
            // ------------------------------------------------------------
            public GenerationIdentity Identity => identity;

            private readonly GenerationIdentity identity;

            // ------------------------------------------------------------
            /// <summary>
            /// Manifest에 전달된 Subtree Seed다.
            /// </summary>
            // ------------------------------------------------------------
            public GenerationSeed Seed => seed;

            private readonly GenerationSeed seed;

            // ------------------------------------------------------------
            /// <summary>
            /// 테스트 Generator의 고정 버전이다.
            /// </summary>
            // ------------------------------------------------------------
            public string GeneratorVersion => "test";

        #endregion

        #region 생성자

            // ------------------------------------------------------------
            /// <summary>
            /// Identity와 Seed를 보관하는 테스트 Manifest를 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public TestManifest(GenerationIdentity identity, GenerationSeed seed)
            {
                this.identity = identity;
                this.seed = seed;
            }

        #endregion
        }

        // ============================================================
        /// <summary>
        /// 전달받은 Context의 Identity와 Seed를 그대로 Manifest로 만드는 Node다.
        /// </summary>
        // ============================================================
        private sealed class SeedManifestNode : IGenerationNode<int, TestManifest>
        {
        #region 메서드

            // ------------------------------------------------------------
            /// <summary>
            /// Context 관찰용 순수 Manifest를 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public TestManifest Generate(GenerationContext<int> context)
            {
                return new TestManifest(context.Identity, context.Seed);
            }

        #endregion
        }

        // ============================================================
        /// <summary>
        /// Error 없이 유효한 Validation Result를 반환하는 Validator다.
        /// </summary>
        // ============================================================
        private sealed class ValidManifestValidator : IGenerationValidator<TestManifest>
        {
        #region 메서드

            // ------------------------------------------------------------
            /// <summary>
            /// 빈 진단 목록으로 유효한 결과를 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public GenerationValidationResult Validate(TestManifest manifest)
            {
                return new GenerationValidationResult(Array.Empty<GenerationValidationIssue>());
            }

        #endregion
        }

        // ============================================================
        /// <summary>
        /// Instantiator 차단 경계를 검증하기 위해 Error를 반환하는 Validator다.
        /// </summary>
        // ============================================================
        private sealed class InvalidManifestValidator : IGenerationValidator<TestManifest>
        {
        #region 메서드

            // ------------------------------------------------------------
            /// <summary>
            /// Manifest와 무관하게 Error 진단 하나를 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public GenerationValidationResult Validate(TestManifest manifest)
            {
                var issues = new[]
                {
                    new GenerationValidationIssue
                    (
                        new GenerationKey("test.invalid"),
                        GenerationIssueSeverity.Error,
                        "테스트용 검증 오류입니다."
                    ),
                };
                return new GenerationValidationResult(issues);
            }

        #endregion
        }

        // ============================================================
        /// <summary>
        /// 항상 Failure를 반환하고 호출 횟수를 기록하는 Attempt Node다.
        /// </summary>
        // ============================================================
        private sealed class AlwaysFailNode : IGenerationAttemptNode<int, TestManifest>
        {
        #region 필드

            // ------------------------------------------------------------
            /// <summary>
            /// Pipeline이 Node를 호출한 횟수다.
            /// </summary>
            // ------------------------------------------------------------
            public int CallCount => callCount;

            private int callCount;

        #endregion

        #region 메서드

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 Attempt를 포함한 구조화된 Failure를 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public GenerationNodeResult<TestManifest> Generate
            (
                GenerationContext<int> context,
                GenerationAttempt attempt
            )
            {
                callCount++;
                var failure = new GenerationFailure
                (
                    context.Identity,
                    new GenerationKey("test.failure"),
                    attempt.Index,
                    "테스트용 생성 실패입니다."
                );
                return GenerationNodeResult<TestManifest>.Failed(failure);
            }

        #endregion
        }

        // ============================================================
        /// <summary>
        /// Pipeline이 호출했는지만 기록하는 Instantiator다.
        /// </summary>
        // ============================================================
        private sealed class CountingInstantiator : IGenerationInstantiator<TestManifest, string>
        {
        #region 필드

            // ------------------------------------------------------------
            /// <summary>
            /// Pipeline이 Runtime 변환을 요청한 횟수다.
            /// </summary>
            // ------------------------------------------------------------
            public int CallCount => callCount;

            private int callCount;

        #endregion

        #region 메서드

            // ------------------------------------------------------------
            /// <summary>
            /// 검증된 Manifest를 전달받은 사실을 기록한다.
            /// </summary>
            // ------------------------------------------------------------
            public string Instantiate(ValidatedGenerationManifest<TestManifest> manifest)
            {
                if (!manifest.IsValidated)
                {
                    throw new InvalidOperationException("Pipeline이 검증한 Manifest만 받을 수 있습니다.");
                }

                callCount++;
                return manifest.Manifest.Identity.ToString();
            }

        #endregion
        }

    #endregion
    }
}
