/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridColumn.cs
수정일 : 2026-11-24

# 설명
TableGrid 열의 불변 표시·편집 사양과 형식 있는 값 경로를 정의한다.
비제네릭 TableGridColumn은 요소·모델·이벤트가 다루는 면이고, TableGridColumn<TItem, TValue>가
행 항목(TableGridRow.Item)에서 값을 꺼내 값 규칙·셀 뷰·편집기에 형식 그대로 넘긴다.
폭 정책은 Xeri Core의 TableGridColumnLayout(고정·가변, 최소·최대 폭, 가중치)을 쓴다.

# 특이사항, 제약사항
열 ID는 공백이 아닌 안정적인 식별자여야 한다.
값은 열 안에서 TItem·TValue로 다뤄 글자·비교·셀 연결에서 박싱하지 않는다. 박싱은 편집·클립보드 경계에만 남는다.
값 규칙·셀 뷰는 열 지정 → 등록소(TableGridCellRegistry) → 기본(Default<T> 값 규칙, 글자·선택형 칩 셀 뷰) 순으로 정한다.
Inline 편집 열의 셀 뷰는 열 지정 → 등록소의 Inline 셀 뷰(기본 입력 셀 뷰 포함) 순으로 정하고, 없으면 기본 셀 뷰(읽기용)를 쓴다.
항목이 TItem이 아니면(그룹 머리 등) 값이 없는 셀로 다룬다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 열의 안정적인 식별자와 표시·편집 정책을 보유하는 불변 계약이다.
    /// </summary>
    // ============================================================
    public abstract class TableGridColumn
    {

    #region 기본값

        // ------------------------------------------------------------
        /// <summary>
        /// 폭 정책을 지정하지 않은 열의 기본 폭 정책.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridColumnLayout DefaultLayout => TableGridColumnLayout.Flexible(160f, 80f, 0f, 1f);

    #endregion

    #region 열 사양

        private protected TableGridColumn
        (
            string id, string title, TableGridColumnLayout layout, TableGridAlignment alignment, TableGridCellEdit edit,
            bool filterable, bool sortable, bool groupable, bool searchable
        )
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("열 ID는 비어 있을 수 없습니다.", nameof(id));
            }

            if (!Enum.IsDefined(typeof(TableGridAlignment), alignment))
            {
                throw new ArgumentOutOfRangeException(nameof(alignment));
            }

            ID = id;
            Title = title ?? string.Empty;
            Layout = layout ?? DefaultLayout;
            Alignment = alignment;
            Edit = edit ?? TableGridCellEdit.None;
            Filterable = filterable;
            Sortable = sortable;
            Groupable = groupable;
            Searchable = searchable;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 화면 순서와 무관한 열 식별자.
        /// </summary>
        // ------------------------------------------------------------
        public string ID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 헤더에 보이는 제목.
        /// </summary>
        // ------------------------------------------------------------
        public string Title { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 폭 정책. 사용자가 경계를 끌어 정한 폭은 TableGrid가 따로 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridColumnLayout Layout { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 내용의 가로 정렬.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridAlignment Alignment { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 편집 방식.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridCellEdit Edit { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 변경을 요청할 수 있는 열인지 여부(편집 방식이 None이 아님).
        /// </summary>
        // ------------------------------------------------------------
        public bool Editable => Edit.Kind != TableGridCellEditKind.None;

        // ------------------------------------------------------------
        /// <summary>
        /// 열 메뉴에서 필터를 편집할 수 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool Filterable { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 메뉴에서 정렬을 요청할 수 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool Sortable { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 메뉴에서 그룹을 요청할 수 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool Groupable { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 검색 글자로 거를 때 이 열을 보는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool Searchable { get; }

    #endregion

    #region 값 사양

        // ------------------------------------------------------------
        /// <summary>
        /// 열 값 형식.
        /// </summary>
        // ------------------------------------------------------------
        public abstract Type ValueType { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 값 규칙. 글자·검색·복사·붙여넣기·검증·비교가 모두 이 규칙을 따른다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract TableGridValueFormat Format { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 뷰. null이면 기본 셀 뷰(글자, 선택형 칩)를 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract TableGridCellView View { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 필터 편집 UI의 입력 종류(값 규칙의 Kind).
        /// </summary>
        // ------------------------------------------------------------
        public TableGridColumnValueKind ValueKind => Format.Kind;

        // ------------------------------------------------------------
        /// <summary>
        /// 선택형 열의 값 후보. 칩 색은 이 순서로 정한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<string> Choices => Format.Choices;

        // ------------------------------------------------------------
        /// <summary>
        /// 행 항목의 셀 값을 값 규칙 글자로 나타낸다. 값이 없으면 빈 글자다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract string GetText(object item);

    #endregion

    #region 값 경로 (내부)

        internal abstract bool TryGetValue(object item, out object value);

        internal abstract int CompareItems(object leftItem, object rightItem);

        internal abstract bool AcceptsValue(object value);

        internal abstract bool TryParseText(string text, out object value, out string error);

        internal abstract bool TryValidateValue(object value, out string error);

        internal abstract object CopyValue(object value);

        internal abstract object ClearValue { get; }

        internal abstract TableGridCellEditor ResolveEditor();

    #endregion

    #region 셀 연결 (내부)

        internal abstract VisualElement MakeCell();

        internal abstract void BindCell(VisualElement element, object item, in TableGridCellContext context);

        internal abstract void UnbindCell(VisualElement element);

        internal abstract float? MeasureCell(object item, in TableGridCellContext context);

    #endregion

    }

    // ============================================================
    /// <summary>
    /// TItem 행 항목에서 TValue 값을 꺼내 보이고 고치는 열.
    /// </summary>
    // ============================================================
    public sealed class TableGridColumn<TItem, TValue> : TableGridColumn
    {

    #region 필드

        private const string TYPE_MISMATCH = "열 형식에 맞지 않는 값입니다";

        private static readonly bool acceptsNull = !typeof(TValue).IsValueType || Nullable.GetUnderlyingType(typeof(TValue)) != null;

        private readonly TableGridValueFormat<TValue> format;
        private readonly TableGridCellView<TValue> typedView;
        private readonly TableGridItemCellView itemView;
        private TableGridCellEditor textEditor;

    #endregion

    #region 생성

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 열을 만든다. value는 행 항목에서 셀 값을 꺼낸다.
        /// <br/> format·view가 null이면 등록소에서 찾고, 없으면 기본 값 규칙·기본 셀 뷰를 쓴다.
        /// <br/> Inline 편집 열의 view는 등록소의 Inline 셀 뷰(기본 입력 셀 뷰 포함)에서 찾는다.
        /// <br/> edit가 null이면 고칠 수 없는 열(None)이다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public TableGridColumn
        (
            string id, string title, Func<TItem, TValue> value,
            TableGridValueFormat<TValue> format = null,
            TableGridColumnLayout layout = null,
            TableGridAlignment alignment = TableGridAlignment.Left,
            TableGridCellEdit edit = null,
            bool filterable = true,
            bool sortable = true,
            bool groupable = true,
            bool searchable = true,
            TableGridCellView view = null
        )
            : base(id, title, layout, alignment, edit, filterable, sortable, groupable, searchable)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));

            // 기반 형식에 등록한 값 규칙은 박싱 경로로 감싸 TValue 값 규칙으로 쓴다.
            formatSource = format ?? TableGridCellRegistry.FindFormat(typeof(TValue)) ?? TableGridFormats.Default<TValue>();
            this.format = formatSource as TableGridValueFormat<TValue> ?? new TableGridFormatAdapter<TValue>(formatSource);

            // Inline 열은 셀 안 입력칸을 그리는 Inline 셀 뷰를, 그 밖의 열은 읽기용 셀 뷰를 등록소에서 찾는다.
            this.view = view ?? (Edit.Kind == TableGridCellEditKind.Inline
                ? TableGridCellRegistry.FindInlineView(typeof(TValue))
                : TableGridCellRegistry.FindView(typeof(TValue)));
            var viewType = this.view?.ValueType;
            if (viewType != null && !viewType.IsAssignableFrom(typeof(TValue)))
            {
                throw new ArgumentException($"셀 뷰 형식 {viewType}이 {typeof(TValue)} 값을 받지 않습니다.", nameof(view));
            }

            typedView = this.view as TableGridCellView<TValue>;
            itemView = this.view as TableGridItemCellView;

            var cellEditor = Edit.CellEditor;
            if (cellEditor != null && !cellEditor.ValueType.IsAssignableFrom(typeof(TValue)))
            {
                throw new ArgumentException($"편집기 형식 {cellEditor.ValueType}이 {typeof(TValue)} 값을 받지 않습니다.", nameof(edit));
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 항목에서 셀 값을 꺼내는 함수.
        /// </summary>
        // ------------------------------------------------------------
        public Func<TItem, TValue> Value { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 값 형식(TValue).
        /// </summary>
        // ------------------------------------------------------------
        public override Type ValueType => typeof(TValue);

        // ------------------------------------------------------------
        /// <summary>
        /// 값 규칙(지정·등록·기본 중 정해진 것).
        /// </summary>
        // ------------------------------------------------------------
        public override TableGridValueFormat Format => formatSource;
        private readonly TableGridValueFormat formatSource;

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 뷰. null이면 기본 셀 뷰를 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public override TableGridCellView View => view;
        private readonly TableGridCellView view;

    #endregion

    #region 값 경로

        // ------------------------------------------------------------
        /// <summary>
        /// 행 항목의 셀 값을 값 규칙 글자로 나타낸다. 값이 없으면 빈 글자다.
        /// </summary>
        // ------------------------------------------------------------
        public override string GetText(object item)
        {
            return item is TItem typed ? format.ToText(Value(typed)) ?? string.Empty : string.Empty;
        }

        internal override bool TryGetValue(object item, out object value)
        {
            if (item is TItem typed)
            {
                value = Value(typed);
                return true;
            }

            value = null;
            return false;
        }

        internal override int CompareItems(object leftItem, object rightItem)
        {
            var hasLeft = leftItem is TItem;
            var hasRight = rightItem is TItem;
            if (hasLeft && hasRight)
            {
                return format.Compare(Value((TItem)leftItem), Value((TItem)rightItem));
            }

            // 값이 없는 항목은 뒤에 둔다.
            if (hasLeft == hasRight)
            {
                return 0;
            }

            return hasLeft ? -1 : 1;
        }

        internal override bool AcceptsValue(object value)
        {
            return value is TValue || (value == null && acceptsNull);
        }

        internal override bool TryParseText(string text, out object value, out string error)
        {
            value = null;
            if (!format.CanParse)
            {
                error = "글자에서 값을 읽을 수 없는 열입니다";
                return false;
            }

            // 읽은 값도 검증을 거쳐야 요청할 수 있다.
            if (!format.TryParse(text ?? string.Empty, out var parsed, out error) || !format.Validate(parsed, out error))
            {
                return false;
            }

            value = parsed;
            return true;
        }

        internal override bool TryValidateValue(object value, out string error)
        {
            if (!AcceptsValue(value))
            {
                error = TYPE_MISMATCH;
                return false;
            }

            return format.Validate(TableGridBoxing.Unbox<TValue>(value), out error);
        }

        internal override object CopyValue(object value)
        {
            return value is TValue typed ? format.CopyValue(typed) : value;
        }

        internal override object ClearValue => format.ClearValue;

        internal override TableGridCellEditor ResolveEditor()
        {
            // 열 지정 → 값 형식 등록 → (선택형은 표의 선택 메뉴) → 글자를 읽을 수 있으면 기본 입력칸.
            var editor = Edit.CellEditor ?? TableGridCellRegistry.FindEditor(typeof(TValue));
            if (editor != null)
            {
                return editor;
            }

            var isChoice = ValueKind == TableGridColumnValueKind.Choice && Choices.Count > 0;
            if (isChoice || !format.CanParse)
            {
                return null;
            }

            textEditor ??= new TableGridTextCellEditor<TValue>(format);
            return textEditor;
        }

    #endregion

    #region 셀 연결

        internal override VisualElement MakeCell()
        {
            if (view != null)
            {
                return view.MakeCell();
            }

            return ValueKind == TableGridColumnValueKind.Choice
                ? TableGridDefaultCellViews.MakeChoice()
                : TableGridDefaultCellViews.MakeText();
        }

        internal override void BindCell(VisualElement element, object item, in TableGridCellContext context)
        {
            if (itemView != null)
            {
                itemView.Bind(element, in context);
                return;
            }

            var hasValue = item is TItem;
            var value = hasValue ? Value((TItem)item) : default;
            if (typedView != null)
            {
                typedView.Bind(element, value, in context);
                return;
            }

            // 기반 형식에 등록한 셀 뷰는 박싱 경로로 넘긴다.
            if (view != null)
            {
                view.BindBoxed(element, value, in context);
                return;
            }

            var text = hasValue ? format.ToText(value) ?? string.Empty : string.Empty;
            TableGridDefaultCellViews.Bind(element, this, text);
        }

        internal override void UnbindCell(VisualElement element)
        {
            if (view != null)
            {
                view.Unbind(element);
                return;
            }

            TableGridDefaultCellViews.Unbind(element);
        }

        internal override float? MeasureCell(object item, in TableGridCellContext context)
        {
            if (itemView != null)
            {
                return itemView.MeasureWidth(in context);
            }

            if (view == null || !(item is TItem typed))
            {
                return null;
            }

            var value = Value(typed);
            return typedView != null ? typedView.MeasureWidth(value, in context) : view.MeasureBoxed(value, in context);
        }

    #endregion

    }
}
