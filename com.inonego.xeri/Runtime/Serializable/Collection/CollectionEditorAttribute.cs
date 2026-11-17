/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionEditorAttribute.cs
수정일 : 2026-11-17

# 설명
일반 List<T>와 1차원 배열 자체를 Xeri Collection Editor로 표시하도록 opt-in하는 PropertyAttribute.
Unique는 중복 진단, VisibleRows는 기본 표시 행 수, Columns는 복합 요소의 직접 편집 열을 정한다.

# 특이사항, 제약사항
Unity 6에서는 collection attribute가 기본적으로 각 원소에 적용되므로 collection 전체를 명시적으로 지정한다.
Columns는 SerializeReference가 아닌 class/struct 요소의 직렬화 필드 이름만 사용할 수 있다.
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

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> Inspector에서 처음 표시할 행 수를 반환하거나 설정한다.
        /// <br/> 사용자는 목록 하단을 끌어 편집 세션 동안 높이를 바꿀 수 있다.
        /// </summary>
        // ------------------------------------------------------------
        public int VisibleRows { get; set; } = 5;

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 복합 요소에서 목록 열로 직접 편집할 직렬화 필드 이름이다.
        /// <br/> 지정하지 않으면 요소 전체를 하나의 열로 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public string[] Columns { get; set; } = null;

    #endregion

    }
}
