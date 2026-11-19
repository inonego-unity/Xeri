/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionEditorAttributeDrawer.cs
수정일 : 2026-11-19

# 설명
[CollectionEditor]를 붙인 List<T>·1차원 배열을 공통 컬렉션 표(CollectionTable)로 표시하는 UI Toolkit PropertyDrawer.
요소 형식, SerializeReference, Unique·Columns·VisibleRows와 ReadOnly는 공통 진입점이 필드에서 읽는다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// CollectionEditorAttribute UI Toolkit PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(CollectionEditorAttribute))]
    public sealed class CollectionEditorAttributeDrawer : PropertyDrawer
    {

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// PropertyField 생성 시 컬렉션 표를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (!property.isArray || property.propertyType == SerializedPropertyType.String)
            {
                return new HelpBox("List<T> 또는 1차원 배열에만 사용할 수 있습니다.", HelpBoxMessageType.Warning);
            }

            return CollectionDrawerGUI.Create(property, fieldInfo, preferredLabel);
        }

    #endregion

    }
}
