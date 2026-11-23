/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionDefaultCellDrawer.cs
수정일 : 2026-11-24

# 설명
다른 drawer가 맡지 않은 셀을 그리는 기본 drawer.
단순 값은 PropertyField로 셀 안에서 바로 고치고, 복합 값·읽기 전용 셀은 요약과 상세 편집 버튼을 보인다.
단순 값 초안 열(키·순서)은 바인딩하지 않은 TableGrid 입력 셀 뷰(CollectionInlineInput이 고른 Inline 셀 뷰)로 고치고, 확정하면 표 호스트가 검증한 뒤 쓴다.
값 형식에 등록된 셀 뷰(CollectionCellView)가 있으면 요약 문자열 대신 그 뷰를 보인다.

# 특이사항, 제약사항
셀 UI는 행이 바뀌어도 재사용하며 PropertyField 바인딩은 경로나 속성 종류가 바뀔 때만 다시 건다.
입력 필드·초안 입력칸·요약 행은 처음 필요할 때 만든다. 한 열의 셀은 대개 한쪽만 쓰므로 나머지는 만들지 않는다.
바인딩 입력 필드의 값 확정은 표가 SerializedPropertyChangeEvent로 처리한다.
초안 입력칸은 표 셀 문맥(CollectionCell.GridContext)이 있을 때만 쓴다. 없으면 요약과 초안 창으로 고친다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TableGrid;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 기본 셀 drawer.
    /// </summary>
    // ============================================================
    internal sealed class CollectionDefaultCellDrawer : ICollectionCellDrawer
    {

    #region 내부 타입

        // ============================================================
        /// <summary>
        /// 재사용하는 셀 UI와 현재 연결 상태.
        /// </summary>
        // ============================================================
        private sealed class CellView : VisualElement
        {
            internal bool DelayedInput;
            internal PropertyField Field;
            internal VisualElement InlineInput;
            internal TableGridCellView InlineView;
            internal SummaryRow Summary;
            internal CollectionCell? Cell;
            internal bool HasFieldBinding;
            internal string FieldPath;
            internal SerializedPropertyType FieldType;
        }

        // ============================================================
        /// <summary>
        /// 요약 문자열(또는 셀 뷰)과 상세 편집 버튼을 가진 행.
        /// </summary>
        // ============================================================
        private sealed class SummaryRow : VisualElement
        {
            internal readonly Label Text = new();
            internal readonly CollectionCellContent Content = new();
            internal readonly Toolbar Toolbar = new();
            internal readonly ToolbarButton EditButton;

            // ------------------------------------------------------------
            /// <summary>
            /// 셀 UI의 요약 행을 만든다.
            /// </summary>
            // ------------------------------------------------------------
            internal SummaryRow(CellView owner)
            {
                AddToClassList("xeri-collection__reference-row");
                Text.AddToClassList("xeri-collection__data-label");
                Add(Text);
                Add(Content);
                EditButton = CollectionIconButton.Create(() => owner.Cell?.OpenDetail());
                EditButton.AddToClassList("xeri-collection__cell-action");
                Toolbar.AddToClassList("xeri-collection__cell-toolbar");
                Toolbar.Add(EditButton);
                Add(Toolbar);
            }
        }

    #endregion

    #region 셀 UI 생성과 연결

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 drawer가 맡지 않은 모든 열을 그린다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Matches(CollectionColumn column)
        {
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 셀 UI를 만든다. 입력 필드와 요약 행은 처음 필요할 때 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement CreateElement(CollectionColumn column)
        {
            var view = new CellView();
            view.AddToClassList("xeri-collection__cell-content");
            view.DelayedInput = column.Edit == CollectionCellEdit.Delayed;
            return view;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 단순 값은 입력 필드, 그 밖에는 요약(또는 셀 뷰)과 상세 버튼으로 연결한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Bind(VisualElement element, CollectionCell cell)
        {
            var view = (CellView)element;
            view.Cell = cell;
            var property = cell.Property;
            if (property == null)
            {
                ReleaseField(view);
                ShowSummary(view, "필드 없음", null);
                view.Summary.Toolbar.style.display = DisplayStyle.None;
                return;
            }

            var editable = cell.CanEdit;
            var inlineEdit = cell.Column.Edit == CollectionCellEdit.Inline || cell.Column.Edit == CollectionCellEdit.Delayed;
            if (editable && inlineEdit && IsSimple(property))
            {
                ShowField(view, property);
                return;
            }

            // 단순 값 초안 열은 셀 안 입력칸으로 고치고, 확정은 표를 거쳐 호스트가 검증한다.
            var draftEdit = cell.Column.Edit == CollectionCellEdit.Draft && cell.GridContext.HasValue;
            if (editable && draftEdit && IsSimple(property) && ShowInlineInput(view, cell, property))
            {
                return;
            }

            ReleaseField(view);
            var cellView = CollectionCellViews.Find(property, cell.Column);
            if (cellView != null)
            {
                ShowCellView(view, cellView, property);
            }
            else
            {
                ShowSummary(view, CollectionCellSummary.GetText(property), CollectionCellSummary.GetDisplayText(property));
            }

            var summary = view.Summary;
            summary.EditButton.iconImage = XeriIcon.LoadBackground(editable ? "pencil" : "eye");
            summary.EditButton.tooltip = !editable
                ? "상세 보기"
                : cell.Column.Edit == CollectionCellEdit.Draft ? $"{cell.Column.Header} 편집" : "상세 편집";
            summary.Toolbar.style.display = DisplayStyle.Flex;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이전 행의 연결을 끊되 UI는 다음 행에서 재사용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Unbind(VisualElement element)
        {
            var view = (CellView)element;
            ReleaseField(view);
            HideInlineInput(view);
            view.Summary?.Content.Release();
            view.Cell = null;
        }

        private static bool IsSimple(SerializedProperty property)
        {
            return !CollectionCellSummary.IsComposite(property);
        }

    #endregion

    #region 입력 필드

        private static void ShowField(CellView view, SerializedProperty property)
        {
            var field = EnsureField(view);
            HideInlineInput(view);
            if (view.Summary != null)
            {
                view.Summary.Content.Release();
                view.Summary.style.display = DisplayStyle.None;
            }

            field.style.display = DisplayStyle.Flex;
            if (view.HasFieldBinding && view.FieldPath == property.propertyPath && view.FieldType == property.propertyType)
            {
                return;
            }

            field.Unbind();
            field.label = string.Empty;
            field.BindProperty(property);
            view.HasFieldBinding = true;
            view.FieldPath = property.propertyPath;
            view.FieldType = property.propertyType;
        }

        private static PropertyField EnsureField(CellView view)
        {
            if (view.Field != null)
            {
                return view.Field;
            }

            var field = new PropertyField();
            if (view.DelayedInput)
            {
                field.RegisterCallback<GeometryChangedEvent>(_ => ConfigureDelayedFields(field));
            }

            // 입력 필드는 요약 행보다 앞에 둔다.
            view.Insert(0, field);
            view.Field = field;
            return field;
        }

        private static void ReleaseField(CellView view)
        {
            if (!view.HasFieldBinding)
            {
                return;
            }

            view.Field.Unbind();
            view.HasFieldBinding = false;
            view.FieldPath = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 입력이 끝났을 때만 확정하도록 숫자 필드를 지연 입력으로 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ConfigureDelayedFields(PropertyField field)
        {
            field.Query<IntegerField>().ForEach(input => input.isDelayed = true);
            field.Query<LongField>().ForEach(input => input.isDelayed = true);
            field.Query<FloatField>().ForEach(input => input.isDelayed = true);
            field.Query<DoubleField>().ForEach(input => input.isDelayed = true);
        }

    #endregion

    #region 초안 입력칸

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 초안 열 값을 바인딩하지 않은 TableGrid 입력 셀 뷰에 보인다.
        /// <br/> 열 형식을 입력칸으로 고칠 수 없거나 속성에서 값을 읽을 수 없으면 false다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static bool ShowInlineInput(CellView view, CollectionCell cell, SerializedProperty property)
        {
            var column = cell.Column;
            var inputView = CollectionInlineInput.FindView(column);
            if (inputView == null || !CollectionInlineInput.TryRead(property, column.ValueType, out var value))
            {
                HideInlineInput(view);
                return false;
            }

            // 열 형식은 고정이므로 입력칸은 한 번 만들고 행이 바뀌면 값만 다시 보인다.
            if (view.InlineInput == null || view.InlineView != inputView)
            {
                view.InlineInput?.RemoveFromHierarchy();
                view.InlineInput = inputView.MakeCell();
                view.InlineInput.AddToClassList(CollectionInlineInput.ClassName);
                view.InlineView = inputView;
                view.Insert(0, view.InlineInput);
            }

            // 열 값 형식은 표 열(object)이 아니라 Collection 열이 안다. 입력칸이 후보·오브젝트 형식을 맞추게 문맥에 준다.
            var context = cell.GridContext.Value.WithValueType(column.ValueType);
            inputView.BindBoxed(view.InlineInput, value, in context);
            CollectionInlineInput.FitScope(view.InlineInput, property);

            ReleaseField(view);
            if (view.Field != null)
            {
                view.Field.style.display = DisplayStyle.None;
            }

            if (view.Summary != null)
            {
                view.Summary.Content.Release();
                view.Summary.style.display = DisplayStyle.None;
            }

            view.InlineInput.style.display = DisplayStyle.Flex;
            return true;
        }

        private static void HideInlineInput(CellView view)
        {
            if (view.InlineInput == null)
            {
                return;
            }

            view.InlineView.Unbind(view.InlineInput);
            view.InlineInput.style.display = DisplayStyle.None;
        }

    #endregion

    #region 요약 행

        private static void ShowSummary(CellView view, string text, string tooltip)
        {
            var summary = ShowSummaryRow(view);
            summary.Content.Release();
            summary.Text.style.display = DisplayStyle.Flex;
            summary.Text.text = text;
            summary.Text.tooltip = tooltip;
        }

        private static void ShowCellView(CellView view, CollectionCellView cellView, SerializedProperty property)
        {
            var summary = ShowSummaryRow(view);
            summary.Text.style.display = DisplayStyle.None;
            summary.Content.Show(cellView, property);
        }

        private static SummaryRow ShowSummaryRow(CellView view)
        {
            if (view.Field != null)
            {
                view.Field.style.display = DisplayStyle.None;
            }

            HideInlineInput(view);
            view.Summary ??= CreateSummaryRow(view);
            view.Summary.style.display = DisplayStyle.Flex;
            return view.Summary;
        }

        private static SummaryRow CreateSummaryRow(CellView view)
        {
            var summary = new SummaryRow(view);
            view.Add(summary);
            return summary;
        }

    #endregion

    #region 요약과 값 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 뷰의 검색 문자열이 있으면 그것을, 없으면 셀 요약 문자열을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string GetSummary(SerializedProperty property, CollectionColumn column)
        {
            return CollectionCellViews.Find(property, column)?.GetSearchText(property)
                ?? CollectionCellSummary.GetText(property);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 drawer는 새 값을 만들지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public ICollectionCellValueCreator ValueCreator => null;

    #endregion

    }
}
