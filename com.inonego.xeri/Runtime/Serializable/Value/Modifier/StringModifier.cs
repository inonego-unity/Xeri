/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : StringModifier.cs
수정일 : 2026-09-18

# 설명
string 값에 SET 연산을 적용하는 IModifier<string> 구현.
Operation 또는 Value가 실제로 변경되면 OnChange를 발행한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// StringModifier 가 수행할 문자열 연산 종류.
    /// </summary>
    // ============================================================
    public enum StringOperation
    {
        SET
    }

    // ============================================================
    /// <summary>
    /// string 값에 문자열 연산을 적용하는 수정자.
    /// </summary>
    // ============================================================
    [Serializable]
    public class StringModifier : IModifier<string>
    {

    #region 필드

        [SerializeField]
        protected StringOperation operation;
        public virtual StringOperation Operation
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
        protected string value;
        public virtual string Value
        {
            get => value;
            set
            {
                if (string.Equals(this.value, value, StringComparison.Ordinal)) return;

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

        public StringModifier()
        {
            // NONE
        }

        public StringModifier(StringOperation operation, string value)
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
        public string Modify(string value)
        {
            return operation switch
            {
                StringOperation.SET => this.value,
                _ => value
            };
        }

    #endregion

    }
}
