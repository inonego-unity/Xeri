/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XSerializedListKindProvider.cs
수정일 : 2026-11-19

# 설명
XHashSet·XQueue·XStack의 종류 정의를 만든다.
세 종류 모두 serialized 목록의 요소 하나가 행이며 셀이다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Reflection;

using UnityEditor;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// XHashSet·XQueue·XStack 종류 Provider.
    /// </summary>
    // ============================================================
    internal sealed class XSerializedListKindProvider : ICollectionKindProvider
    {

    #region 필드

        private const string rowsPath = "serialized";
        private const string valueColumnID = "Value";

        private static readonly Type[] definitions =
        {
            typeof(XHashSet_R<>), typeof(XHashSet_V<>),
            typeof(XQueue_R<>), typeof(XQueue_V<>),
            typeof(XStack_R<>), typeof(XStack_V<>),
        };

    #endregion

    #region 종류 생성

        // ------------------------------------------------------------
        /// <summary>
        /// XHashSet·XQueue·XStack 속성이면 종류 정의를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryCreate(SerializedProperty property, FieldInfo field, out CollectionKind kind)
        {
            kind = FindType(property) != null ? Create(property) : null;
            return kind != null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 속성의 Runtime 형식으로 종류 정의를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionKind Create(SerializedProperty property)
        {
            var instance = SerializedPropertyHelper.GetTargetObject(property);
            var type = FindType(property) ?? throw new ArgumentException("XHashSet·XQueue·XStack 속성이 아닙니다.", nameof(property));
            var definition = type.GetGenericTypeDefinition();
            var elementType = type.GetGenericArguments()[0];
            var isReference = definition == typeof(XHashSet_R<>) || definition == typeof(XQueue_R<>) || definition == typeof(XStack_R<>);
            var columns = new[]
            {
                new CollectionColumn
                (
                    valueColumnID, "값", TableColumnLayout.Flexible(240f, 100f, 0f, 1f), null,
                    elementType, isReference, createsValueOnAdd: isReference
                ),
            };

            const CollectionEdits common = CollectionEdits.Add | CollectionEdits.Remove | CollectionEdits.Duplicate | CollectionEdits.Paste;
            if (definition == typeof(XHashSet_R<>) || definition == typeof(XHashSet_V<>))
            {
                var comparer = CollectionKinds.CreateComparer(elementType, CollectionKinds.GetRuntimeComparer(instance));
                return new CollectionKind
                (
                    "XHashSet", "중복 항목은 편집 중 유지되지만, 실행 시 첫 값만 사용됩니다.",
                    columns, rowsPath, common, CollectionSync.Runtime,
                    keyRule: new CollectionKeyRule(valueColumnID, comparer, CollectionDuplicateWinner.First, false, "값")
                );
            }

            if (definition == typeof(XQueue_R<>) || definition == typeof(XQueue_V<>))
            {
                return new CollectionKind
                (
                    "XQueue", "위에서 아래로 처리하며, 새 항목은 맨 뒤에 추가됩니다.",
                    columns, rowsPath, common | CollectionEdits.Move, CollectionSync.Runtime
                );
            }

            return new CollectionKind
            (
                "XStack", "위에서 아래로 꺼내며, 새 항목은 맨 위에 추가됩니다.",
                columns, rowsPath, common | CollectionEdits.Move, CollectionSync.Runtime, CollectionInsert.Start
            );
        }

        private static Type FindType(SerializedProperty property)
        {
            var instance = SerializedPropertyHelper.GetTargetObject(property);
            return CollectionKinds.FindGenericType(instance?.GetType(), definitions);
        }

    #endregion

    }
}
