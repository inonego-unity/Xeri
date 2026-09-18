/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : NumericIModifier.cs
수정일 : 2026-09-18

# 설명
int 값에 SET / ADD / SUB / MUL / DIV 수치 연산을 적용하는 IModifier<int> 구현.
Operation 또는 Value가 실제로 변경되면 OnChange를 발행한다.
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
            set
            {
                if (operation == value) return;

                operation = value;

                InvokeOnChange();
            }
        }

        [SerializeField]
        protected int value;
        public virtual int Value
        {
            get => value;
            set
            {
                if (this.value == value) return;

                this.value = value;

                InvokeOnChange();
            }
        }

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Modify 결과에 영향을 주는 내부 상태가 변경된 뒤 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        [field: NonSerialized]
        public event Action OnChange = null;

        // ------------------------------------------------------------
        /// <summary>
        /// OnChange를 발생시킨다.
        /// </summary>
        // ------------------------------------------------------------
        protected void InvokeOnChange() => OnChange?.Invoke();

    #endregion

    #region 생성자

        public NumericIModifier()
        {
            // NONE
        }

        public NumericIModifier(NumericIOperation operation, int value)
        {
            this.operation = operation;
            this.value = value;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Operation 에 따라 value 를 수정해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
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

    }
}
