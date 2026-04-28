/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XNumericI.cs
수정일 : 2026-04-28

# 설명
int 기반 INumeric<TSelf, TValue> 구현 구조체.
int와의 암시적 변환을 지원한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Primitive
{

    // ============================================================
    /// <summary>
    /// float → 정수 변환 방식.
    /// </summary>
    // ============================================================
    public enum FloatConvertMode { Truncate, Floor, Round, Ceiling }

    // ============================================================
    /// <summary>
    /// int 기반 수치 연산 래퍼 구조체
    /// </summary>
    // ============================================================
    [Serializable]
    public struct XNumericI : INumeric<XNumericI, int>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// int 값
        /// </summary>
        // ------------------------------------------------------------
        public int Value
        {
            get => value;
            set => this.value = value;
        }

        [SerializeField]
        private int value;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// int 값으로 초기화
        /// </summary>
        // ------------------------------------------------------------
        public XNumericI(int value)
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
        public XNumericI Add(XNumericI other) => new XNumericI(value + other.value);

        // ------------------------------------------------------------
        /// <summary>
        /// 뺄셈
        /// </summary>
        // ------------------------------------------------------------
        public XNumericI Sub(XNumericI other) => new XNumericI(value - other.value);

        // ------------------------------------------------------------
        /// <summary>
        /// 곱셈
        /// </summary>
        // ------------------------------------------------------------
        public XNumericI Mul(XNumericI other) => new XNumericI(value * other.value);

        // ------------------------------------------------------------
        /// <summary>
        /// 나눗셈
        /// </summary>
        // ------------------------------------------------------------
        public XNumericI Div(XNumericI other) => new XNumericI(value / other.value);

        // ------------------------------------------------------------
        /// <summary>
        /// 나머지
        /// </summary>
        // ------------------------------------------------------------
        public XNumericI Mod(XNumericI other) => new XNumericI(value % other.value);

    #endregion

    #region 수치 연산

        // ------------------------------------------------------------
        /// <summary>
        /// 부호 반전
        /// </summary>
        // ------------------------------------------------------------
        public XNumericI Negate() => new XNumericI(-value);

        // ------------------------------------------------------------
        /// <summary>
        /// 절댓값
        /// </summary>
        // ------------------------------------------------------------
        public XNumericI Abs() => new XNumericI(Math.Abs(value));

        // ------------------------------------------------------------
        /// <summary>
        /// 양수 여부
        /// </summary>
        // ------------------------------------------------------------
        public readonly bool IsPositive => value > 0;

        // ------------------------------------------------------------
        /// <summary>
        /// 음수 여부
        /// </summary>
        // ------------------------------------------------------------
        public readonly bool IsNegative => value < 0;

        // ------------------------------------------------------------
        /// <summary>
        /// 0 여부
        /// </summary>
        // ------------------------------------------------------------
        public readonly bool IsZero => value == 0;

    #endregion

    #region 범위

        // ------------------------------------------------------------
        /// <summary>
        /// 두 값 중 작은 값 반환
        /// </summary>
        // ------------------------------------------------------------
        public XNumericI Min(XNumericI other) => new XNumericI(Math.Min(value, other.value));

        // ------------------------------------------------------------
        /// <summary>
        /// 두 값 중 큰 값 반환
        /// </summary>
        // ------------------------------------------------------------
        public XNumericI Max(XNumericI other) => new XNumericI(Math.Max(value, other.value));

        // ------------------------------------------------------------
        /// <summary>
        /// 값을 [min, max] 범위로 제한
        /// </summary>
        // ------------------------------------------------------------
        public XNumericI Clamp(XNumericI min, XNumericI max)
        {
            return new XNumericI(Math.Max(min.value, Math.Min(max.value, value)));
        }

    #endregion

    #region 접근자

        // ------------------------------------------------------------
        /// <summary>
        /// int 값 반환
        /// </summary>
        // ------------------------------------------------------------
        public int Get() => value;

        // ------------------------------------------------------------
        /// <summary>
        /// int 값 설정
        /// </summary>
        // ------------------------------------------------------------
        public void Set(int v) => value = v;

    #endregion

    #region 비교

        // ------------------------------------------------------------
        /// <summary>
        /// 동등 비교
        /// </summary>
        // ------------------------------------------------------------
        public bool Equals(XNumericI other) => value == other.value;

        // ------------------------------------------------------------
        /// <summary>
        /// 순서 비교
        /// </summary>
        // ------------------------------------------------------------
        public int CompareTo(XNumericI other) => value.CompareTo(other.value);

        // ------------------------------------------------------------
        /// <summary>
        /// object 타입 동등 비교
        /// </summary>
        // ------------------------------------------------------------
        public override bool Equals(object obj) => obj is XNumericI other && Equals(other);

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
        /// float 으로부터 XNumericI 를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public readonly XNumericI FromFloat(float v)
        {
            return FromFloat(v, FloatConvertMode.Truncate);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// float 으로부터 XNumericI 를 생성한다 (변환 방식 지정).
        /// </summary>
        // ------------------------------------------------------------
        public readonly XNumericI FromFloat(float v, FloatConvertMode mode)
        {
            float @float = mode switch
            {
                FloatConvertMode.Floor   => MathF.Floor(v),
                FloatConvertMode.Round   => MathF.Round(v),
                FloatConvertMode.Ceiling => MathF.Ceiling(v),
                _                        => v,
            };
            
            return new((int)@float);
        }

    #endregion

    #region 변환

        // ------------------------------------------------------------
        /// <summary>
        /// int → XNumericI 암시적 변환
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator XNumericI(int v) => new XNumericI(v);

        // ------------------------------------------------------------
        /// <summary>
        /// XNumericI → int 암시적 변환
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator int(XNumericI n) => n.value;

    #endregion

    }

}
