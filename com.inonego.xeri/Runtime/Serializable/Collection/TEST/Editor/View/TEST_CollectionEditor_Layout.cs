/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_CollectionEditor_Layout.cs
수정일 : 2026-11-19

# 설명
Collection Editor의 표시 행 수, 채움 모드와 필드 열 계약을 검증한다.

# 테스트 구성
 L: 높이 정책과 필드 열
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
    // ======================================================================
    /// <summary>
    /// 컬렉션 표의 높이 정책과 필드 열 계약을 검증하는 테스트 partial 클래스다.
    /// </summary>
    // ======================================================================
    public partial class TEST_CollectionEditor
    {

    #region 픽스처

        [Serializable]
        private sealed class ColumnRow
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 표시 이름 열에 연결되는 필드다.
            /// </summary>
            // ------------------------------------------------------------
            [InspectorName("이름")]
            public string DisplayName = "Desktop";

            // ------------------------------------------------------------
            /// <summary>
            /// 정렬 순서 열에 연결되는 필드다.
            /// </summary>
            // ------------------------------------------------------------
            public int Order = 0;

            // ------------------------------------------------------------
            /// <summary>
            /// 열로 나열하지 않아 상세 편집에서만 보이는 필드다.
            /// </summary>
            // ------------------------------------------------------------
            public string Note = "";
        }

        private sealed class ColumnTarget : ScriptableObject
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 하위 필드를 열로 연결하는 행 목록이다.
            /// </summary>
            // ------------------------------------------------------------
            [CollectionEditor(Columns = new[]
            {
                "DisplayName",
                "Order"
            })]
            public List<ColumnRow> Rows = new()
            {
                new ColumnRow()
            };

            // ------------------------------------------------------------
            /// <summary>
            /// 표시 행 수를 3으로 지정한 숫자 목록이다.
            /// </summary>
            // ------------------------------------------------------------
            [CollectionEditor(VisibleRows = 3)]
            public List<int> Numbers = new()
            {
                1,
                2
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 채움 모드를 켜는 호스트 스타일을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private static StyleSheet CreateFillStyle()
        {
            // USS 커스텀 속성은 인라인 스타일로 지정할 수 없으므로 임시 USS 에셋을 가져온다.
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/TEST_CollectionEditorFill.uss");
            System.IO.File.WriteAllText(path, ".fill-host .xeri-collection { --xeri-collection-fill: 1; }");
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
        }

    #endregion

    #region L-1: 높이 정책과 필드 열

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 행 모드는 지정한 행 수만큼 높이를 사용한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionEditor_VisibleRows_지정행수높이()
        {
            var list = OpenList(serializedObject, "Numbers", typeof(int));
            SessionState.EraseInt($"UniXeri.CollectionEditor.Rows.{target.GetEntityId()}.Numbers");
            var view = new CollectionTable(list, new CollectionTableOptions(3, "숫자"));
            Show(view);

            var rows = view.Q<ListView>("collection-rows");
            Assert.AreEqual(3 * 28f + 6f, rows.style.height.value.value, 0.01f);
            Assert.AreEqual("숫자", view.Q<Label>("collection-title").text);
            Assert.IsNotNull(view.Q<VisualElement>("collection-resize-handle"));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 호스트가 채움 모드를 켜면 고정 높이를 해제하고 손잡이를 숨긴다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_CollectionEditor_Fill_호스트스타일로부모높이사용()
        {
            var sheet = CreateFillStyle();
            var path = AssetDatabase.GetAssetPath(sheet);
            var host = new VisualElement();
            try
            {
                host.AddToClassList("fill-host");
                host.styleSheets.Add(sheet);
                host.style.height = 300f;
                var view = new CollectionTable(OpenList(serializedObject, "Numbers", typeof(int)));
                host.Add(view);
                Show(host);
                yield return WaitForBinding(() => view.ClassListContains("xeri-collection--fill"));

                // 클래스 변경 뒤 다음 스타일 갱신에서 손잡이가 숨겨진다.
                var handle = view.Q<VisualElement>("collection-resize-handle");
                yield return WaitForBinding(() => handle.resolvedStyle.display == DisplayStyle.None);

                var rows = view.Q<ListView>("collection-rows");
                Assert.AreEqual(StyleKeyword.Null, rows.style.height.keyword);
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 필드 열은 지정한 하위 필드를 셀로 연결하고 헤더 이름을 사용한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionEditor_Columns_하위필드를열로연결()
        {
            var sample = ScriptableObject.CreateInstance<ColumnTarget>();
            try
            {
                using var owner = new SerializedObject(sample);
                var columns = new[]
                {
                    "DisplayName", "Order"
                };
                var list = OpenList(owner, "Rows", typeof(ColumnRow), columns);

                Assert.AreEqual(2, list.Kind.Columns.Count);
                Assert.AreEqual("이름", list.Kind.Columns[0].Header);
                Assert.AreEqual("Order", list.Kind.Columns[1].Header);
                Assert.AreEqual("Desktop", GetCell(list, 0, "DisplayName").stringValue);

                // 열로 나열하지 않은 Note 필드가 있으므로 행 상세 편집 버튼을 보인다.
                var table = new CollectionTable(list);
                var hasUnlisted = typeof(CollectionTable).GetMethod("HasUnlistedFields", BindingFlags.Instance | BindingFlags.NonPublic);
                var arguments = new object[]
                {
                    0
                };
                Assert.IsTrue((bool)hasUnlisted.Invoke(table, arguments));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sample);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 존재하지 않는 필드 열은 생성 단계에서 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionEditor_Columns_없는필드는거부()
        {
            var sample = ScriptableObject.CreateInstance<ColumnTarget>();
            try
            {
                using var owner = new SerializedObject(sample);
                var columns = new[]
                {
                    "missing"
                };
                Assert.Throws<InvalidOperationException>
                (
                    () => SerializedListKindProvider.Create(owner.FindProperty("Rows"), false, typeof(ColumnRow), false, columns)
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sample);
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 분할 뷰처럼 첫 레이아웃 뒤에 폭이 줄어도 실제 열 폭이 가중치 비율로 헤더 폭을 채운다.
        /// </summary>
        // --------------------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_CollectionEditor_Columns_헤더폭변화에맞춰열을채움()
        {
            var sample = ScriptableObject.CreateInstance<ColumnTarget>();
            using var owner = new SerializedObject(sample);
            try
            {
                var columns = new[]
                {
                    "DisplayName", "Order"
                };
                var view = new CollectionTable(OpenList(owner, "Rows", typeof(ColumnRow), columns));
                var split = new TwoPaneSplitView(0, 200f, TwoPaneSplitViewOrientation.Horizontal);
                split.Add(new Label("left"));
                var host = new VisualElement();
                host.style.flexGrow = 1f;
                host.Add(view);
                split.Add(host);
                Show(split);

                Expand(view);

                var table = view.Q<MultiColumnListView>();
                var header = table.Q<VisualElement>(className: "unity-multi-column-header");
                // 실제 헤더 셀 폭으로 검사한다. Column.width만 바뀌고 화면은 그대로인 경우를 잡기 위해서다.
                bool Fitted()
                {
                    var cells = table.Query<VisualElement>(className: "unity-multi-column-header__column").ToList();
                    if (cells.Count != 2 || header.layout.width <= 0f)
                    {
                        return false;
                    }

                    var sum = cells[0].layout.width + cells[1].layout.width;
                    // 첫 열(1.2)과 숫자 열(1)의 가중치가 균등 분할이 아니라 비율대로 적용됐는지 본다.
                    var weighted = cells[0].layout.width > cells[1].layout.width * 1.1f;
                    return Mathf.Abs(sum - header.layout.width) <= 1.5f && weighted;
                }

                yield return WaitForBinding(Fitted);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sample);
            }
        }

    #endregion

    }
}
