/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionSession.cs
수정일 : 2026-11-19

# 설명
컬렉션 편집의 단일 쓰기 입구다.
모든 편집은 Apply에서 같은 순서로 처리한다:
검증 → Undo 기록 → 쓰기 → Apply → Runtime 동기화 → 소유자 동기화 → 변경 세대 진행 → 통지 → 결과 행 재탐색.
외부 변경과 Undo/Redo는 Observe·Restore로 받아들이며 새 편집으로 확정하지 않는다.

# 특이사항, 제약사항
Undo는 Unity Undo(Undo.RecordObjects)만 사용한다. 자체 실행 이력을 두지 않는다.
결과 행은 정규화 전에 기록한 행 식별 값, 키 값 순서로 다시 찾는다.
직렬화 상태 비교는 RevisionTracker가 대상·경로마다 한 번만 하고, 세션은 마지막으로 받아들인 세대 번호만 비교한다.
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
    /// 컬렉션 편집의 단일 쓰기 입구.
    /// </summary>
    // ============================================================
    internal sealed class CollectionSession
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 행·셀 읽기 면을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionSource Source { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 읽기 전용 세션인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsReadOnly { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 종류 정의를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionKind Kind => Source.Kind;

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션을 담은 SerializedObject를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public SerializedObject SerializedObject => Source.SerializedObject;

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 대상과 경로가 공유하는 변경 세대를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public ulong Revision => Source.Revision;

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> SerializedObject를 아직 쓸 수 있는지 반환한다.
        /// <br/> Inspector 재구성 등으로 해제됐거나 대상이 사라지면 false다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsAlive
        {
            get
            {
                // Unity는 해제된 SerializedObject를 확인하는 공개 API가 없어 접근 예외로 판단한다.
                try
                {
                    return SerializedObject.targetObject != null;
                }
                catch (NullReferenceException)
                {
                    return false;
                }
            }
        }

        // 직렬화 상태 비교는 RevisionTracker가 경로마다 한 번만 하고, 세션은 마지막으로 받아들인 세대만 기억한다.
        private ulong acceptedRevision;
        private bool isCommitting;

        // 마지막 진단 결과와 그 결과를 계산한 변경 세대.
        private CollectionValidation cachedValidation;
        private ulong cachedValidationRevision;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 확정한 변경을 같은 대상의 다른 편집기에 알린다.
        /// <br/> 인자는 대상, 컬렉션 경로, 바뀐 속성 경로(행 구조면 null)다.
        /// </summary>
        // ------------------------------------------------------------
        internal static event Action<UnityEngine.Object, string, string> Committed;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 읽기 면으로 편집 세션을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionSession(CollectionSource source, bool isReadOnly = false)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            IsReadOnly = isReadOnly;
            RevisionTracker.Observe(Source.GetProperty());
            AcceptRevision();
            Kind.Hooks?.Attach(Source);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 컬렉션을 다른 SerializedObject로 편집하는 세션을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionSession Reopen(SerializedObject serializedObject)
        {
            return new CollectionSession(Source.Reopen(serializedObject), IsReadOnly);
        }

    #endregion

    #region 외부 변경과 세대 수용

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 마지막 확정 이후 컬렉션이 바뀌었는지 확인한다.
        /// <br/> 바뀌었으면 공유 세대를 진행하고 true를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Observe()
        {
            SerializedObject.UpdateIfRequiredOrScript();
            if (!HasUnacceptedChange())
            {
                return false;
            }

            AcceptRevision();
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Undo/Redo로 복원된 상태를 받아들이고 소유자를 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        public void Restore()
        {
            SerializedObject.UpdateIfRequiredOrScript();
            Kind.Hooks?.SyncOwner(Source);
            RevisionTracker.Observe(Source.GetProperty());
            AcceptRevision();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 이 세션이 아직 받아들이지 않은 변경이 있는지 반환한다.
        /// <br/> 다른 표가 같은 경로의 변경을 먼저 관찰했어도 세대 차이로 알아챈다.
        /// </summary>
        // ----------------------------------------------------------------------
        private bool HasUnacceptedChange()
        {
            // 관찰 기준은 세션이 아니라 대상 객체와 경로별로 공유한다.
            RevisionTracker.Observe(Source.GetProperty());
            return Source.Revision != acceptedRevision;
        }

        private void AcceptRevision()
        {
            acceptedRevision = Source.Revision;
        }

    #endregion

    #region 검증

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 현재 컬렉션의 행 진단과 무효 행을 반환한다.
        /// <br/> 진단은 컬렉션 내용만으로 정해지므로 같은 변경 세대에서는 이전 결과를 쓴다.
        /// </summary>
        // ----------------------------------------------------------------------
        public CollectionValidation Validate()
        {
            RevisionTracker.Observe(Source.GetProperty());
            var revision = Source.Revision;
            if (cachedValidation == null || cachedValidationRevision != revision)
            {
                cachedValidation = BuildValidation();
                cachedValidationRevision = revision;
            }

            return cachedValidation;
        }

        private CollectionValidation BuildValidation()
        {
            var result = new CollectionValidation();
            var count = Source.Count;
            if (Kind.KeyRule != null)
            {
                ValidateKeys(result, count);
            }

            if (Kind.Hooks == null)
            {
                return result;
            }

            // hook 진단은 행 무효 여부와 무관한 안내다.
            var extra = new List<CollectionCellDiagnostic>();
            for (var index = 0; index < count; index++)
            {
                extra.Clear();
                Kind.Hooks.AppendDiagnostics(Source, index, extra);
                foreach (var diagnostic in extra)
                {
                    result.Add(index, diagnostic);
                }
            }

            return result;
        }

        private void ValidateKeys(CollectionValidation result, int count)
        {
            var rule = Kind.KeyRule;
            var groups = new Dictionary<object, List<int>>(rule.Comparer);
            var order = new List<List<int>>();
            for (var index = 0; index < count; index++)
            {
                var key = Source.GetKey(index);
                if (key == null)
                {
                    if (rule.RequiresKey)
                    {
                        result.AddInvalid
                        (
                            index,
                            new CollectionCellDiagnostic
                            (
                                "collection.key.missing", DiagnosticSeverity.Warning,
                                $"{rule.Noun} 미지정: 실행 시 제외됩니다.", rule.ColumnID
                            )
                        );
                    }

                    continue;
                }

                if (!groups.TryGetValue(key, out var group))
                {
                    group = new List<int>();
                    groups.Add(key, group);
                    order.Add(group);
                }

                group.Add(index);
            }

            // Runtime이 쓰는 한 행만 남기고 나머지 중복 행은 무효로 표시한다.
            foreach (var group in order)
            {
                if (group.Count < 2)
                {
                    continue;
                }

                var winner = rule.Winner == CollectionDuplicateWinner.First ? group[0] : group[^1];
                foreach (var index in group)
                {
                    if (index == winner)
                    {
                        result.Add
                        (
                            index,
                            new CollectionCellDiagnostic
                            (
                                "collection.key.active-duplicate", DiagnosticSeverity.Info,
                                $"{rule.Noun} 중복: 이 항목이 사용됩니다.", rule.ColumnID
                            )
                        );
                        continue;
                    }

                    result.AddInvalid
                    (
                        index,
                        new CollectionCellDiagnostic
                        (
                            "collection.key.duplicate", DiagnosticSeverity.Warning,
                            $"{rule.Noun} 중복: {winner + 1}번 항목이 사용됩니다.", rule.ColumnID
                        )
                    );
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 초안 값의 형식, 필수 여부와 키 중복을 검사한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionValidation Validate(CollectionDraft draft)
        {
            if (draft == null)
            {
                throw new ArgumentNullException(nameof(draft));
            }

            var result = new CollectionValidation();
            var count = Source.Count;
            if (draft.SourceIndex.HasValue && (draft.SourceIndex.Value < 0 || draft.SourceIndex.Value >= count))
            {
                result.Block(DraftError("collection.target.stale", "편집할 행이 존재하지 않습니다.", null));
                return result;
            }

            foreach (var pair in draft.Values)
            {
                var column = Kind.FindColumn(pair.Key);
                if (column == null)
                {
                    result.Block(DraftError("collection.column.unknown", $"알 수 없는 열입니다: {pair.Key}", pair.Key));
                    continue;
                }

                ValidateDraftValue(result, draft, column, pair.Value, count);
            }

            return result;
        }

        private void ValidateDraftValue
        (
            CollectionValidation result, CollectionDraft draft, CollectionColumn column, object value, int count
        )
        {
            var rule = Kind.KeyRule;
            var isKey = rule != null && column.ID == rule.ColumnID;
            if (value == null)
            {
                var required = isKey && rule.RequiresKey || column.ValueType?.IsValueType == true;
                if (required)
                {
                    result.Block(DraftError("collection.draft.missing", $"{column.Header} 값을 지정하세요.", column.ID));
                }

                return;
            }

            if (column.ValueType != null && !column.ValueType.IsInstanceOfType(value))
            {
                var message = $"{column.Header} 형식이 올바르지 않습니다. ({column.ValueType.Name} 필요)";
                result.Block(DraftError("collection.draft.type", message, column.ID));
                return;
            }

            if (!isKey)
            {
                return;
            }

            // 자기 자신을 제외한 행과 같은 키면 Runtime에서 한쪽이 사라진다.
            for (var index = 0; index < count; index++)
            {
                if (index != draft.SourceIndex && rule.Comparer.Equals(value, Source.GetValue(index, column)))
                {
                    var message = $"{rule.Noun} 중복: {index + 1}번 항목과 같습니다.";
                    result.Block(DraftError("collection.draft.duplicate", message, column.ID));
                    return;
                }
            }
        }

        private static CollectionCellDiagnostic DraftError(string code, string message, string columnID)
        {
            return new CollectionCellDiagnostic(code, DiagnosticSeverity.Error, message, columnID);
        }

    #endregion

    #region 편집 적용

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 요청을 한 번의 Undo로 적용하고 결과를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionEditResult Apply(CollectionEdit edit)
        {
            if (edit == null)
            {
                throw new ArgumentNullException(nameof(edit));
            }

            if (IsReadOnly)
            {
                return CollectionEditResult.Failure("읽기 전용 컬렉션입니다.");
            }

            return edit.Type switch
            {
                CollectionEditType.AddRow => ApplyAddRow(edit.Draft),
                CollectionEditType.Duplicate => ApplyDuplicate(edit.SourceIndex),
                CollectionEditType.Remove => ApplyRemove(edit.SourceIndices, edit.UndoName),
                CollectionEditType.Move => ApplyMove(edit.SourceIndex, edit.DestinationIndex),
                CollectionEditType.SetCell => ApplySetCell(edit.SourceIndex, edit.ColumnID, edit.Value),
                CollectionEditType.CommitDraft => ApplyCommitDraft(edit.Draft),
                CollectionEditType.CommitBound => ApplyCommitBound(edit.PropertyPath),
                CollectionEditType.Paste => ApplyPaste(edit.SourceIndex),
                CollectionEditType.ApplyKeySet => ApplyKeySet(edit.Keys, edit.KeySetOperation),
                _ => CollectionEditResult.Failure($"지원하지 않는 편집입니다: {edit.Type}"),
            };
        }

        private CollectionEditResult ApplyAddRow(CollectionDraft draft)
        {
            if (!Kind.Supports(CollectionEdits.Add))
            {
                return Unsupported();
            }

            if (draft.SourceIndex.HasValue)
            {
                return CollectionEditResult.Failure("새 행 초안이 아닙니다.");
            }

            var validation = Validate(draft);
            if (!validation.CanApply)
            {
                return CollectionEditResult.Failure(validation.Diagnostics[0].Message);
            }

            return Write
            (
                $"{Kind.Label} 항목 추가",
                rows =>
                {
                    var index = SerializedPropertyHelper.InsertArrayElement(rows, InsertIndex);
                    SerializedPropertyHelper.SetDefaultValue(rows.GetArrayElementAtIndex(index));
                    WriteDraft(index, draft);
                    Kind.Hooks?.InitializeRow(Source, index);
                    return index;
                }
            );
        }

        private CollectionEditResult ApplyDuplicate(int sourceIndex)
        {
            if (!Kind.Supports(CollectionEdits.Duplicate))
            {
                return Unsupported();
            }

            if (!IsRow(sourceIndex))
            {
                return InvalidRow();
            }

            // 원본 값을 Undo 기록 전에 독립 스냅샷으로 떠 둔다.
            SerializedObject.UpdateIfRequiredOrScript();
            if (!SerializedPropertySnapshot.TryCreate(Source.GetRow(sourceIndex), out var snapshot, out var reason))
            {
                return CollectionEditResult.Failure($"복제 실패: {reason}");
            }

            return Write
            (
                $"{Kind.Label} 항목 복제",
                rows =>
                {
                    var index = SerializedPropertyHelper.InsertArrayElement(rows, InsertIndex);
                    if (!snapshot.TryApply(rows.GetArrayElementAtIndex(index), out var applyReason))
                    {
                        throw new InvalidOperationException($"복제 적용 실패: {applyReason}");
                    }

                    return index;
                }
            );
        }

        private CollectionEditResult ApplyRemove(IReadOnlyList<int> sourceIndices, string undoName)
        {
            if (!Kind.Supports(CollectionEdits.Remove))
            {
                return Unsupported();
            }

            var indices = new SortedSet<int>(Comparer<int>.Create((left, right) => right.CompareTo(left)));
            foreach (var index in sourceIndices)
            {
                if (IsRow(index))
                {
                    indices.Add(index);
                }
            }

            if (indices.Count == 0)
            {
                return CollectionEditResult.Unchanged;
            }

            return Write
            (
                undoName ?? $"{Kind.Label} 항목 삭제",
                rows =>
                {
                    // 뒤쪽부터 지워 앞쪽 인덱스를 유지한다.
                    foreach (var index in indices)
                    {
                        SerializedPropertyHelper.DeleteArrayElement(rows, index);
                    }

                    return -1;
                }
            );
        }

        private CollectionEditResult ApplyMove(int sourceIndex, int destinationIndex)
        {
            if (!Kind.Supports(CollectionEdits.Move))
            {
                return Unsupported();
            }

            if (!IsRow(sourceIndex) || !IsRow(destinationIndex))
            {
                return InvalidRow();
            }

            if (sourceIndex == destinationIndex)
            {
                return CollectionEditResult.Success(sourceIndex);
            }

            return Write
            (
                $"{Kind.Label} 항목 이동",
                rows =>
                {
                    rows.MoveArrayElement(sourceIndex, destinationIndex);
                    return destinationIndex;
                }
            );
        }

        private CollectionEditResult ApplySetCell(int sourceIndex, string columnID, object value)
        {
            var column = Kind.FindColumn(columnID);
            if (column == null)
            {
                return CollectionEditResult.Failure($"알 수 없는 열입니다: {columnID}");
            }

            if (column.Edit == CollectionCellEdit.ReadOnly)
            {
                return CollectionEditResult.Failure($"{column.Header} 열은 수정할 수 없습니다.");
            }

            if (column.Edit == CollectionCellEdit.Draft)
            {
                return CollectionEditResult.Failure($"{column.Header} 열은 검증 후 확정해야 합니다.");
            }

            if (!IsRow(sourceIndex))
            {
                return InvalidRow();
            }

            return Write
            (
                $"{Kind.Label} {column.Header} 변경",
                _ =>
                {
                    WriteValue(Source.GetCell(sourceIndex, column), value);
                    return sourceIndex;
                }
            );
        }

        private CollectionEditResult ApplyCommitDraft(CollectionDraft draft)
        {
            if (!draft.SourceIndex.HasValue)
            {
                return CollectionEditResult.Failure("기존 행 초안이 아닙니다.");
            }

            var validation = Validate(draft);
            if (!validation.CanApply)
            {
                return CollectionEditResult.Failure(validation.Diagnostics[0].Message);
            }

            var sourceIndex = draft.SourceIndex.Value;
            return Write
            (
                $"{Kind.Label} 항목 변경",
                _ =>
                {
                    WriteDraft(sourceIndex, draft);
                    return sourceIndex;
                }
            );
        }

        private CollectionEditResult ApplyCommitBound(string propertyPath)
        {
            if (isCommitting || !SerializedPropertyHelper.IsSameOrDescendantPath(propertyPath, Source.Path))
            {
                return CollectionEditResult.Unchanged;
            }

            // 같은 값의 바인딩 통지는 새 편집으로 확정하지 않는다.
            if (!HasUnacceptedChange())
            {
                return CollectionEditResult.Unchanged;
            }

            var tracked = Source.TryLocate(propertyPath, out var sourceIndex, out _) ? sourceIndex : -1;
            return Commit(tracked, propertyPath, recordUndo: true);
        }

        private CollectionEditResult ApplyPaste(int sourceIndex)
        {
            if (!Kind.Supports(CollectionEdits.Paste))
            {
                return Unsupported();
            }

            if (!IsRow(sourceIndex))
            {
                return InvalidRow();
            }

            SerializedObject.UpdateIfRequiredOrScript();
            if (!SerializedPropertyClipboard.CanPasteValue(Source.GetRow(sourceIndex), out var reason))
            {
                return CollectionEditResult.Failure(reason);
            }

            return Write
            (
                "컬렉션 행 붙여넣기",
                rows =>
                {
                    if (!SerializedPropertyClipboard.TryPasteValue(rows.GetArrayElementAtIndex(sourceIndex), out var pasteReason))
                    {
                        throw new InvalidOperationException(pasteReason);
                    }

                    return sourceIndex;
                }
            );
        }

        private CollectionEditResult ApplyKeySet(IReadOnlyList<object> keys, CollectionKeySetOperation operation)
        {
            if (!Kind.Supports(CollectionEdits.KeySet) || Kind.KeyColumn == null)
            {
                return Unsupported();
            }

            var undoName = operation switch
            {
                CollectionKeySetOperation.Add => $"{Kind.Label} 키 추가",
                CollectionKeySetOperation.Remove => $"{Kind.Label} 키 제거",
                _ => $"{Kind.Label} 키 범위 제한",
            };
            return Write
            (
                undoName,
                rows =>
                {
                    WriteKeySet(rows, keys, operation);
                    return -1;
                }
            );
        }

        private CollectionEditResult Unsupported()
        {
            return CollectionEditResult.Failure($"{Kind.Label}에서 지원하지 않는 편집입니다.");
        }

        private static CollectionEditResult InvalidRow()
        {
            return CollectionEditResult.Failure("대상 행이 유효하지 않습니다.");
        }

        private bool IsRow(int sourceIndex)
        {
            return sourceIndex >= 0 && sourceIndex < Source.Count;
        }

        private int? InsertIndex => Kind.Insert == CollectionInsert.Start ? 0 : null;

    #endregion

    #region 쓰기

        // ------------------------------------------------------------
        /// <summary>
        /// Undo를 기록하고 쓰기를 실행한 뒤 변경을 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        private CollectionEditResult Write(string undoName, Func<SerializedProperty, int> write)
        {
            Undo.RecordObjects(SerializedObject.targetObjects, undoName);
            SerializedObject.Update();

            int tracked;
            try
            {
                tracked = write(Source.GetRows());
            }
            catch (Exception exception)
            {
                // 아직 Apply하지 않은 변경은 모두 버린다.
                SerializedObject.Update();
                return CollectionEditResult.Failure(exception.Message);
            }

            return Commit(tracked, null, recordUndo: false);
        }

        private void WriteDraft(int sourceIndex, CollectionDraft draft)
        {
            foreach (var pair in draft.Values)
            {
                var column = Kind.FindColumn(pair.Key);
                WriteValue(Source.GetCell(sourceIndex, column), pair.Value);
            }
        }

        private void WriteKeySet
        (
            SerializedProperty rows, IReadOnlyList<object> keys, CollectionKeySetOperation operation
        )
        {
            var rule = Kind.KeyRule;
            var column = Kind.KeyColumn;
            var requested = new HashSet<object>(rule.Comparer);
            foreach (var key in keys)
            {
                if (key != null)
                {
                    requested.Add(key);
                }
            }

            switch (operation)
            {
                case CollectionKeySetOperation.Add:
                    var existing = new HashSet<object>(rule.Comparer);
                    for (var index = 0; index < rows.arraySize; index++)
                    {
                        var key = Source.GetValue(index, column);
                        if (key != null)
                        {
                            existing.Add(key);
                        }
                    }

                    AppendKeys(rows, column, keys, existing);
                    break;

                case CollectionKeySetOperation.Remove:
                    for (var index = rows.arraySize - 1; index >= 0; index--)
                    {
                        var key = Source.GetValue(index, column);
                        if (key != null && requested.Contains(key))
                        {
                            SerializedPropertyHelper.DeleteArrayElement(rows, index);
                        }
                    }

                    break;

                case CollectionKeySetOperation.Confine:
                    ConfineKeys(rows, column, rule, requested);
                    AppendKeys(rows, column, keys, CollectKeys(rows, column, rule));
                    break;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 요청 집합에 없는 행과 Runtime이 쓰지 않는 중복 행을 지운다.
        /// <br/> 중복 키는 Runtime이 사용하는 행을 남긴다.
        /// </summary>
        // ------------------------------------------------------------
        private void ConfineKeys
        (
            SerializedProperty rows, CollectionColumn column, CollectionKeyRule rule, HashSet<object> requested
        )
        {
            var count = rows.arraySize;
            var retained = new HashSet<object>(rule.Comparer);
            var removed = new List<int>();
            var fromLast = rule.Winner == CollectionDuplicateWinner.Last;
            for (var step = 0; step < count; step++)
            {
                var index = fromLast ? count - 1 - step : step;
                var key = Source.GetValue(index, column);
                if (key == null || !requested.Contains(key) || !retained.Add(key))
                {
                    removed.Add(index);
                }
            }

            removed.Sort((left, right) => right.CompareTo(left));
            foreach (var index in removed)
            {
                SerializedPropertyHelper.DeleteArrayElement(rows, index);
            }
        }

        private HashSet<object> CollectKeys(SerializedProperty rows, CollectionColumn column, CollectionKeyRule rule)
        {
            var keys = new HashSet<object>(rule.Comparer);
            for (var index = 0; index < rows.arraySize; index++)
            {
                var key = Source.GetValue(index, column);
                if (key != null)
                {
                    keys.Add(key);
                }
            }

            return keys;
        }

        private void AppendKeys
        (
            SerializedProperty rows, CollectionColumn column, IReadOnlyList<object> keys, HashSet<object> existing
        )
        {
            foreach (var key in keys)
            {
                if (key == null || !existing.Add(key))
                {
                    continue;
                }

                var index = SerializedPropertyHelper.AppendDefaultArrayElement(rows);
                WriteValue(Source.GetCell(index, column), key);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 속성 종류에 맞는 방식으로 값을 넣는다.
        /// </summary>
        // ------------------------------------------------------------
        private static void WriteValue(SerializedProperty property, object value)
        {
            if (property == null)
            {
                throw new InvalidOperationException("값을 넣을 셀을 찾을 수 없습니다.");
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.ManagedReference:
                    property.managedReferenceValue = value;
                    break;

                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = value as UnityEngine.Object;
                    break;

                default:
                    property.boxedValue = value;
                    break;
            }
        }

    #endregion

    #region 변경 확정

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 쓴 값을 Apply하고 Runtime·소유자를 맞춘 뒤 한 번만 통지한다.
        /// <br/> 추적 행은 정규화 전에 식별 값과 키를 기록해 다시 찾는다.
        /// </summary>
        // ----------------------------------------------------------------------
        private CollectionEditResult Commit(int trackedIndex, string propertyPath, bool recordUndo)
        {
            if (isCommitting)
            {
                return CollectionEditResult.Unchanged;
            }

            isCommitting = true;
            try
            {
                SerializedObject.ApplyModifiedProperties();
                SerializedObject.UpdateIfRequiredOrScript();

                var tracked = IsRow(trackedIndex);
                var identity = tracked ? Source.GetIdentity(trackedIndex) : null;
                var key = tracked ? Source.GetKey(trackedIndex) : null;

                SyncRuntime(recordUndo);
                Kind.Hooks?.SyncOwner(Source);
                SerializedObject.Update();

                // 세대와 스냅샷을 먼저 갱신한 뒤 다른 편집기에 알린다.
                AdvanceRevision();
                AcceptRevision();
                var notifiedPath = AffectsOrder(propertyPath) ? null : propertyPath;
                foreach (var target in SerializedObject.targetObjects)
                {
                    EditorUtility.SetDirty(target);
                    Committed?.Invoke(target, Source.Path, notifiedPath);
                }

                return CollectionEditResult.Success(tracked ? ResolveRow(trackedIndex, identity, key) : -1);
            }
            finally
            {
                isCommitting = false;
            }
        }

        private void SyncRuntime(bool recordUndo)
        {
            var instance = SerializedPropertyHelper.GetTargetObject(Source.GetProperty());
            switch (Kind.Sync)
            {
                case CollectionSync.Runtime:
                    if (instance is not ISerializedRuntimeCollection collection)
                    {
                        throw new InvalidOperationException($"이 컬렉션은 런타임 동기화를 지원하지 않습니다: {Source.Path}");
                    }

                    collection.UpdateRuntime();
                    break;

                case CollectionSync.Normalize:
                    // 바인딩 편집은 Unity가 값만 기록했으므로 정규화 결과도 Undo에 남긴다.
                    if (recordUndo)
                    {
                        Undo.RecordObjects(SerializedObject.targetObjects, $"{Kind.Label} 정리");
                    }

                    (instance as ISerializedCollectionNormalizer)?.NormalizeSerializedState();
                    break;
            }
        }

        private void AdvanceRevision()
        {
            foreach (var target in SerializedObject.targetObjects)
            {
                if (target == SerializedObject.targetObject)
                {
                    RevisionTracker.Advance(SerializedObject, Source.Path);
                    continue;
                }

                using var other = new SerializedObject(target);
                RevisionTracker.Advance(other, Source.Path);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 바뀐 셀이 행 순서를 정하는 열이면 행 구조 변경으로 본다.
        /// </summary>
        // ------------------------------------------------------------
        private bool AffectsOrder(string propertyPath)
        {
            if (propertyPath == null)
            {
                return true;
            }

            if (!Source.TryLocate(propertyPath, out _, out var column) || column == null)
            {
                return false;
            }

            return column == Kind.DisplayOrderColumn || column.Edit == CollectionCellEdit.Draft;
        }

        private int ResolveRow(int trackedIndex, object identity, object key)
        {
            var row = Source.Find(identity);
            if (row >= 0)
            {
                return row;
            }

            row = Source.FindKey(key);
            if (row >= 0)
            {
                return row;
            }

            // 정규화하지 않는 종류는 쓴 위치가 그대로 유지된다.
            return Kind.Sync != CollectionSync.Normalize && IsRow(trackedIndex) ? trackedIndex : -1;
        }

    #endregion

    }
}
