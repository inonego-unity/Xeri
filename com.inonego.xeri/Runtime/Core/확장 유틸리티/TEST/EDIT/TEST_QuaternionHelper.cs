/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_QuaternionHelper.cs
수정일 : 2026-11-22

# 설명
QuaternionHelper.TryNormalize의 정규화 결과와 거절 조건을 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Utility._QuaternionHelper
{
    using inonego;
    using inonego.Xeri;

    // ============================================================
    /// <summary>
    /// QuaternionHelper 정규화 테스트.
    /// </summary>
    // ============================================================
    public class TEST_QuaternionHelper
    {
        private static void AssertUnit(Quaternion expected, Quaternion actual)
        {
            var dot = expected.x * actual.x + expected.y * actual.y + expected.z * actual.z + expected.w * actual.w;

            Assert.That(dot, Is.EqualTo(1f).Within(1e-6f), $"expected {expected} but was {actual}");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 단위·크기가 다른·아주 작은·아주 큰 유한 값을 같은 단위 Quaternion으로 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_QuaternionHelper_TryNormalize_유한값정규화()
        {
            var half = Mathf.Sqrt(0.5f);
            var quarter = new Quaternion(0f, half, 0f, half);

            Assert.That(quarter.TryNormalize(out var unit), Is.True);
            AssertUnit(quarter, unit);

            Assert.That(new Quaternion(0f, 2f * half, 0f, 2f * half).TryNormalize(out var scaled), Is.True);
            AssertUnit(quarter, scaled);

            Assert.That(new Quaternion(0f, 0f, 0f, 2e-6f).TryNormalize(out var tiny), Is.True);
            AssertUnit(Quaternion.identity, tiny);

            // float 제곱이 넘치는 크기도 double로 계산해 정규화한다.
            Assert.That(new Quaternion(1e20f, 0f, 0f, 1e20f).TryNormalize(out var large), Is.True);
            AssertUnit(new Quaternion(half, 0f, 0f, half), large);

            Assert.That(new Quaternion(3e38f, 3e38f, 3e38f, 3e38f).TryNormalize(out var diagonal), Is.True);
            AssertUnit(new Quaternion(0.5f, 0.5f, 0.5f, 0.5f), diagonal);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 성분이 0인 값과 유한하지 않은 값은 거절하고 회전 없음을 돌려준다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_QuaternionHelper_TryNormalize_영과비유한거절()
        {
            Assert.That(new Quaternion(0f, 0f, 0f, 0f).TryNormalize(out var zero), Is.False);
            Assert.That(zero, Is.EqualTo(Quaternion.identity));

            Assert.That(new Quaternion(float.NaN, 0f, 0f, 1f).TryNormalize(out var nan), Is.False);
            Assert.That(nan, Is.EqualTo(Quaternion.identity));

            Assert.That(new Quaternion(0f, float.PositiveInfinity, 0f, 1f).TryNormalize(out var infinity), Is.False);
            Assert.That(infinity, Is.EqualTo(Quaternion.identity));
        }
    }
}
