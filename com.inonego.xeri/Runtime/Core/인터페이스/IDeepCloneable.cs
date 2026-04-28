/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IDeepCloneable.cs
수정일 : 2026-04-28

# 설명
깊은 복제(Deep Clone)를 지원하기 위한 인터페이스 및 확장 메서드 정의.
IDeepCloneableFrom — 외부 소스로부터 복제를 수신.
IDeepCloneable     — 자체 복제 기능 제공 (기본 구현 포함).
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// IDeepCloneable 계열 인터페이스를 위한 확장 메서드 모음.
    /// </summary>
    // ============================================================
    public static class IDeepCloneable
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 파라미터 없는 깊은 복제를 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        public static T Clone<T>(this T source)
        where T : IDeepCloneable<T>
        {
            return source.Clone();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 파라미터를 받아 깊은 복제를 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        public static T Clone<T, TParam>(this T source, TParam param)
        where T : IDeepCloneable<T, TParam>
        {
            return source.Clone(param);
        }
    }

#region 인터페이스 IDeepCloneableFrom

    // ============================================================
    /// <summary>
    /// 외부 소스로부터 파라미터 없는 깊은 복제를 수신하는 인터페이스.
    /// </summary>
    // ============================================================
    public interface IDeepCloneableFrom<in T>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// source 의 데이터를 this 로 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloneFrom(T source);
    }

    // ============================================================
    /// <summary>
    /// 파라미터를 참조하여 외부 소스로부터 깊은 복제를 수신하는 인터페이스.
    /// </summary>
    // ============================================================
    public interface IDeepCloneableFrom<in T, TParam>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 파라미터를 참조하여 source 의 데이터를 this 로 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloneFrom(T source, TParam param);
    }

#endregion

#region 인터페이스 IDeepCloneable

    // ============================================================
    /// <summary>
    /// 파라미터 없는 깊은 복제를 지원하는 인터페이스.
    /// </summary>
    // ============================================================
    public interface IDeepCloneable<T> : IDeepCloneableFrom<T>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 빈 새 인스턴스를 생성해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public T @new();

        // ------------------------------------------------------------
        /// <summary>
        /// @new() 로 새 인스턴스를 생성한 뒤 CloneFrom 을 호출하는 기본 구현.
        /// </summary>
        // ------------------------------------------------------------
        public T Clone()
        {
            var result = @new();
            var cloneable = result as IDeepCloneableFrom<T>;

            if (this is T src)
            {
                cloneable.CloneFrom(src);
            }

            return result;
        }
    }

    // ============================================================
    /// <summary>
    /// 파라미터를 받는 깊은 복제를 지원하는 인터페이스.
    /// </summary>
    // ============================================================
    public interface IDeepCloneable<T, TParam> : IDeepCloneableFrom<T, TParam>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 빈 새 인스턴스를 생성해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public T @new();

        // ---------------------------------------------------------------------
        /// <summary>
        /// @new() 로 새 인스턴스를 생성한 뒤 CloneFrom(param) 을 호출하는 기본 구현.
        /// </summary>
        // ---------------------------------------------------------------------
        public T Clone(TParam param)
        {
            var result = @new();
            var cloneable = result as IDeepCloneableFrom<T, TParam>;

            if (this is T src)
            {
                cloneable.CloneFrom(src, param);
            }

            return result;
        }
    }

#endregion

}
