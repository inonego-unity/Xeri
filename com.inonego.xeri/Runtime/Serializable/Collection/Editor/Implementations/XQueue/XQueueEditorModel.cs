/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XQueueEditorModel.cs
수정일 : 2026-09-29

# 설명
XQueue의 serialized row를 Front → Back 순서로 편집하고 runtime Queue state rebuild를 담당한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// XQueue Collection Editor Model.
    /// </summary>
    // ============================================================
    internal sealed class XQueueEditorModel : XSerializedListEditorModelBase
    {

    #region 상태

        /// <inheritdoc />
        public override bool CanReorder => true;
        /// <inheritdoc />
        public override string Description => "위에서 아래로 처리하며, 새 항목은 맨 뒤에 추가됩니다.";

        /// <inheritdoc />
        protected override string AddUndoName => "XQueue 항목 추가";
        /// <inheritdoc />
        protected override string RemoveUndoName => "XQueue 항목 삭제";

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// XQueueEditorModel 인스턴스를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public XQueueEditorModel(SerializedProperty property)
            : base(property, "앞 → 뒤")
        {
            // NONE
        }

    #endregion

    }
}
