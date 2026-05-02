/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : StringModifier.cs
수정일 : 2026-05-02

# 설명
string 값에 SET 연산을 적용하는 IModifier<string> 구현.
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
            set => operation = value;
        }

        [SerializeField]
        protected string value;
        public virtual string Value
        {
            get => value;
            set => this.value = value;
        }

    #endregion

    #region 생성자

        public StringModifier() {}

        public StringModifier(StringOperation operation, string value)
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
        public string Modify(string value)
        {
            return operation switch
            {
                StringOperation.SET => this.value,
                _ => value
            };
        }

    #endregion

    #region 복제

        public IModifier<string> @new() => new StringModifier();

        public void CloneFrom(IModifier<string> source)
        {
            if (source is StringModifier other)
            {
                operation = other.operation;
                value     = other.value;
            }
        }

    #endregion

    }
}
