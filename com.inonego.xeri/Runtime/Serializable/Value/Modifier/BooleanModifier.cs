/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : BooleanModifier.cs
수정일 : 2026-09-18

# 설명
bool 값에 SET / AND / OR / XOR 논리 연산을 적용하는 IModifier<bool> 구현.
NOT 정적 인스턴스를 제공하며 Operation 또는 Value 변경 시 OnChange를 발행한다.
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

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// NOT 연산(XOR true) 수정자 인스턴스.
        /// </summary>
        // ------------------------------------------------------------
        public static BooleanModifier NOT => new BooleanModifier(BooleanOperation.XOR, true);

        [SerializeField]
        protected BooleanOperation operation;
        public virtual BooleanOperation Operation
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
        protected bool value;
        public virtual bool Value
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

        public BooleanModifier()
        {
            // NONE
        }

        public BooleanModifier(BooleanOperation operation, bool value)
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
        public bool Modify(bool value)
        {
            return operation switch
            {
                BooleanOperation.SET => this.value,
                BooleanOperation.AND => value && this.value,
                BooleanOperation.OR  => value || this.value,
                BooleanOperation.XOR => value ^ this.value,
                _ => value
            };
        }

    #endregion

    }
}
