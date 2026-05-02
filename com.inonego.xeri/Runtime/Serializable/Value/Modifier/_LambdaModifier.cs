/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : _LambdaModifier.cs
수정일 : 2026-05-02

# 설명
임의의 Func<T, T> 람다를 적용하는 IModifier<T> 제네릭 구현.
Func 직렬화 불가로 [Serializable]을 붙이지 않으며 런타임 전용으로 사용한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// 임의 Func&lt;T, T&gt; 람다로 값을 수정하는 수정자.
    /// </summary>
    // ============================================================
    public class LambdaModifier<T> : IModifier<T>
    {

    #region 필드

        protected Func<T, T> lambda;
        public virtual Func<T, T> Lambda
        {
            get => lambda;
            set => lambda = value;
        }

    #endregion

    #region 생성자

        public LambdaModifier() {}

        public LambdaModifier(Func<T, T> lambda)
        {
            this.lambda = lambda;
        }

    #endregion

    #region 메서드

        // -----------------------------------------------------------------
        /// <summary>
        /// 람다를 적용한 결과를 반환한다. 람다가 null 이면 입력값을 그대로 반환.
        /// </summary>
        // -----------------------------------------------------------------
        public T Modify(T value)
        {
            return lambda != null ? lambda(value) : value;
        }

    #endregion

    #region 복제

        public IModifier<T> @new() => new LambdaModifier<T>();

        // -----------------------------------------------------------------
        /// <summary>
        /// source 의 람다 참조를 그대로 복사한다(얕은 복제).
        /// </summary>
        // -----------------------------------------------------------------
        public void CloneFrom(IModifier<T> source)
        {
            if (source is LambdaModifier<T> other)
            {
                lambda = other.lambda;
            }
        }

    #endregion

    }
}
