/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Value.cs
수정일 : 2026-10-29

# 설명
단일 값을 관리하며 변경 이벤트를 발행하는 제네릭 클래스.
ProcessBase 오버라이드로 값 전처리를 확장할 수 있다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    using Primitive; // inonego.Xeri.Primitive

    // ============================================================
    /// <summary>
    /// 값을 관리하는 클래스.
    /// </summary>
    // ============================================================
    [Serializable]
    public class Value<T> : IValue<T>
    {

    #region 필드

        protected static readonly EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 값.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        protected T @base;
        public T Base
        {
            get => @base;
            set => Set(value);
        }

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 값이 변경될 때 발생하는 이벤트.
        /// </summary>
        // ------------------------------------------------------------
        [field: NonSerialized]
        public event ValueChangeEventHandler<T> OnBaseChange = null;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Base의 실제 변경을 내부 의존 객체 및 Editor 관찰자에 전달한다.
        /// <br/> invokeEvent는 외부 이벤트 발행 요청 여부이며 발행 완료 여부는 아니다.
        /// <br/> invokeEvent가 false여도 내부 상태 동기화를 위해 호출된다.
        /// </summary>
        // ----------------------------------------------------------------------
        [field: NonSerialized]
        internal event Action<T, T, bool> OnBaseApplied;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 기본값을 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public Value() : this(default)
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 초기값을 저장한다. 파생 타입의 초기 정규화는 초기화 완료 후 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        public Value(T value)
        {
            @base = value;
        }

    #endregion

    #region 메서드

        // ----------------------------------------------------------------------
        /// <summary>
        /// 값을 설정한다. invokeEvent 가 false 이면 이벤트를 발행하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public virtual void Set(T value, bool invokeEvent = true)
        {
            var (prev, next) = (@base, value);

            ProcessBase(prev, ref next);

            if (comparer.Equals(prev, next)) return;

            @base = next;
            OnBaseApplied?.Invoke(prev, next, invokeEvent);

            if (invokeEvent)
            {
                RaiseBaseChange(prev, next);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 값을 설정하기 전에 처리하는 훅. 서브클래스에서 오버라이드한다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void ProcessBase(in T prev, ref T next)
        {
            // NONE
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 파생 타입이 준비된 뒤 초기값을 정규화한다. 외부 이벤트는 발생시키지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected void InitializeBase(T value)
        {
            ProcessBase(default, ref value);
            @base = value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이미 확정된 Base 변경을 변경 당시 값으로 통지한다.
        /// </summary>
        // ------------------------------------------------------------
        protected void RaiseBaseChange(T previous, T current)
        {
            OnBaseChange?.Invoke(this, new(previous, current));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화로 적용된 Base와 이전 상태를 비교해 변경을 통지한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void FinalizeSerializedChange(T previous)
        {
            if (!comparer.Equals(previous, Base))
            {
                RaiseBaseChange(previous, Base);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화로 이미 변경된 상태를 고정된 값 스냅샷으로 통지한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void PublishSerializedChange(T previous, T current)
        {
            if (!comparer.Equals(previous, current))
            {
                RaiseBaseChange(previous, current);
            }
        }

    #endregion

    #region 암시적 변환

        // ------------------------------------------------------------
        /// <summary>
        /// Value&lt;T&gt;에서 T로의 암시적 변환.
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator T(Value<T> wrapper)
        {
            return wrapper != null ? wrapper.@base : default;
        }

    #endregion

    #region Object 오버라이드

        public override bool Equals(object obj)
        {
            if (obj is Value<T> other && other.GetType() == GetType())
                return comparer.Equals(@base, other.@base);
            if (obj is T directValue)
                return comparer.Equals(@base, directValue);
            return false;
        }

        public override int GetHashCode() => comparer.GetHashCode(@base);

        public override string ToString() => @base != null ? @base.ToString() : string.Empty;

    #endregion

    }
}
