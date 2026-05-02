/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : NumericFModifier.cs
수정일 : 2026-05-02

# 설명
float 값에 SET / ADD / SUB / MUL / DIV 수치 연산을 적용하는 IModifier<float> 구현.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// NumericFModifier 가 수행할 수치 연산 종류.
    /// </summary>
    // ============================================================
    public enum NumericFOperation
    {
        SET, ADD, SUB, MUL, DIV
    }

    // ============================================================
    /// <summary>
    /// float 값에 수치 연산을 적용하는 수정자.
    /// </summary>
    // ============================================================
    [Serializable]
    public class NumericFModifier : IModifier<float>
    {

    #region 필드

        [SerializeField]
        protected NumericFOperation operation;
        public virtual NumericFOperation Operation
        {
            get => operation;
            set => operation = value;
        }

        [SerializeField]
        protected float value;
        public virtual float Value
        {
            get => value;
            set => this.value = value;
        }

    #endregion

    #region 생성자

        public NumericFModifier() {}

        public NumericFModifier(NumericFOperation operation, float value)
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
        public float Modify(float value)
        {
            return operation switch
            {
                NumericFOperation.SET => this.value,
                NumericFOperation.ADD => value + this.value,
                NumericFOperation.SUB => value - this.value,
                NumericFOperation.MUL => value * this.value,
                NumericFOperation.DIV => value / this.value,
                _ => value
            };
        }

    #endregion

    #region 복제

        public IModifier<float> @new() => new NumericFModifier();

        public void CloneFrom(IModifier<float> source)
        {
            if (source is NumericFModifier other)
            {
                operation = other.operation;
                value     = other.value;
            }
        }

    #endregion

    }
}
