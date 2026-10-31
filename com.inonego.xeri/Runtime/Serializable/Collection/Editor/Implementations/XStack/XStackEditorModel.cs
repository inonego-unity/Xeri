/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XStackEditorModel.cs
수정일 : 2026-10-17

# 설명
XStack의 serialized row를 Top → Bottom 순서로 편집하고 top insertion과 runtime Stack rebuild를 담당한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// XStack Collection Editor Model.
    /// </summary>
    // ============================================================
    internal sealed class XStackEditorModel : XSerializedListEditorModelBase
    {

    #region 상태

        /// <inheritdoc />
        public override bool CanReorder => true;
        /// <inheritdoc />
        public override string Description => "위에서 아래로 꺼내며, 새 항목은 맨 위에 추가됩니다.";

        /// <inheritdoc />
        protected override string AddUndoName => "XStack 항목 추가";
        /// <inheritdoc />
        protected override string RemoveUndoName => "XStack 항목 삭제";

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// XStackEditorModel 인스턴스를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public XStackEditorModel(SerializedProperty property)
            : base(property, "위 → 아래")
        {
            // NONE
        }

    #endregion

    #region 행 변경

        /// <inheritdoc />
        protected override int? InsertIndex => 0;

    #endregion

    }
}
