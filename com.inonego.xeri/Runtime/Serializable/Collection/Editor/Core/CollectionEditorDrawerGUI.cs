/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionEditorDrawerGUI.cs
수정일 : 2026-10-31

# 설명
Collection Editor의 Model·View 생성과 ReadOnly 모드 적용을 담당한다.
여러 개체 편집 제한과 View 생성 오류도 동일한 진입점에서 표시한다.
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
    /// Collection Editor Drawer 공통 GUI 진입점.
    /// </summary>
    // ============================================================
    [InitializeOnLoad]
    internal static class CollectionEditorDrawerGUI
    {

    #region 초기화

        static CollectionEditorDrawerGUI()
        {
            ReadOnlyDrawer.CreateReadOnlyView = CreateReadOnlyView;
        }

    #endregion

    #region 메서드

        // ----------------------------------------------------------------------
        /// <summary>
        /// 단일 Object에서는 Model/View를 생성하고 실패 시 오류 UI를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static VisualElement Create
        (
            SerializedProperty property,
            string displayName,
            Func<ICollectionEditorModel> createModel,
            bool isReadOnly = false
        )
        {
            if (property.serializedObject.isEditingMultipleObjects)
            {
                return new HelpBox
                (
                    $"{displayName} 여러 개체를 동시에 편집할 수 없습니다.",
                    HelpBoxMessageType.Info
                );
            }

            try
            {
                return new CollectionEditorView(createModel(), isReadOnly);
            }
            catch (Exception exception)
            {
                return new HelpBox
                (
                    $"{displayName} 편집기를 열 수 없습니다.\n{exception.Message}",
                    HelpBoxMessageType.Error
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 타입 기반 컬렉션을 공통 Editor Model과 View에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualElement CreateForType(SerializedProperty property, FieldInfo field)
        {
            var isReadOnly = field?.IsDefined(typeof(ReadOnlyAttribute), inherit: true) == true;
            return TryCreateForType(property, field?.FieldType, isReadOnly, out var view)
                ? view
                : new HelpBox("지원하지 않는 컬렉션 형식입니다.", HelpBoxMessageType.Warning);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// ReadOnly Drawer가 선택되더라도 컬렉션의 조회 기능을 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement CreateReadOnlyView
        (
            SerializedProperty property, FieldInfo field
        )
        {
            if (field == null)
            {
                return null;
            }

            if (field.IsDefined(typeof(CollectionEditorAttribute), inherit: true))
            {
                return CollectionEditorAttributeDrawer.CreateGUI(property, field);
            }

            return TryCreateForType(property, field.FieldType, true, out var view)
                ? view
                : null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 타입별 Model을 생성하고 읽기 전용 여부를 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool TryCreateForType
        (
            SerializedProperty property,
            Type fieldType,
            bool isReadOnly,
            out VisualElement view
        )
        {
            for (var type = fieldType; type != null; type = type.BaseType)
            {
                if (!type.IsGenericType)
                {
                    continue;
                }

                var definition = type.GetGenericTypeDefinition();
                if (definition == typeof(XDictionary_RR<,>)
                    || definition == typeof(XDictionary_RV<,>)
                    || definition == typeof(XDictionary_VR<,>)
                    || definition == typeof(XDictionary_VV<,>))
                {
                    view = Create(property, "XDictionary", () => new XDictionaryEditorModel(property), isReadOnly);
                    return true;
                }

                if (definition == typeof(XOrdered<,>) || definition == typeof(XOrdered<,,>))
                {
                    view = Create(property, "XOrdered", () => new XOrderedEditorModel(property), isReadOnly);
                    return true;
                }

                if (definition == typeof(XHashSet_R<>) || definition == typeof(XHashSet_V<>))
                {
                    view = Create(property, "XHashSet", () => new XHashSetEditorModel(property), isReadOnly);
                    return true;
                }

                if (definition == typeof(XQueue_R<>) || definition == typeof(XQueue_V<>))
                {
                    view = Create(property, "XQueue", () => new XQueueEditorModel(property), isReadOnly);
                    return true;
                }

                if (definition == typeof(XStack_R<>) || definition == typeof(XStack_V<>))
                {
                    view = Create(property, "XStack", () => new XStackEditorModel(property), isReadOnly);
                    return true;
                }

                if (definition == typeof(XPriorityQueue<,>))
                {
                    view = Create(property, "XPriorityQueue", () => new XPriorityQueueEditorModel(property), isReadOnly);
                    return true;
                }
            }

            view = null;
            return false;
        }

    #endregion

    }
}
