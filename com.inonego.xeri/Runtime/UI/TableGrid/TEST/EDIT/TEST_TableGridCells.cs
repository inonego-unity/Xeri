/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_TableGridCells.cs
수정일 : 2026-11-27

# 설명
TableGrid 셀 구성의 값 규칙(글자로 나타내기·읽기·검증·비교·지우기·복사), 등록소 찾는 순서,
Inline 셀 뷰(기본 입력 셀 뷰와 열의 선택), 형식 있는 열의 값 경로를 검증한다.

# 특이사항, 제약사항
등록소는 전역이므로 테스트 전용 값 형식에만 등록하고 테스트마다 지운다.
숫자 글자는 문화권에 따라 다르므로 고정 문화권으로도 읽히는 값만 쓴다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using NUnit;
using NUnit.Framework;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TableGrid;

namespace inonego.Xeri.TEST.UI._TableGrid
{
    // ============================================================
    /// <summary>
    /// 값 규칙·등록소·형식 있는 열의 계약을 확인한다.
    /// </summary>
    // ============================================================
    [TestFixture]
    public sealed class TEST_TableGridCells
    {

    #region 내부 데이터

        private interface ITestMarker
        {
            // NONE
        }

        private class BaseValue
        {
            // NONE
        }

        private class ChildValue : BaseValue, ITestMarker
        {
            // NONE
        }

        private sealed class GrandChildValue : ChildValue
        {
            // NONE
        }

        private sealed class MarkedValue : ITestMarker
        {
            // NONE
        }

        [Serializable]
        private sealed class CopyableValue
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 복사 확인용 값.
            /// </summary>
            // ------------------------------------------------------------
            public int Number;
        }

        private sealed class PlainValue
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 복사 확인용 값.
            /// </summary>
            // ------------------------------------------------------------
            public int Number;
        }

        private sealed class NamedFormat<T> : TableGridValueFormat<T>
        {
            private readonly string name;

            // ------------------------------------------------------------
            /// <summary>
            /// 찾은 값 규칙을 구분할 이름으로 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public NamedFormat(string name)
            {
                this.name = name;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 값과 상관없이 이름을 글자로 낸다.
            /// </summary>
            // ------------------------------------------------------------
            public override string ToText(T value)
            {
                return name;
            }
        }

        private sealed class StubView : TableGridCellView<BaseValue>
        {
            public override VisualElement MakeCell()
            {
                return new VisualElement();
            }

            public override void Bind(VisualElement element, BaseValue value, in TableGridCellContext context)
            {
                // NONE
            }
        }

        private sealed class RecordingHost : ITableGridCellHost
        {
            public readonly List<object> Replaced = new();
            public int CommitCount;

            public void Replace(object value)
            {
                Replaced.Add(value);
            }

            public void Commit()
            {
                CommitCount++;
            }

            public void OpenDetail()
            {
                // NONE
            }
        }

        private static readonly Type[] registeredTypes =
        {
            typeof(BaseValue), typeof(ChildValue), typeof(GrandChildValue), typeof(MarkedValue), typeof(ITestMarker),
        };

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 전용 값 형식의 등록을 지운다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            foreach (var type in registeredTypes)
            {
                TableGridCellRegistry.UnregisterFormat(type);
                TableGridCellRegistry.UnregisterView(type);
                TableGridCellRegistry.UnregisterInlineView(type);
                TableGridCellRegistry.UnregisterEditor(type);
            }
        }

        private static string NameOf(TableGridValueFormat format)
        {
            return format?.ToTextBoxed(null);
        }

        // enum 입력 셀은 단일 값 입력칸 자체이거나(Runtime 기본) 입력칸을 품은 요소(Editor)다.
        private static EnumField FindEnumField(VisualElement element)
        {
            return element as EnumField ?? element.Q<EnumField>();
        }

    #endregion

    #region 기본 값 규칙

        // ------------------------------------------------------------
        /// <summary>
        /// 글자 값 규칙은 글자를 그대로 쓰고 지우면 빈 글자다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Text_글자를_그대로_쓰고_빈글자로_지운다()
        {
            var format = TableGridFormats.Text;

            Assert.AreEqual(string.Empty, format.ToText(null));
            Assert.IsTrue(format.TryParse("abc", out var value, out _));
            Assert.AreEqual("abc", value);
            Assert.IsTrue(format.CanParse);
            Assert.IsTrue(format.CanClear);
            Assert.AreEqual(string.Empty, format.ClearValue);
            Assert.AreEqual(TableGridColumnValueKind.Text, format.Kind);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실수 값 규칙은 유한한 숫자만 읽고 지우기를 받지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Number_유한한_숫자만_읽고_서식으로_나타낸다()
        {
            var format = TableGridFormats.Number("N0");

            Assert.AreEqual(1200d.ToString("N0", CultureInfo.CurrentCulture), format.ToText(1200d));
            Assert.IsTrue(format.TryParse("1200", out var value, out _));
            Assert.AreEqual(1200d, value);
            Assert.IsFalse(format.TryParse("abc", out _, out var error));
            Assert.IsNotNull(error);
            Assert.IsFalse(format.TryParse("NaN", out _, out _));
            Assert.IsFalse(format.Validate(double.PositiveInfinity, out _));
            Assert.Less(format.Compare(1d, 2d), 0);
            Assert.IsFalse(format.CanClear);
            Assert.AreEqual(TableGridColumnValueKind.Number, format.Kind);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 정수 값 규칙은 소수를 읽지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Integer_정수만_읽는다()
        {
            var format = TableGridFormats.Integer();

            Assert.IsTrue(format.TryParse(" 42 ", out var value, out _));
            Assert.AreEqual(42, value);
            Assert.IsFalse(format.TryParse("4.5", out _, out _));
            Assert.AreEqual(TableGridColumnValueKind.Number, format.Kind);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 숫자 글자 값 규칙은 빈 글자와 숫자를 받고 숫자로 비교한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void NumberText_빈글자와_숫자를_받고_숫자로_비교한다()
        {
            var format = TableGridFormats.NumberText;

            Assert.IsTrue(format.TryParse(string.Empty, out var empty, out _));
            Assert.AreEqual(string.Empty, empty);
            Assert.IsTrue(format.TryParse("12.5", out var number, out _));
            Assert.AreEqual("12.5", number);
            Assert.IsFalse(format.TryParse("x", out _, out _));
            Assert.IsFalse(format.Validate("1e400", out _));
            Assert.Greater(format.Compare("10", "9"), 0);
            Assert.IsTrue(format.CanClear);
            Assert.AreEqual(string.Empty, format.ClearValue);
            Assert.AreEqual(TableGridColumnValueKind.Number, format.Kind);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택형 값 규칙은 후보와 빈 글자만 받고 후보 순서로 비교한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Choice_후보만_받고_후보순서로_비교한다()
        {
            var choices = new[]
            {
                "Common", "Rare", string.Empty, "Epic",
            };
            var format = TableGridFormats.Choice(choices);

            Assert.AreEqual(3, format.Choices.Count);
            Assert.AreEqual(TableGridColumnValueKind.Choice, format.Kind);
            Assert.IsTrue(format.TryParse("Rare", out var value, out _));
            Assert.AreEqual("Rare", value);
            Assert.IsTrue(format.TryParse(string.Empty, out _, out _));
            Assert.IsFalse(format.TryParse("Mythic", out _, out var error));
            Assert.IsNotNull(error);
            Assert.Less(format.Compare("Rare", "Epic"), 0);
            Assert.Less(format.Compare("Epic", "Mythic"), 0);
            Assert.IsTrue(format.CanClear);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 값 규칙은 ToString으로만 나타내고 읽지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Default_ToString으로만_나타내고_읽지않는다()
        {
            var format = TableGridFormats.Default<int?>();

            Assert.AreEqual("5", format.ToText(5));
            Assert.AreEqual(string.Empty, format.ToText(null));
            Assert.IsFalse(format.CanParse);
            Assert.IsFalse(format.TryParse("5", out _, out var error));
            Assert.IsNotNull(error);
            Assert.IsFalse(format.CanClear);
            Assert.Less(format.Compare(1, 2), 0);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 기본 복사는 [Serializable] 클래스만 깊은 복사하고 나머지는 그대로 쓴다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void CopyValue_Serializable_클래스만_깊은복사한다()
        {
            var copyable = new CopyableValue
            {
                Number = 7,
            };
            var plain = new PlainValue
            {
                Number = 7,
            };

            var copied = TableGridFormats.Default<CopyableValue>().CopyValue(copyable);
            Assert.AreNotSame(copyable, copied);
            Assert.AreEqual(7, copied.Number);
            Assert.AreSame(plain, TableGridFormats.Default<PlainValue>().CopyValue(plain));
            Assert.AreEqual("text", TableGridFormats.Text.CopyValue("text"));
        }

    #endregion

    #region 등록소

        // ------------------------------------------------------------
        /// <summary>
        /// 정확한 형식 등록이 기반 형식·인터페이스 등록보다 먼저다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Registry_정확한_형식이_먼저다()
        {
            TableGridCellRegistry.RegisterFormat(typeof(BaseValue), new NamedFormat<BaseValue>("base"), true);
            TableGridCellRegistry.RegisterFormat(typeof(ChildValue), new NamedFormat<ChildValue>("child"));

            Assert.AreEqual("child", NameOf(TableGridCellRegistry.FindFormat(typeof(ChildValue))));
            Assert.AreEqual("base", NameOf(TableGridCellRegistry.FindFormat(typeof(BaseValue))));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 기반 형식 등록은 UseForChildren일 때만 하위 형식에 쓰고,
        /// <br/> 가까운 기반 형식부터 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Registry_기반형식은_UseForChildren일때_가까운것부터_쓴다()
        {
            TableGridCellRegistry.RegisterFormat(typeof(BaseValue), new NamedFormat<BaseValue>("base"));
            Assert.IsNull(TableGridCellRegistry.FindFormat(typeof(ChildValue)));

            TableGridCellRegistry.RegisterFormat(typeof(BaseValue), new NamedFormat<BaseValue>("base"), true);
            Assert.AreEqual("base", NameOf(TableGridCellRegistry.FindFormat(typeof(GrandChildValue))));

            TableGridCellRegistry.RegisterFormat(typeof(ChildValue), new NamedFormat<ChildValue>("child"), true);
            Assert.AreEqual("child", NameOf(TableGridCellRegistry.FindFormat(typeof(GrandChildValue))));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 인터페이스 등록은 기반 형식 등록이 없을 때 UseForChildren이면 쓴다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void Registry_인터페이스는_기반형식_다음이다()
        {
            TableGridCellRegistry.RegisterFormat(typeof(ITestMarker), new NamedFormat<ITestMarker>("marker"), true);
            Assert.AreEqual("marker", NameOf(TableGridCellRegistry.FindFormat(typeof(MarkedValue))));
            Assert.AreEqual("marker", NameOf(TableGridCellRegistry.FindFormat(typeof(ChildValue))));

            TableGridCellRegistry.RegisterFormat(typeof(BaseValue), new NamedFormat<BaseValue>("base"), true);
            Assert.AreEqual("base", NameOf(TableGridCellRegistry.FindFormat(typeof(ChildValue))));

            TableGridCellRegistry.RegisterFormat(typeof(ITestMarker), new NamedFormat<ITestMarker>("marker"));
            Assert.IsNull(TableGridCellRegistry.FindFormat(typeof(MarkedValue)));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 값 형식을 받지 않는 항목은 등록할 수 없다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Registry_값형식을_받지않는_항목은_거부한다()
        {
            Assert.Throws<ArgumentException>
            (
                () => TableGridCellRegistry.RegisterFormat(typeof(BaseValue), new NamedFormat<ChildValue>("child"))
            );
            Assert.Throws<ArgumentNullException>(() => TableGridCellRegistry.RegisterFormat(null));
        }

    #endregion

    #region Inline 셀 뷰

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 입력 셀 뷰(글자·숫자·bool·enum)는 등록소가 미리 담고 있다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Registry_기본_입력셀뷰가_미리_있다()
        {
            foreach (var type in new[] { typeof(string), typeof(int), typeof(long), typeof(float), typeof(double), typeof(bool) })
            {
                Assert.AreEqual(type, TableGridCellRegistry.FindInlineView(type)?.ValueType, type.Name);
            }

            Assert.AreEqual(typeof(Enum), TableGridCellRegistry.FindInlineView(typeof(DayOfWeek))?.ValueType);
            Assert.IsNull(TableGridCellRegistry.FindInlineView(typeof(BaseValue)));
            Assert.IsNull(TableGridCellRegistry.FindView(typeof(string)));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Inline 셀 뷰는 읽기용 셀 뷰와 따로 등록하며, 열은 편집 방식에 맞는 쪽만 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Column_편집방식에_맞는_셀뷰를_등록소에서_찾는다()
        {
            var inlineView = new StubView();
            TableGridCellRegistry.RegisterInlineView(typeof(BaseValue), inlineView, true);

            var inline = new TableGridColumn<ChildValue, ChildValue>("a", "a", item => item, edit: TableGridCellEdit.Inline());
            var readOnly = new TableGridColumn<ChildValue, ChildValue>("b", "b", item => item);

            Assert.AreSame(inlineView, TableGridCellRegistry.FindInlineView(typeof(ChildValue)));
            Assert.IsNull(TableGridCellRegistry.FindView(typeof(BaseValue)));
            Assert.AreSame(inlineView, inline.View);
            Assert.IsNull(readOnly.View);

            var readOnlyView = new StubView();
            TableGridCellRegistry.RegisterView(typeof(BaseValue), readOnlyView, true);
            var again = new TableGridColumn<ChildValue, ChildValue>("c", "c", item => item);
            var inlineAgain = new TableGridColumn<ChildValue, ChildValue>("d", "d", item => item, edit: TableGridCellEdit.Inline());
            Assert.AreSame(readOnlyView, again.View);
            Assert.AreSame(inlineView, inlineAgain.View);

            Assert.IsTrue(TableGridCellRegistry.UnregisterInlineView(typeof(BaseValue)));
            Assert.IsNull(TableGridCellRegistry.FindInlineView(typeof(ChildValue)));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 뷰를 지정하지 않은 Inline 열은 값 형식의 기본 입력 셀 뷰를, 읽기 열은 기본 셀 뷰를 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Column_Inline열은_기본_입력셀뷰를_쓴다()
        {
            var inline = new TableGridColumn<string, string>("id", "ID", item => item, edit: TableGridCellEdit.Inline());
            var readOnly = new TableGridColumn<string, string>("id", "ID", item => item);
            var explicitView = new StubView();
            var custom = new TableGridColumn<BaseValue, BaseValue>("v", "v", item => item, edit: TableGridCellEdit.Inline(), view: explicitView);
            var unsupported = new TableGridColumn<BaseValue, BaseValue>("n", "n", item => item, edit: TableGridCellEdit.Inline());

            Assert.AreEqual(typeof(string), inline.View.ValueType);
            Assert.IsInstanceOf<TextField>(inline.MakeCell());
            Assert.IsNull(readOnly.View);
            Assert.IsInstanceOf<Label>(readOnly.MakeCell());
            Assert.AreSame(explicitView, custom.View);
            Assert.IsNull(unsupported.View);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 입력 셀 뷰는 값을 알림 없이 보이고, 고칠 수 없는 셀은 비활성으로 보이며, 연결을 끊으면 호스트를 놓는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void InputCellView_값을_보이고_고칠수_없으면_비활성이다()
        {
            var column = new TableGridColumn<int, int>("n", "n", item => item, edit: TableGridCellEdit.Inline());
            var host = new RecordingHost();
            var editable = new TableGridCellContext("r", 0, 5, column, true, host);
            var locked = new TableGridCellContext("r", 0, 5, column, false, host);

            var element = column.MakeCell();
            column.View.BindBoxed(element, 5, in editable);

            Assert.AreEqual(5, ((IntegerField)element).value);
            Assert.IsTrue(element.enabledSelf);
            Assert.IsEmpty(host.Replaced);

            column.View.BindBoxed(element, 8, in locked);
            Assert.AreEqual(8, ((IntegerField)element).value);
            Assert.IsFalse(element.enabledSelf);

            column.View.Unbind(element);
            Assert.IsEmpty(host.Replaced);
            Assert.AreEqual(0, host.CommitCount);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 글자·숫자 입력칸은 즉시 반영 열이면 입력을 마칠 때만 요청하고, 확정 열이면 입력마다 보류 값을 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void InputCellView_반영시점에_따라_지연입력을_정한다()
        {
            var immediate = new TableGridColumn<int, int>("a", "a", item => item, edit: TableGridCellEdit.Inline(TableGridInlineCommit.Immediate));
            var confirm = new TableGridColumn<int, string>("b", "b", item => string.Empty, edit: TableGridCellEdit.Inline(TableGridInlineCommit.Confirm));
            var number = immediate.MakeCell();
            var text = confirm.MakeCell();

            var numberContext = new TableGridCellContext("r", 0, 1, immediate, true, null);
            var textContext = new TableGridCellContext("r", 0, 1, confirm, true, null);
            immediate.View.BindBoxed(number, 1, in numberContext);
            confirm.View.BindBoxed(text, "값", in textContext);

            Assert.IsTrue(((IntegerField)number).isDelayed);
            Assert.IsFalse(((TextField)text).isDelayed);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// enum 입력 셀 뷰는 문맥의 값 형식(열 값 형식이나 호스트가 준 형식)으로 후보를 정한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void InputCellView_enum은_문맥의_값형식으로_후보를_정한다()
        {
            var column = new TableGridColumn<int, DayOfWeek>
            (
                "day", "요일", item => (DayOfWeek)item, edit: TableGridCellEdit.Inline()
            );
            var objectColumn = new TableGridColumn<int, object>("any", "any", item => item, edit: TableGridCellEdit.Inline());
            var typed = new TableGridCellContext("r", 0, 5, column, true, null);
            var plain = new TableGridCellContext("r", 0, 5, objectColumn, true, null);
            var hosted = plain.WithValueType(typeof(DayOfWeek));

            Assert.AreEqual(typeof(DayOfWeek), typed.ValueType);
            Assert.AreEqual(typeof(object), plain.ValueType);
            Assert.AreEqual(typeof(DayOfWeek), hosted.ValueType);

            var element = column.MakeCell();
            column.View.BindBoxed(element, DayOfWeek.Friday, in typed);
            Assert.AreEqual(DayOfWeek.Friday, FindEnumField(element).value);

            var view = TableGridCellRegistry.FindInlineView(typeof(DayOfWeek));
            var other = view.MakeCell();
            view.BindBoxed(other, DayOfWeek.Monday, in hosted);
            Assert.AreEqual(DayOfWeek.Monday, FindEnumField(other).value);
        }

    #endregion

    #region 형식 있는 열

        // ----------------------------------------------------------------------
        /// <summary>
        /// 열은 지정하지 않은 값 규칙을 등록소에서 찾고, 기반 형식 값 규칙도 감싸 쓴다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void Column_값규칙을_등록소에서_찾는다()
        {
            TableGridCellRegistry.RegisterFormat(typeof(BaseValue), new NamedFormat<BaseValue>("base"), true);

            var column = new TableGridColumn<ChildValue, ChildValue>("value", "값", item => item);
            var plain = new TableGridColumn<int, int>("number", "수", item => item);

            Assert.AreEqual("base", column.GetText(new ChildValue()));
            Assert.AreEqual(string.Empty, column.GetText("다른 항목"));
            Assert.AreEqual("12", plain.GetText(12));
            Assert.IsFalse(plain.Format.CanParse);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 값 경로는 값 규칙으로 읽고 검증하며 열 형식이 아닌 값을 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Column_값경로는_값규칙과_형식을_따른다()
        {
            var column = new TableGridColumn<string[], string>
            (
                "price", "가격", cells => cells[0], TableGridFormats.NumberText, edit: TableGridCellEdit.Editor()
            );

            Assert.IsTrue(column.Editable);
            Assert.AreEqual(TableGridColumnValueKind.Number, column.ValueKind);
            Assert.IsTrue(column.TryParseText("12", out var parsed, out _));
            Assert.AreEqual("12", parsed);
            Assert.IsFalse(column.TryParseText("abc", out _, out _));
            Assert.IsFalse(column.TryValidateValue(12, out _));
            Assert.IsTrue(column.TryValidateValue(null, out _));
            var ten = new[]
            {
                "10",
            };
            var nine = new[]
            {
                "9",
            };
            Assert.Greater(column.CompareItems(ten, nine), 0);
            Assert.IsTrue(column.TryGetValue(nine, out var value));
            Assert.AreEqual("9", value);
            Assert.AreEqual(string.Empty, column.ClearValue);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 방식이 없으면 고칠 수 없는 열이다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Column_편집방식이_없으면_읽기전용이다()
        {
            var column = new TableGridColumn<string, string>("id", "ID", item => item);

            Assert.IsFalse(column.Editable);
            Assert.AreEqual(TableGridCellEditKind.None, column.Edit.Kind);
            Assert.AreEqual(TableGridInlineCommit.Confirm, TableGridCellEdit.Inline(TableGridInlineCommit.Confirm).InlineCommit);
            Assert.Throws<ArgumentException>(() => new TableGridColumn<string, string>(" ", "빈 ID", item => item));
        }

    #endregion

    }
}
