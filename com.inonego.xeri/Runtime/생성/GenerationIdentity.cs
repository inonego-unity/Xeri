/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenerationIdentity.cs
수정일 : 2026-08-04

# 설명
Recipe·Slot·Pass를 묶어 하나의 생성 Subtree 또는 Pass 결과를 식별한다.

# 제약사항
도메인 오브젝트, 공간 좌표, Unity 참조를 포함하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Generation
{
    // ============================================================
    /// <summary>
    /// 부모가 예약한 하나의 생성 위치를 식별하는 안정 Slot이다.
    /// </summary>
    // ============================================================
    [Serializable]
    public readonly struct GenerationSlot : IEquatable<GenerationSlot>
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Slot을 식별하는 안정 Key다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationKey Key => key;

        private readonly GenerationKey key;

        // ------------------------------------------------------------
        /// <summary>
        /// Slot Key가 설정됐는지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDefined => key.IsDefined;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 안정 Key로 생성 Slot을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationSlot(GenerationKey key)
        {
            if (!key.IsDefined)
            {
                throw new ArgumentException("Generation Slot에는 정의된 Key가 필요합니다.", nameof(key));
            }

            this.key = key;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 Slot이 같은 안정 Key를 가졌는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Equals(GenerationSlot other)
        {
            return key.Equals(other.key);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 객체가 같은 Generation Slot인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Equals(object obj)
        {
            return obj is GenerationSlot other && Equals(other);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Slot의 런타임 Hash Code를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override int GetHashCode()
        {
            return key.GetHashCode();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 진단에 사용할 Slot Key 문자열을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToString()
        {
            return key.ToString();
        }

    #endregion
    }

    // ============================================================
    /// <summary>
    /// Recipe·Slot·Pass 조합으로 생성 입력과 결과의 범위를 식별한다.
    /// </summary>
    // ============================================================
    [Serializable]
    public readonly struct GenerationIdentity : IEquatable<GenerationIdentity>
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 이 결과를 만든 Recipe의 안정 Key다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationKey RecipeKey => recipeKey;

        private readonly GenerationKey recipeKey;

        // ------------------------------------------------------------
        /// <summary>
        /// 부모가 예약한 생성 위치다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationSlot Slot => slot;

        private readonly GenerationSlot slot;

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 Recipe와 Slot 안의 생성 Pass를 구분하는 Key다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationKey PassKey => passKey;

        private readonly GenerationKey passKey;

        // ------------------------------------------------------------
        /// <summary>
        /// Seed 파생과 Manifest 진단에 사용할 완전한 식별자인지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDefined => recipeKey.IsDefined && slot.IsDefined && passKey.IsDefined;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Recipe·Slot·Pass를 묶은 생성 식별자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationIdentity
        (
            GenerationKey recipeKey,
            GenerationSlot slot,
            GenerationKey passKey
        )
        {
            if (!recipeKey.IsDefined || !slot.IsDefined || !passKey.IsDefined)
            {
                throw new ArgumentException("Generation Identity의 Recipe, Slot, Pass는 모두 필요합니다.");
            }

            this.recipeKey = recipeKey;
            this.slot = slot;
            this.passKey = passKey;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 Identity가 같은 Recipe·Slot·Pass 조합인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Equals(GenerationIdentity other)
        {
            return recipeKey.Equals(other.recipeKey)
                && slot.Equals(other.slot)
                && passKey.Equals(other.passKey);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 객체가 같은 Generation Identity인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Equals(object obj)
        {
            return obj is GenerationIdentity other && Equals(other);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Dictionary와 Set에 사용할 Hash Code를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = recipeKey.GetHashCode();
                hash = (hash * 397) ^ slot.GetHashCode();
                hash = (hash * 397) ^ passKey.GetHashCode();
                return hash;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 진단에 사용할 계층 식별 문자열을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToString()
        {
            return $"{recipeKey}/{slot}/{passKey}";
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 두 Generation Identity가 같은지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator ==(GenerationIdentity left, GenerationIdentity right)
        {
            return left.Equals(right);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 두 Generation Identity가 다른지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator !=(GenerationIdentity left, GenerationIdentity right)
        {
            return !left.Equals(right);
        }

    #endregion
    }
}
