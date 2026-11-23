/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_TableGridDataRowNumbers.cs
수정일 : 2026-11-22

# 설명
그룹 머리 행이 낀 결과를 실제 Editor 창에 띄워, 사용자에게 보이는 번호·개수가 데이터 행만 세는지 확인한다.
페이지 바 범위·전체 개수, 상태 줄 범위·전체 행 수, 행 선택 칸의 행 번호(위에서 아래 순서), 행 추가 능력(CanAddRows)을 본다.

# 특이사항, 제약사항
첫 배치는 원인(보이는 높이)이 아니라 결과(글자·배치된 번호)가 나올 때까지 프레임 한도 안에서 기다린다.
페이지 이동·소스 교체는 동기로 반영되므로 기다리지 않고 바로 확인한다.
범위 글자는 표기(대시·단위)가 아니라 그 안의 숫자만 비교한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TableGrid;

using TableGridView = inonego.Xeri.UI.TableGrid.TableGrid;

namespace inonego.Xeri.TEST.UI._TableGrid
{
    // ============================================================
    /// <summary>
    /// 그룹 머리 행이 낀 표의 보이는 번호·개수를 확인한다.
    /// </summary>
    // ============================================================
    [TestFixture]
    public sealed class TEST_TableGridDataRowNumbers
    {

    #region 내부 형식

        // ============================================================
        /// <summary>
        /// 표 하나를 띄우는 시험 창.
        /// </summary>
        // ============================================================
        private sealed class DataRowNumbersTestWindow : EditorWindow
        {
            // NONE
        }

        private sealed class Item
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 행 ID.
            /// </summary>
            // ------------------------------------------------------------
            public string ID { get; set; }
        }

    #endregion

    #region 필드

        private const string NUMBER_CLASS = "xeri-table__selector-number";
        private const int MAX_LAYOUT_FRAMES = 120;

        private static readonly Regex numberPattern = new(@"\d+");

        private DataRowNumbersTestWindow window;

    #endregion

    #region 준비

        // ------------------------------------------------------------
        /// <summary>
        /// 연 시험 창이 있으면 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            if (window != null)
            {
                window.Close();
                window = null;
            }
        }

        private static TableGridRow Group(string id)
        {
            return new TableGridRow(id, id, TableGridRowKind.GroupHeader);
        }

        private static TableGridRow Data(string id)
        {
            var item = new Item
            {
                ID = id,
            };
            return new TableGridRow(id, item);
        }

        // 결과: [그룹1, row-a, row-b, 그룹2, row-c]
        private static List<TableGridRow> GroupedRows()
        {
            return new List<TableGridRow>
            {
                Group("group-1"), Data("row-a"), Data("row-b"), Group("group-2"), Data("row-c"),
            };
        }

        private static TableGridView CreateGrid(IReadOnlyList<TableGridRow> rows, bool paged, TableGridAccess access = TableGridAccess.Editable)
        {
            var options = TableGridOptions.Workspace(access);
            if (paged)
            {
                options.Navigation = TableGridNavigation.Paged;
                options.PageSize = 2;
            }

            var grid = new TableGridView(options);
            grid.style.height = 300f;
            grid.style.flexShrink = 0f;

            var columns = new TableGridColumn[]
            {
                new TableGridColumn<Item, string>("id", "ID", item => item.ID, TableGridFormats.Text),
            };
            grid.SetColumns(columns);
            grid.SetSource(new TableGridRowListSource(rows));

            return grid;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 시험 창에 표를 띄우고 ready가 참이 될 때까지(프레임 한도 안에서) 기다린다.
        /// <br/> ready는 확인할 결과(글자·배치된 번호)를 묻는다.
        /// <br/> 한도를 넘기면 이어지는 단언이 실패를 알린다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private IEnumerator Show(TableGridView grid, Func<bool> ready)
        {
            window = ScriptableObject.CreateInstance<DataRowNumbersTestWindow>();
            window.position = new Rect(100f, 100f, 500f, 500f);
            window.ShowUtility();
            window.rootVisualElement.Add(grid);

            for (var frame = 0; frame < MAX_LAYOUT_FRAMES && !ready(); frame++)
            {
                window.Repaint();
                yield return null;
            }
        }

        private static Func<bool> Shows(TableGridView grid, List<int> page, List<int> status)
        {
            return () => NumbersEqual(PageNumbers(grid), page) && NumbersEqual(StatusNumbers(grid), status);
        }

        private static bool NumbersEqual(List<int> actual, List<int> expected)
        {
            if (actual.Count != expected.Count)
            {
                return false;
            }

            for (var index = 0; index < actual.Count; index++)
            {
                if (actual[index] != expected[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static List<int> PageNumbers(TableGridView grid)
        {
            return ReadNumbers(grid, "count-label");
        }

        private static List<int> StatusNumbers(TableGridView grid)
        {
            return ReadNumbers(grid, "range-label");
        }

        private static List<int> ReadNumbers(TableGridView grid, string labelName)
        {
            var label = grid.Q<Label>(labelName);
            Assert.IsNotNull(label, $"{labelName} 글자를 찾지 못했다");

            var numbers = new List<int>();
            foreach (Match match in numberPattern.Matches(label.text))
            {
                numbers.Add(int.Parse(match.Value));
            }

            return numbers;
        }

        private static List<Label> CollectShownNumberLabels(TableGridView grid)
        {
            // 숨긴 풀 행은 이전 글자를 들고 있으므로 보이는 행만 모은다.
            var labels = new List<Label>();
            foreach (var label in grid.Query<Label>(className: NUMBER_CLASS).ToList())
            {
                if (IsShown(label))
                {
                    labels.Add(label);
                }
            }

            return labels;
        }

        private static bool AreNumbersLaidOut(TableGridView grid, int count)
        {
            // 위치로 정렬하려면 번호 칸이 배치를 마쳐 높이를 가져야 한다.
            var labels = CollectShownNumberLabels(grid);
            if (labels.Count != count)
            {
                return false;
            }

            foreach (var label in labels)
            {
                if (!(label.worldBound.height > 0f))
                {
                    return false;
                }
            }

            return true;
        }

        private static List<string> CollectShownNumbersTopDown(TableGridView grid)
        {
            var labels = CollectShownNumberLabels(grid);

            labels.Sort((left, right) => left.worldBound.yMin.CompareTo(right.worldBound.yMin));
            var numbers = new List<string>(labels.Count);
            foreach (var label in labels)
            {
                numbers.Add(label.text);
            }

            return numbers;
        }

        private static bool IsShown(VisualElement element)
        {
            // 숨김 방식(인라인 지정·USS)과 상관없이, 지정한 값과 해석된 값 둘 다 본다.
            for (var current = element; current != null; current = current.parent)
            {
                if (current.style.display == DisplayStyle.None || current.resolvedStyle.display == DisplayStyle.None)
                {
                    return false;
                }
            }

            return true;
        }

    #endregion

    #region 시험

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Paged 표의 페이지 바와 상태 줄은 페이지 구간 안의 데이터 행 번호와
        /// <br/> 전체 데이터 행 수를 보인다(페이지는 머리 행을 포함한 위치로 나눈다).
        /// <br/> 페이지 이동은 동기로 반영된다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator 페이지_바와_상태_줄은_데이터_행만_센다()
        {
            var grid = CreateGrid(GroupedRows(), true);
            var first = new List<int>
            {
                1, 1, 3,
            };
            yield return Show(grid, Shows(grid, first, first));
            CollectionAssert.AreEqual(first, PageNumbers(grid));
            CollectionAssert.AreEqual(first, StatusNumbers(grid));

            grid.PageIndex = 1;
            var second = new List<int>
            {
                2, 2, 3,
            };
            CollectionAssert.AreEqual(second, PageNumbers(grid));
            CollectionAssert.AreEqual(second, StatusNumbers(grid));

            grid.PageIndex = 2;
            var third = new List<int>
            {
                3, 3, 3,
            };
            CollectionAssert.AreEqual(third, PageNumbers(grid));
            CollectionAssert.AreEqual(third, StatusNumbers(grid));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 전체 행 수·페이지는 같고 머리 행 위치만 바뀌어도 페이지 바를 다시 센다
        /// <br/> (페이지 값이 그대로라 페이지 바가 스스로 다시 그리지 않는 경우).
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator 머리_행_위치만_바뀌어도_페이지_바를_다시_센다()
        {
            var grid = CreateGrid(GroupedRows(), true);
            var before = new List<int>
            {
                1, 1, 3,
            };
            yield return Show(grid, Shows(grid, before, before));
            CollectionAssert.AreEqual(before, PageNumbers(grid));

            // 같은 5행, 첫 페이지 [row-a, row-b]
            var moved = new List<TableGridRow>
            {
                Data("row-a"), Data("row-b"), Group("group-1"), Data("row-c"), Group("group-2"),
            };
            grid.SetSource(new TableGridRowListSource(moved), true);

            var after = new List<int>
            {
                1, 2, 3,
            };
            CollectionAssert.AreEqual(after, PageNumbers(grid));
            CollectionAssert.AreEqual(after, StatusNumbers(grid));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 머리 행만 있는 페이지는 페이지 바와 상태 줄 모두 0-0이다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator 머리_행만_있는_페이지는_0_0이다()
        {
            var rows = new List<TableGridRow>
            {
                Group("group-1"), Group("group-2"), Data("row-a"),
            };
            var grid = CreateGrid(rows, true);
            var expected = new List<int>
            {
                0, 0, 1,
            };
            yield return Show(grid, Shows(grid, expected, expected));
            CollectionAssert.AreEqual(expected, PageNumbers(grid));
            CollectionAssert.AreEqual(expected, StatusNumbers(grid));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 행 선택 칸의 번호는 위에서부터 데이터 행에만 1부터 이어 붙고 머리 행은 비어 있다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator 행_번호는_데이터_행에만_차례로_붙는다()
        {
            var grid = CreateGrid(GroupedRows(), false);
            yield return Show(grid, () => AreNumbersLaidOut(grid, 5));

            var expected = new List<string>
            {
                string.Empty, "1", "2", string.Empty, "3",
            };
            CollectionAssert.AreEqual(expected, CollectShownNumbersTopDown(grid));

            var range = new List<int>
            {
                1, 3, 3,
            };
            CollectionAssert.AreEqual(range, StatusNumbers(grid));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 추가 요청을 받는 편집 가능한 표만 CanAddRows가 켜진다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void 편집_가능하고_행_추가를_받는_표만_CanAddRows다()
        {
            var editable = CreateGrid(GroupedRows(), false);
            Assert.IsFalse(editable.Capabilities.CanAddRows, "받는 호스트가 없는데 켜졌다");

            editable.AddRowRequested += (_, _) => { };
            Assert.IsTrue(editable.Capabilities.CanAddRows);

            var readOnly = CreateGrid(GroupedRows(), false, TableGridAccess.ReadOnly);
            readOnly.AddRowRequested += (_, _) => { };
            Assert.IsFalse(readOnly.Capabilities.CanAddRows, "읽기 전용 표에서 켜졌다");
        }

    #endregion

    }
}
