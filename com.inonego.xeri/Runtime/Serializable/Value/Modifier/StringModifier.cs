/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : StringModifier.cs
수정일 : 2026-11-22

# 설명
string 값에 SET 연산을 적용하는 IModifier<string> 구현.

# 특이사항, 제약사항
Value 비교는 서수(Ordinal) 비교이며 값이 실제로 바뀔 때만 OnChange를 발행한다.
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
    public class StringModifier : OperationModifier<string, StringOperation>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// SET 연산이면 입력 값과 무관한 결과를 낸다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Replaces => operation == StringOperation.SET;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 연산(SET)과 null 값으로 수정자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public StringModifier()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 연산과 피연산자로 수정자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public StringModifier(StringOperation operation, string value) : base(operation, value)
        {
            // NONE
        }

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 빈 StringModifier 를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override IModifier<string> @new() => new StringModifier();

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Operation 에 따라 value 를 수정해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override string Modify(string value)
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
