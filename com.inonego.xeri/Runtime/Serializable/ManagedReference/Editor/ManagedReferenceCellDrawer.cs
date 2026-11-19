/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ManagedReferenceCellDrawer.cs
수정일 : 2026-11-19

# 설명
컬렉션 Editor에서 SerializeReference 열의 셀을 그리고 새 참조 값을 만든다.
요약(또는 현재 값 형식에 등록된 셀 뷰)·형식 툴팁·상세/작업 버튼·작업 메뉴와 형식 선택·생성·복제가 모두 여기서 정해지므로
컬렉션 Editor의 표와 기본 drawer는 SerializeReference를 알지 못한다.

# 특이사항, 제약사항
형식 바꾸기는 새 값을 만들어 셀 문맥에 교체를 요청한다. 즉시 반영할지 초안 창을 열지는 표가 열 정의로 정한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// SerializeReference 셀 drawer.
    /// </summary>
    // ============================================================
    internal sealed class ManagedReferenceCellDrawer : ICollectionCellDrawer, ICollectionCellValueCreator
    {

    #region 내부 타입

        // ============================================================
        /// <summary>
        /// 재사용하는 셀 UI와 현재 연결 상태.
        /// </summary>
        // ============================================================
        private sealed class CellView : VisualElement
        {
            internal readonly Label Summary = new();
            internal readonly CollectionCellContent Content = new();
            internal readonly Toolbar Toolbar = new();
            internal ToolbarButton EditButton;
            internal ToolbarButton ActionButton;
            internal CollectionCell? Cell;
        }

    #endregion

    #region 셀 UI

        // ------------------------------------------------------------
        /// <summary>
        /// SerializeReference 열을 그린다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Matches(CollectionColumn column)
        {
            return column.IsManagedReference;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 요약과 상세·작업 버튼을 가진 셀 UI를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement CreateElement(CollectionColumn column)
        {
            var view = new CellView();
            view.AddToClassList("xeri-collection__cell-content");
            view.AddToClassList("xeri-collection__reference-row");
            view.Summary.AddToClassList("xeri-collection__data-label");
            view.Add(view.Summary);
            view.Add(view.Content);

            view.EditButton = CollectionIconButton.Create(() => view.Cell?.OpenDetail());
            view.EditButton.AddToClassList("xeri-collection__cell-action");
            view.ActionButton = CollectionIconButton.Create(() => ShowActions(view), "ellipsis", "작업");
            view.ActionButton.AddToClassList("xeri-collection__cell-action");
            view.Toolbar.AddToClassList("xeri-collection__cell-toolbar");
            view.Toolbar.Add(view.EditButton);
            view.Toolbar.Add(view.ActionButton);
            view.Add(view.Toolbar);
            return view;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 참조의 요약과 버튼 상태를 셀 문맥에 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        public void Bind(VisualElement element, CollectionCell cell)
        {
            var view = (CellView)element;
            view.Cell = cell;
            var property = cell.Property;
            if (property == null)
            {
                ShowSummary(view, "필드 없음", null);
                view.Toolbar.style.display = DisplayStyle.None;
                return;
            }

            var valueType = property.managedReferenceValue?.GetType();
            var editable = cell.CanEdit;
            var cellView = CollectionCellViews.Find(valueType);
            if (cellView != null)
            {
                view.Summary.style.display = DisplayStyle.None;
                view.Content.Show(cellView, property);
                view.Content.tooltip = valueType.FullName;
            }
            else
            {
                ShowSummary(view, GetSummary(property), valueType == null ? "형식 없음" : valueType.FullName);
            }

            // 값이 없으면 고칠 내용이 없으므로 상세 편집은 읽기 전용 보기에서만 연다.
            view.EditButton.SetEnabled(!editable || valueType != null);
            view.EditButton.iconImage = XeriIcon.LoadBackground(editable ? "pencil" : "eye");
            view.EditButton.tooltip = !editable
                ? "상세 보기"
                : cell.Column.Edit == CollectionCellEdit.Draft ? $"{cell.Column.Header} 편집" : "상세 편집";
            view.ActionButton.style.display = GetDeclaredType(property, cell.Column) != null
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            view.Toolbar.style.display = DisplayStyle.Flex;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이전 행의 연결을 끊되 UI는 다음 행에서 재사용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Unbind(VisualElement element)
        {
            var view = (CellView)element;
            view.Content.Release();
            view.Cell = null;
        }

        private static void ShowSummary(CellView view, string text, string tooltip)
        {
            view.Content.Release();
            view.Summary.style.display = DisplayStyle.Flex;
            view.Summary.text = text;
            view.Summary.tooltip = tooltip;
        }

        private static Type GetDeclaredType(SerializedProperty property, CollectionColumn column)
        {
            return ManagedReferenceTypeCatalog.GetDeclaredReferenceType(property) ?? column.ValueType;
        }

    #endregion

    #region 작업 메뉴

        // ------------------------------------------------------------
        /// <summary>
        /// 형식 선택, null 지정, 값·링크 복사 메뉴를 연다.
        /// </summary>
        // ------------------------------------------------------------
        private void ShowActions(CellView view)
        {
            if (view.Cell is not CollectionCell cell || !cell.TryResolve(out var property))
            {
                return;
            }

            var items = ManagedReferenceEditorAction.CreateMenuItems
            (
                property, GetDeclaredType(property, cell.Column),
                cell.Commit,
                () => ChooseType(view.ActionButton, cell),
                () => cell.TryResolve(out _),
                canChangeReference: cell.CanEdit && cell.Column.Edit != CollectionCellEdit.Draft,
                canChooseType: cell.CanEdit
            );
            new XeriContextMenu(view.ActionButton).OpenBelow(items);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 형식을 고르고 새 값을 만들어 셀 교체를 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ChooseType(VisualElement anchor, CollectionCell cell)
        {
            if (!cell.CanEdit || !cell.TryResolve(out var property))
            {
                return;
            }

            PickType
            (
                GetDeclaredType(property, cell.Column), anchor,
                type =>
                {
                    // 선택기를 연 사이 행이 바뀌었으면 다른 셀에 넣지 않는다.
                    if (!cell.TryResolve(out _))
                    {
                        return;
                    }

                    if (TryCreate(type, out var value, out var reason))
                    {
                        cell.Replace(value);
                        return;
                    }

                    Debug.LogWarning(reason);
                }
            );
        }

    #endregion

    #region 요약

        // ----------------------------------------------------------------------
        /// <summary>
        /// 셀 뷰의 검색 문자열이 있으면 그것을, 없으면 참조 값의 요약 문자열을 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public string GetSummary(SerializedProperty property, CollectionColumn column)
        {
            return CollectionCellViews.Find(property, column)?.GetSearchText(property) ?? GetSummary(property);
        }

        private static string GetSummary(SerializedProperty property)
        {
            var value = property?.managedReferenceValue;
            if (value == null)
            {
                return "None";
            }

            // 형식 이름은 SerializeReference 피커와 같은 표기(카탈로그)로 보인다.
            return CollectionCellSummary.TryBuildAutomaticSummary(property, out var summary)
                ? summary
                : ManagedReferenceTypeCatalog.GetDisplayName(value.GetType());
        }

    #endregion

    #region 값 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 이 drawer가 새 참조 값을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public ICollectionCellValueCreator ValueCreator => this;

        // ------------------------------------------------------------
        /// <summary>
        /// 선언 형식에 대입 가능한 구현 형식을 고르게 한다.
        /// </summary>
        // ------------------------------------------------------------
        public void PickType(Type declaredType, VisualElement anchor, Action<Type> onPicked)
        {
            ManagedReferenceEditorAction.ShowCreationTypePicker(declaredType, anchor, onPicked);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 새 행에 넣을 형식을 정한다.
        /// <br/> 선언 형식을 바로 만들 수 있으면 선택기 없이 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        public void PickTypeForNew
        (
            Type declaredType, VisualElement anchor, Action<Type> onPicked, Action<string> onFailed
        )
        {
            if (ManagedReferenceTypeCatalog.CanCreate(declaredType))
            {
                onPicked(declaredType);
                return;
            }

            if (ManagedReferenceTypeCatalog.GetCreationCandidates(declaredType).Count == 0)
            {
                onFailed("생성 가능한 타입이 없습니다. 행을 추가하지 않았습니다.");
                return;
            }

            PickType(declaredType, anchor, onPicked);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 고른 형식의 새 참조 값을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryCreate(Type type, out object value, out string reason)
        {
            return ManagedReferenceEditorAction.TryCreateReference(type, out value, out reason);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀의 현재 참조 값을 직렬화해 편집용 복제본을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public object CreateEditCopy(SerializedProperty property)
        {
            var current = property?.propertyType == SerializedPropertyType.ManagedReference
                ? property.managedReferenceValue
                : null;
            if (current == null)
            {
                return null;
            }

            var serialized = ManagedReferenceValueSerializer.Serialize(current);
            return ManagedReferenceValueSerializer.Deserialize(current.GetType(), serialized);
        }

    #endregion

    }
}
