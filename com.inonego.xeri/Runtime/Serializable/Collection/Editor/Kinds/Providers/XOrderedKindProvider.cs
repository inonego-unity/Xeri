/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XOrderedKindProvider.cs
수정일 : 2026-11-19

# 설명
XOrdered<TOrder, TValue>·XOrdered<TOrder, TKey, TValue>의 종류 정의를 만든다.
순서·키는 초안 열이며 확정 뒤 Runtime 정규화가 순서대로 정렬한다. 중복 키는 앞쪽 행이 사용된다.
MValue처럼 추가 처리가 필요한 소유자는 hook을 넣어 같은 종류 정의를 쓴다.
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
    /// XOrdered 종류 Provider.
    /// </summary>
    // ============================================================
    internal sealed class XOrderedKindProvider : ICollectionKindProvider
    {

    #region 필드

        private const string orderColumnID = "order";
        private const string keyColumnID = "key";
        private const string valueColumnID = "value";

        private static readonly Type[] definitions =
        {
            typeof(XOrdered<,>), typeof(XOrdered<,,>),
        };

    #endregion

    #region 종류 생성

        // ------------------------------------------------------------
        /// <summary>
        /// XOrdered 속성이면 종류 정의를 만든다.
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
        /// 속성의 Runtime 형식과 선택적 hook으로 종류 정의를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionKind Create(SerializedProperty property, ICollectionKindHooks hooks = null)
        {
            var instance = SerializedPropertyHelper.GetTargetObject(property);
            var type = CollectionKinds.FindGenericType(instance?.GetType(), definitions)
                ?? throw new ArgumentException("XOrdered 속성이 아닙니다.", nameof(property));
            var arguments = type.GetGenericArguments();
            var isKeyed = arguments.Length == 3;
            var orderType = arguments[0];
            var valueType = arguments[^1];

            // 순서는 핸들·편집 버튼과 두세 자리 값이 함께 들어가도록 96px로 고정하고 키:값은 4:6 가중치로 나눈다.
            var order = new CollectionColumn
            (
                orderColumnID, "순서", TableColumnLayout.Fixed(96f), orderColumnID,
                orderType, false, CollectionCellEdit.Draft
            );
            var value = new CollectionColumn
            (
                valueColumnID, "값", TableColumnLayout.Flexible(isKeyed ? 150f : 240f, 100f, 0f, isKeyed ? 6f : 1f),
                valueColumnID, valueType, true, CollectionCellEdit.Inline, true
            );
            if (!isKeyed)
            {
                return CreateKind
                (
                    new[]
                    {
                        order, value
                    }, null, hooks
                );
            }

            var keyType = arguments[1];
            var key = new CollectionColumn
            (
                keyColumnID, "키", TableColumnLayout.Flexible(150f, 100f, 0f, 4f), keyColumnID,
                keyType, false, CollectionCellEdit.Draft
            );
            var keyRule = new CollectionKeyRule
            (
                keyColumnID, CollectionKinds.CreateComparer(keyType), CollectionDuplicateWinner.First, true, "키"
            );
            return CreateKind
            (
                new[]
                {
                    order, key, value
                }, keyRule, hooks
            );
        }

        private static CollectionKind CreateKind
        (
            CollectionColumn[] columns, CollectionKeyRule keyRule, ICollectionKindHooks hooks
        )
        {
            const CollectionEdits edits = CollectionEdits.Add | CollectionEdits.Remove | CollectionEdits.Duplicate
                | CollectionEdits.Move | CollectionEdits.Paste;
            return new CollectionKind
            (
                "XOrdered", "순서 값에 따라 정렬하며, 같은 순서의 항목은 드래그한 순서를 유지합니다.",
                columns, "entries", edits, CollectionSync.Normalize,
                keyRule: keyRule, hooks: hooks
            );
        }

    #endregion

    }
}
