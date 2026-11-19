/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_CollectionEditor.cs
수정일 : 2026-11-19

# 설명
편집 세션의 변경 확정, 외부 상태 동기화와 Undo/Redo 계약을 검증한다.
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
    // ============================================================
    /// <summary>
    /// 편집 세션 확정·외부 동기화·Undo/Redo 회귀 테스트.
    /// </summary>
    // ============================================================
    public partial class TEST_CollectionEditor
    {

    #region 픽스처

        private sealed class Target : ScriptableObject
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 수정자를 가진 MValue 직렬화 필드.
            /// </summary>
            // ------------------------------------------------------------
            public MValue<int> Value = new(100);

            // ------------------------------------------------------------
            /// <summary>
            /// 정수 목록 직렬화 필드.
            /// </summary>
            // ------------------------------------------------------------
            public List<int> Numbers = new();
        }

        private sealed class TestWindow : EditorWindow
        {
            // NONE
        }

        private sealed class OrderedTarget : ScriptableObject
        {
            // ------------------------------------------------------------
            /// <summary>
            /// XOrdered 직렬화 필드.
            /// </summary>
            // ------------------------------------------------------------
            public XOrdered<int, string, NumericIModifier> Ordered = new();
        }

        private sealed class CollectionTarget : ScriptableObject
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 값 HashSet 직렬화 필드.
            /// </summary>
            // ------------------------------------------------------------
            public XHashSet_V<int> Set = new();

            // ------------------------------------------------------------
            /// <summary>
            /// 참조 HashSet 직렬화 필드.
            /// </summary>
            // ------------------------------------------------------------
            public XHashSet_R<NumericIModifier> ReferenceSet = new();

            // ------------------------------------------------------------
            /// <summary>
            /// 딕셔너리 직렬화 필드.
            /// </summary>
            // ------------------------------------------------------------
            public XDictionary_VV<string, int> Dictionary = new();

            // ------------------------------------------------------------
            /// <summary>
            /// 큐 직렬화 필드.
            /// </summary>
            // ------------------------------------------------------------
            public XQueue_V<int> Queue = new();

            // ------------------------------------------------------------
            /// <summary>
            /// 스택 직렬화 필드.
            /// </summary>
            // ------------------------------------------------------------
            public XStack_V<int> Stack = new();
        }

        private Target target;
        private SerializedObject serializedObject;
        private CollectionSession session;
        private EditorWindow window;
        private Action<UnityEngine.Object, string, string> changeHandler;

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트마다 Target과 편집 세션을 새로 준비한다.
        /// </summary>
        // ------------------------------------------------------------
        [SetUp]
        public void SetUp()
        {
            target = ScriptableObject.CreateInstance<Target>();
            target.Numbers.Add(1);
            target.Numbers.Add(2);
            target.Value.AddModifier("a", new NumericIModifier(NumericIOperation.ADD, 5));
            serializedObject = new SerializedObject(target);
            var modifiers = serializedObject.FindProperty("Value.modifiers");
            session = Open(serializedObject, modifiers.propertyPath, MValueModifierHooks<int>.CreateKind(modifiers));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 창, 구독, Undo 기록과 Target을 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            if (changeHandler != null)
            {
                CollectionSession.Committed -= changeHandler;
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

        private static CollectionSession Open(SerializedObject owner, string path, CollectionKind kind, bool isReadOnly = false)
        {
            return new CollectionSession(new CollectionSource(owner, path, kind), isReadOnly);
        }

        private static CollectionSession Open(SerializedObject owner, string path, Func<SerializedProperty, CollectionKind> create)
        {
            return Open(owner, path, create(owner.FindProperty(path)));
        }

        private static CollectionSession OpenList(SerializedObject owner, string path, Type elementType, IReadOnlyList<string> columns = null, bool isReadOnly = false)
        {
            var kind = SerializedListKindProvider.Create(owner.FindProperty(path), false, elementType, false, columns);
            return Open(owner, path, kind, isReadOnly);
        }

        private static SerializedProperty GetCell(CollectionSession target, int sourceIndex, string columnID)
        {
            return target.Source.GetCell(sourceIndex, target.Kind.FindColumn(columnID));
        }

        private SerializedProperty GetValueProperty()
        {
            return GetCell(session, 0, "value").FindPropertyRelative("value");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 표를 펼친 상태로 테스트 창에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Show(VisualElement element)
        {
            window = ScriptableObject.CreateInstance<TestWindow>();
            window.titleContent = new GUIContent("Collection Editor Tests");
            window.position = new Rect(100, 100, 600, 400);
            window.rootVisualElement.Add(element);

            // SessionState에 남은 접힘 상태가 행 바인딩 테스트에 영향을 주지 않도록 한다.
            if (element is CollectionTable table)
            {
                Expand(table);
            }

            window.ShowUtility();
        }

        private static void Expand(CollectionTable table)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var expand = typeof(CollectionTable).GetMethod("SetExpanded", flags);
            Assert.IsNotNull(expand);
            var arguments = new object[]
            {
                true
            };
            expand.Invoke(table, arguments);
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

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 값의 바인딩 통지는 확정하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionEditor_동일값_바인딩통지는_확정하지않음()
        {
            var notices = 0;
            changeHandler = (_, _, _) => notices++;
            CollectionSession.Committed += changeHandler;

            Assert.IsFalse(session.Apply(CollectionEdit.CommitBound(GetValueProperty().propertyPath)).Succeeded);
            Assert.AreEqual(0, notices);
            Assert.AreEqual(105, target.Value.Modified);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 추가는 owner 갱신 뒤 구조 변경으로 한 번 통지한다.
        /// </summary>
        // ------------------------------------------------------------
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
            CollectionSession.Committed += changeHandler;

            var draft = CollectionDraft.ForNewRow()
                .Set("key", "b")
                .Set("value", new NumericIModifier(NumericIOperation.ADD, 3));
            Assert.IsTrue(session.Apply(CollectionEdit.AddRow(draft)).Succeeded);

            Assert.AreEqual(1, notices);
            Assert.IsNull(changedPath);
            Assert.AreEqual(108, modifiedAtNotice);
        }

    #endregion

    #region U-1: 외부 상태와 실행 취소

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 변경 관찰 뒤의 통지는 새 편집으로 확정하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionEditor_외부동기화는_새편집으로확정하지않음()
        {
            var notices = 0;
            changeHandler = (_, _, _) => notices++;
            CollectionSession.Committed += changeHandler;
            var property = GetValueProperty();
            property.intValue = 9;
            serializedObject.ApplyModifiedProperties();
            target.Value.OnAfterDeserialize();

            session.Observe();

            Assert.IsFalse(session.Apply(CollectionEdit.CommitBound(GetValueProperty().propertyPath)).Succeeded);
            Assert.AreEqual(0, notices);
            Assert.AreEqual(109, target.Value.Modified);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 값 편집의 Undo와 Redo가 데이터와 Modified를 함께 되돌린다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionEditor_값편집_UndoRedo로_데이터와Modified복원()
        {
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            var property = GetValueProperty();
            property.intValue = 7;
            serializedObject.ApplyModifiedProperties();
            session.Apply(CollectionEdit.CommitBound(property.propertyPath));
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);

            Undo.PerformUndo();
            session.Restore();
            Assert.AreEqual(5, GetValueProperty().intValue);
            Assert.AreEqual(105, target.Value.Modified);

            Undo.PerformRedo();
            session.Restore();
            Assert.AreEqual(7, GetValueProperty().intValue);
            Assert.AreEqual(107, target.Value.Modified);
        }

    #endregion

    }
}
