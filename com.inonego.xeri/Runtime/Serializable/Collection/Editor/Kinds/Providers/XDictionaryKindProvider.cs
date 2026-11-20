/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XDictionaryKindProvider.cs
수정일 : 2026-11-19

# 설명
XDictionary_RR·RV·VR·VV의 종류 정의를 만든다.
키는 초안 열이고 중복 키는 Runtime처럼 뒤쪽 행이 사용된다. 정수·열거형 키는 키 자동 채우기를 허용한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Reflection;

using UnityEditor;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.Editor;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// XDictionary 종류 Provider.
    /// </summary>
    // ============================================================
    internal sealed class XDictionaryKindProvider : ICollectionKindProvider
    {

    #region 필드

        private const string keyColumnID = "Key";
        private const string valueColumnID = "Value";

        private static readonly Type[] definitions =
        {
            typeof(XDictionary_RR<,>), typeof(XDictionary_RV<,>),
            typeof(XDictionary_VR<,>), typeof(XDictionary_VV<,>),
        };

    #endregion

    #region 종류 생성

        // ------------------------------------------------------------
        /// <summary>
        /// XDictionary 속성이면 종류 정의를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryCreate(SerializedProperty property, FieldInfo field, out CollectionKind kind)
        {
            var instance = SerializedPropertyHelper.GetTargetObject(property);
            kind = CollectionKinds.FindGenericType(instance?.GetType(), definitions) != null ? Create(property) : null;
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
            var type = CollectionKinds.FindGenericType(instance?.GetType(), definitions)
                ?? throw new ArgumentException("XDictionary 속성이 아닙니다.", nameof(property));
            var definition = type.GetGenericTypeDefinition();
            var arguments = type.GetGenericArguments();
            var keyType = arguments[0];
            var valueType = arguments[1];
            var keyIsReference = definition == typeof(XDictionary_RR<,>) || definition == typeof(XDictionary_RV<,>);
            var valueIsReference = definition == typeof(XDictionary_RR<,>) || definition == typeof(XDictionary_VR<,>);

            // 초기 기준 폭은 같게 두고 실제 키:값 분배는 4:6 가중치에 맡긴다.
            var columns = new[]
            {
                new CollectionColumn
                (
                    keyColumnID, "키", TableColumnLayout.Flexible(150f, 100f, 0f, 4f), keyColumnID,
                    keyType, keyIsReference, CollectionCellEdit.Draft, keyIsReference
                ),
                new CollectionColumn
                (
                    valueColumnID, "값", TableColumnLayout.Flexible(150f, 100f, 0f, 6f), valueColumnID,
                    valueType, valueIsReference, CollectionCellEdit.Inline, valueIsReference
                ),
            };

            var edits = CollectionEdits.Add | CollectionEdits.Remove | CollectionEdits.Duplicate | CollectionEdits.Paste;
            if (keyType == typeof(int) || keyType.IsEnum)
            {
                edits |= CollectionEdits.KeySet;
            }

            var comparer = CollectionKinds.CreateComparer(keyType, CollectionKinds.GetRuntimeComparer(instance));
            return new CollectionKind
            (
                "XDictionary", "중복 키는 마지막 값만 사용하고, 키가 없으면 제외됩니다.",
                columns, "serialized", edits, CollectionSync.Runtime,
                keyRule: new CollectionKeyRule(keyColumnID, comparer, CollectionDuplicateWinner.Last, true, "키")
            );
        }

    #endregion

    }
}
