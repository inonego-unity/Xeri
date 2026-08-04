/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ManagedReferenceField.cs
수정일 : 2026-08-04

# 설명
managed-reference type과 관련 action을 한 행에 배치하는 UI Toolkit field.
Unity Inspector의 BaseField label 정렬과 UXML 자식 구성을 함께 제공한다.

# 특이사항
UXML 자식은 BaseField가 생성한 input container에 추가된다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine.UIElements;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// managed-reference type 선택 행을 구성하는 Inspector field.
    /// </summary>
    // ============================================================
    [UxmlElement]
    internal partial class ManagedReferenceField : BaseField<Type>
    {
    #region 필드

        private readonly VisualElement input = null;

    #endregion

    #region 프로퍼티

        // ------------------------------------------------------------
        /// <summary>
        /// UXML 자식을 label 옆의 input 영역에 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement contentContainer => input ?? base.contentContainer;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UXML factory에서 사용할 기본 생성자.
        /// </summary>
        // ------------------------------------------------------------
        public ManagedReferenceField() : this(new VisualElement()) {}

        // ------------------------------------------------------------
        /// <summary>
        /// BaseField에 전달한 input을 UXML content container로 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        private ManagedReferenceField(VisualElement input)
            : base(string.Empty, input)
        {
            this.input = input;

            // 실제 BaseField가 Inspector 폭과 indent에 맞춰 label 폭을 계산하도록 정렬 계약을 활성화한다.
            AddToClassList(alignedFieldUssClassName);
            input.AddToClassList("managed-reference-input");
        }

    #endregion

    }
}
