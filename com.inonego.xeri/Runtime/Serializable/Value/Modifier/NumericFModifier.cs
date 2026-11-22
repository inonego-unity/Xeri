/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : NumericFModifier.cs
수정일 : 2026-11-22

# 설명
float 값에 SET / ADD / SUB / MUL / DIV / MIN / MAX 수치 연산을 적용하는 IModifier<float> 구현.
연산 규칙은 NumericFOperationHelper.Apply에 있어 Modifier 객체 없이도 같은 규칙을 쓸 수 있다.
DIV 0은 입력 값을 유지하고, MIN·MAX는 NaN인 피연산자를 무시한다.
Vector2·Vector3·Vector4 오버로드는 같은 규칙을 성분마다 적용한다.

# 특이사항, 제약사항
Operation은 Unity가 정수로 직렬화하므로 기존 값의 순서를 바꾸지 않고 뒤에만 추가한다.
IsDefined는 SET부터 마지막 연산(MAX)까지의 연속 범위로 판정하므로 연산을 추가하면 상한을 함께 갱신한다.
벡터의 MIN·MAX는 성분별 비교이며 벡터 크기 제한이 아니다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Runtime;
using System.Runtime.CompilerServices;

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
        SET, ADD, SUB, MUL, DIV, MIN, MAX
    }

    // ============================================================
    /// <summary>
    /// NumericFOperation 을 값에 적용하는 정적 헬퍼.
    /// </summary>
    // ============================================================
    public static class NumericFOperationHelper
    {

    #region 연산

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 연산을 current 에 value 로 적용한 결과를 반환한다.
        /// <br/> DIV 0은 current 를 유지하고, MIN·MAX는 NaN인 value 를 무시한다.
        /// <br/> 정의되지 않은 연산은 current 를 그대로 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Apply(this NumericFOperation operation, float current, float value)
        {
            return operation switch
            {
                NumericFOperation.SET => value,
                NumericFOperation.ADD => current + value,
                NumericFOperation.SUB => current - value,
                NumericFOperation.MUL => current * value,
                NumericFOperation.DIV => value == 0f ? current : current / value,
                NumericFOperation.MIN => value < current ? value : current,
                NumericFOperation.MAX => value > current ? value : current,
                _ => current
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// float 연산 규칙을 Vector2 의 성분마다 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Apply(this NumericFOperation operation, Vector2 current, Vector2 value)
        {
            return new Vector2
            (
                operation.Apply(current.x, value.x),
                operation.Apply(current.y, value.y)
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// float 연산 규칙을 Vector3 의 성분마다 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Apply(this NumericFOperation operation, Vector3 current, Vector3 value)
        {
            return new Vector3
            (
                operation.Apply(current.x, value.x),
                operation.Apply(current.y, value.y),
                operation.Apply(current.z, value.z)
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// float 연산 규칙을 Vector4 의 성분마다 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Apply(this NumericFOperation operation, Vector4 current, Vector4 value)
        {
            return new Vector4
            (
                operation.Apply(current.x, value.x),
                operation.Apply(current.y, value.y),
                operation.Apply(current.z, value.z),
                operation.Apply(current.w, value.w)
            );
        }

    #endregion

    #region 정의 여부

        // ------------------------------------------------------------
        /// <summary>
        /// 연산이 NumericFOperation 에 정의된 값인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDefined(this NumericFOperation operation)
        {
            return operation >= NumericFOperation.SET && operation <= NumericFOperation.MAX;
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// float 값에 수치 연산을 적용하는 수정자.
    /// </summary>
    // ============================================================
    [Serializable]
    public class NumericFModifier : OperationModifier<float, NumericFOperation>
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
        /// 기본 연산(SET)과 0 값으로 수정자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public NumericFModifier()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 연산과 피연산자로 수정자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public NumericFModifier(NumericFOperation operation, float value) : base(operation, value)
        {
            // NONE
        }

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 빈 NumericFModifier 를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override IModifier<float> @new() => new NumericFModifier();

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Operation 에 따라 value 를 수정해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override float Modify(float value)
        {
            return operation.Apply(value, this.value);
        }

    #endregion

    }
}
