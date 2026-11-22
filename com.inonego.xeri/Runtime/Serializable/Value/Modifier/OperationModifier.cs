/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : OperationModifier.cs
수정일 : 2026-11-22

# 설명
연산 종류(Operation)와 피연산자(Value)를 가진 IModifier<T> 구현의 공통 기반.
Operation·Value 보관과 변경 알림, 같은 구체 타입 사이의 복제를 담당한다.

# 특이사항, 제약사항
파생 타입은 Modify, Replaces, @new만 구현한다.
CloneFrom은 같은 구체 타입의 source만 받으며 상태가 바뀌면 OnChange를 한 번 발행한다.
구체 Modifier를 다시 상속하는 타입은 상태가 없어도 @new를 재정의해 자기 타입을 만든다. 재정의하지 않으면 Clone이 실패한다.
상태를 추가하는 파생 타입은 CloneFrom도 재정의해야 복제가 완전하다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// 연산 종류와 피연산자를 가진 수정자의 공통 기반.
    /// </summary>
    // ============================================================
    [Serializable]
    public abstract class OperationModifier<T, TOperation> : IModifier<T>
    where TOperation : struct, Enum
    {

    #region 필드

        [SerializeField]
        protected TOperation operation;

        // ------------------------------------------------------------
        /// <summary>
        /// 적용할 연산 종류.
        /// </summary>
        // ------------------------------------------------------------
        public virtual TOperation Operation
        {
            get => operation;
            set
            {
                if (EqualityComparer<TOperation>.Default.Equals(operation, value)) return;

                operation = value;

                InvokeOnChange();
            }
        }

        [SerializeField]
        protected T value;

        // ------------------------------------------------------------
        /// <summary>
        /// 연산의 피연산자.
        /// </summary>
        // ------------------------------------------------------------
        public virtual T Value
        {
            get => value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(this.value, value)) return;

                this.value = value;

                InvokeOnChange();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 결과가 입력 값과 무관한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract bool Replaces { get; }

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Modify 결과에 영향을 주는 내부 상태가 변경된 뒤 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        [field: NonSerialized]
        public event Action OnChange = null;

        // ------------------------------------------------------------
        /// <summary>
        /// OnChange를 발생시킨다.
        /// </summary>
        // ------------------------------------------------------------
        protected void InvokeOnChange() => OnChange?.Invoke();

    #endregion

    #region 생성자

        protected OperationModifier()
        {
            // NONE
        }

        protected OperationModifier(TOperation operation, T value)
        {
            this.operation = operation;
            this.value = value;
        }

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 복제에 사용할 같은 구체 타입의 빈 인스턴스를 만든다.
        /// <br/> 이 타입을 다시 상속하면 상태가 없어도 재정의한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract IModifier<T> @new();

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> source의 Operation과 Value를 복사한다.
        /// <br/> 상태가 바뀌면 OnChange를 한 번 발행한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void CloneFrom(IModifier<T> source)
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

            var typed = (OperationModifier<T, TOperation>)source;

            var changed = !EqualityComparer<TOperation>.Default.Equals(operation, typed.operation) ||
                          !EqualityComparer<T>.Default.Equals(value, typed.value);

            operation = typed.operation;
            value = typed.value;

            // 등록된 MValue의 캐시가 어긋나지 않도록 상태가 바뀐 경우에만 알린다.
            if (changed)
            {
                InvokeOnChange();
            }
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Operation 에 따라 value 를 수정해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract T Modify(T value);

    #endregion

    }
}
