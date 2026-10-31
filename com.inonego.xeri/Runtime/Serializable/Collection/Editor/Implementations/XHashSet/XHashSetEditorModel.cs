/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XHashSetEditorModel.cs
수정일 : 2026-09-29

# 설명
XHashSet의 serialized row를 편집하고 duplicate validation과 runtime rebuild를 담당한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// XHashSet Collection Editor Model.
    /// </summary>
    // ============================================================
    internal sealed class XHashSetEditorModel : XSerializedListEditorModelBase
    {

    #region 상태

        /// <inheritdoc />
        public override bool CanReorder => false;
        /// <inheritdoc />
        public override string Description => "중복 항목은 편집 중 유지되지만, 실행 시 첫 값만 사용됩니다.";

        /// <inheritdoc />
        protected override string AddUndoName => "XHashSet 항목 추가";
        /// <inheritdoc />
        protected override string RemoveUndoName => "XHashSet 항목 삭제";

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// XHashSetEditorModel 인스턴스를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public XHashSetEditorModel(SerializedProperty property)
            : base(property, "값")
        {
            // NONE
        }

    #endregion

    #region 조회

        /// <inheritdoc />
        public override string GetRowValidationMessage(int sourceIndex)
        {
            var value = GetElementObject(sourceIndex);

            // HashSet 역직렬화는 먼저 Add된 값을 유지하므로 뒤쪽 duplicate만 invalid다.
            for (var index = 0; index < sourceIndex; index++)
            {
                if (elementEquals(value, GetElementObject(index)))
                {
                    return "이전 행과 값이 중복됩니다.";
                }
            }

            return string.Empty;
        }

    #endregion

    }
}
