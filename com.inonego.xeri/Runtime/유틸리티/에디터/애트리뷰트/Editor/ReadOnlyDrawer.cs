/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ReadOnlyDrawer.cs
수정일 : 2026-04-30

# 설명
ReadOnlyAttribute 전용 PropertyDrawer.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// ReadOnlyAttribute 전용 PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        // ------------------------------------------------------------
        /// <summary>
        /// UIToolkit Inspector용 읽기 전용 필드를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new PropertyField(property);
            field.SetEnabled(false);
            return field;
        }
    }

}
