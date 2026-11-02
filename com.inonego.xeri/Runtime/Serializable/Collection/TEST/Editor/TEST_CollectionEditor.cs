/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_CollectionEditor.cs
수정일 : 2026-10-31

# 설명
변경 확정, 외부 상태 동기화와 Undo/Redo 계약을 검증한다.
공통 SerializedObject 및 EditorWindow 준비와 정리는 이 픽스처에 둔다.

# 테스트 구성
 C: 변경 확정과 중복 통지
 U: 외부 상태와 Undo/Redo
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    #region 픽스처

        private sealed class Target : ScriptableObject
        {
            public MValue<int> Value = new(100);
            public List<int> Numbers = new();
        }

        private sealed class TestWindow : EditorWindow
        {
            // NONE
        }

        private sealed class OrderedTarget : ScriptableObject
        {
            public XOrdered<int, string, NumericIModifier> Ordered = new();
        }

        private sealed class CollectionTarget : ScriptableObject
        {
            public XHashSet_V<int> Set = new();
            public XHashSet_R<NumericIModifier> ReferenceSet = new();
            public XDictionary_VV<string, int> Dictionary = new();
            public XQueue_V<int> Queue = new();
            public XStack_V<int> Stack = new();
        }

        private Target target;
        private SerializedObject serializedObject;
        private MValueModifierEditorModel<int> model;
        private EditorWindow window;
        private Action<UnityEngine.Object, string, string> changeHandler;

        [SetUp]
        public void SetUp()
        {
            target = ScriptableObject.CreateInstance<Target>();
            target.Numbers.Add(1);
            target.Numbers.Add(2);
            target.Value.AddModifier("a", new NumericIModifier(NumericIOperation.ADD, 5));
            serializedObject = new SerializedObject(target);
            model = new MValueModifierEditorModel<int>(serializedObject.FindProperty("Value.modifiers"));
        }

        [TearDown]
        public void TearDown()
        {
            if (changeHandler != null)
            {
                CollectionEditorModelBase.Committed -= changeHandler;
            }

            if (window != null)
            {
                window.rootVisualElement.Unbind();
                window.Close();
            }

            Undo.ClearUndo(target);
            serializedObject.Dispose();
            UnityEngine.Object.DestroyImmediate(target);
        }

        private SerializedProperty GetValueProperty()
        {
            return model.GetCellProperty(0, "value").FindPropertyRelative("value");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Collection 행을 펼친 상태로 테스트 창에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Show(VisualElement element)
        {
            window = ScriptableObject.CreateInstance<TestWindow>();
            window.titleContent = new GUIContent("Collection Editor Tests");
            window.position = new Rect(100, 100, 600, 400);
            window.rootVisualElement.Add(element);

            // SessionState에 남은 접힘 상태가 행 바인딩 테스트에 영향을 주지 않도록 한다.
            if (element is CollectionEditorView view)
            {
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var expand = typeof(CollectionEditorView).GetMethod("SetExpanded", flags);
                Assert.IsNotNull(expand);

                var arguments = new object[]
                {
                    true
                };
                expand.Invoke(view, arguments);
            }

            window.ShowUtility();
        }

        private static IEnumerator WaitForBinding(Func<bool> ready)
        {
            var deadline = EditorApplication.timeSinceStartup + 5;
            while (!ready() && EditorApplication.timeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(ready(), "패널에 실제 바인딩된 편집 필드를 찾지 못했습니다.");
        }

    #endregion

    #region C-1: 변경 확정과 중복 통지

        [Test]
        public void TEST_CollectionEditor_동일값_바인딩통지는_확정하지않음()
        {
            var notices = 0;
            changeHandler = (_, _, _) => notices++;
            CollectionEditorModelBase.Committed += changeHandler;

            Assert.IsFalse(model.CommitPropertyChange(GetValueProperty()));
            Assert.AreEqual(0, notices);
            Assert.AreEqual(105, target.Value.Modified);
        }

        [Test]
        public void TEST_CollectionEditor_행추가도_owner갱신후_구조변경으로통지()
        {
            var notices = 0;
            var modifiedAtNotice = 0;
            string changedPath = "unset";
            changeHandler = (_, _, path) =>
            {
                notices++;
                modifiedAtNotice = target.Value.Modified;
                changedPath = path;
            };
            CollectionEditorModelBase.Committed += changeHandler;

            model.Add
            (
                index =>
                {
                    model.GetCellProperty(index, "key").stringValue = "b";
                    model.GetCellProperty(index, "value").managedReferenceValue =
                        new NumericIModifier(NumericIOperation.ADD, 3);
                }
            );

            Assert.AreEqual(1, notices);
            Assert.IsNull(changedPath);
            Assert.AreEqual(108, modifiedAtNotice);
        }

    #endregion

    #region U-1: 외부 상태와 실행 취소

        [Test]
        public void TEST_CollectionEditor_외부동기화는_새편집으로확정하지않음()
        {
            var notices = 0;
            changeHandler = (_, _, _) => notices++;
            CollectionEditorModelBase.Committed += changeHandler;
            var property = GetValueProperty();
            property.intValue = 9;
            serializedObject.ApplyModifiedProperties();
            target.Value.OnAfterDeserialize();

            model.Synchronize();

            Assert.IsFalse(model.CommitPropertyChange(GetValueProperty()));
            Assert.AreEqual(0, notices);
            Assert.AreEqual(109, target.Value.Modified);
        }

        [Test]
        public void TEST_CollectionEditor_값편집_UndoRedo로_데이터와Modified복원()
        {
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            var property = GetValueProperty();
            property.intValue = 7;
            serializedObject.ApplyModifiedProperties();
            model.CommitPropertyChange(property);
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);

            Undo.PerformUndo();
            model.RestoreSerializedState();
            Assert.AreEqual(5, GetValueProperty().intValue);
            Assert.AreEqual(105, target.Value.Modified);

            Undo.PerformRedo();
            model.RestoreSerializedState();
            Assert.AreEqual(7, GetValueProperty().intValue);
            Assert.AreEqual(107, target.Value.Modified);
        }

    #endregion

    }
}
