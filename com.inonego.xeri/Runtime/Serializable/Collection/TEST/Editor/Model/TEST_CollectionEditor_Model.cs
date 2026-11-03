/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_CollectionEditor_Model.cs
수정일 : 2026-11-03

# 설명
Editor Model의 명령·직렬화 동기화와 값 검증을 확인한다.
Runtime 컬렉션 자체의 Apply/Update 계약은 TEST_CollectionRuntime에서 검증한다.

# 테스트 구성
 X: Editor Model 명령·Runtime 동기화·Revision·직렬화 값 조회
 V: 모델의 값 검증
========================================================================= BLOCK_HEADER_END */

using System;
using System.Linq;

using UnityEditor;
using UnityEngine;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;
using inonego.Xeri.Serializable.Editor;

namespace inonego.Xeri.TEST.Serializable._Collection
{
    public partial class TEST_CollectionEditor
    {

    #region X-1: Editor Model 명령과 Runtime 동기화

        [Test]
        public void TEST_CollectionEditor_HashSet참조형_Add시_직렬화콜백과Runtime동기화()
        {
            var collectionTarget = ScriptableObject.CreateInstance<CollectionTarget>();
            using var so = new SerializedObject(collectionTarget);
            try
            {
                var setModel = new XHashSetEditorModel(so.FindProperty("ReferenceSet"));
                var addedIndex = setModel.Add
                (
                    index =>
                    {
                        setModel.GetCellProperty(index, "Value").managedReferenceValue =
                            new NumericIModifier(NumericIOperation.ADD, 7);
                    }
                );

                Assert.IsTrue(addedIndex.Succeeded);
                Assert.AreEqual(0, addedIndex.RowIndex);
                Assert.AreEqual(1, collectionTarget.ReferenceSet.Count);
                Assert.AreEqual(1, setModel.Count);

                so.Update();
                Assert.AreEqual
                (
                    SerializedPropertyType.ManagedReference,
                    setModel.GetCellProperty(0, "Value").propertyType
                );

                setModel.RemoveAt(0);
                Assert.AreEqual(0, collectionTarget.ReferenceSet.Count);
                Assert.AreEqual(0, setModel.Count);
            }
            finally
            {
                Undo.ClearUndo(collectionTarget);
                UnityEngine.Object.DestroyImmediate(collectionTarget);
            }
        }

        [Test]
        public void TEST_CollectionEditor_컬렉션4종_EditorAddRemove동기화()
        {
            var collectionTarget = ScriptableObject.CreateInstance<CollectionTarget>();
            using var so = new SerializedObject(collectionTarget);
            try
            {
                var set = new XHashSetEditorModel(so.FindProperty("Set"));
                set.Add(index => set.GetCellProperty(index, "Value").intValue = 5);
                Assert.IsTrue(collectionTarget.Set.Contains(5));

                var dictionary = new XDictionaryEditorModel(so.FindProperty("Dictionary"));
                dictionary.Add
                (
                    index =>
                    {
                        dictionary.GetCellProperty(index, "Key").stringValue = "a";
                        dictionary.GetCellProperty(index, "Value").intValue = 7;
                    }
                );
                Assert.AreEqual(7, collectionTarget.Dictionary["a"]);

                var queue = new XQueueEditorModel(so.FindProperty("Queue"));
                queue.Add(index => queue.GetCellProperty(index, "Value").intValue = 11);
                Assert.AreEqual(11, collectionTarget.Queue.Peek());

                var stack = new XStackEditorModel(so.FindProperty("Stack"));
                stack.Add(index => stack.GetCellProperty(index, "Value").intValue = 13);
                Assert.AreEqual(13, collectionTarget.Stack.Peek());

                set.RemoveAt(0);
                dictionary.RemoveAt(0);
                queue.RemoveAt(0);
                stack.RemoveAt(0);

                Assert.AreEqual(0, collectionTarget.Set.Count);
                Assert.AreEqual(0, collectionTarget.Dictionary.Count);
                Assert.AreEqual(0, collectionTarget.Queue.Count);
                Assert.AreEqual(0, collectionTarget.Stack.Count);
            }
            finally
            {
                Undo.ClearUndo(collectionTarget);
                UnityEngine.Object.DestroyImmediate(collectionTarget);
            }
        }

        [Test]
        public void TEST_CollectionEditor_SerializedApply후_Runtime동기화()
        {
            var collectionTarget = ScriptableObject.CreateInstance<CollectionTarget>();
            collectionTarget.Set.Add(1);
            collectionTarget.Set.ApplyRuntime();
            collectionTarget.Dictionary.Add("a", 1);
            collectionTarget.Dictionary.ApplyRuntime();
            collectionTarget.Queue.Enqueue(1);
            collectionTarget.Queue.ApplyRuntime();
            collectionTarget.Stack.Push(1);
            collectionTarget.Stack.ApplyRuntime();

            using var so = new SerializedObject(collectionTarget);
            try
            {
                var setModel = new XHashSetEditorModel(so.FindProperty("Set"));
                var setValue = setModel.GetCellProperty(0, "Value");
                setValue.intValue = 2;
                so.ApplyModifiedProperties();
                Assert.IsTrue(setModel.CommitPropertyChange(setValue));
                Assert.IsTrue(collectionTarget.Set.Contains(2));
                Assert.IsFalse(collectionTarget.Set.Contains(1));

                var dictionaryModel = new XDictionaryEditorModel(so.FindProperty("Dictionary"));
                var dictionaryValue = dictionaryModel.GetCellProperty(0, "Value");
                dictionaryValue.intValue = 3;
                so.ApplyModifiedProperties();
                Assert.IsTrue(dictionaryModel.CommitPropertyChange(dictionaryValue));
                Assert.AreEqual(3, collectionTarget.Dictionary["a"]);

                var queueModel = new XQueueEditorModel(so.FindProperty("Queue"));
                var queueValue = queueModel.GetCellProperty(0, "Value");
                queueValue.intValue = 4;
                so.ApplyModifiedProperties();
                Assert.IsTrue(queueModel.CommitPropertyChange(queueValue));
                Assert.AreEqual(4, collectionTarget.Queue.Peek());

                var stackModel = new XStackEditorModel(so.FindProperty("Stack"));
                var stackValue = stackModel.GetCellProperty(0, "Value");
                stackValue.intValue = 5;
                so.ApplyModifiedProperties();
                Assert.IsTrue(stackModel.CommitPropertyChange(stackValue));
                Assert.AreEqual(5, collectionTarget.Stack.Peek());
            }
            finally
            {
                Undo.ClearUndo(collectionTarget);
                UnityEngine.Object.DestroyImmediate(collectionTarget);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> XOrdered의 행 이동은 동일 Order 항목의 상대 순서를 변경한다.
        /// <br/> 서로 다른 Order를 가로지른 이동도 오름차순으로 정규화한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_CollectionEditor_XOrdered_행이동후_동일Order순서와_오름차순유지()
        {
            var orderedTarget = ScriptableObject.CreateInstance<OrderedTarget>();
            orderedTarget.Ordered.Add(1, "a", new NumericIModifier(NumericIOperation.ADD, 1));
            orderedTarget.Ordered.Add(1, "b", new NumericIModifier(NumericIOperation.ADD, 2));
            orderedTarget.Ordered.Add(2, "c", new NumericIModifier(NumericIOperation.ADD, 3));
            orderedTarget.Ordered.Add(2, "d", new NumericIModifier(NumericIOperation.ADD, 4));

            using var so = new SerializedObject(orderedTarget);
            try
            {
                var orderedModel = new XOrderedEditorModel(so.FindProperty("Ordered"));
                Assert.IsTrue(orderedModel.CanReorder);

                Assert.AreEqual(1, orderedModel.Move(0, 1));
                Assert.AreEqual("b", orderedTarget.Ordered[0].Key);
                Assert.AreEqual("a", orderedTarget.Ordered[1].Key);
                Assert.AreEqual(1, orderedTarget.Ordered[0].Order);
                Assert.AreEqual(1, orderedTarget.Ordered[1].Order);

                // 다른 Order 구간으로 이동해도 Order 정렬이 우선한다.
                // 같은 Order의 두 번째 그룹에서는 드래그 순서가 유지된다.
                Assert.AreEqual(2, orderedModel.Move(3, 1));
                Assert.AreEqual("b", orderedTarget.Ordered[0].Key);
                Assert.AreEqual("a", orderedTarget.Ordered[1].Key);
                Assert.AreEqual("d", orderedTarget.Ordered[2].Key);
                Assert.AreEqual("c", orderedTarget.Ordered[3].Key);
                Assert.AreEqual(2, orderedTarget.Ordered[2].Order);
                Assert.AreEqual(2, orderedTarget.Ordered[3].Order);
            }
            finally
            {
                Undo.ClearUndo(orderedTarget);
                UnityEngine.Object.DestroyImmediate(orderedTarget);
            }
        }

    #endregion

    #region X-2: 공유 변경 세대

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 같은 컬렉션의 여러 Model이 동일한 세대를 관찰한다.
        /// <br/> 무관한 컬렉션은 유지하고 Undo 시 이전 세대가 무효화된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionEditor_공유Revision은_Commit과Undo에반응하고_무관한컬렉션은유지()
        {
            var collectionTarget = ScriptableObject.CreateInstance<CollectionTarget>();
            using var firstObject = new SerializedObject(collectionTarget);
            using var secondObject = new SerializedObject(collectionTarget);
            try
            {
                var first = new XHashSetEditorModel(firstObject.FindProperty("Set"));
                var second = new XHashSetEditorModel(secondObject.FindProperty("Set"));
                var unrelated = new XDictionaryEditorModel(secondObject.FindProperty("Dictionary"));
                var before = first.Revision;
                var unrelatedBefore = unrelated.Revision;

                Undo.IncrementCurrentGroup();
                var group = Undo.GetCurrentGroup();
                first.Add(index => first.GetCellProperty(index, "Value").intValue = 5);
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(group);

                var applied = first.Revision;
                Assert.AreNotEqual(before, applied);
                Assert.AreEqual(applied, second.Revision);
                Assert.AreEqual(unrelatedBefore, unrelated.Revision);

                Undo.PerformUndo();
                first.RestoreSerializedState();
                second.RestoreSerializedState();
                Assert.AreNotEqual(applied, second.Revision);
                Assert.AreEqual(0, collectionTarget.Set.Count);
            }
            finally
            {
                Undo.ClearUndo(collectionTarget);
                UnityEngine.Object.DestroyImmediate(collectionTarget);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 무관한 직렬화 변경은 컬렉션 세대를 유지한다.
        /// <br/> 다른 Model이 처리한 변경은 중복 증가하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionEditor_외부변경은_해당컬렉션세대만_한번갱신()
        {
            var collectionTarget = ScriptableObject.CreateInstance<CollectionTarget>();
            using var firstObject = new SerializedObject(collectionTarget);
            using var secondObject = new SerializedObject(collectionTarget);
            try
            {
                var first = new XHashSetEditorModel(firstObject.FindProperty("Set"));
                var second = new XHashSetEditorModel(secondObject.FindProperty("Set"));
                var unrelated = new XDictionaryEditorModel(firstObject.FindProperty("Dictionary"));
                var initial = first.Revision;

                Assert.IsFalse(first.ObserveExternalChange());
                Assert.IsFalse(second.ObserveExternalChange());

                unrelated.Add
                (
                    index =>
                    {
                        unrelated.GetCellProperty(index, "Key").stringValue = "other";
                        unrelated.GetCellProperty(index, "Value").intValue = 3;
                    }
                );

                Assert.IsFalse(first.ObserveExternalChange());
                Assert.AreEqual(initial, first.Revision);

                first.Add(index => first.GetCellProperty(index, "Value").intValue = 5);
                var committed = first.Revision;
                Assert.AreNotEqual(initial, committed);
                Assert.IsTrue(second.ObserveExternalChange());
                Assert.AreEqual(committed, second.Revision);
                Assert.IsFalse(second.ObserveExternalChange());
                var stale = new XHashSetEditorModel(secondObject.FindProperty("Set"));

                // Model 파이프라인 외부에서 확정된 SerializedObject 수정도 판정한다.
                first.GetCellProperty(0, "Value").intValue = 7;
                firstObject.ApplyModifiedProperties();

                Assert.IsTrue(second.ObserveExternalChange());
                var externallyChanged = second.Revision;
                Assert.AreNotEqual(committed, externallyChanged);
                Assert.IsTrue(first.ObserveExternalChange());
                Assert.AreEqual(externallyChanged, first.Revision);
                Assert.IsFalse(first.ObserveExternalChange());

                // 오래된 Model이 뒤늦게 관찰한 두 번째 변경도 새 세대로 구분한다.
                first.GetCellProperty(0, "Value").intValue = 9;
                firstObject.ApplyModifiedProperties();
                Assert.IsTrue(stale.ObserveExternalChange());
                var latest = stale.Revision;
                Assert.AreNotEqual(externallyChanged, latest);
                Assert.IsTrue(second.ObserveExternalChange());
                Assert.AreEqual(latest, second.Revision);
                Assert.IsFalse(stale.ObserveExternalChange());

                // 화면 새로고침이 바인딩 알림보다 먼저 실행된 경우도 세대를 갱신한다.
                first.GetCellProperty(0, "Value").intValue = 11;
                firstObject.ApplyModifiedProperties();
                second.Synchronize();
                var synchronized = second.Revision;
                Assert.AreNotEqual(latest, synchronized);
                Assert.IsTrue(first.ObserveExternalChange());
                Assert.AreEqual(synchronized, first.Revision);
                Assert.IsFalse(second.ObserveExternalChange());
            }
            finally
            {
                Undo.ClearUndo(collectionTarget);
                UnityEngine.Object.DestroyImmediate(collectionTarget);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 상위·하위 경로는 같은 변경 세대를 공유한다.
        /// <br/> 유사 이름이나 독립 경로는 영향을 받지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionEditor_경로관계는_구분자경계를_유지()
        {
            Assert.IsTrue(SerializedPropertyHelper.IsRelatedPath("Set", "Set.Array.data[0]"));
            Assert.IsTrue(SerializedPropertyHelper.IsRelatedPath("Set.Array.data[0]", "Set"));
            Assert.IsFalse(SerializedPropertyHelper.IsRelatedPath("Set", "SetOther"));
            Assert.IsFalse(SerializedPropertyHelper.IsRelatedPath("Set", "Dictionary"));
            Assert.IsFalse(SerializedPropertyHelper.IsRelatedPath(null, "Set"));
            Assert.IsTrue(SerializedPropertyHelper.IsSameOrDescendantPath("Set", "Set"));
            Assert.IsTrue(SerializedPropertyHelper.IsSameOrDescendantPath("Set.Array.data[0]", "Set"));
            Assert.IsFalse(SerializedPropertyHelper.IsSameOrDescendantPath("Set", "Set.Array.data[0]"));

            var target = ScriptableObject.CreateInstance<CollectionTarget>();
            try
            {
                var before = RevisionTracker.Get(target, "Dictionary");
                RevisionTracker.Get(target, "Set");
                RevisionTracker.Get(target, "Set.Array.data[0]");
                RevisionTracker.Get(target, "SetOther");

                using var targetObject = new SerializedObject(target);
                var initial = RevisionTracker.Get(target, "Set");
                Assert.IsFalse(RevisionTracker.Observe(targetObject.FindProperty("Set")));
                Assert.AreEqual(initial, RevisionTracker.Get(target, "Set"));

                RevisionTracker.Advance(targetObject, "Set");
                var updated = RevisionTracker.Get(target, "Set");
                Assert.AreEqual(updated, RevisionTracker.Get(target, "Set.Array.data[0]"));
                Assert.AreEqual(before, RevisionTracker.Get(target, "Dictionary"));
                Assert.AreNotEqual(updated, RevisionTracker.Get(target, "SetOther"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

    #endregion

    #region X-3: SerializedProperty 값 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 확정 전의 직렬화 값이 원본 객체의 이전 값보다 우선한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionEditor_미적용직렬화값은_행Identity에반영()
        {
            var property = serializedObject.FindProperty("Numbers")
                .GetArrayElementAtIndex(0);
            property.intValue = 17;

            var listModel = new SerializedListEditorModel
            (
                serializedObject.FindProperty("Numbers"),
                unique: false,
                elementType: typeof(int),
                isManagedReference: false
            );

            Assert.AreEqual(1, target.Numbers[0]);
            Assert.AreEqual(17, SerializedPropertyHelper.GetValue(property));
            Assert.AreEqual(17, listModel.GetRowIdentity(0));

            var reference = model.GetCellProperty(0, "value");
            Assert.AreSame
            (
                reference.managedReferenceValue,
                SerializedPropertyHelper.GetValue(reference)
            );
        }

    #endregion

    #region V-1: Modifier 검증

        // ----------------------------------------------------------------------
        /// <summary>
        /// 숫자 Modifier의 DIV 0은 Value에 경고하지만 행 정리 대상으로 표시하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_CollectionEditor_DIV0은_Value경고만표시()
        {
            var value = model.GetCellProperty(0, "value");
            value.FindPropertyRelative("operation").enumValueIndex = (int)NumericIOperation.DIV;
            value.FindPropertyRelative("value").intValue = 0;
            serializedObject.ApplyModifiedProperties();

            var invalid = model.Validate(CollectionValidationContext.ForRow(0));
            Assert.IsTrue(invalid.CanApply);
            Assert.IsFalse(invalid.IsRowInvalid);
            Assert.IsTrue
            (
                invalid.Diagnostics.Any
                (
                    diagnostic => diagnostic.ColumnID == "value"
                        && diagnostic.Severity == DiagnosticSeverity.Warning
                        && diagnostic.Code == "modifier.numeric.divide-by-zero"
                )
            );

            value = model.GetCellProperty(0, "value");
            value.FindPropertyRelative("value").intValue = 2;
            serializedObject.ApplyModifiedProperties();

            var valid = model.Validate(CollectionValidationContext.ForRow(0));
            Assert.IsFalse
            (
                valid.Diagnostics.Any
                (
                    diagnostic => diagnostic.Code == "modifier.numeric.divide-by-zero"
                )
            );
        }

    #endregion

    }
}
