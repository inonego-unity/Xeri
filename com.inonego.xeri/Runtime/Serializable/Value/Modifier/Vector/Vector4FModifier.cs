/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Vector4FModifier.cs
수정일 : 2026-11-22

# 설명
Vector4 값에 NumericFOperation 수치 연산을 성분마다 적용하는 IModifier<Vector4> 구현.
연산 규칙은 NumericFOperationHelper.Apply의 Vector4 오버로드와 같다.

# 특이사항, 제약사항
MIN·MAX는 성분별 비교이며 벡터 크기 제한이 아니다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// Vector4 값에 수치 연산을 성분마다 적용하는 수정자.
    /// </summary>
    // ============================================================
    [Serializable]
    public class Vector4FModifier : OperationModifier<Vector4, NumericFOperation>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// SET 연산이면 입력 값과 무관한 결과를 낸다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Replaces => operation == NumericFOperation.SET;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 연산(SET)과 0 벡터로 수정자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public Vector4FModifier()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 연산과 피연산자로 수정자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public Vector4FModifier(NumericFOperation operation, Vector4 value) : base(operation, value)
        {
            // NONE
        }

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 빈 Vector4FModifier 를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override IModifier<Vector4> @new() => new Vector4FModifier();

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Operation 에 따라 value 를 성분마다 수정해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override Vector4 Modify(Vector4 value)
        {
            return operation.Apply(value, this.value);
        }

    #endregion

    }
}
