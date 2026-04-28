/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XNumericF.cs
수정일 : 2026-04-28

# 설명
float 기반 INumeric<TSelf, TValue> 구현 구조체.
float와의 암시적 변환을 지원한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Primitive
{

    // ============================================================
    /// <summary>
    /// float 기반 수치 연산 래퍼 구조체
    /// </summary>
    // ============================================================
    [Serializable]
    public struct XNumericF : INumeric<XNumericF, float>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// float 값
        /// </summary>
        // ------------------------------------------------------------
        public float Value
        {
            get => value;
            set => this.value = value;
        }

        [SerializeField]
        private float value;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// float 값으로 초기화
        /// </summary>
        // ------------------------------------------------------------
        public XNumericF(float value)
        {
            this.value = value;
        }

    #endregion

    #region 사칙연산

        // ------------------------------------------------------------
        /// <summary>
        /// 덧셈
        /// </summary>
        // ------------------------------------------------------------
        public XNumericF Add(XNumericF other) => new XNumericF(value + other.value);

        // ------------------------------------------------------------
        /// <summary>
        /// 뺄셈
        /// </summary>
        // ------------------------------------------------------------
        public XNumericF Sub(XNumericF other) => new XNumericF(value - other.value);

        // ------------------------------------------------------------
        /// <summary>
        /// 곱셈
        /// </summary>
        // ------------------------------------------------------------
        public XNumericF Mul(XNumericF other) => new XNumericF(value * other.value);

        // ------------------------------------------------------------
        /// <summary>
        /// 나눗셈
        /// </summary>
        // ------------------------------------------------------------
        public XNumericF Div(XNumericF other) => new XNumericF(value / other.value);

        // ------------------------------------------------------------
        /// <summary>
        /// 나머지
        /// </summary>
        // ------------------------------------------------------------
        public XNumericF Mod(XNumericF other) => new XNumericF(value % other.value);

    #endregion

    #region 수치 연산

        // ------------------------------------------------------------
        /// <summary>
        /// 부호 반전
        /// </summary>
        // ------------------------------------------------------------
        public XNumericF Negate() => new XNumericF(-value);

        // ------------------------------------------------------------
        /// <summary>
        /// 절댓값
        /// </summary>
        // ------------------------------------------------------------
        public XNumericF Abs() => new XNumericF(Math.Abs(value));

        // ------------------------------------------------------------
        /// <summary>
        /// 양수 여부
        /// </summary>
        // ------------------------------------------------------------
        public readonly bool IsPositive => value > 0f;

        // ------------------------------------------------------------
        /// <summary>
        /// 음수 여부
        /// </summary>
        // ------------------------------------------------------------
        public readonly bool IsNegative => value < 0f;

        // ------------------------------------------------------------
        /// <summary>
        /// 0 여부
        /// </summary>
        // ------------------------------------------------------------
        public readonly bool IsZero => value == 0f;

    #endregion

    #region 범위

        // ------------------------------------------------------------
        /// <summary>
        /// 두 값 중 작은 값 반환
        /// </summary>
        // ------------------------------------------------------------
        public XNumericF Min(XNumericF other) => new XNumericF(Math.Min(value, other.value));

        // ------------------------------------------------------------
        /// <summary>
        /// 두 값 중 큰 값 반환
        /// </summary>
        // ------------------------------------------------------------
        public XNumericF Max(XNumericF other) => new XNumericF(Math.Max(value, other.value));

        // ------------------------------------------------------------
        /// <summary>
        /// 값을 [min, max] 범위로 제한
        /// </summary>
        // ------------------------------------------------------------
        public XNumericF Clamp(XNumericF min, XNumericF max)
        {
            return new XNumericF(Math.Max(min.value, Math.Min(max.value, value)));
        }

    #endregion

    #region 접근자

        // ------------------------------------------------------------
        /// <summary>
        /// float 값 반환
        /// </summary>
        // ------------------------------------------------------------
        public float Get() => value;

        // ------------------------------------------------------------
        /// <summary>
        /// float 값 설정
        /// </summary>
        // ------------------------------------------------------------
        public void Set(float v) => value = v;

    #endregion

    #region 비교

        // ------------------------------------------------------------
        /// <summary>
        /// 동등 비교
        /// </summary>
        // ------------------------------------------------------------
        public bool Equals(XNumericF other) => value == other.value;

        // ------------------------------------------------------------
        /// <summary>
        /// 순서 비교
        /// </summary>
        // ------------------------------------------------------------
        public int CompareTo(XNumericF other) => value.CompareTo(other.value);

        // ------------------------------------------------------------
        /// <summary>
        /// object 타입 동등 비교
        /// </summary>
        // ------------------------------------------------------------
        public override bool Equals(object obj) => obj is XNumericF other && Equals(other);

        // ------------------------------------------------------------
        /// <summary>
        /// 해시코드 반환
        /// </summary>
        // ------------------------------------------------------------
        public override int GetHashCode() => value.GetHashCode();

        // ------------------------------------------------------------
        /// <summary>
        /// 문자열 표현 반환
        /// </summary>
        // ------------------------------------------------------------
        public override string ToString() => value.ToString();

    #endregion

    #region 변환 (float)

        // ------------------------------------------------------------
        /// <summary>
        /// float 으로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public readonly float ToFloat() => value;

        // ------------------------------------------------------------
        /// <summary>
        /// float 으로부터 XNumericF 를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public readonly XNumericF FromFloat(float v) => new(v);

    #endregion

    #region 변환

        // ------------------------------------------------------------
        /// <summary>
        /// float → XNumericF 암시적 변환
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator XNumericF(float v) => new XNumericF(v);

        // ------------------------------------------------------------
        /// <summary>
        /// XNumericF → float 암시적 변환
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator float(XNumericF n) => n.value;

    #endregion

    }

}
