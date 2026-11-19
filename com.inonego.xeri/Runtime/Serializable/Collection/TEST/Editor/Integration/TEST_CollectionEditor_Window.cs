/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_CollectionEditor_Window.cs
수정일 : 2026-11-19

# 설명
키와 정렬 순서의 초안 확정, 초안 창의 임시 버퍼와 값 상세 창의 원본·표 갱신을 검증한다.

# 테스트 구성
 W: 초안 확정·상세 편집창·지연 명령 통합
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;
using inonego.Xeri.Serializable.Editor;

namespace inonego.Xeri.TEST.Serializable._Collection
{
    // ============================================================
    /// <summary>
    /// 초안 확정·상세 편집창·지연 명령 통합 회귀 테스트.
    /// </summary>
    // ============================================================
    public partial class TEST_CollectionEditor
    {

    #region W-1: 초안 확정과 상세 편집창

        // ------------------------------------------------------------
        /// <summary>
        /// XOrdered 키 Commit이 변경을 한 번만 통지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionEditor_XOrdered_Key_단일Commit으로_변경통지()
        {
            var orderedTarget = ScriptableObject.CreateInstance<OrderedTarget>();
            orderedTarget.Ordered.Add(0, "old", new NumericIModifier(NumericIOperation.ADD, 5));
            using var so = new SerializedObject(orderedTarget);
            try
            {
                var ordered = Open(so, "Ordered", property => XOrderedKindProvider.Create(property));
                var notices = 0;
                string notifiedPath = "unset";
                changeHandler = (changed, _, path) =>
                {
                    if (changed == orderedTarget)
                    {
                        notices++;
                        notifiedPath = path;
                    }
                };
                CollectionSession.Committed += changeHandler;

                Assert.IsTrue(ordered.Apply(CollectionEdit.CommitDraft(CollectionDraft.ForRow(0).Set("key", "new"))).Succeeded);

                Assert.AreEqual("new", GetCell(ordered, 0, "key").stringValue);
                Assert.IsTrue(orderedTarget.Ordered.ContainsKey("new"));
                Assert.AreEqual(1, notices);
                Assert.IsNull(notifiedPath);
            }
            finally
            {
                Undo.ClearUndo(orderedTarget);
                UnityEngine.Object.DestroyImmediate(orderedTarget);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Order 변경은 재정렬하고 한 번만 통지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionEditor_XOrdered_Order변경은_재정렬하고_한번만통지()
        {
            var orderedTarget = ScriptableObject.CreateInstance<OrderedTarget>();
            orderedTarget.Ordered.Add(0, "first", new NumericIModifier(NumericIOperation.ADD, 5));
            orderedTarget.Ordered.Add(10, "second", new NumericIModifier(NumericIOperation.ADD, 9));
            using var so = new SerializedObject(orderedTarget);
            try
            {
                var ordered = Open(so, "Ordered", property => XOrderedKindProvider.Create(property));
                var notices = 0;
                string lastPath = "unset";
                changeHandler = (targetObject, _, path) =>
                {
                    if (targetObject == orderedTarget)
                    {
                        notices++;
                        lastPath = path;
                    }
                };
                CollectionSession.Committed += changeHandler;

                var moved = ordered.Apply(CollectionEdit.CommitDraft(CollectionDraft.ForRow(1).Set("order", -1)));

                Assert.AreEqual(0, moved.RowIndex);
                Assert.AreEqual("second", orderedTarget.Ordered[0].Key);
                Assert.AreEqual(-1, orderedTarget.Ordered[0].Order);
                Assert.AreEqual("first", orderedTarget.Ordered[1].Key);
                Assert.AreEqual(1, notices);
                Assert.IsNull(lastPath);
            }
            finally
            {
                Undo.ClearUndo(orderedTarget);
                UnityEngine.Object.DestroyImmediate(orderedTarget);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키 검증은 임시 버퍼를 Apply하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_CollectionEditor_Key검증은_임시버퍼를Apply하지않음()
        {
            var orderedTarget = ScriptableObject.CreateInstance<OrderedTarget>();
            orderedTarget.Ordered.Add(0, "old", new NumericIModifier(NumericIOperation.ADD, 5));
            using var so = new SerializedObject(orderedTarget);
            CollectionDraftWindow editor = null;
            try
            {
                var ordered = Open(so, "Ordered", property => XOrderedKindProvider.Create(property));
                Assert.IsTrue(CollectionDraftWindow.OpenForRow(ordered, 0, ordered.Kind.FindColumn("key")));
                editor = CollectionEditWindowCoordinator.Find<CollectionDraftWindow>();
                Assert.IsNotNull(editor);
                yield return WaitForBinding(() => editor.rootVisualElement.Q<TextField>() != null);

                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var buffer = (SerializedObject)typeof(CollectionDraftWindow).GetField("editBufferObject", flags).GetValue(editor);
                var validate = typeof(CollectionDraftWindow).GetMethod("RefreshValidation", flags);
                Assert.IsNotNull(validate);
                buffer.FindProperty("Text").stringValue = "new";

                for (var index = 0; index < 10; index++)
                {
                    validate.Invoke(editor, null);
                    Assert.IsTrue(buffer.hasModifiedProperties);
                }

                Assert.IsTrue(orderedTarget.Ordered.ContainsKey("old"));
                Assert.IsFalse(orderedTarget.Ordered.ContainsKey("new"));
            }
            finally
            {
                editor?.Close();
                Undo.ClearUndo(orderedTarget);
                UnityEngine.Object.DestroyImmediate(orderedTarget);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// XOrdered 값 팝업 편집이 원본과 표를 함께 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_CollectionEditor_XOrdered_Value팝업_원본과표_함께갱신()
        {
            var orderedTarget = ScriptableObject.CreateInstance<OrderedTarget>();
            orderedTarget.Ordered.Add(0, "key", new NumericIModifier(NumericIOperation.ADD, 5));
            using var so = new SerializedObject(orderedTarget);
            CollectionValueEditorWindow editor = null;
            try
            {
                var ordered = Open(so, "Ordered", property => XOrderedKindProvider.Create(property));
                var table = new CollectionTable(ordered);
                Show(table);
                yield return WaitForBinding(() => table.Q<Label>(className: "xeri-collection__data-label") != null);
                CollectionValueEditorWindow.Open(ordered, GetCell(ordered, 0, "value"));
                editor = CollectionEditWindowCoordinator.Find<CollectionValueEditorWindow>();
                Assert.IsNotNull(editor);
                yield return WaitForBinding(() => editor.rootVisualElement.Q<IntegerField>() != null);
                editor.rootVisualElement.Q<IntegerField>().value = 9;
                yield return null;
                yield return null;
                yield return null;

                so.UpdateIfRequiredOrScript();
                var property = GetCell(ordered, 0, "value");
                Assert.AreEqual(9, property.FindPropertyRelative("value").intValue);
                AssertPreview(table, ordered, property, "9", "Value 수정 후 컬렉션 목록 요약이 갱신되지 않았습니다.");
            }
            finally
            {
                editor?.Close();
                Undo.ClearUndo(orderedTarget);
                UnityEngine.Object.DestroyImmediate(orderedTarget);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// MValue 상세 값 변경 후 목록 미리보기가 갱신된다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_CollectionEditor_MValue_상세값변경후_목록미리보기갱신()
        {
            var table = new CollectionTable(session);
            Show(table);
            yield return WaitForBinding(() => table.Q<Label>(className: "xeri-collection__data-label") != null);

            CollectionValueEditorWindow editor = null;
            try
            {
                CollectionValueEditorWindow.Open(session, GetCell(session, 0, "value"));
                editor = CollectionEditWindowCoordinator.Find<CollectionValueEditorWindow>();
                Assert.IsNotNull(editor);
                yield return WaitForBinding(() => editor.rootVisualElement.Q<IntegerField>() != null);

                editor.rootVisualElement.Q<IntegerField>().value = 12;
                yield return null;
                yield return null;

                serializedObject.UpdateIfRequiredOrScript();
                Assert.AreEqual(12, GetValueProperty().intValue);
                AssertPreview(table, session, GetCell(session, 0, "value"), "12", "MValue 상세 편집 후 표 미리보기가 갱신되지 않았습니다.");
            }
            finally
            {
                editor?.Close();
            }
        }

        private static void AssertPreview
        (
            CollectionTable table, CollectionSession owner, SerializedProperty property, string expected, string message
        )
        {
            var updated = false;
            var previews = new List<string>();
            table.Query<Label>(className: "xeri-collection__data-label").ForEach
            (
                label =>
                {
                    previews.Add(label.text);
                    updated |= label.text?.Contains(expected) == true;
                }
            );

            var valueColumn = owner.Kind.FindColumn("value");
            var summary = CollectionCellDrawers.Resolve(valueColumn).GetSummary(property, valueColumn);
            Assert.IsTrue(updated, message + " ExpectedSummary=" + summary + " ViewLabels=" + string.Join(" | ", previews));
        }

    #endregion

    #region W-2: 지연된 행 명령의 유효성

        // ------------------------------------------------------------
        /// <summary>
        /// UI 관찰 이전의 외부 변경도 지연된 행 명령 실행 시 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_CollectionEditor_외부변경후_지연행명령은_실행하지않음()
        {
            var table = new CollectionTable(session);
            Show(table);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var isCurrent = typeof(CollectionTable).GetMethod("IsCurrentPageRow", flags);
            var execute = typeof(CollectionTable).GetMethod("ExecuteContextRowAction", flags);
            Assert.IsNotNull(isCurrent);
            Assert.IsNotNull(execute);

            var rowArguments = new object[]
            {
                0, 0
            };
            yield return WaitForBinding(() => (bool)isCurrent.Invoke(table, rowArguments));

            var originalRevision = session.Revision;
            var rowLocator = new SerializedPropertyLocator(session.Source.GetRow(0));
            GetValueProperty().intValue = 7;
            serializedObject.ApplyModifiedProperties();
            target.Value.OnAfterDeserialize();

            var executed = false;
            execute.Invoke
            (
                table,
                new object[]
                {
                    0, 0, originalRevision, rowLocator,
                    (Action)(() => executed = true),
                }
            );

            Assert.IsFalse(executed);
            Assert.AreNotEqual(originalRevision, session.Revision);
        }

    #endregion

    }
}
