/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : NumericIModifier.cs
수정일 : 2026-05-02

# 설명
int 값에 SET / ADD / SUB / MUL / DIV 수치 연산을 적용하는 IModifier<int> 구현.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// NumericIModifier 가 수행할 수치 연산 종류.
    /// </summary>
    // ============================================================
    public enum NumericIOperation
    {
        SET, ADD, SUB, MUL, DIV
    }

    // ============================================================
    /// <summary>
    /// int 값에 수치 연산을 적용하는 수정자.
    /// </summary>
    // ============================================================
    [Serializable]
    public class NumericIModifier : IModifier<int>
    {

    #region 필드

        [SerializeField]
        protected NumericIOperation operation;
        public virtual NumericIOperation Operation
        {
            get => operation;
            set => operation = value;
        }

        [SerializeField]
        protected int value;
        public virtual int Value
        {
            get => value;
            set => this.value = value;
        }

    #endregion

    #region 생성자

        public NumericIModifier() {}

        public NumericIModifier(NumericIOperation operation, int value)
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
        public int Modify(int value)
        {
            return operation switch
            {
                NumericIOperation.SET => this.value,
                NumericIOperation.ADD => value + this.value,
                NumericIOperation.SUB => value - this.value,
                NumericIOperation.MUL => value * this.value,
                NumericIOperation.DIV => value / this.value,
                _ => value
            };
        }

    #endregion

    #region 복제

        public IModifier<int> @new() => new NumericIModifier();

        public void CloneFrom(IModifier<int> source)
        {
            if (source is NumericIModifier other)
            {
                operation = other.operation;
                value     = other.value;
            }
        }

    #endregion

    }
}
