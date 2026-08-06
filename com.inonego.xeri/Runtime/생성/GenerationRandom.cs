/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenerationRandom.cs
수정일 : 2026-08-04

# 설명
Generation Seed에서 시작하는 결정적 난수열을 제공한다.

# 제약사항
분포 선택·Noise·가중치·배치 점수 같은 도메인 알고리즘은 포함하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Generation
{
    // ============================================================
    /// <summary>
    /// Subtree Seed에만 의존하는 결정적 의사 난수열이다.
    /// </summary>
    // ============================================================
    public struct GenerationRandom
    {
    #region 내부 데이터

        private const ulong Increment = 0x9E3779B97F4A7C15UL;
        private const ulong Multiplier0 = 0xBF58476D1CE4E5B9UL;
        private const ulong Multiplier1 = 0x94D049BB133111EBUL;

    #endregion

    #region 필드

        private ulong state;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 Seed에서 난수열을 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationRandom(GenerationSeed seed)
        {
            state = seed.Value;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 다음 균등 64비트 난수 값을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public ulong NextUInt64()
        {
            state += Increment;
            var value = state;
            value = (value ^ (value >> 30)) * Multiplier0;
            value = (value ^ (value >> 27)) * Multiplier1;
            return value ^ (value >> 31);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// minInclusive 이상 maxExclusive 미만의 균등 정수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            var range = (ulong)((long)maxExclusive - minInclusive);
            var limit = ulong.MaxValue - (ulong.MaxValue % range);
            ulong value;

            do
            {
                value = NextUInt64();
            }
            while (value >= limit);

            return (int)(minInclusive + (long)(value % range));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 0 이상 1 미만의 균등 부동소수 값을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public float NextFloat01()
        {
            const float divisor = 1 << 24;
            return (NextUInt64() >> 40) / divisor;
        }

    #endregion
    }
}
