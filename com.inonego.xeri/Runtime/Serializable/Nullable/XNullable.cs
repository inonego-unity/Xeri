/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XNullable.cs
수정일 : 2026-05-02

# 설명
직렬화 가능한 Nullable 구조체.
인스펙터에서도 표시될 수 있도록 PropertyDrawer(XNullableDrawer)가 함께 구현되어 있다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// 직렬화 가능한 Nullable 구조체.
    /// </summary>
    // ============================================================
    [Serializable]
    public struct XNullable<T> where T : struct
    {

    #region 필드

        [SerializeField] private bool hasValue;
        [SerializeField] private T value;

    #endregion

    #region 프로퍼티

        // ------------------------------------------------------------
        /// <summary>
        /// 값이 존재하면 true.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasValue => hasValue;

        // ------------------------------------------------------------
        /// <summary>
        /// 값을 반환한다. HasValue가 false이면 예외를 던진다.
        /// </summary>
        // ------------------------------------------------------------
        public T Value => hasValue ? value : throw new InvalidOperationException("Nullable의 값이 null입니다. HasValue를 통해 먼저 값이 있는지 확인해주세요.");

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 값으로 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public XNullable(T value)
        {
            this.hasValue = true;
            this.value = value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// System.Nullable&lt;T&gt;로 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public XNullable(T? nullable)
        {
            this.hasValue = nullable.HasValue;
            this.value = nullable.GetValueOrDefault();
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// T?를 XNullable&lt;T&gt;로 암시적 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator XNullable<T>(T? value) => new(value);

        // ------------------------------------------------------------
        /// <summary>
        /// XNullable&lt;T&gt;를 T?로 암시적 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator T?(XNullable<T> nullable) => nullable.GetValueOrDefault();

        // ------------------------------------------------------------
        /// <summary>
        /// T를 XNullable&lt;T&gt;로 암시적 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator XNullable<T>(T value) => new(value);

        // ------------------------------------------------------------
        /// <summary>
        /// XNullable&lt;T&gt;를 T로 명시적 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static explicit operator T(XNullable<T> nullable) => nullable.GetValueOrDefault();

        // ------------------------------------------------------------
        /// <summary>
        /// 동등성 비교.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator ==(XNullable<T> left, XNullable<T> right)
        {
            if (!left.hasValue && !right.hasValue) return true;
            if (!left.hasValue || !right.hasValue) return false;
            return left.value.Equals(right.value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비동등성 비교.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator !=(XNullable<T> left, XNullable<T> right) => !(left == right);

        // ------------------------------------------------------------
        /// <summary>
        /// 값이 있으면 반환, 없으면 default를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public T GetValueOrDefault() => hasValue ? value : default;

        // ------------------------------------------------------------
        /// <summary>
        /// 값이 있으면 반환, 없으면 defaultValue를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public T GetValueOrDefault(T defaultValue) => hasValue ? value : defaultValue;

        // ------------------------------------------------------------
        /// <summary>
        /// 동등성 비교.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Equals(object obj)
        {
            if (obj is XNullable<T> other)  return this == other;
            if (obj is T directValue)       return hasValue && value.Equals(directValue);
            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 해시 코드 반환.
        /// </summary>
        // ------------------------------------------------------------
        public override int GetHashCode() => hasValue ? value.GetHashCode() : 0;

        // ------------------------------------------------------------
        /// <summary>
        /// 문자열 표현 반환.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToString() => hasValue ? value.ToString() : "null";

    #endregion

    }
}
