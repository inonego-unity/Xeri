/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XType.cs
수정일 : 2026-05-01

# 설명
Unity 직렬화를 지원하는 System.Type 래퍼.
AssemblyQualifiedName을 string으로 저장하고 런타임에 지연 로딩한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// 직렬화 가능한 System.Type 래퍼.
    /// </summary>
    // ============================================================
    [Serializable]
    public struct XType : IEquatable<XType>, IEquatable<Type>
    {

    #region 필드

        [SerializeField]
        private string name;

        [NonSerialized]
        private Type value;

        // ------------------------------------------------------------
        /// <summary>
        /// 래핑된 System.Type. 처음 접근 시 AssemblyQualifiedName으로 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        public Type Value
        {
            get
            {
                if (value == null)
                {
                    value = Type.GetType(name);
                }

                return value;
            }
        }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 타입으로 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public XType(Type value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "타입이 null입니다.");
            }

            (this.name, this.value) = (value.AssemblyQualifiedName, value);
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Type을 XType으로 암시적 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator XType(Type value) => new(value);

        // ------------------------------------------------------------
        /// <summary>
        /// XType을 Type으로 암시적 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator Type(XType value) => value.Value;

        // ------------------------------------------------------------
        /// <summary>
        /// XType 동등성 비교.
        /// </summary>
        // ------------------------------------------------------------
        public bool Equals(XType other) => Value == other.Value;

        // ------------------------------------------------------------
        /// <summary>
        /// Type 동등성 비교.
        /// </summary>
        // ------------------------------------------------------------
        public bool Equals(Type other) => Value == other;

        // ------------------------------------------------------------
        /// <summary>
        /// 동등성 비교.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Equals(object obj)
        {
            if (obj is XType x) return Equals(x);
            if (obj is Type  t) return Equals(t);
            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 해시 코드 반환.
        /// </summary>
        // ------------------------------------------------------------
        public override int GetHashCode() => Value.GetHashCode();

    #endregion

    }
}
