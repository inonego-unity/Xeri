/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SerializeReferenceWrapper.cs
수정일 : 2026-05-02

# 설명
[SerializeReference]를 구조체 필드로서 사용하기 위한 제네릭 래퍼.
구조체는 [SerializeReference]를 직접 가질 수 없으나,
이 래퍼 구조체를 [SerializeField]로 직렬화하면 동일한 효과를 얻는다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// [SerializeReference]를 구조체 필드로 사용하기 위한 래퍼.
    /// </summary>
    // ============================================================
    [Serializable]
    public struct SerializeReferenceWrapper<T> where T : class
    {

    #region 필드

        [SerializeReference]
        public T Value;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 값으로 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public SerializeReferenceWrapper(T value)
        {
            Value = value;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// T를 SerializeReferenceWrapper&lt;T&gt;로 암시적 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator SerializeReferenceWrapper<T>(T value) => new(value);

        // ------------------------------------------------------------
        /// <summary>
        /// SerializeReferenceWrapper&lt;T&gt;를 T로 암시적 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator T(SerializeReferenceWrapper<T> wrapper) => wrapper.Value;

        // ------------------------------------------------------------
        /// <summary>
        /// 동등성 비교.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator ==(SerializeReferenceWrapper<T> left, SerializeReferenceWrapper<T> right) => Equals(left.Value, right.Value);

        // ------------------------------------------------------------
        /// <summary>
        /// 비동등성 비교.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator !=(SerializeReferenceWrapper<T> left, SerializeReferenceWrapper<T> right) => !Equals(left.Value, right.Value);

        // ------------------------------------------------------------
        /// <summary>
        /// 동등성 비교.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Equals(object obj)
        {
            if (obj is SerializeReferenceWrapper<T> other) return Equals(Value, other.Value);
            if (obj is T directValue)                      return Equals(Value, directValue);
            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 해시 코드 반환.
        /// </summary>
        // ------------------------------------------------------------
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;

        // ------------------------------------------------------------
        /// <summary>
        /// 문자열 표현 반환.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToString() => Value?.ToString() ?? "null";

    #endregion

    }
}
