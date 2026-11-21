/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_VectorHelper.cs
수정일 : 2026-11-21

# 설명
VectorHelper.TryNormalize의 정규화 결과와 거절 조건을 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Utility._VectorHelper
{
    using inonego;
    using inonego.Xeri;

    // ============================================================
    /// <summary>
    /// VectorHelper 정규화 테스트.
    /// </summary>
    // ============================================================
    public class TEST_VectorHelper
    {
        private static void AssertUnit(Vector3 expected, Vector3 actual)
        {
            Assert.That((expected - actual).magnitude, Is.LessThan(1e-6f), $"expected {expected} but was {actual}");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 일반·아주 작은·아주 큰 유한 벡터를 같은 단위 벡터로 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_VectorHelper_TryNormalize_유한벡터정규화()
        {
            Assert.That(new Vector3(0f, 2f, 0f).TryNormalize(out var up), Is.True);
            AssertUnit(Vector3.up, up);

            Assert.That(new Vector3(2e-6f, 0f, 0f).TryNormalize(out var tiny), Is.True);
            AssertUnit(Vector3.right, tiny);

            // float 제곱이 넘치는 크기도 double로 계산해 정규화한다.
            Assert.That(new Vector3(1e20f, 0f, 0f).TryNormalize(out var large), Is.True);
            AssertUnit(Vector3.right, large);

            Assert.That(new Vector3(3e38f, 3e38f, 0f).TryNormalize(out var diagonal), Is.True);
            AssertUnit(new Vector3(1f, 1f, 0f).normalized, diagonal);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 0 벡터와 유한하지 않은 벡터는 거절하고 0을 돌려준다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_VectorHelper_TryNormalize_영벡터와비유한거절()
        {
            Assert.That(Vector3.zero.TryNormalize(out var zero), Is.False);
            Assert.That(zero, Is.EqualTo(Vector3.zero));

            Assert.That(new Vector3(float.NaN, 1f, 0f).TryNormalize(out var nan), Is.False);
            Assert.That(nan, Is.EqualTo(Vector3.zero));

            Assert.That(new Vector3(float.PositiveInfinity, 0f, 0f).TryNormalize(out var infinity), Is.False);
            Assert.That(infinity, Is.EqualTo(Vector3.zero));
        }
    }
}
