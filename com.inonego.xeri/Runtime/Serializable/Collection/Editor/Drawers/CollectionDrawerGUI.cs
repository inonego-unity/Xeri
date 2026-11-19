/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionDrawerGUI.cs
수정일 : 2026-11-19

# 설명
컬렉션 PropertyDrawer들의 공통 진입점이다.
속성에 맞는 종류 정의를 Provider 레지스트리에서 찾아 세션과 표를 만들고, 읽기 전용 여부를 적용한다.

# 특이사항, 제약사항
여러 오브젝트를 동시에 편집하는 경우는 지원하지 않고 안내만 표시한다.
ReadOnly Drawer가 선택되더라도 컬렉션의 조회 기능은 유지한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Reflection;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 컬렉션 PropertyDrawer 공통 진입점.
    /// </summary>
    // ============================================================
    [InitializeOnLoad]
    internal static class CollectionDrawerGUI
    {

    #region 초기화

        static CollectionDrawerGUI()
        {
            ReadOnlyDrawer.CreateReadOnlyView = CreateReadOnlyView;
        }

    #endregion

    #region 표 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 필드에 맞는 컬렉션 표를 만들고 실패하면 안내를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualElement Create(SerializedProperty property, FieldInfo field, string label = null)
        {
            var isReadOnly = field?.IsDefined(typeof(ReadOnlyAttribute), inherit: true) == true;
            return Create(property, field, isReadOnly, label)
                ?? new HelpBox("지원하지 않는 컬렉션 형식입니다.", HelpBoxMessageType.Warning);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// ReadOnly Drawer가 선택되어도 컬렉션 조회 표를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement CreateReadOnlyView(SerializedProperty property, FieldInfo field)
        {
            return field == null ? null : Create(property, field, true, null);
        }

        private static VisualElement Create(SerializedProperty property, FieldInfo field, bool isReadOnly, string label)
        {
            if (property.serializedObject.isEditingMultipleObjects)
            {
                return new HelpBox($"{property.displayName}: 여러 개체를 동시에 편집할 수 없습니다.", HelpBoxMessageType.Info);
            }

            try
            {
                if (!CollectionKinds.TryCreate(property, field, out var kind))
                {
                    return null;
                }

                var source = new CollectionSource(property.serializedObject, property.propertyPath, kind);
                var visibleRows = field?.GetCustomAttribute<CollectionEditorAttribute>(inherit: true)?.VisibleRows
                    ?? CollectionTableOptions.DefaultVisibleRows;
                return new CollectionTable(new CollectionSession(source, isReadOnly), new CollectionTableOptions(visibleRows, label));
            }
            catch (Exception exception)
            {
                return new HelpBox($"{property.displayName} 편집기를 열 수 없습니다.\n{exception.Message}", HelpBoxMessageType.Error);
            }
        }

    #endregion

    }
}
