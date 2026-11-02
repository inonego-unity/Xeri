/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : BoxedEqualityComparer.cs
수정일 : 2026-11-01

# 설명
컬렉션 Editor에서 런타임 타입의 동등성 비교자를 object 기반 비교 계약으로 연결한다.
전달된 비교자가 해당 타입과 호환되지 않으면 기본 동등성 비교자를 사용한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 형식이 결정된 비교자를 object 기반 동등성 비교에 연결한다.
    /// </summary>
    // ============================================================
    internal sealed class BoxedEqualityComparer<T> : IEqualityComparer<object>
    {

    #region 필드

        private readonly IEqualityComparer<T> comparer;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 런타임 비교자가 없으면 해당 형식의 기본 비교자를 사용한다.
        /// </summary>
        // ------------------------------------------------------------
        public BoxedEqualityComparer(object runtimeComparer)
        {
            comparer = runtimeComparer as IEqualityComparer<T> ?? EqualityComparer<T>.Default;
        }

    #endregion

    #region 비교

        // ------------------------------------------------------------
        /// <summary>
        /// 원래 형식의 비교자로 두 boxed 값의 동등성을 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public new bool Equals(object left, object right)
        {
            return comparer.Equals
            (
                left == null ? default : (T)left,
                right == null ? default : (T)right
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원래 형식의 비교자로 boxed 값의 해시를 계산한다.
        /// </summary>
        // ------------------------------------------------------------
        public int GetHashCode(object value)
        {
            return value == null ? 0 : comparer.GetHashCode((T)value);
        }

    #endregion

    }
}
