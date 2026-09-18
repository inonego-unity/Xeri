/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : _LambdaModifier.cs
수정일 : 2026-09-18

# 설명
생성 시 고정된 Func<T, T> 람다를 적용하는 런타임 전용 IModifier<T> 구현.

# 특이사항, 제약사항
Lambda 자체와 Lambda가 참조하는 계산 의존성은 등록 후 불변이어야 한다.
외부 mutable closure의 변경은 추적하지 않으며 MValue의 자동 갱신 계약 대상이 아니다.
Func 직렬화 불가로 [Serializable]을 붙이지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// 생성 시 고정된 Func&lt;T, T&gt; 람다로 값을 수정하는 수정자.
    /// </summary>
    // ============================================================
    public class LambdaModifier<T> : IModifier<T>
    {

    #region 필드

        public Func<T, T> Lambda => lambda;

        private readonly Func<T, T> lambda;

    #endregion

    #region 이벤트

        // ----------------------------------------------------------------------
        /// <summary>
        /// 불변 LambdaModifier는 내부 상태 변경이 없으므로 발생하지 않는 호환용 이벤트.
        /// </summary>
        // ----------------------------------------------------------------------
        public event Action OnChange
        {
            add
            {
                // NONE
            }
            remove
            {
                // NONE
            }
        }

    #endregion

    #region 생성자

        public LambdaModifier()
        {
            // NONE
        }

        public LambdaModifier(Func<T, T> lambda)
        {
            this.lambda = lambda;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 람다를 적용한 결과를 반환한다. 람다가 null 이면 입력값을 그대로 반환.
        /// </summary>
        // ------------------------------------------------------------
        public T Modify(T value)
        {
            return lambda != null ? lambda(value) : value;
        }

    #endregion

    }
}
