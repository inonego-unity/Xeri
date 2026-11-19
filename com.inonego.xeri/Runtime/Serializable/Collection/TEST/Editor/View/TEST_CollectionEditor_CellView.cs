/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_CollectionEditor_CellView.cs
수정일 : 2026-11-19

# 설명
[CustomCollectionCell]로 등록한 셀 뷰가 요약 자리를 대신 그리고 검색 문자열을 제공하는지 검증한다.

# 테스트 구성
 V: 셀 뷰 등록과 표시
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
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
    /// 셀 뷰 등록과 표시 계약을 검증하는 테스트 partial 클래스다.
    /// </summary>
    // ============================================================
    public partial class TEST_CollectionEditor
    {

    #region 픽스처

        private interface ICellViewMarker
        {
            // NONE
        }

        [Serializable]
        private sealed class CellViewRow
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 셀 뷰가 표시하는 이름이다.
            /// </summary>
            // ------------------------------------------------------------
            public string Name = "Desktop";

            // ------------------------------------------------------------
            /// <summary>
            /// 자동 요약 대상(직접 하위 필드 2개 이하)을 넘기기 위한 필드다.
            /// </summary>
            // ------------------------------------------------------------
            public int Order = 3;

            // ------------------------------------------------------------
            /// <summary>
            /// 자동 요약 대상을 넘기기 위한 필드다.
            /// </summary>
            // ------------------------------------------------------------
            public bool Enabled = true;
        }

        private sealed class MarkedValue : ICellViewMarker
        {
            // NONE
        }

        private sealed class CellViewTarget : ScriptableObject
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 셀 뷰가 그리는 행 목록이다.
            /// </summary>
            // ------------------------------------------------------------
            [CollectionEditor]
            public List<CellViewRow> Rows = new()
            {
                new CellViewRow()
            };
        }

        [CustomCollectionCell(typeof(CellViewRow))]
        private sealed class CellViewRowView : CollectionCellView
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 테스트에서 찾을 클래스를 가진 라벨을 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public override VisualElement MakeCell()
            {
                var label = new Label();
                label.AddToClassList("test-cell-view");
                return label;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 행 이름을 접두어와 함께 표시한다.
            /// </summary>
            // ------------------------------------------------------------
            public override void BindCell(VisualElement element, SerializedProperty property)
            {
                ((Label)element).text = "custom:" + property.FindPropertyRelative("Name").stringValue;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 행 이름을 검색 접두어와 함께 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public override string GetSearchText(SerializedProperty property)
            {
                return "search:" + property.FindPropertyRelative("Name").stringValue;
            }
        }

        [CustomCollectionCell(typeof(ICellViewMarker), UseForChildren = true)]
        private sealed class MarkerView : CollectionCellView
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 빈 라벨을 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public override VisualElement MakeCell()
            {
                return new Label();
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 조회만 검증하므로 표시할 값이 없다.
            /// </summary>
            // ------------------------------------------------------------
            public override void BindCell(VisualElement element, SerializedProperty property)
            {
                // NONE
            }
        }

    #endregion

    #region V-1: 셀 뷰 등록과 표시

        // ----------------------------------------------------------------------
        /// <summary>
        /// 같은 형식은 바로, 파생·구현 형식은 UseForChildren일 때만 셀 뷰를 찾는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_CollectionEditor_CellView_형식으로등록된뷰를찾음()
        {
            Assert.IsInstanceOf<CellViewRowView>(CollectionCellViews.Find(typeof(CellViewRow)));
            Assert.IsInstanceOf<MarkerView>(CollectionCellViews.Find(typeof(MarkedValue)));
            Assert.IsNull(CollectionCellViews.Find(typeof(ColumnRow)));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 등록된 셀 뷰가 요약 문자열 자리를 대신 그리고 검색 문자열을 제공한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_CollectionEditor_CellView_요약대신뷰를그림()
        {
            var sample = ScriptableObject.CreateInstance<CellViewTarget>();
            using var owner = new SerializedObject(sample);
            try
            {
                var list = OpenList(owner, "Rows", typeof(CellViewRow));
                var column = list.Kind.Columns[0];
                var property = list.Source.GetCell(0, column);
                Assert.AreEqual("search:Desktop", CollectionCellDrawers.Resolve(column).GetSummary(property, column));

                var view = new CollectionTable(list);
                Show(view);
                yield return WaitForBinding(() => view.Q<Label>(className: "test-cell-view")?.text == "custom:Desktop");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sample);
            }
        }

    #endregion

    }
}
