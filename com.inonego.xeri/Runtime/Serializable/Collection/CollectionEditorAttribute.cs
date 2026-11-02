/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionEditorAttribute.cs
수정일 : 2026-10-17

# 설명
일반 List<T>와 1차원 배열 자체를 Xeri Collection Editor로 표시하도록 opt-in하는 PropertyAttribute.
Unique는 collection element 중복을 authoring validation으로 표시하는 선택 정책이다.

# 특이사항, 제약사항
Unity 6에서는 collection attribute가 기본적으로 각 원소에 적용되므로 collection 전체를 명시적으로 지정한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ======================================================================
    /// <summary>
    /// 일반 Unity serialized collection의 Xeri Editor opt-in 계약.
    /// </summary>
    // ======================================================================
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CollectionEditorAttribute : PropertyAttribute
    {

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// CollectionEditorAttribute 인스턴스를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionEditorAttribute()
            : base(applyToCollection: true)
        {
            // NONE
        }

    #endregion

    #region 프로퍼티

        // ------------------------------------------------------------
        /// <summary>
        /// Unique 값을 반환하거나 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Unique { get; set; } = false;

    #endregion

    }
}
