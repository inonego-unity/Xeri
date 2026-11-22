/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Vector3IModifier.cs
수정일 : 2026-11-22

# 설명
Vector3Int 값에 NumericIOperation 수치 연산을 성분마다 적용하는 IModifier<Vector3Int> 구현.
연산 규칙은 NumericIOperationHelper.Apply의 Vector3Int 오버로드와 같다.

# 특이사항, 제약사항
MIN·MAX는 성분별 비교이며 벡터 크기 제한이 아니다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// Vector3Int 값에 수치 연산을 성분마다 적용하는 수정자.
    /// </summary>
    // ============================================================
    [Serializable]
    public class Vector3IModifier : OperationModifier<Vector3Int, NumericIOperation>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// SET 연산이면 입력 값과 무관한 결과를 낸다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Replaces => operation == NumericIOperation.SET;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 연산(SET)과 0 벡터로 수정자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public Vector3IModifier()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 연산과 피연산자로 수정자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public Vector3IModifier(NumericIOperation operation, Vector3Int value) : base(operation, value)
        {
            // NONE
        }

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 빈 Vector3IModifier 를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override IModifier<Vector3Int> @new() => new Vector3IModifier();

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Operation 에 따라 value 를 성분마다 수정해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override Vector3Int Modify(Vector3Int value)
        {
            return operation.Apply(value, this.value);
        }

    #endregion

    }
}
