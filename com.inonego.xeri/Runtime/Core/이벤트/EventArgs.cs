/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EventArgs.cs
수정일 : 2026-04-28

# 설명
값 변경 이벤트에 사용되는 델리게이트 및 이벤트 인수 구조체 정의.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 값 변경 이벤트 핸들러 델리게이트.
    /// </summary>
    // ============================================================
    public delegate void ValueChangeEventHandler<T>(object sender, ValueChangeEventArgs<T> e);

    // ============================================================
    /// <summary>
    /// 값 변경 이전·이후 상태를 담는 이벤트 인수 구조체.
    /// </summary>
    // ============================================================
    [Serializable]
    public struct ValueChangeEventArgs<T>
    {
        public T Previous;
        public T Current;

        // ------------------------------------------------------------
        /// <summary>
        /// 이전 값과 현재 값이 다른지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasChanged => !Equals(Previous, Current);

        // ------------------------------------------------------------
        /// <summary>
        /// 이전 값과 현재 값으로 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public ValueChangeEventArgs(T previous, T current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
