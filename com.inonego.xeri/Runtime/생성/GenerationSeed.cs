/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenerationSeed.cs
수정일 : 2026-08-04

# 설명
Root Seed에서 안정 Generation Identity별 Subtree Seed를 결정적으로 파생한다.

# 제약사항
난수 분포 선택이나 도메인 배치 규칙은 소유하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System.Text;

namespace inonego.Xeri.Generation
{
    // ============================================================
    /// <summary>
    /// 생성 결과를 결정적으로 파생하기 위한 64비트 Seed다.
    /// </summary>
    // ============================================================
    public readonly struct GenerationSeed
    {
    #region 내부 데이터

        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Seed의 원본 64비트 값이다.
        /// </summary>
        // ------------------------------------------------------------
        public ulong Value => value;

        private readonly ulong value;

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
        /// 안정 Recipe·Slot·Pass 조합에 대응하는 독립 Subtree Seed를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationSeed Derive(GenerationIdentity identity)
        {
            if (!identity.IsDefined)
            {
                throw new System.ArgumentException("Seed 파생에는 정의된 Generation Identity가 필요합니다.", nameof(identity));
            }

            var hash = HashUInt64(FnvOffsetBasis, value);

            // 각 Key 앞에 길이를 포함해 서로 다른 Key 묶음이 같은 바이트열이 되지 않게 한다.
            hash = HashKey(hash, identity.RecipeKey);
            hash = HashKey(hash, identity.Slot.Key);
            hash = HashKey(hash, identity.PassKey);
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

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Subtree Seed에서 지정 재시도 전용 Seed를 결정적으로 파생한다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationSeed DeriveAttempt(int attemptIndex)
        {
            if (attemptIndex < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(attemptIndex));
            }

            if (attemptIndex == 0)
            {
                return this;
            }

            var hash = HashUInt64(FnvOffsetBasis, value);
            hash = HashUInt64(hash, (ulong)attemptIndex);
            return new GenerationSeed(hash);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 진단에 사용할 Seed의 16진수 문자열을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToString()
        {
            return value.ToString("X16");
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

        // ------------------------------------------------------------
        /// <summary>
        /// 안정 Key를 길이와 UTF-8 바이트 순서로 FNV-1a 입력에 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        private static ulong HashKey(ulong hash, GenerationKey key)
        {
            var bytes = Encoding.UTF8.GetBytes(key.Value);
            hash = HashUInt64(hash, (ulong)bytes.Length);

            foreach (var value in bytes)
            {
                hash ^= value;
                hash *= FnvPrime;
            }

            return hash;
        }

    #endregion
    }
}
