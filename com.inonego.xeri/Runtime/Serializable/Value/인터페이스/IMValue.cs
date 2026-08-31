/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IMValue.cs
수정일 : 2026-08-31

# 설명
MValue(수정자 적용 Value) 인터페이스 모음.

- IReadOnlyMValue<T> : 외부 노출용. Modifiers·Modified·OnModifiedChange 를 읽기 전용으로 노출.
- IMValue<T>        : 시스템 내부용. 수정자 추가·제거 메서드 제공.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Serializable
{
    using Primitive; // inonego.Xeri.Primitive

    // ===================================================================
    /// <summary>
    /// 수정자가 적용되는 Value 읽기 전용 뷰 인터페이스.
    /// </summary>
    // ===================================================================
    public interface IReadOnlyMValue<T> : IReadOnlyValue<T>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 수정자 목록(Order 오름차순).
        /// </summary>
        // ------------------------------------------------------------
        IReadOnlyList<ModifierEntry<T>> Modifiers { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 수정자가 적용된 값.
        /// </summary>
        // ------------------------------------------------------------
        T Modified { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 수정자 적용 값이 변경될 때 발생하는 이벤트.
        /// </summary>
        // ------------------------------------------------------------
        event ValueChangeEventHandler<T> OnModifiedChange;
    }

    // ===================================================================
    /// <summary>
    /// 수정자 적용 Value 변경 가능 인터페이스.
    /// </summary>
    // ===================================================================
    public interface IMValue<T> : IReadOnlyMValue<T>, IValue<T>
    {
        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 Base와 등록된 수정자를 다시 평가해 Modified 캐시를 갱신한다.
        /// </summary>
        // ----------------------------------------------------------------------
        void Refresh(bool invokeEvent = true);

        // ----------------------------------------------------------------------
        /// <summary>
        /// 키를 명시하여 수정자를 추가한다.
        /// </summary>
        // ----------------------------------------------------------------------
        void AddModifier(string key, IModifier<T> modifier, int order = 0, bool invokeEvent = true);

        // ----------------------------------------------------------------------
        /// <summary>
        /// IKeyable<string> 을 구현한 수정자를 자기 키로 추가한다.
        /// </summary>
        // ----------------------------------------------------------------------
        void AddModifier<TModifier>(TModifier modifier, int order = 0, bool invokeEvent = true)
        where TModifier : IModifier<T>, IKeyable<string>;

        // ----------------------------------------------------------------------
        /// <summary>
        /// 수정자가 IKeyable<string> 을 구현하면 자기 키로 추가한다. 아니면 예외.
        /// </summary>
        // ----------------------------------------------------------------------
        void AddModifier(IModifier<T> modifier, int order = 0, bool invokeEvent = true);

        // ------------------------------------------------------------
        /// <summary>
        /// 키로 수정자를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        bool RemoveModifier(string key, bool invokeEvent = true);

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 수정자를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        void ClearModifiers(bool invokeEvent = true);
    }
}
