/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : _LambdaModifier.cs
수정일 : 2026-11-22

# 설명
Func<T, T> 람다를 적용하는 런타임 전용 IModifier<T> 구현.

# 특이사항, 제약사항
Lambda는 생성자와 CloneFrom으로만 바뀌며, CloneFrom으로 바뀌면 OnChange를 발행한다.
외부 mutable closure의 변경은 추적하지 않으며 MValue의 자동 갱신 계약 대상이 아니다.
복제는 delegate 참조를 공유한다(delegate는 불변).
결과가 입력과 무관한지 알 수 없으므로 Replaces는 항상 false다.
Func 직렬화 불가로 [Serializable]을 붙이지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// Func&lt;T, T&gt; 람다로 값을 수정하는 수정자.
    /// </summary>
    // ============================================================
    public class LambdaModifier<T> : IModifier<T>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 적용할 람다. null 이면 입력 값을 그대로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Func<T, T> Lambda => lambda;
        private Func<T, T> lambda;

        // ------------------------------------------------------------
        /// <summary>
        /// 람다의 결과가 입력과 무관한지 알 수 없으므로 항상 false.
        /// </summary>
        // ------------------------------------------------------------
        public bool Replaces => false;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// CloneFrom으로 Lambda가 바뀐 뒤 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action OnChange = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 람다가 없는 수정자를 만든다. 람다가 없으면 입력 값을 그대로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public LambdaModifier()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 람다를 지정해 수정자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public LambdaModifier(Func<T, T> lambda)
        {
            this.lambda = lambda;
        }

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 빈 LambdaModifier 를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public IModifier<T> @new() => new LambdaModifier<T>();

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> source의 Lambda 참조를 복사한다.
        /// <br/> 참조가 바뀌면 OnChange를 한 번 발행한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloneFrom(IModifier<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source.GetType() != GetType())
            {
                throw new ArgumentException
                (
                    $"같은 구체 타입에서만 복제할 수 있습니다. ({source.GetType().Name} → {GetType().Name}) 파생 타입이 @new를 재정의했는지 확인하세요.",
                    nameof(source)
                );
            }

            var other = ((LambdaModifier<T>)source).lambda;

            if (ReferenceEquals(lambda, other)) return;

            lambda = other;

            OnChange?.Invoke();
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
