/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenerationSeed.cs
수정일 : 2026-08-24

# 설명
Root Seed에서 안정 문자열 Key별 독립 Seed를 결정적으로 파생한다.

# 제약사항
Recipe·Slot·Pass 같은 생성 구조와 재시도 정책을 강제하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Text;

using UnityEngine;

namespace inonego.Xeri.Generation
{
    // ============================================================
    /// <summary>
    /// 생성 결과를 결정적으로 파생하기 위한 64비트 Seed.
    /// </summary>
    // ============================================================
    [Serializable]
    public struct GenerationSeed : IEquatable<GenerationSeed>
    {
    #region 내부 데이터

        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Seed의 원본 64비트 값.
        /// </summary>
        // ------------------------------------------------------------
        public ulong Value => value;

        [SerializeField]
        private ulong value;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 64비트 값으로 Generation Seed를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationSeed(ulong value)
        {
            this.value = value;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Seed에서 안정 문자열 Key에 대응하는 독립 Seed를 파생한다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationSeed Derive(string stableKey)
        {
            if (string.IsNullOrWhiteSpace(stableKey))
            {
                throw new ArgumentException("Seed 파생 Key를 비워 둘 수 없습니다.", nameof(stableKey));
            }

            var hash = HashUInt64(FnvOffsetBasis, value);
            var bytes = Encoding.UTF8.GetBytes(stableKey);
            hash = HashUInt64(hash, (ulong)bytes.Length);

            foreach (var item in bytes)
            {
                hash ^= item;
                hash *= FnvPrime;
            }

            return new GenerationSeed(hash);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Seed에서 시작하는 독립적인 결정적 난수열을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationRandom CreateRandom()
        {
            return new GenerationRandom(this);
        }

        public bool Equals(GenerationSeed other)
        {
            return value == other.value;
        }

        public override bool Equals(object obj)
        {
            return obj is GenerationSeed other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value.ToString("X16");
        }

        public static bool operator ==(GenerationSeed left, GenerationSeed right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GenerationSeed left, GenerationSeed right)
        {
            return !left.Equals(right);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 64비트 값을 FNV-1a 입력으로 누적한다.
        /// </summary>
        // ------------------------------------------------------------
        private static ulong HashUInt64(ulong hash, ulong input)
        {
            for (var index = 0; index < sizeof(ulong); index++)
            {
                hash ^= (byte)input;
                hash *= FnvPrime;
                input >>= 8;
            }

            return hash;
        }

    #endregion
    }
}
