/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionStructuralEditorWindow.cs
수정일 : 2026-11-01

# 설명
Dictionary Key와 XOrdered Key/Order를 원본과 분리된 임시 편집 버퍼에서 편집하는 비모달 창.
선택한 필드의 PropertyDrawer를 표시하고 검증 후 Apply에서만 원본에 반영한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Reflection;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ======================================================================
    /// <summary>
    /// Key와 Order를 EditBuffer에서 수정하고 적용 시 확정하는 EditorWindow.
    /// </summary>
    // ======================================================================
    internal sealed class CollectionStructuralEditorWindow : CollectionCellEditorWindowBase
    {

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// 원본에 영향을 주지 않는 임시 SerializedProperty 보관소.
        /// </summary>
        // ============================================================
        private sealed class EditBuffer : ScriptableObject
        {
            [SerializeField] public string Text;
            [SerializeField] public int Integer;
            [SerializeField] public long Long;
            [SerializeField] public float Float;
            [SerializeField] public double Double;
            [SerializeField] public bool Boolean;
            [SerializeField] public UnityEngine.Object Object;
            [SerializeField] public Color Color;
            [SerializeField] public Vector2 Vector2;
            [SerializeField] public Vector3 Vector3;
            [SerializeField] public Vector4 Vector4;
            [SerializeField] public Vector2Int Vector2Int;
            [SerializeField] public Vector3Int Vector3Int;
            [SerializeField] public Quaternion Quaternion;
            [SerializeField] public Rect Rect;
            [SerializeField] public Bounds Bounds;
            [SerializeReference] public object Reference;
        }

    #endregion

    #region 필드

        private ICollectionEditorModel model;
        private SerializedObject targetObject;
        private SerializedObject editBufferObject;
        private SerializedPropertyLocator locator;
        private SerializedPropertySnapshot originalSnapshot;
        private EditBuffer editBuffer;
        private int? sourceIndex;
        private string columnID;
        private string editFieldName;
        private Enum enumEditValue;
        private Action<int> onApplied;
        private Button applyButton;
        private bool isShown;

    #endregion

    #region 창 생성과 종료

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 또는 새 Dictionary ManagedReference Key의 편집창을 연다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Open
        (
            XDictionaryEditorModel sourceModel,
            int? sourceIndex,
            Type type,
            Action<int> onApplied = null
        )
        {
            if (type == null)
            {
                return;
            }

            var existing = CollectionEditWindowCoordinator.Find<CollectionStructuralEditorWindow>();
            if (existing != null
                && existing.IsSameTarget(sourceModel, sourceIndex, "Key")
                && existing.editFieldName == nameof(EditBuffer.Reference)
                && existing.editBuffer?.Reference?.GetType() == type)
            {
                existing.onApplied = onApplied;
                CollectionEditWindowCoordinator.Activate<CollectionStructuralEditorWindow>();
                existing.Focus();
                return;
            }

            var window = CreateWindow(sourceModel, sourceIndex, "Key", onApplied);
            if (window == null)
            {
                return;
            }

            var current = sourceIndex.HasValue
                ? window.model.GetCellProperty(sourceIndex.Value, "Key").managedReferenceValue
                : null;
            if (current != null && current.GetType() == type)
            {
                window.editBuffer.Reference = ManagedReferenceValueSerializer.Deserialize
                (
                    type, ManagedReferenceValueSerializer.Serialize(current)
                );
            }
            else if (!ManagedReferenceEditorAction.TryCreateReference(type, out window.editBuffer.Reference, out var reason))
            {
                Debug.LogWarning(reason);
                window.Close();
                return;
            }

            window.editFieldName = nameof(EditBuffer.Reference);
            window.editBufferObject.Update();
            window.ShowEditor();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 기존 Dictionary Key 또는 XOrdered Key/Order를 임시 버퍼에서 편집한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void Open
        (
            ICollectionEditorModel sourceModel,
            int sourceIndex,
            string columnID,
            Action<int> onApplied = null
        )
        {
            var existing = CollectionEditWindowCoordinator.Find<CollectionStructuralEditorWindow>();
            if (existing != null && existing.IsSameTarget(sourceModel, sourceIndex, columnID))
            {
                existing.onApplied = onApplied;
                CollectionEditWindowCoordinator.Activate<CollectionStructuralEditorWindow>();
                existing.Focus();
                return;
            }

            var window = CreateWindow(sourceModel, sourceIndex, columnID, onApplied);
            if (window == null)
            {
                return;
            }

            var property = window.model.GetCellProperty(sourceIndex, columnID);
            if (!window.InitializeEditBuffer(property))
            {
                Debug.LogWarning
                (
                    $"[CollectionStructuralEditor] {property.propertyType} 형식은 현재 지연 편집을 지원하지 않습니다."
                );
                window.Close();
                return;
            }

            window.ShowEditor();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 위치와 독립 SerializedObject를 캡처한다.
        /// </summary>
        // ------------------------------------------------------------
        private static CollectionStructuralEditorWindow CreateWindow
        (
            ICollectionEditorModel sourceModel,
            int? sourceIndex,
            string columnID,
            Action<int> onApplied
        )
        {
            var targetObject = new SerializedObject(sourceModel.SerializedObject.targetObject);
            ICollectionEditorModel model;
            SerializedPropertyLocator locator;
            SerializedPropertySnapshot originalSnapshot = null;
            try
            {
                model = sourceModel.CreateFor
                (
                    targetObject.FindProperty(sourceModel.CollectionPropertyPath)
                );
                var property = sourceIndex.HasValue
                    ? model.GetCellProperty(sourceIndex.Value, columnID)
                    : model.GetCollectionProperty();
                locator = new SerializedPropertyLocator(property);
                if (sourceIndex.HasValue && !SerializedPropertySnapshot.TryCreate
                (
                    property, out originalSnapshot, out var reason
                ))
                {
                    throw new InvalidOperationException(reason);
                }
            }
            catch (Exception exception)
            {
                targetObject.Dispose();
                Debug.LogWarning
                (
                    $"[CollectionStructuralEditor] 원본 필드 캡처 실패: {exception.Message}"
                );
                return null;
            }

            var window = CollectionEditWindowCoordinator.Acquire<CollectionStructuralEditorWindow>();
            window.ReleaseEditSession();
            window.targetObject = targetObject;
            window.model = model;
            window.locator = locator;
            window.originalSnapshot = originalSnapshot;
            window.sourceIndex = sourceIndex;
            window.columnID = columnID;
            window.onApplied = onApplied;
            window.editBuffer = CreateInstance<EditBuffer>();
            window.editBuffer.hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable;
            window.editBufferObject = new SerializedObject(window.editBuffer);
            return window;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 대상과 적용 전 원본 값이 유지되는지 검사한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool TryResolveOriginal(out SerializedProperty property, out string reason)
        {
            property = null;
            reason = "편집 대상을 찾을 수 없습니다.";
            if (locator == null || targetObject == null
                || !locator.TryResolve(targetObject, out property, out reason))
            {
                return false;
            }

            if (originalSnapshot == null || originalSnapshot.Matches(property))
            {
                return true;
            }

            property = null;
            reason = "원본 키나 순서가 바뀌었습니다. 편집창을 다시 여세요.";
            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 필드를 다시 열 때 적용 전 편집 내용을 보존할지 판단한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool IsSameTarget
        (
            ICollectionEditorModel sourceModel, int? index, string field
        )
        {
            return isShown && targetObject != null && locator != null
                && targetObject.targetObject == sourceModel.SerializedObject.targetObject
                && model.CollectionPropertyPath == sourceModel.CollectionPropertyPath
                && sourceIndex == index
                && string.Equals(columnID, field, StringComparison.Ordinal)
                && TryResolveOriginal(out _, out _);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 편집 대상으로 전환할 때 이전 임시 버퍼와 바인딩을 폐기한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseEditSession()
        {
            UnregisterRootCallbacks();
            ReleaseEditorLayout();
            applyButton = null;
            editBufferObject?.Dispose();
            editBufferObject = null;
            targetObject?.Dispose();
            targetObject = null;
            if (editBuffer != null)
            {
                DestroyImmediate(editBuffer);
                editBuffer = null;
            }

            locator = null;
            originalSnapshot = null;
            enumEditValue = null;
            editFieldName = null;
            model = null;
            onApplied = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 필드와 직렬화 타입이 호환되는 편집 버퍼를 준비한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool InitializeEditBuffer(SerializedProperty property)
        {
            var row = model.GetRowProperty(sourceIndex.Value);
            var rowInstance = SerializedPropertyHelper.GetTargetObject(row);
            var fieldType = rowInstance?.GetType().GetField
            (
                columnID, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            )?.FieldType;
            if (fieldType == null)
            {
                return false;
            }

            if (property.propertyType == SerializedPropertyType.Enum)
            {
                enumEditValue = (Enum)Enum.ToObject(fieldType, property.intValue);
                return true;
            }

            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                var current = property.managedReferenceValue;
                if (current == null)
                {
                    return false;
                }

                editBuffer.Reference = ManagedReferenceValueSerializer.Deserialize
                (
                    current.GetType(), ManagedReferenceValueSerializer.Serialize(current)
                );
                editFieldName = nameof(EditBuffer.Reference);
                editBufferObject.Update();
                return true;
            }

            editFieldName = property.propertyType switch
            {
                SerializedPropertyType.String => nameof(EditBuffer.Text),
                SerializedPropertyType.Integer when fieldType == typeof(int) => nameof(EditBuffer.Integer),
                SerializedPropertyType.Integer when fieldType == typeof(long) => nameof(EditBuffer.Long),
                SerializedPropertyType.Float when fieldType == typeof(float) => nameof(EditBuffer.Float),
                SerializedPropertyType.Float when fieldType == typeof(double) => nameof(EditBuffer.Double),
                SerializedPropertyType.Boolean => nameof(EditBuffer.Boolean),
                SerializedPropertyType.ObjectReference => nameof(EditBuffer.Object),
                SerializedPropertyType.Color => nameof(EditBuffer.Color),
                SerializedPropertyType.Vector2 => nameof(EditBuffer.Vector2),
                SerializedPropertyType.Vector3 => nameof(EditBuffer.Vector3),
                SerializedPropertyType.Vector4 => nameof(EditBuffer.Vector4),
                SerializedPropertyType.Vector2Int => nameof(EditBuffer.Vector2Int),
                SerializedPropertyType.Vector3Int => nameof(EditBuffer.Vector3Int),
                SerializedPropertyType.Quaternion => nameof(EditBuffer.Quaternion),
                SerializedPropertyType.Rect => nameof(EditBuffer.Rect),
                SerializedPropertyType.Bounds => nameof(EditBuffer.Bounds),
                _ => null,
            };

            if (editFieldName == null)
            {
                return false;
            }

            var destination = editBufferObject.FindProperty(editFieldName);
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                destination.objectReferenceValue = property.objectReferenceValue;
            }
            else
            {
                destination.boxedValue = property.boxedValue;
            }

            editBufferObject.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 공통 레이아웃을 사용하고 편집 대상에 따라 버튼 문구를 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ShowEditor()
        {
            var title = sourceIndex.HasValue ? "수정" : "추가";
            titleContent = new GUIContent($"{model.Label} · {columnID} {title}");
            minSize = new Vector2(420f, 260f);
            CollectionEditWindowCoordinator.Activate<CollectionStructuralEditorWindow>();
            if (isShown)
            {
                CreateGUI();
                Focus();
            }
            else
            {
                isShown = true;
                ShowUtility();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Undo/Redo 발생 후 오래된 편집 버퍼를 적용하지 못하도록 구독한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnEnable()
        {
            Undo.undoRedoPerformed += HandleUndoRedo;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Undo/Redo는 원본의 항목 식별을 바꿀 수 있으므로 창을 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleUndoRedo()
        {
            Close();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 비모달 편집창으로 돌아올 때 외부 변경에 따른 적용 가능 여부를 갱신한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void OnFocus()
        {
            if (applyButton != null)
            {
                RefreshValidation();
            }
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
            ReleaseEditSession();
        }

    #endregion

    #region 표시와 검증

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 버퍼의 필드와 적용·취소 버튼을 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CreateGUI()
        {
            UnregisterRootCallbacks();
            ReleaseEditorLayout();
            if (editBufferObject == null)
            {
                rootVisualElement.Add(new HelpBox("인스펙터에서 편집창을 다시 여세요.", HelpBoxMessageType.Info));
                return;
            }

            var scroll = BuildEditorLayout
            (
                columnID + " 편집",
                "적용하면 변경됩니다."
            );
            if (enumEditValue != null)
            {
                var enumField = new EnumField(columnID, enumEditValue);
                enumField.RegisterValueChangedCallback
                (
                    evt =>
                    {
                        enumEditValue = evt.newValue;
                        RefreshValidation();
                    }
                );
                scroll.Add(enumField);
            }
            else
            {
                var property = editBufferObject.FindProperty(editFieldName);
                property.isExpanded = true;
                scroll.Add(new PropertyField(property.Copy(), columnID));
                rootVisualElement.Bind(editBufferObject);
            }

            rootVisualElement.Q<Label>("editor-footnote").text = "취소하면 변경되지 않습니다.";

            applyButton = rootVisualElement.Q<Button>("editor-apply");
            applyButton.text = sourceIndex.HasValue ? "적용" : "추가";
            applyButton.clicked += Apply;

            var cancelButton = rootVisualElement.Q<Button>("editor-cancel");
            cancelButton.text = "취소";
            cancelButton.clicked += Close;

            rootVisualElement.RegisterCallback<SerializedPropertyChangeEvent>(HandleEditBufferChanged);
            rootVisualElement.RegisterCallback<KeyDownEvent>(HandleKeyDown);
            RefreshValidation();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 창 재구성과 종료에서 이벤트 연결을 대칭으로 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        private void UnregisterRootCallbacks()
        {
            rootVisualElement.UnregisterCallback<SerializedPropertyChangeEvent>(HandleEditBufferChanged);
            rootVisualElement.UnregisterCallback<KeyDownEvent>(HandleKeyDown);
        }

        private void HandleEditBufferChanged(SerializedPropertyChangeEvent _)
        {
            RefreshValidation();
        }

        private void HandleKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                Close();
                evt.StopPropagation();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 임시 편집 값을 실제 Key/Order 타입으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private object GetEditBufferValue()
        {
            if (enumEditValue != null)
            {
                return enumEditValue;
            }

            return SerializedPropertyHelper.GetValue(editBufferObject.FindProperty(editFieldName));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 행의 유효성과 Key 중복을 확인해 Apply 가능 여부를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshValidation()
        {
            if (applyButton == null || targetObject == null)
            {
                return;
            }

            // 원본에는 적용하지 않고 임시 값만 검증한다.
            var valid = TryResolveOriginal(out _, out var reason);
            var result = valid
                ? model.Validate
                (
                    CollectionValidationContext.ForCandidate
                    (
                        sourceIndex, columnID, GetEditBufferValue()
                    )
                )
                : new CollectionValidationResult();

            if (!valid)
            {
                result.Add
                (
                    new CollectionCellDiagnostic
                    (
                        "collection.target.stale", DiagnosticSeverity.Error,
                        reason, columnID
                    )
                );
                result.BlockApply();
            }

            applyButton.SetEnabled(result.CanApply);
            ShowDiagnostics(result.Diagnostics);
        }

    #endregion

    #region 확정

        // ------------------------------------------------------------
        /// <summary>
        /// 유효한 임시 편집 버퍼만 한 번의 Undo로 원본에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Apply()
        {
            RefreshValidation();
            if (!applyButton.enabledSelf)
            {
                return;
            }

            // 검증을 통과한 경우에만 임시 버퍼를 한 번 확정한다.
            editBufferObject.ApplyModifiedPropertiesWithoutUndo();
            var candidate = GetEditBufferValue();
            // Order 재정렬은 행 경로를 바꾸므로 기존 Value 창을 닫아 오편집을 방지한다.
            if (model is XOrderedEditorModel
                && string.Equals(columnID, "order", StringComparison.Ordinal))
            {
                CollectionValueEditorWindow.CloseForCollection
                (
                    model.SerializedObject.targetObject,
                    model.CollectionPropertyPath
                );
            }

            int index;
            if (model is XDictionaryEditorModel dictionary)
            {
                if (sourceIndex.HasValue)
                {
                    index = dictionary.ReplaceKey(sourceIndex.Value, candidate);
                }
                else
                {
                    var result = dictionary.Add
                    (
                        row => dictionary.GetCellProperty(row, "Key").managedReferenceValue = candidate
                    );
                    if (!result.Succeeded)
                    {
                        ShowDiagnostics
                        (
                            new[]
                            {
                                new CollectionCellDiagnostic
                                (
                                    "collection.edit.failed", DiagnosticSeverity.Error,
                                    "변경사항을 적용하지 못했습니다.", columnID
                                )
                            }
                        );
                        return;
                    }

                    index = result.RowIndex;
                }
            }
            else if (model is XOrderedEditorModel ordered)
            {
                index = ordered.ReplaceStructuralValue(sourceIndex.Value, columnID, candidate);
            }
            else
            {
                return;
            }

            if (index < 0)
            {
                // 변경은 완료됐지만 동일 값의 행이 여럿이면 선택 위치만 생략한다.
                Close();
                return;
            }

            onApplied?.Invoke(index);
            Close();
        }

    #endregion

    }
}
