/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : BooleanModifier.cs
수정일 : 2026-11-22

# 설명
bool 값에 SET / AND / OR / XOR 논리 연산을 적용하는 IModifier<bool> 구현.
NOT 정적 인스턴스를 제공한다.
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
    public class BooleanModifier : OperationModifier<bool, BooleanOperation>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// NOT 연산(XOR true) 수정자 인스턴스.
        /// </summary>
        // ------------------------------------------------------------
        public static BooleanModifier NOT => new BooleanModifier(BooleanOperation.XOR, true);

        // ------------------------------------------------------------
        /// <summary>
        /// SET 연산이면 입력 값과 무관한 결과를 낸다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Replaces => operation == BooleanOperation.SET;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 연산(SET)과 false 값으로 수정자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public BooleanModifier()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 연산과 피연산자로 수정자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public BooleanModifier(BooleanOperation operation, bool value) : base(operation, value)
        {
            // NONE
        }

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 빈 BooleanModifier 를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override IModifier<bool> @new() => new BooleanModifier();

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Operation 에 따라 value 를 수정해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Modify(bool value)
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
