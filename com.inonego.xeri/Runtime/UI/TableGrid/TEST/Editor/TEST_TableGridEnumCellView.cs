/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_TableGridEnumCellView.cs
수정일 : 2026-11-27

# 설명
Editor enum 셀 뷰(TableGridEnumCellView)가 [Flags] enum을 여러 값 입력칸으로 보이고,
입력 요소(TableGridEnumInput)가 확정에 넘기는 값이 정의된 플래그 안에 머무는지 확인한다.

# 특이사항, 제약사항
입력 요소는 Editor 어셈블리의 internal 형식이므로 Editor 테스트 어셈블리에 둔다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TableGrid;
using inonego.Xeri.UI.TableGrid.Editor;

namespace inonego.Xeri.TEST.UI._TableGrid
{
    // ============================================================
    /// <summary>
    /// Editor enum 셀 뷰의 [Flags] enum 입력을 확인한다.
    /// </summary>
    // ============================================================
    [TestFixture]
    public sealed class TEST_TableGridEnumCellView
    {

    #region 내부 데이터

        [Flags]
        private enum TestFlags
        {
            None = 0,
            Alpha = 1,
            Beta = 2,
        }

    #endregion

    #region 도우미

        // Inline 편집 [Flags] enum 열의 셀을 만들고 값을 연결해 입력 요소를 돌려준다.
        private static TableGridEnumInput BindFlags(TestFlags value)
        {
            var column = new TableGridColumn<int, TestFlags>("f", "f", item => TestFlags.None, edit: TableGridCellEdit.Inline());
            var context = new TableGridCellContext("r", 0, 0, column, true, null);
            var element = column.MakeCell();
            column.View.BindBoxed(element, value, in context);

            var input = element.Q<TableGridEnumInput>();
            Assert.IsNotNull(input);
            return input;
        }

    #endregion

    #region Flags enum

        // ------------------------------------------------------------
        /// <summary>
        /// [Flags] enum은 여러 값을 고르는 입력칸에 조합 값 그대로 보인다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Flags_enum은_조합_값을_보인다()
        {
            var combined = TestFlags.Alpha | TestFlags.Beta;
            var input = BindFlags(combined);

            var flagsField = input.Q<EnumFlagsField>();
            Assert.AreEqual(DisplayStyle.Flex, flagsField.style.display.value);
            Assert.AreEqual(DisplayStyle.None, input.Q<EnumField>().style.display.value);
            Assert.AreEqual(combined, input.value);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/>여러 값 입력칸이 모든 플래그를 Everything(모든 비트)으로 바꿔도
        /// <br/>입력 요소의 값은 정의된 플래그를 모두 합한 값이다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void Flags_enum은_Everything을_정의된_플래그_합으로_되돌린다()
        {
            var input = BindFlags(TestFlags.None);

            var everything = (Enum)Enum.ToObject(typeof(TestFlags), -1);
            input.Q<EnumFlagsField>().SetValueWithoutNotify(everything);

            Assert.AreEqual(TestFlags.Alpha | TestFlags.Beta, input.value);
        }

    #endregion

    }
}
