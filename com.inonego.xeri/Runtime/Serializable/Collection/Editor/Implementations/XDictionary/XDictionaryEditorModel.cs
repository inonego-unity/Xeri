/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XDictionaryEditorModel.cs
수정일 : 2026-10-31

# 설명
XDictionary의 serialized pair list를 Key/Value table로 노출한다.
managed-reference Add, 비교자 기반 중복 상태, mutation, bulk key set과 runtime dictionary rebuild를 담당한다.
런타임 Dictionary가 행 순서를 보장하지 않으므로 재정렬 기능은 제공하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// XDictionary의 Key 집합에 적용할 작업.
    /// </summary>
    // ============================================================
    internal enum CollectionKeySetOperation
    {
        Add,
        Remove,
        Confine,
    }

    // ============================================================
    /// <summary>
    /// XDictionary Collection Editor Model.
    /// </summary>
    // ============================================================
    internal sealed class XDictionaryEditorModel : CollectionEditorModelBase
    {

    #region 상수

        private const string serializedFieldName = "serialized";
        private const string valueFieldName = "Value";

    #endregion

    #region 상태

        /// <inheritdoc />
        public override IReadOnlyList<CollectionColumnSpec> Columns => columns;
        private readonly IReadOnlyList<CollectionColumnSpec> columns;

        /// <inheritdoc />
        public override int Count => GetSerializedList().arraySize;

        /// <inheritdoc />
        public override bool CanReorder => false;

        /// <inheritdoc />
        public override string ValidationColumnID => keyFieldName;
        private const string keyFieldName = "Key";

        // ------------------------------------------------------------
        /// <summary>
        /// 사전 키의 실제 타입을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Type KeyType => keyType;
        private readonly Type keyType;
        private readonly Type valueType;
        private readonly bool keyIsManagedReference;
        private readonly bool valueIsManagedReference;
        private readonly Func<object, object, bool> keyEquals;
        private readonly IEqualityComparer<object> keyComparer;

        /// <inheritdoc />
        public override string Description => "중복 키는 마지막 값만 사용하고, 키가 없으면 제외됩니다.";

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// XDictionary property의 Editor Model을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public XDictionaryEditorModel(SerializedProperty property)
            : base(property)
        {
            var instance = SerializedPropertyHelper.GetTargetObject(property);
            var collectionType = FindCollectionType
            (
                instance?.GetType(),
                typeof(XDictionary_RR<,>), typeof(XDictionary_RV<,>),
                typeof(XDictionary_VR<,>), typeof(XDictionary_VV<,>)
            );
            var genericArguments = collectionType?.GetGenericArguments();

            keyType = genericArguments != null && genericArguments.Length > 0
                ? genericArguments[0]
                : null;
            valueType = genericArguments != null && genericArguments.Length > 1
                ? genericArguments[1]
                : null;

            var runtimeComparer = instance?.GetType().GetProperty("Comparer")?.GetValue(instance);
            var adapterType = typeof(BoxedKeyComparer<>).MakeGenericType(keyType ?? typeof(object));
            keyComparer = (IEqualityComparer<object>)Activator.CreateInstance(adapterType, runtimeComparer);
            keyEquals = keyComparer.Equals;

            ResolveManagedReferenceColumns
            (
                instance?.GetType(),
                out keyIsManagedReference,
                out valueIsManagedReference
            );

            columns = new[]
            {
                // 초기 기준 폭은 동일하게 두고, 실제 Key:Value 분배는 4:6 가중치에 맡긴다.
                new CollectionColumnSpec(keyFieldName, "키", 150f, 70f, stretchWeight: 4f),
                new CollectionColumnSpec(valueFieldName, "값", 150f, 100f, stretchWeight: 6f),
            };

            _ = GetSerializedList();
        }

    #endregion

    #region 조회

        /// <inheritdoc />
        public override SerializedProperty GetRowProperty(int sourceIndex)
        {
            return GetSerializedList().GetArrayElementAtIndex(sourceIndex);
        }

        /// <inheritdoc />
        public override SerializedProperty GetCellProperty
        (
            int sourceIndex,
            string columnID
        )
        {
            return GetRowProperty(sourceIndex).FindPropertyRelative(columnID);
        }

        /// <inheritdoc />
        public override CollectionCellEditMode GetCellEditMode
        (
            int sourceIndex,
            string columnID
        )
        {
            if (!string.Equals(columnID, keyFieldName, StringComparison.Ordinal))
            {
                return CollectionCellEditMode.Default;
            }

            var property = GetCellProperty(sourceIndex, columnID);
            if (property == null)
            {
                return CollectionCellEditMode.ReadOnly;
            }

            // Key는 타입에 관계없이 별도 편집창에서만 변경한다.
            return CollectionCellEditMode.ReplaceOnly;
        }

        /// <inheritdoc />
        public override bool TryGetManagedReferenceAddTarget
        (
            out string columnID,
            out Type declaredType
        )
        {
            if (keyIsManagedReference && keyType != null)
            {
                columnID = keyFieldName;
                declaredType = keyType;
                return true;
            }

            if (valueIsManagedReference && valueType != null)
            {
                columnID = valueFieldName;
                declaredType = valueType;
                return true;
            }

            columnID = null;
            declaredType = null;
            return false;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/>실제 Dictionary Comparer를 사용해 무효한 Key 행을 식별한다.
        /// <br/>중복 시 마지막 행이 유효하므로 이전 행만 정리 대상으로 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public override string GetRowValidationMessage(int sourceIndex)
        {
            var key = GetKeyObject(sourceIndex);
            if (key == null)
            {
                return "키가 지정되지 않아 제외됩니다.";
            }

            var lastDuplicateIndex = -1;
            for (var index = sourceIndex + 1; index < Count; index++)
            {
                var other = GetKeyObject(index);
                if (other != null && keyEquals(key, other))
                {
                    lastDuplicateIndex = index;
                }
            }

            return lastDuplicateIndex < 0
                ? string.Empty
                : $"중복 키: {lastDuplicateIndex + 1}번 항목의 값을 사용합니다.";
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 중복된 Key 중 마지막 유효 행도 사용자에게 안내한다.
        /// </summary>
        // ------------------------------------------------------------
        public string GetActiveDuplicateMessage(int sourceIndex)
        {
            var key = GetKeyObject(sourceIndex);
            if (key == null || !string.IsNullOrEmpty(GetRowValidationMessage(sourceIndex)))
            {
                return string.Empty;
            }

            var firstDuplicateIndex = -1;
            for (var index = 0; index < sourceIndex; index++)
            {
                var other = GetKeyObject(index);
                if (other != null && keyEquals(key, other))
                {
                    firstDuplicateIndex = index;
                    break;
                }
            }

            return firstDuplicateIndex < 0
                ? string.Empty
                : $"중복 키: {firstDuplicateIndex + 1}번 항목보다 이 값이 우선됩니다.";
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 Key 검증 및 적용 전 후보 검증을 공통 진단 결과로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override CollectionValidationResult Validate(CollectionValidationContext context)
        {
            if (context.IsCandidate)
            {
                var result = new CollectionValidationResult();
                var reason = ValidateKeyReplacement(context.CandidateValue, context.SourceIndex);
                if (!string.IsNullOrEmpty(reason))
                {
                    result.Add
                    (
                        new CollectionCellDiagnostic
                        (
                            "collection.key.invalid-candidate", DiagnosticSeverity.Error,
                            reason, keyFieldName
                        )
                    );
                    result.BlockApply();
                }

                return result;
            }

            var current = base.Validate(context);
            if (context.SourceIndex.HasValue && !current.IsRowInvalid)
            {
                var message = GetActiveDuplicateMessage(context.SourceIndex.Value);
                if (!string.IsNullOrEmpty(message))
                {
                    current.Add
                    (
                        new CollectionCellDiagnostic
                        (
                            "collection.key.active-duplicate", DiagnosticSeverity.Info,
                            message, keyFieldName
                        )
                    );
                }
            }

            return current;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Null 및 중복 Key를 runtime Comparer 기준으로 한 번의 순회에서 확인한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool HasInvalidRows()
        {
            var seen = new HashSet<object>(keyComparer);
            for (var index = Count - 1; index >= 0; index--)
            {
                var key = GetKeyObject(index);
                if (key == null || !seen.Add(key))
                {
                    return true;
                }
            }

            return false;
        }

        private SerializedProperty GetSerializedList()
        {
            var root = GetCollectionProperty();
            var list = root?.FindPropertyRelative(serializedFieldName);

            if (list == null || !list.isArray)
            {
                throw new InvalidOperationException
                (
                    "XDictionary의 직렬화 목록을 찾을 수 없습니다."
                );
            }

            return list;
        }

        private object GetKeyObject(int sourceIndex)
        {
            return GetPropertyValue
            (
                GetCellProperty(sourceIndex, keyFieldName)
            );
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// RR/RV/VR/VV 변형에서 Key/Value의 SerializeReference 여부를 해석한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static void ResolveManagedReferenceColumns
        (
            Type collectionType,
            out bool keyManagedReference,
            out bool valueManagedReference
        )
        {
            keyManagedReference = false;
            valueManagedReference = false;

            for (var type = collectionType; type != null; type = type.BaseType)
            {
                if (!type.IsGenericType)
                {
                    continue;
                }

                var definition = type.GetGenericTypeDefinition();
                if (definition == typeof(XDictionary_RR<,>))
                {
                    keyManagedReference = true;
                    valueManagedReference = true;
                    return;
                }

                if (definition == typeof(XDictionary_RV<,>))
                {
                    keyManagedReference = true;
                    return;
                }

                if (definition == typeof(XDictionary_VR<,>))
                {
                    valueManagedReference = true;
                    return;
                }

                if (definition == typeof(XDictionary_VV<,>))
                {
                    return;
                }
            }
        }

    #endregion

    #region 행 변경

        // ----------------------------------------------------------------------
        /// <summary>
        /// 확정할 Key 편집 값의 타입, null과 runtime equality 충돌을 검사한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public string ValidateKeyReplacement(object key, int? sourceIndex)
        {
            if (key == null)
            {
                return "키를 지정하세요.";
            }

            if (keyType != null && !keyType.IsInstanceOfType(key))
            {
                return $"키 형식이 올바르지 않습니다. ({keyType.Name} 필요)";
            }

            for (var index = 0; index < Count; index++)
            {
                if (index != sourceIndex && keyEquals(key, GetKeyObject(index)))
                {
                    return $"{index + 1}번 항목과 키가 중복됩니다.";
                }
            }

            return string.Empty;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 검증된 Key를 타입에 맞는 전체 값 교체로 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        public int ReplaceKey(int sourceIndex, object key)
        {
            var reason = ValidateKeyReplacement(key, sourceIndex);
            if (!string.IsNullOrEmpty(reason))
            {
                throw new InvalidOperationException(reason);
            }

            Undo.RecordObjects(SerializedObject.targetObjects, "XDictionary 키 변경");
            SerializedObject.Update();
            var property = GetCellProperty(sourceIndex, keyFieldName);
            var path = property.propertyPath;
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                property.managedReferenceValue = key;
            }
            else if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = (UnityEngine.Object)key;
            }
            else
            {
                property.boxedValue = key;
            }

            return CommitRowChanges(sourceIndex, path);
        }

        /// <inheritdoc />
        public override int Add(Action<int> initialize = null)
        {
            return AddDefaultRow
            (
                GetSerializedList,
                insertIndex: null,
                "XDictionary 항목 추가",
                initialize
            );
        }

        /// <inheritdoc />
        public override int DuplicateAt(int sourceIndex)
        {
            return DuplicateRow
            (
                GetSerializedList,
                sourceIndex,
                insertIndex: null,
                "XDictionary 항목 복제",
                "XDictionary 항목"
            );
        }

        /// <inheritdoc />
        public override void RemoveAt(int sourceIndex)
        {
            RemoveRow
            (
                GetSerializedList,
                sourceIndex,
                "XDictionary 항목 삭제"
            );
        }

        /// <inheritdoc />
        public override void RemoveMany
        (
            IReadOnlyList<int> sourceIndices,
            string undoName
        )
        {
            RemoveRows(GetSerializedList, sourceIndices, undoName);
        }

        /// <inheritdoc />
        public override void Move(int sourceIndex, int destinationIndex)
        {
            // NONE
        }

        /// <inheritdoc />
        protected override void CommitSerializedChanges(bool recordUndo)
        {
            // Runtime은 null Key를 건너뛰고 유효한 행을 투영하므로 전체 갱신을 중단하지 않는다.
            UpdateRuntimeCollection();

        }

    #endregion

    #region Key 집합 편집

        // ----------------------------------------------------------------------
        /// <summary>
        /// 생성 Key 집합을 Add/Remove/Confine 방식으로 backing pair 목록에 반영한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void ApplyKeySet
        (
            IReadOnlyList<object> keys,
            CollectionKeySetOperation operation
        )
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            var undoName = operation switch
            {
                CollectionKeySetOperation.Add => "XDictionary 키 추가",
                CollectionKeySetOperation.Remove => "XDictionary 키 제거",
                CollectionKeySetOperation.Confine => "XDictionary 키 범위 제한",
                _ => "XDictionary 키 변경",
            };
            Undo.RecordObjects(SerializedObject.targetObjects, undoName);
            SerializedObject.Update();

            try
            {
                var list = GetSerializedList();

                switch (operation)
                {
                    case CollectionKeySetOperation.Add:
                        AddGeneratedKeys(list, keys);
                        break;

                    case CollectionKeySetOperation.Remove:
                        RemoveGeneratedKeys(list, keys);
                        break;

                    case CollectionKeySetOperation.Confine:
                        ConfineGeneratedKeys(list, keys);
                        break;
                }
            }
            catch
            {
                // 실패한 일괄 편집은 Apply 이전의 미확정 변경만 폐기한다.
                SerializedObject.Update();
                throw;
            }

            CommitRowChanges();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 Key와 새 Key를 동일한 runtime Comparer로 중복 검사한다.
        /// </summary>
        // ------------------------------------------------------------
        private void AddGeneratedKeys
        (
            SerializedProperty list,
            IReadOnlyList<object> keys
        )
        {
            var existing = new HashSet<object>(keyComparer);
            for (var index = 0; index < list.arraySize; index++)
            {
                existing.Add(GetSimpleKeyValue(list, index));
            }

            foreach (var key in keys)
            {
                if (existing.Add(key))
                {
                    AddPairWithKey(list, key);
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 생성 Key 집합에 포함된 serialized 행을 역순으로 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RemoveGeneratedKeys
        (
            SerializedProperty list,
            IReadOnlyList<object> keys
        )
        {
            var generated = new HashSet<object>(keys, keyComparer);
            for (var index = list.arraySize - 1; index >= 0; index--)
            {
                if (generated.Contains(GetSimpleKeyValue(list, index)))
                {
                    DeleteArrayElement(list, index);
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 집합만 남기며 중복 Key는 가장 뒤쪽 serialized 행을 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ConfineGeneratedKeys
        (
            SerializedProperty list,
            IReadOnlyList<object> keys
        )
        {
            var generated = new HashSet<object>(keys, keyComparer);
            var retained = new HashSet<object>(keyComparer);

            // 뒤쪽 행부터 검사해 runtime에서 유효한 마지막 Key를 보존한다.
            for (var index = list.arraySize - 1; index >= 0; index--)
            {
                var key = GetSimpleKeyValue(list, index);
                if (!generated.Contains(key) || !retained.Add(key))
                {
                    DeleteArrayElement(list, index);
                }
            }

            foreach (var key in keys)
            {
                if (retained.Add(key))
                {
                    AddPairWithKey(list, key);
                }
            }
        }

        private static void AddPairWithKey
        (
            SerializedProperty list,
            object key
        )
        {
            var index = AppendDefaultElement(list);
            var pair = list.GetArrayElementAtIndex(index);

            pair.FindPropertyRelative(keyFieldName).boxedValue = key;
        }

        // ================================================================================
        /// <summary>
        /// 원본 Dictionary의 IEqualityComparer와 동일한 Equals/Hash 계약을 사용한다.
        /// </summary>
        // ================================================================================
        private sealed class BoxedKeyComparer<T> : IEqualityComparer<object>
        {
            private readonly IEqualityComparer<T> comparer;

            // ------------------------------------------------------------
            /// <summary>
            /// BoxedKeyComparer 인스턴스를 초기화한다.
            /// </summary>
            // ------------------------------------------------------------
            public BoxedKeyComparer(object runtimeComparer)
            {
                comparer = runtimeComparer as IEqualityComparer<T> ?? EqualityComparer<T>.Default;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 두 값의 동등성을 비교한다.
            /// </summary>
            // ------------------------------------------------------------
            public new bool Equals(object left, object right)
            {
                return comparer.Equals
                (
                    left == null ? default : (T)left,
                    right == null ? default : (T)right
                );
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 지정한 값의 해시 코드를 계산한다.
            /// </summary>
            // ------------------------------------------------------------
            public int GetHashCode(object value)
            {
                return value == null ? 0 : comparer.GetHashCode((T)value);
            }
        }

        private static object GetSimpleKeyValue
        (
            SerializedProperty list,
            int sourceIndex
        )
        {
            var pair = list.GetArrayElementAtIndex(sourceIndex);
            return pair.FindPropertyRelative(keyFieldName).boxedValue;
        }

    #endregion

    }
}
