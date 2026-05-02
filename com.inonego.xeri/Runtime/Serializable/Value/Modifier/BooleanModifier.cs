/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : BooleanModifier.cs
수정일 : 2026-05-02

# 설명
bool 값에 SET / AND / OR / XOR 논리 연산을 적용하는 IModifier<bool> 구현.
NOT 정적 인스턴스를 제공한다(XOR true).
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// BooleanModifier 가 수행할 논리 연산 종류.
    /// </summary>
    // ============================================================
    public enum BooleanOperation
    {
        SET, AND, OR, XOR
    }

    // ============================================================
    /// <summary>
    /// bool 값에 논리 연산을 적용하는 수정자.
    /// </summary>
    // ============================================================
    [Serializable]
    public class BooleanModifier : IModifier<bool>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// NOT 연산(XOR true) 수정자 인스턴스.
        /// </summary>
        // ------------------------------------------------------------
        public static BooleanModifier NOT => new BooleanModifier(BooleanOperation.XOR, true);

    #region 필드

        [SerializeField]
        protected BooleanOperation operation;
        public virtual BooleanOperation Operation
        {
            get => operation;
            set => operation = value;
        }

        [SerializeField]
        protected bool value;
        public virtual bool Value
        {
            get => value;
            set => this.value = value;
        }

    #endregion

    #region 생성자

        public BooleanModifier() {}

        public BooleanModifier(BooleanOperation operation, bool value)
        {
            this.operation = operation;
            this.value = value;
        }

    #endregion

    #region 메서드

        // -----------------------------------------------------------------
        /// <summary>
        /// Operation 에 따라 value 를 수정해 반환한다.
        /// </summary>
        // -----------------------------------------------------------------
        public bool Modify(bool value)
        {
            return operation switch
            {
                BooleanOperation.SET => this.value,
                BooleanOperation.AND => value && this.value,
                BooleanOperation.OR  => value || this.value,
                BooleanOperation.XOR => value ^  this.value,
                _ => value
            };
        }

    #endregion

    #region 복제

        public IModifier<bool> @new() => new BooleanModifier();

        public void CloneFrom(IModifier<bool> source)
        {
            if (source is BooleanModifier other)
            {
                operation = other.operation;
                value     = other.value;
            }
        }

    #endregion

    }
}
