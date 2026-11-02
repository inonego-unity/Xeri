/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionCellRenderer.cs
수정일 : 2026-10-29

# 설명
Collection 셀의 요약·상세보기·SerializeReference 작업 메뉴를 공유한다.
ReadOnly에서는 수정 명령만 차단하고 UI와 콜백은 셀 수명 동안 재사용한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 셀 UI의 수명과 현재 행의 데이터 연결을 분리한다.
    /// </summary>
    // ============================================================
    internal sealed class CollectionCellRenderer : VisualElement
    {

    #region 필드

        private readonly ICollectionEditorModel model;
        private readonly string columnID;
        private readonly bool isReadOnly;
        private readonly Action<int> onKeyEditApplied;
        private readonly PropertyField field;
        private readonly VisualElement summaryRow;
        private readonly Label summary;
        private readonly ToolbarButton editButton;
        private readonly ToolbarButton actionButton;
        private SerializedPropertyLocator locator;
        private int sourceIndex = -1;
        private int bindingVersion;
        private bool hasFieldBinding;
        private string fieldPath;
        private SerializedPropertyType fieldType;

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막 컬럼의 행 명령과 함께 배치할 셀 동작 툴바이다.
        /// </summary>
        // ------------------------------------------------------------
        public Toolbar ActionToolbar => toolbar;
        private readonly Toolbar toolbar;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 셀의 데이터 연결이 유효한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsCurrent => TryResolve(out _);

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// CollectionCellRenderer 인스턴스를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionCellRenderer
        (
            ICollectionEditorModel model,
            string columnID,
            Action<int> onKeyEditApplied,
            bool isReadOnly = false
        )
        {
            this.model = model;
            this.columnID = columnID;
            this.onKeyEditApplied = onKeyEditApplied;
            this.isReadOnly = isReadOnly;
            AddToClassList("xeri-collection__cell-content");
            field = new PropertyField();
            if (columnID == "Priority")
            {
                field.RegisterCallback<GeometryChangedEvent>(_ => ConfigureDelayedFields());
            }

            Add(field);

            summaryRow = new VisualElement();
            summaryRow.AddToClassList("xeri-collection__reference-row");
            summary = new Label();
            summary.AddToClassList("xeri-collection__data-label");
            summaryRow.Add(summary);
            editButton = new ToolbarButton(Edit)
            {
                iconImage = XeriIcon.LoadBackground("pencil"),
                tooltip = "상세 편집",
            };
            editButton.AddToClassList("xeri-collection__icon-button");
            editButton.AddToClassList("xeri-collection__cell-action");

            actionButton = new ToolbarButton(ShowActions)
            {
                iconImage = XeriIcon.LoadBackground("ellipsis"),
                tooltip = "작업",
            };
            actionButton.AddToClassList("xeri-collection__icon-button");
            actionButton.AddToClassList("xeri-collection__cell-action");
            toolbar = new Toolbar();
            toolbar.AddToClassList("xeri-collection__cell-toolbar");
            toolbar.Add(editButton);
            toolbar.Add(actionButton);
            summaryRow.Add(toolbar);
            summaryRow.RegisterCallback<PointerDownEvent>(HandleDoubleClick);
            Add(summaryRow);
        }

    #endregion

    #region 바인딩과 표시

        // ------------------------------------------------------------
        /// <summary>
        /// 셀을 지정한 행의 데이터에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Bind(int index)
        {
            bindingVersion++;
            sourceIndex = index;
            RefreshDisplay();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이전 행의 연결을 끝내되 다음 행에서 사용할 UI는 보존한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ReleaseBinding()
        {
            bindingVersion++;
            field.Unbind();
            hasFieldBinding = false;
            fieldPath = null;
            locator = null;
            sourceIndex = -1;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 입력 필드의 바인딩은 유지하며 요약, 편집 정책과 현재 경로를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public void RefreshDisplay()
        {
            if (sourceIndex < 0 || sourceIndex >= model.Count)
            {
                return;
            }

            var property = model.GetCellProperty(sourceIndex, columnID);
            if (property == null)
            {
                ReleaseBinding();
                field.style.display = DisplayStyle.None;
                summaryRow.style.display = DisplayStyle.Flex;
                summary.text = "필드 없음";
                toolbar.style.display = DisplayStyle.None;
                return;
            }

            locator = new SerializedPropertyLocator(property);
            var mode = model.GetCellEditMode(sourceIndex, columnID);
            var managed = property.propertyType == SerializedPropertyType.ManagedReference;
            var readOnlyCell = isReadOnly || mode == CollectionCellEditMode.ReadOnly;
            var inline = !readOnlyCell && mode == CollectionCellEditMode.Default && !IsComplex(property);
            field.style.display = inline ? DisplayStyle.Flex : DisplayStyle.None;
            summaryRow.style.display = inline ? DisplayStyle.None : DisplayStyle.Flex;
            if (inline)
            {
                toolbar.style.display = DisplayStyle.None;
                if (!hasFieldBinding || fieldPath != property.propertyPath || fieldType != property.propertyType)
                {
                    field.Unbind();
                    field.label = string.Empty;
                    field.BindProperty(property);
                    fieldPath = property.propertyPath;
                    fieldType = property.propertyType;
                    hasFieldBinding = true;
                }

                return;
            }

            if (hasFieldBinding)
            {
                field.Unbind();
                hasFieldBinding = false;
                fieldPath = null;
            }

            summary.text = CollectionCellSummary.GetText(property);
            summary.tooltip = CollectionCellSummary.GetDisplayText(property);
            if (managed)
            {
                var type = property.managedReferenceValue?.GetType();
                summary.tooltip = type == null ? "형식 없음" : type.FullName;
            }

            var declaredType = managed ? ManagedReferenceTypeCatalog.GetDeclaredReferenceType(property) : null;
            actionButton.style.display = declaredType != null ? DisplayStyle.Flex : DisplayStyle.None;
            editButton.style.display = DisplayStyle.Flex;
            editButton.SetEnabled(readOnlyCell || !managed || property.managedReferenceValue != null);
            editButton.iconImage = XeriIcon.LoadBackground(readOnlyCell ? "eye" : "pencil");
            if (readOnlyCell)
            {
                editButton.tooltip = "상세 보기";
            }
            else if (mode == CollectionCellEditMode.ReplaceOnly)
            {
                editButton.tooltip = columnID == "order" ? "순서 편집" : "키 편집";
            }
            else
            {
                editButton.tooltip = "상세 편집";
            }

            toolbar.style.display = DisplayStyle.Flex;
        }

        private static bool IsComplex(SerializedProperty property)
        {
            return property.isArray && property.propertyType != SerializedPropertyType.String
                || property.propertyType == SerializedPropertyType.Generic
                || property.propertyType == SerializedPropertyType.ManagedReference;
        }

        private void ConfigureDelayedFields()
        {
            field.Query<IntegerField>().ForEach(input => input.isDelayed = true);
            field.Query<LongField>().ForEach(input => input.isDelayed = true);
            field.Query<FloatField>().ForEach(input => input.isDelayed = true);
            field.Query<DoubleField>().ForEach(input => input.isDelayed = true);
        }

    #endregion

    #region 편집 명령

        private bool TryResolve(out SerializedProperty property)
        {
            property = null;
            return sourceIndex >= 0 && locator != null
                && locator.TryResolve(model.SerializedObject, out property, out _);
        }

        private void Edit()
        {
            if (TryResolve(out var property))
            {
                OpenEditor
                (
                    model, sourceIndex, property,
                    model.GetCellEditMode(sourceIndex, columnID),
                    onKeyEditApplied,
                    isReadOnly
                );
            }
        }

        private void ChooseType()
        {
            if (!TryResolve(out var property))
            {
                return;
            }

            var mode = model.GetCellEditMode(sourceIndex, columnID);
            if (isReadOnly || mode == CollectionCellEditMode.ReadOnly)
            {
                return;
            }

            var declaredType = ManagedReferenceTypeCatalog.GetDeclaredReferenceType(property);
            if (mode == CollectionCellEditMode.ReplaceOnly)
            {
                if (model is XDictionaryEditorModel dictionary)
                {
                    var index = sourceIndex;
                    var version = bindingVersion;
                    ManagedReferenceEditorAction.ShowCreationTypePicker
                    (
                        declaredType, actionButton, type =>
                        {
                            if (version == bindingVersion && TryResolve(out _))
                            {
                                CollectionStructuralEditorWindow.Open(dictionary, index, type, onKeyEditApplied);
                            }
                        }
                    );
                }
                else
                {
                    Edit();
                }

                return;
            }

            var path = property.propertyPath;
            var currentVersion = bindingVersion;
            ManagedReferenceEditorAction.ShowTypePicker
            (
                property, declaredType, actionButton,
                () => model.CommitChanges(path),
                () => currentVersion == bindingVersion && TryResolve(out _)
            );
        }

        private void ShowActions()
        {
            if (!TryResolve(out var property))
            {
                return;
            }

            var path = property.propertyPath;
            var version = bindingVersion;
            var mode = model.GetCellEditMode(sourceIndex, columnID);
            ManagedReferenceEditorAction.ShowActions
            (
                property, ManagedReferenceTypeCatalog.GetDeclaredReferenceType(property),
                () => model.CommitChanges(path),
                chooseType: ChooseType,
                canApply: () => version == bindingVersion && TryResolve(out _),
                menuOwner: actionButton,
                canChangeReference: !isReadOnly && mode == CollectionCellEditMode.Default,
                canChooseType: !isReadOnly && mode != CollectionCellEditMode.ReadOnly
            );
        }

        private void HandleDoubleClick(PointerDownEvent evt)
        {
            if (evt.button == 0 && evt.clickCount == 2)
            {
                Edit();
                evt.StopPropagation();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 셀의 편집 정책에 맞는 상세 창으로 진입한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool OpenEditor
        (
            ICollectionEditorModel model,
            int sourceIndex,
            SerializedProperty cellProperty,
            CollectionCellEditMode editMode,
            Action<int> onKeyEditApplied = null,
            bool isReadOnly = false
        )
        {
            if (model == null || cellProperty == null || sourceIndex < 0
                || sourceIndex >= model.Count)
            {
                return false;
            }

            if (isReadOnly || editMode == CollectionCellEditMode.ReadOnly)
            {
                CollectionValueEditorWindow.Open(model, cellProperty, true);
                return true;
            }

            if (editMode == CollectionCellEditMode.ReplaceOnly)
            {
                if (model is XDictionaryEditorModel dictionary
                    && cellProperty.propertyType == SerializedPropertyType.ManagedReference)
                {
                    if (cellProperty.managedReferenceValue == null)
                    {
                        return false;
                    }

                    CollectionStructuralEditorWindow.Open
                    (
                        dictionary, sourceIndex, cellProperty.managedReferenceValue.GetType(), onKeyEditApplied
                    );
                }
                else
                {
                    CollectionStructuralEditorWindow.Open(model, sourceIndex, cellProperty.name, onKeyEditApplied);
                }

                return true;
            }

            if (!IsComplex(cellProperty)
                || cellProperty.propertyType == SerializedPropertyType.ManagedReference
                    && cellProperty.managedReferenceValue == null)
            {
                return false;
            }

            CollectionValueEditorWindow.Open(model, cellProperty);
            return true;
        }

    #endregion

    }
}
