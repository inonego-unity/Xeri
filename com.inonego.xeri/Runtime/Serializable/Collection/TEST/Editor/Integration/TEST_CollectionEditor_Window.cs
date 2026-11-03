/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_CollectionEditor_Window.cs
수정일 : 2026-11-03

# 설명
키와 정렬 순서의 구조 변경, 임시 버퍼 적용과 상세 편집창의 원본 갱신을 검증한다.

# 테스트 구성
 W: 구조 변경·상세 편집창·지연 명령 통합
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
    public partial class TEST_CollectionEditor
    {

    #region W-1: 구조 변경과 상세 편집창

        [Test]
        public void TEST_CollectionEditor_XOrdered_Key_단일Commit으로_변경통지()
        {
            var orderedTarget = ScriptableObject.CreateInstance<OrderedTarget>();
            orderedTarget.Ordered.Add
            (
                0, "old", new NumericIModifier(NumericIOperation.ADD, 5)
            );
            using var so = new SerializedObject(orderedTarget);
            try
            {
                var orderedModel = new XOrderedEditorModel(so.FindProperty("Ordered"));
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
                CollectionEditorModelBase.Committed += changeHandler;

                orderedModel.ReplaceStructuralValue(0, "key", "new");

                Assert.AreEqual("new", orderedModel.GetCellProperty(0, "key").stringValue);
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

        [Test]
        public void TEST_CollectionEditor_XOrdered_Order변경은_재정렬하고_한번만통지()
        {
            var orderedTarget = ScriptableObject.CreateInstance<OrderedTarget>();
            orderedTarget.Ordered.Add
            (
                0, "first", new NumericIModifier(NumericIOperation.ADD, 5)
            );
            orderedTarget.Ordered.Add
            (
                10, "second", new NumericIModifier(NumericIOperation.ADD, 9)
            );
            using var so = new SerializedObject(orderedTarget);
            try
            {
                var orderedModel = new XOrderedEditorModel(so.FindProperty("Ordered"));
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
                CollectionEditorModelBase.Committed += changeHandler;

                var movedIndex = orderedModel.ReplaceStructuralValue(1, "order", -1);

                Assert.AreEqual(0, movedIndex);
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

        [UnityTest]
        public IEnumerator TEST_CollectionEditor_Key검증은_임시버퍼를Apply하지않음()
        {
            var orderedTarget = ScriptableObject.CreateInstance<OrderedTarget>();
            orderedTarget.Ordered.Add
            (
                0, "old", new NumericIModifier(NumericIOperation.ADD, 5)
            );
            using var so = new SerializedObject(orderedTarget);
            CollectionStructuralEditorWindow editor = null;
            try
            {
                var orderedModel = new XOrderedEditorModel(so.FindProperty("Ordered"));
                CollectionStructuralEditorWindow.Open(orderedModel, 0, "key");
                editor = CollectionEditWindowCoordinator.Find<CollectionStructuralEditorWindow>();
                Assert.IsNotNull(editor);
                yield return WaitForBinding
                (
                    () => editor.rootVisualElement.Q<TextField>() != null
                );

                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var buffer = (SerializedObject)typeof(CollectionStructuralEditorWindow)
                    .GetField("editBufferObject", flags).GetValue(editor);
                var validate = typeof(CollectionStructuralEditorWindow)
                    .GetMethod("RefreshValidation", flags);
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

        [UnityTest]
        public IEnumerator TEST_CollectionEditor_XOrdered_Value팝업_원본과표_함께갱신()
        {
            var orderedTarget = ScriptableObject.CreateInstance<OrderedTarget>();
            orderedTarget.Ordered.Add
            (
                0, "key", new NumericIModifier(NumericIOperation.ADD, 5)
            );
            using var so = new SerializedObject(orderedTarget);
            CollectionValueEditorWindow editor = null;
            try
            {
                var orderedModel = new XOrderedEditorModel(so.FindProperty("Ordered"));
                var view = new CollectionEditorView(orderedModel);
                Show(view);
                yield return WaitForBinding
                (
                    () => view.Q<Label>(className: "xeri-collection__data-label") != null
                );
                CollectionValueEditorWindow.Open
                (
                    orderedModel, orderedModel.GetCellProperty(0, "value")
                );
                editor = CollectionEditWindowCoordinator.Find<CollectionValueEditorWindow>();
                Assert.IsNotNull(editor);
                yield return WaitForBinding
                (
                    () => editor.rootVisualElement.Q<IntegerField>() != null
                );
                editor.rootVisualElement.Q<IntegerField>().value = 9;
                yield return null;
                yield return null;
                yield return null;

                so.UpdateIfRequiredOrScript();
                var property = orderedModel.GetCellProperty(0, "value");
                Assert.AreEqual(9, property.FindPropertyRelative("value").intValue);
                var updated = false;
                var previews = new List<string>();
                view.Query<Label>(className: "xeri-collection__data-label").ForEach
                (
                    label =>
                    {
                        previews.Add(label.text);
                        updated |= label.text?.Contains("9") == true;
                    }
                );
                Assert.IsTrue
                (
                    updated,
                    "Value 수정 후 Collection 목록 요약이 갱신되지 않았습니다."
                        + " ExpectedSummary=" + CollectionCellSummary.GetText(property)
                        + " ViewLabels=" + string.Join(" | ", previews)
                );
            }
            finally
            {
                editor?.Close();
                Undo.ClearUndo(orderedTarget);
                UnityEngine.Object.DestroyImmediate(orderedTarget);
            }
        }

        [UnityTest]
        public IEnumerator TEST_CollectionEditor_MValue_상세값변경후_목록미리보기갱신()
        {
            var view = new CollectionEditorView(model);
            Show(view);
            yield return WaitForBinding
            (
                () => view.Q<Label>(className: "xeri-collection__data-label") != null
            );

            CollectionValueEditorWindow editor = null;
            try
            {
                CollectionValueEditorWindow.Open
                (
                    model, model.GetCellProperty(0, "value")
                );
                editor = CollectionEditWindowCoordinator.Find<CollectionValueEditorWindow>();
                Assert.IsNotNull(editor);
                yield return WaitForBinding
                (
                    () => editor.rootVisualElement.Q<IntegerField>() != null
                );

                editor.rootVisualElement.Q<IntegerField>().value = 12;
                yield return null;
                yield return null;

                serializedObject.UpdateIfRequiredOrScript();
                Assert.AreEqual(12, GetValueProperty().intValue);
                var updated = false;
                var previews = new List<string>();
                view.Query<Label>(className: "xeri-collection__data-label").ForEach
                (
                    label =>
                    {
                        previews.Add(label.text);
                        updated |= label.text?.Contains("12") == true;
                    }
                );
                Assert.IsTrue
                (
                    updated,
                    "MValue 상세 편집 후 표 미리보기가 갱신되지 않았습니다."
                        + " ExpectedSummary=" + CollectionCellSummary.GetText(model.GetCellProperty(0, "value"))
                        + " ViewLabels=" + string.Join(" | ", previews)
                );
            }
            finally
            {
                editor?.Close();
            }
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
            var view = new CollectionEditorView(model);
            Show(view);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var isCurrent = typeof(CollectionEditorView).GetMethod("IsCurrentPageRow", flags);
            var execute = typeof(CollectionEditorView).GetMethod("ExecuteContextRowAction", flags);
            Assert.IsNotNull(isCurrent);
            Assert.IsNotNull(execute);

            yield return WaitForBinding
            (
                () => (bool)isCurrent.Invoke
                (
                    view,
                    new object[]
                    {
                        0, 0,
                    }
                )
            );

            var originalRevision = model.Revision;
            var rowLocator = new SerializedPropertyLocator(model.GetRowProperty(0));
            GetValueProperty().intValue = 7;
            serializedObject.ApplyModifiedProperties();
            target.Value.OnAfterDeserialize();

            var executed = false;
            execute.Invoke
            (
                view,
                new object[]
                {
                    0, 0, originalRevision, rowLocator,
                    (Action)(() => executed = true),
                }
            );

            Assert.IsFalse(executed);
            Assert.AreNotEqual(originalRevision, model.Revision);
        }

    #endregion

    }
}
