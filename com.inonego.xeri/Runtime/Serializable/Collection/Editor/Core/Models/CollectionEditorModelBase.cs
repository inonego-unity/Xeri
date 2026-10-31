/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionEditorModelBase.cs
수정일 : 2026-10-31

# 설명
Collection Editor Model이 공통으로 사용하는 SerializedObject, property path, label과 mutation helper를 제공한다.
구체 Model은 collection별 행 구조, 검증과 변경 정책을 정의한다. 검색 문자열 구성과 창 수명 관리는 UI 계층이 담당한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;
using UnityEngine;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// Collection Editor Model 공통 기반.
    /// </summary>
    // ============================================================
    internal abstract class CollectionEditorModelBase : ICollectionEditorModel
    {

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 확정된 컬렉션 변경을 같은 대상의 에디터에 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static event Action<UnityEngine.Object, string, string> Committed;

    #endregion

    #region 상태

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 대상이 포함된 SerializedObject를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public SerializedObject SerializedObject { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 대상 컬렉션의 프로퍼티 경로를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string CollectionPropertyPath { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 대상 컬렉션의 표시 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Label { get; }
        private uint committedHash;
        private int committedContentFingerprint;
        private bool isCommitting;

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 내용을 반영할 때 행 재정렬이 필요한지 판단한다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual bool ReordersRowsOnCommit(string propertyPath) => false;

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션의 편집 규칙을 설명하는 문구를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual string Description => string.Empty;

        // ------------------------------------------------------------
        /// <summary>
        /// 검증 메시지를 표시할 열의 식별자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual string ValidationColumnID => Columns[0].ID;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 직렬화된 행 수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract int Count { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 순서를 변경할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract bool CanReorder { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집기에 표시할 열 구성을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract IReadOnlyList<CollectionColumnSpec> Columns { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// collection root property의 안정적인 locator 상태를 캡처한다.
        /// </summary>
        // ------------------------------------------------------------
        protected CollectionEditorModelBase(SerializedProperty property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            SerializedObject = property.serializedObject;
            CollectionPropertyPath = property.propertyPath;
            Label = property.displayName;
            committedHash = property.contentHash;
            committedContentFingerprint = ComputeContentFingerprint(property);
        }

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 대상 컬렉션의 현재 SerializedProperty를 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public SerializedProperty GetCollectionProperty()
        {
            return SerializedObject.FindProperty(CollectionPropertyPath);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 독립 SerializedObject에 같은 collection Model을 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual ICollectionEditorModel CreateFor(SerializedProperty property)
        {
            return (ICollectionEditorModel)Activator.CreateInstance(GetType(), property);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 순서의 source row를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual IEnumerable<int> GetSourceRows()
        {
            for (var index = 0; index < Count; index++)
            {
                yield return index;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 row의 값 또는 참조 identity를 얻는다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual object GetRowIdentity(int sourceIndex)
        {
            return GetPropertyValue(GetRowProperty(sourceIndex));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// identity가 유일한 row를 찾고 모호하면 선택을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public int FindRow(object identity)
        {
            var result = -1;
            for (var index = 0; index < Count; index++)
            {
                var candidate = GetRowIdentity(index);
                var matches = identity != null && (identity.GetType().IsValueType
                    || identity is string
                        ? Equals(identity, candidate)
                        : ReferenceEquals(identity, candidate));
                if (!matches)
                {
                    continue;
                }

                // 같은 값이나 공유 참조를 가진 여러 행은 위치만으로 추적하지 않는다.
                if (result >= 0)
                {
                    return -1;
                }

                result = index;
            }

            return result;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 행의 SerializedProperty를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract SerializedProperty GetRowProperty(int sourceIndex);

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 행과 열에 해당하는 SerializedProperty를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract SerializedProperty GetCellProperty(int sourceIndex, string columnID);

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 행과 열에 적용할 편집 방식을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual CollectionCellEditMode GetCellEditMode(int sourceIndex, string columnID)
        {
            return CollectionCellEditMode.Default;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Add 전에 type 선택이 필요한 managed-reference column 정보를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public virtual bool TryGetManagedReferenceAddTarget(out string columnID, out Type declaredType)
        {
            columnID = null;
            declaredType = null;
            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 행의 유효성 검사 메시지를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual string GetRowValidationMessage(int sourceIndex)
        {
            return string.Empty;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 원본 행 검증을 공통 진단 결과로 변환한다.
        /// <br/> 무효한 행 여부는 메시지 심각도와 독립적으로 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual CollectionValidationResult Validate(CollectionValidationContext context)
        {
            var result = new CollectionValidationResult();
            if (context.IsCandidate)
            {
                result.Add
                (
                    new CollectionCellDiagnostic
                    (
                        "collection.edit.unsupported", DiagnosticSeverity.Error,
                        "이 컬렉션의 후보 값 편집은 지원되지 않습니다.", context.ColumnID
                    )
                );
                result.BlockApply();
                return result;
            }

            if (!context.SourceIndex.HasValue
                || context.SourceIndex.Value < 0 || context.SourceIndex.Value >= Count)
            {
                return result;
            }

            var message = GetRowValidationMessage(context.SourceIndex.Value);
            if (!string.IsNullOrEmpty(message))
            {
                result.Add
                (
                    new CollectionCellDiagnostic
                    (
                        "collection.row.invalid", DiagnosticSeverity.Warning,
                        message, ValidationColumnID
                    )
                );
                result.MarkRowInvalid();
            }

            return result;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 비교/validation에 사용할 현재 SerializedProperty 값을 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected static object GetPropertyValue(SerializedProperty property)
        {
            if (property == null)
            {
                return null;
            }

            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                return property.managedReferenceValue;
            }

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                return property.objectReferenceValue;
            }

            return SerializedPropertyHelper.GetTargetObject(property);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 collection 정의를 상속 계층에서 찾아 타입 인자를 해석한다.
        /// </summary>
        // ------------------------------------------------------------
        protected static Type FindCollectionType(Type type, params Type[] definitions)
        {
            for (; type != null; type = type.BaseType)
            {
                if (type.IsGenericType
                    && Array.IndexOf(definitions, type.GetGenericTypeDefinition()) >= 0)
                {
                    return type;
                }
            }

            return null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// runtime의 typed comparer를 boxed validation 값에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        protected static Func<object, object, bool> CreateEquality(Type type, object comparer = null)
        {
            var method = typeof(CollectionEditorModelBase).GetMethod(nameof(CreateTypedEquality), BindingFlags.NonPublic | BindingFlags.Static);
            var arguments = new object[1];
            arguments[0] = comparer;
            return (Func<object, object, bool>)method.MakeGenericMethod(type)
                .Invoke(null, arguments);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// generic equality를 validation delegate로 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private static Func<object, object, bool> CreateTypedEquality<T>(object comparer)
        {
            var typed = comparer as IEqualityComparer<T> ?? EqualityComparer<T>.Default;
            return (left, right) => typed.Equals(left == null ? default : (T)left, right == null ? default : (T)right);
        }

    #endregion

    #region 행 명령

        // ------------------------------------------------------------
        /// <summary>
        /// 항목을 추가하고 생성된 행 인덱스를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract int Add(Action<int> initialize = null);

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 행을 복제하고 새 행 인덱스를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract int DuplicateAt(int sourceIndex);

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 인덱스의 행을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract void RemoveAt(int sourceIndex);

        // ------------------------------------------------------------
        /// <summary>
        /// 선택된 여러 행을 한 번의 작업으로 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract void RemoveMany(IReadOnlyList<int> sourceIndices, string undoName);

        // ------------------------------------------------------------
        /// <summary>
        /// 행을 지정한 인덱스로 이동한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract void Move(int sourceIndex, int destinationIndex);

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> Clipboard 행을 단일 Undo/Apply/Commit으로 붙여넣는다.
        /// <br/> 선택과 메시지는 호출자가 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryPasteAt(int sourceIndex, out int resolvedIndex, out string reason)
        {
            resolvedIndex = -1;
            reason = string.Empty;
            if (sourceIndex < 0 || sourceIndex >= Count)
            {
                reason = "붙여넣을 원본 행이 유효하지 않습니다.";
                return false;
            }

            SerializedObject.UpdateIfRequiredOrScript();
            if (!SerializedPropertyClipboard.CanPasteValue(GetRowProperty(sourceIndex), out reason))
            {
                return false;
            }

            Undo.RecordObjects(SerializedObject.targetObjects, "컬렉션 행 붙여넣기");
            SerializedObject.Update();

            if (!SerializedPropertyClipboard.TryPasteValue(GetRowProperty(sourceIndex), out reason))
            {
                SerializedObject.Update();
                return false;
            }

            resolvedIndex = CommitRowChanges(sourceIndex);
            // 순서가 유지되는 컬렉션에서는 값이 중복되어 identity 조회가
            // 모호해져도 실제 붙여넣은 위치를 그대로 선택할 수 있다.
            if (resolvedIndex < 0 && CanReorder && sourceIndex < Count)
            {
                resolvedIndex = sourceIndex;
            }

            return true;
        }

    #endregion

    #region 상태 동기화와 변경 확정

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 상태를 받아들여 표시 동기화를 새 편집으로 확정하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public void Synchronize()
        {
            SerializedObject.UpdateIfRequiredOrScript();
            CaptureCommittedState();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Undo/Redo에서 복원된 소유자 상태를 정리한 뒤 Model 스냅샷을 갱신한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void RestoreSerializedState()
        {
            SerializedObject.UpdateIfRequiredOrScript();
            RestoreOwner();
            CaptureCommittedState();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 바인딩 통지 중 실제 데이터가 달라진 편집만 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool CommitPropertyChange(SerializedProperty property)
        {
            if (isCommitting || property == null
                || property.propertyPath != CollectionPropertyPath
                    && !property.propertyPath.StartsWith(CollectionPropertyPath + ".", StringComparison.Ordinal)
                || property.serializedObject != SerializedObject)
            {
                return false;
            }

            var collection = GetCollectionProperty();
            if (collection == null || collection.contentHash == committedHash
                && ComputeContentFingerprint(collection) == committedContentFingerprint)
            {
                return false;
            }

            CommitChanges(property.propertyPath);
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Value 상세 편집은 구조 정규화 없이 확정하고 모든 View에 통지한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CommitValueChange(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath)
                || !propertyPath.StartsWith(CollectionPropertyPath + ".", StringComparison.Ordinal))
            {
                return;
            }

            var collection = GetCollectionProperty();
            if (collection == null || collection.contentHash == committedHash
                && ComputeContentFingerprint(collection) == committedContentFingerprint)
            {
                return;
            }

            // Value 편집도 같은 Commit 파이프라인을 거치되 Order/Key 정규화는 생략한다.
            CommitChanges(recordUndo: false, propertyPath, trackedSourceIndex: null, normalizeStructure: false);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 내용을 직렬화 대상에 반영하고 변경 상태를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CommitChanges(string propertyPath = null)
        {
            CommitChanges(recordUndo: true, propertyPath, trackedSourceIndex: null);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이미 Undo를 기록한 행 명령의 최종 동기화 경계.
        /// </summary>
        // ------------------------------------------------------------
        protected void CommitRowChanges()
        {
            CommitChanges(recordUndo: false, propertyPath: null, trackedSourceIndex: null);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Apply 직후, 정규화 전에 행 identity를 캡처하여 이동 후 위치를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected int CommitRowChanges(int sourceIndex, string propertyPath = null)
        {
            var identity = CommitChanges(recordUndo: false, propertyPath, trackedSourceIndex: sourceIndex);
            return FindRow(identity);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션별 직렬화 및 Undo 반영 작업을 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract void CommitSerializedChanges(bool recordUndo);

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 변경 후 소유자의 상태를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void RefreshOwner()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Undo/Redo로 복원된 소유자의 파생 상태와 변경 통지를 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void RestoreOwner()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 불변식과 owner 갱신을 끝낸 뒤 확정한 변경을 한 번만 통지한다.
        /// </summary>
        // ------------------------------------------------------------
        private object CommitChanges
        (
            bool recordUndo, string propertyPath, int? trackedSourceIndex,
            bool normalizeStructure = true
        )
        {
            if (isCommitting)
            {
                return null;
            }

            isCommitting = true;
            try
            {
                SerializedObject.ApplyModifiedProperties();
                SerializedObject.UpdateIfRequiredOrScript();
                var identity = trackedSourceIndex.HasValue
                    && trackedSourceIndex.Value >= 0
                    && trackedSourceIndex.Value < Count
                        ? GetRowIdentity(trackedSourceIndex.Value)
                        : null;
                if (normalizeStructure)
                {
                    CommitSerializedChanges(recordUndo);
                }

                RefreshOwner();
                SerializedObject.Update();
                CaptureCommittedState();

                foreach (var target in SerializedObject.targetObjects)
                {
                    EditorUtility.SetDirty(target);
                    Committed?.Invoke(target, CollectionPropertyPath, ReordersRowsOnCommit(propertyPath) ? null : propertyPath);
                }

                return identity;
            }
            finally
            {
                isCommitting = false;
            }
        }

        private void CaptureCommittedState()
        {
            var collection = GetCollectionProperty();
            if (collection != null)
            {
                committedHash = collection.contentHash;
                committedContentFingerprint = ComputeContentFingerprint(collection);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 중첩 SerializeReference 필드를 포함한 현재 직렬화 상태를 비교한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static int ComputeContentFingerprint(SerializedProperty root)
        {
            var iterator = root.Copy();
            var end = root.GetEndProperty();
            var fingerprint = 17;
            do
            {
                unchecked
                {
                    fingerprint = fingerprint * 31 + iterator.contentHash.GetHashCode();
                }
            }
            while (iterator.Next(true) && !SerializedProperty.EqualContents(iterator, end));

            return fingerprint;
        }

    #endregion

    #region 행 작업 공통 구현

        // ----------------------------------------------------------------------
        /// <summary>
        /// 기본값 row를 지정 위치 또는 끝에 추가하고 collection 변경을 commit한다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected int AddDefaultRow
        (
            Func<SerializedProperty> getArray, int? insertIndex, string undoName,
            Action<int> initialize = null
        )
        {
            Undo.RecordObjects(SerializedObject.targetObjects, undoName);
            SerializedObject.Update();

            int index;
            try
            {
                var array = getArray();
                index = InsertElement(array, insertIndex);
                SerializedPropertyHelper.SetDefaultValue(array.GetArrayElementAtIndex(index));
                initialize?.Invoke(index);
            }
            catch (Exception exception)
            {
                SerializedObject.Update();
                Debug.LogException(exception);
                return -1;
            }

            var resolved = CommitRowChanges(index);
            return resolved >= 0 ? resolved : (CanReorder ? Math.Min(index, Count - 1) : -1);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// source row snapshot을 지정 위치 또는 끝에 독립 복제하고 변경을 commit한다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected int DuplicateRow
        (
            Func<SerializedProperty> getArray, int sourceIndex, int? insertIndex,
            string undoName, string logName
        )
        {
            if (sourceIndex < 0 || sourceIndex >= Count)
            {
                return -1;
            }

            SerializedObject.UpdateIfRequiredOrScript();

            var source = GetRowProperty(sourceIndex);
            var isCreated = SerializedPropertySnapshot.TryCreate(source, out var snapshot, out var reason);
            if (!isCreated)
            {
                Debug.LogWarning($"{logName} 복제 실패: {reason}");
                return -1;
            }

            Undo.RecordObjects(SerializedObject.targetObjects, undoName);
            SerializedObject.Update();

            var array = getArray();
            var index = InsertElement(array, insertIndex);
            var destination = array.GetArrayElementAtIndex(index);

            if (!snapshot.TryApply(destination, out reason))
            {
                // 아직 Apply하지 않은 SerializedObject 변경은 전체 폐기한다.
                SerializedObject.Update();
                Debug.LogWarning($"{logName} 복제 적용 실패: {reason}");
                return -1;
            }

            var resolved = CommitRowChanges(index);
            return resolved >= 0 ? resolved : (CanReorder ? Math.Min(index, Count - 1) : -1);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 row를 제거하고 collection 변경을 commit한다.
        /// </summary>
        // ------------------------------------------------------------
        protected void RemoveRow(Func<SerializedProperty> getArray, int sourceIndex, string undoName)
        {
            if (sourceIndex < 0 || sourceIndex >= Count)
            {
                return;
            }

            Undo.RecordObjects(SerializedObject.targetObjects, undoName);
            SerializedObject.Update();

            DeleteArrayElement(getArray(), sourceIndex);

            CommitRowChanges();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 여러 source row를 하나의 Undo/Apply/Commit 경계에서 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        protected void RemoveRows(Func<SerializedProperty> getArray, IReadOnlyList<int> sourceIndices, string undoName)
        {
            if (sourceIndices == null || sourceIndices.Count == 0)
            {
                return;
            }

            var indices = new List<int>(sourceIndices.Count);
            for (var index = 0; index < sourceIndices.Count; index++)
            {
                var sourceIndex = sourceIndices[index];
                if (sourceIndex < 0 || sourceIndex >= Count
                    || indices.Contains(sourceIndex))
                {
                    continue;
                }

                indices.Add(sourceIndex);
            }

            if (indices.Count == 0)
            {
                return;
            }

            indices.Sort((left, right) => right.CompareTo(left));

            Undo.RecordObjects(SerializedObject.targetObjects, undoName);
            SerializedObject.Update();

            var array = getArray();
            foreach (var sourceIndex in indices)
            {
                DeleteArrayElement(array, sourceIndex);
            }

            CommitRowChanges();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 row를 이동하고 collection 변경을 commit한다.
        /// </summary>
        // ------------------------------------------------------------
        protected void MoveRow(Func<SerializedProperty> getArray, int sourceIndex, int destinationIndex, string undoName)
        {
            if (sourceIndex < 0
                || destinationIndex < 0
                || sourceIndex >= Count
                || destinationIndex >= Count
                || sourceIndex == destinationIndex)
            {
                return;
            }

            Undo.RecordObjects(SerializedObject.targetObjects, undoName);
            SerializedObject.Update();

            getArray().MoveArrayElement(sourceIndex, destinationIndex);

            CommitRowChanges();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 지정 위치 또는 array 끝에 새 element를 추가하고 실제 index를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static int InsertElement(SerializedProperty array, int? insertIndex)
        {
            if (!insertIndex.HasValue || insertIndex.Value >= array.arraySize)
            {
                return AppendElement(array);
            }

            var index = Math.Max(0, insertIndex.Value);
            array.InsertArrayElementAtIndex(index);
            return index;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// array 끝에 새 element를 추가하고 index를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static int AppendElement(SerializedProperty array)
        {
            var index = array.arraySize;
            array.arraySize++;
            return index;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// array 끝에 타입 기본값 element를 추가하고 index를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        protected static int AppendDefaultElement(SerializedProperty array)
        {
            var index = AppendElement(array);
            SerializedPropertyHelper.SetDefaultValue(array.GetArrayElementAtIndex(index));
            return index;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Unity ObjectReference 배열의 2단계 삭제까지 포함해 element를 제거한다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected static void DeleteArrayElement(SerializedProperty array, int index)
        {
            var previousSize = array.arraySize;
            array.DeleteArrayElementAtIndex(index);

            if (array.arraySize == previousSize && index < array.arraySize)
            {
                array.DeleteArrayElementAtIndex(index);
            }
        }

    #endregion

    #region 런타임 복원과 정규화

        // --------------------------------------------------------------------------------
        /// <summary>
        /// Editor Apply 이후 현재 serialized state에서 runtime을 명시적으로 복원한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        protected void UpdateRuntimeCollection()
        {
            var instance = SerializedPropertyHelper.GetTargetObject(GetCollectionProperty());
            if (instance is not ISerializedRuntimeCollection collection)
            {
                throw new InvalidOperationException("이 컬렉션은 런타임 동기화를 지원하지 않습니다: " + CollectionPropertyPath);
            }

            collection.UpdateRuntime();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime 컬렉션의 정규화 API를 호출해 직렬화 상태를 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        protected void NormalizeSerializedState()
        {
            var instance = SerializedPropertyHelper.GetTargetObject(GetCollectionProperty());
            if (instance is ISerializedCollectionNormalizer normalizer)
            {
                normalizer.NormalizeSerializedState();
            }
        }

    #endregion

    }
}
