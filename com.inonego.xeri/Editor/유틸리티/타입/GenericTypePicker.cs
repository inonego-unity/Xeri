/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenericTypePicker.cs
수정일 : 2026-08-05

# 설명
열린 제네릭 타입의 중첩 인자를 구성해 닫힌 CLR 타입을 선택하는 Editor 진입점.

# 특이사항
후보 목록, 표시 이름, 완성 결과 검증은 소비자가 제공한다.
이 타입은 제네릭 인자 트리의 편집과 닫힌 타입 생성을 관리한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 중첩 제네릭 인자를 구성하는 Editor 선택기의 공개 진입점.
    /// </summary>
    // ============================================================
    public static class GenericTypePicker
    {

    #region Show

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> genericTypeDefinition의 닫힌 타입을 구성하는 선택기를 표시한다.
        /// <br/> 후보와 결과 검증은 호출자가 소유하고, 선택기는 제네릭 구성만 관리한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void Show
        (
            Type genericTypeDefinition,
            Func<Type, bool, IReadOnlyList<Type>> candidateProvider,
            Func<Type, string> displayNameProvider,
            Action<Type> onSelected,
            IReadOnlyDictionary<Type, Type> fixedArguments = null,
            Func<Type, string> validationMessageProvider = null
        )
        {
            GenericTypePickerWindow.Open
            (
                genericTypeDefinition,
                candidateProvider,
                displayNameProvider,
                onSelected,
                fixedArguments,
                validationMessageProvider
            );
        }

    #endregion

    }
}
