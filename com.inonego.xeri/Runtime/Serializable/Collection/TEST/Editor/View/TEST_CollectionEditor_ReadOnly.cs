/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_CollectionEditor_ReadOnly.cs
수정일 : 2026-11-19

# 설명
Collection Editor의 ReadOnly 조회, 편집 차단과 상세 보기 유지 계약을 검증한다.

# 테스트 구성
 R: ReadOnly 표시와 변경 차단
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
    /// Collection Editor의 ReadOnly 조회 회귀 테스트.
    /// </summary>
    // ============================================================
    public partial class TEST_CollectionEditor
    {

    #region 픽스처

        private sealed class ReadOnlyTarget : ScriptableObject
        {
            // ------------------------------------------------------------
            /// <summary>
            /// ReadOnly 어트리뷰트가 먼저 붙은 조회 전용 목록.
            /// </summary>
            // ------------------------------------------------------------
            [ReadOnly]
            [CollectionEditor]
            public List<int> FirstReadOnly = new();

            // ------------------------------------------------------------
            /// <summary>
            /// CollectionEditor 어트리뷰트가 먼저 붙은 조회 전용 목록.
            /// </summary>
            // ------------------------------------------------------------
            [CollectionEditor]
            [ReadOnly]
            public List<int> FirstCollection = new();

            // ------------------------------------------------------------
            /// <summary>
            /// ReadOnly 딕셔너리 필드.
            /// </summary>
            // ------------------------------------------------------------
            [ReadOnly]
            public XDictionary_VV<int, int> DictionaryReadOnly = new();

            // ------------------------------------------------------------
            /// <summary>
            /// 기본 편집 가능 딕셔너리 필드.
            /// </summary>
            // ------------------------------------------------------------
            public XDictionary_VV<int, int> DictionaryDefault = new();
        }

    #endregion

    #region R-1: ReadOnly 조회와 편집 차단

        // ----------------------------------------------------------------------
        /// <summary>
        /// 컬렉션 진입 방식과 어트리뷰트 순서에 관계없이 조회 전용 View를 표시한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_CollectionEditor_ReadOnly_모든Drawer에서조회기능유지()
        {
            var sample = ScriptableObject.CreateInstance<ReadOnlyTarget>();
            using var owner = new SerializedObject(sample);
            var root = new VisualElement();
            var names = new[]
            {
                nameof(ReadOnlyTarget.FirstReadOnly),
                nameof(ReadOnlyTarget.FirstCollection),
                nameof(ReadOnlyTarget.DictionaryReadOnly),
                nameof(ReadOnlyTarget.DictionaryDefault)
            };
            var fields = new List<PropertyField>();
            try
            {
                foreach (var name in names)
                {
                    var field = new PropertyField(owner.FindProperty(name));
                    fields.Add(field);
                    root.Add(field);
                }

                Show(root);
                root.Bind(owner);
                yield return WaitForBinding
                (
                    () => fields.TrueForAll(field => field.Q<CollectionTable>() != null)
                );

                for (var index = 0; index < fields.Count; index++)
                {
                    var view = fields[index].Q<CollectionTable>();
                    var readOnly = index != names.Length - 1;
                    Assert.IsTrue(view.enabledInHierarchy);
                    Assert.AreEqual(!readOnly, view.Q<Button>("collection-add").enabledInHierarchy);
                    Assert.IsTrue(view.Q<Button>("collection-foldout").enabledInHierarchy);
                    Assert.IsTrue(view.Q<ToolbarSearchField>("collection-search").enabledInHierarchy);
                }
            }
            finally
            {
                root.Unbind();
                window?.Close();
                window = null;
                UnityEngine.Object.DestroyImmediate(sample);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// ReadOnly에서는 상세 창의 값 입력만 막고 닫기와 탐색을 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_CollectionEditor_ReadOnly_상세보기는열리고입력만비활성화()
        {
            var list = OpenList(serializedObject, "Numbers", typeof(int), isReadOnly: true);
            var view = new CollectionTable(list);
            Show(view);
            Assert.IsTrue(CollectionDetailEditors.OpenCell(list, 0, list.Kind.Columns[0]));

            var editor = CollectionEditWindowCoordinator.Find<CollectionValueEditorWindow>();
            Assert.IsNotNull(editor);
            try
            {
                yield return WaitForBinding
                (
                    () => editor.rootVisualElement.Q<IntegerField>() != null
                );

                var input = editor.rootVisualElement.Q<IntegerField>();
                Assert.IsFalse(input.enabledInHierarchy);
                Assert.IsTrue(editor.rootVisualElement.Q<Button>("editor-cancel").enabledInHierarchy);
                Assert.AreEqual("읽기 전용입니다.", editor.rootVisualElement.Q<Label>("editor-footnote").text);
                Assert.AreEqual(1, target.Numbers[0]);
            }
            finally
            {
                editor.Close();
            }
        }

    #endregion

    }
}
