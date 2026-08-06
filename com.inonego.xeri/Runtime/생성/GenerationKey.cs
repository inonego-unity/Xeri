/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenerationKey.cs
수정일 : 2026-08-04

# 설명
생성 Recipe·Pass 등 결과 재현과 진단에 사용할 안정 문자열 Key를 표현한다.

# 제약사항
Unity Instance ID나 배열 Index를 대체하지 않으며, 호출자가 안정성 정책을 정한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Generation
{
    // ============================================================
    /// <summary>
    /// 생성 입력과 결과를 식별하는 불변 문자열 Key다.
    /// </summary>
    // ============================================================
    [Serializable]
    public readonly struct GenerationKey : IEquatable<GenerationKey>
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 정의되지 않은 Key를 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public static GenerationKey Empty => default;

        // ------------------------------------------------------------
        /// <summary>
        /// Key의 원본 문자열이다.
        /// </summary>
        // ------------------------------------------------------------
        public string Value => value;

        private readonly string value;

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 식별 문자열이 설정됐는지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDefined => !string.IsNullOrWhiteSpace(value);

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 비어 있지 않은 안정 문자열로 생성 Key를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Generation Key를 비워 둘 수 없습니다.", nameof(value));
            }

            this.value = value;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 문자열을 가진 다른 Generation Key와 같은지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Equals(GenerationKey other)
        {
            return string.Equals(value, other.value, StringComparison.Ordinal);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 객체가 같은 Generation Key인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Equals(object obj)
        {
            return obj is GenerationKey other && Equals(other);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Key의 런타임 Hash Code를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override int GetHashCode()
        {
            return value == null ? 0 : StringComparer.Ordinal.GetHashCode(value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 진단과 로그에 표시할 Key 문자열을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToString()
        {
            return value ?? string.Empty;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 두 Generation Key가 같은 문자열을 가졌는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator ==(GenerationKey left, GenerationKey right)
        {
            return left.Equals(right);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 두 Generation Key가 다른 문자열을 가졌는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator !=(GenerationKey left, GenerationKey right)
        {
            return !left.Equals(right);
        }

    #endregion
    }
}
