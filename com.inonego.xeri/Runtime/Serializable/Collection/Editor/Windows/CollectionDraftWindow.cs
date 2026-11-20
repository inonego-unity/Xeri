/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionDraftWindow.cs
수정일 : 2026-11-19

# 설명
초안 열(키·순서)의 값을 원본과 분리된 임시 편집 버퍼에서 고치는 비모달 창.
선택한 값의 PropertyDrawer를 표시하고 세션의 초안 검증을 통과했을 때만 원본에 반영한다.
기존 행의 값 수정과, 값을 먼저 정해야 하는 새 행 추가를 함께 다룬다.

# 특이사항, 제약사항
창은 원본과 다른 SerializedObject로 세션을 다시 열어 사용한다.
원본 셀이 창을 연 뒤 바뀌었거나 Undo/Redo가 일어나면 적용하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Editor;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 초안 열 값을 임시 버퍼에서 고치고 검증 후 확정하는 EditorWindow.
    /// </summary>
    // ============================================================
    internal sealed class CollectionDraftWindow : CollectionCellEditorWindowBase
    {

    #region 내부 타입

        // ============================================================
        /// <summary>
        /// 원본에 영향을 주지 않는 임시 SerializedProperty 보관소.
        /// </summary>
        // ============================================================
        private sealed class EditBuffer : ScriptableObject
        {
            [SerializeField] internal string Text;
            [SerializeField] internal int Integer;
            [SerializeField] internal long Long;
            [SerializeField] internal float Float;
            [SerializeField] internal double Double;
            [SerializeField] internal bool Boolean;
            [SerializeField] internal UnityEngine.Object Object;
            [SerializeField] internal Color Color;
            [SerializeField] internal Vector2 Vector2;
            [SerializeField] internal Vector3 Vector3;
            [SerializeField] internal Vector4 Vector4;
            [SerializeField] internal Vector2Int Vector2Int;
            [SerializeField] internal Vector3Int Vector3Int;
            [SerializeField] internal Quaternion Quaternion;
            [SerializeField] internal Rect Rect;
            [SerializeField] internal Bounds Bounds;
            [SerializeReference] internal object Reference;
        }

    #endregion

    #region 필드

        private CollectionSession session;
        private CollectionColumn column;
        private int? sourceIndex;
        private CollectionDraft baseDraft;
        private Action<int> onApplied;
        private SerializedPropertyLocator locator;
        private SerializedPropertySnapshot originalSnapshot;
        private EditBuffer editBuffer;
        private SerializedObject editBufferObject;
        private string editFieldName;
        private Enum enumEditValue;
        private Button applyButton;
        private bool isShown;

    #endregion

    #region 창 열기

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 행의 현재 값을 초안으로 연다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool OpenForRow
        (
            CollectionSession source, int sourceIndex, CollectionColumn column, Action<int> onApplied = null
        )
        {
            var existing = CollectionEditWindowCoordinator.Find<CollectionDraftWindow>();
            if (existing != null && existing.IsSameTarget(source, sourceIndex, column))
            {
                existing.onApplied = onApplied;
                CollectionEditWindowCoordinator.Activate<CollectionDraftWindow>();
                existing.Focus();
                return true;
            }

            var window = Create(source, sourceIndex, column, null, onApplied);
            if (window == null)
            {
                return false;
            }

            var property = window.session.Source.GetCell(sourceIndex, column);
            if (!window.InitializeFromProperty(property))
            {
                Debug.LogWarning($"[CollectionDraft] {property?.propertyType} 형식은 초안 편집을 지원하지 않습니다.");
                window.Close();
                return false;
            }

            window.ShowEditor();
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 행의 값을 새로 만든 값으로 바꾸는 초안을 연다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool OpenForRow
        (
            CollectionSession source, int sourceIndex, CollectionColumn column, object value, Action<int> onApplied
        )
        {
            return OpenWithValue(Create(source, sourceIndex, column, null, onApplied), value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 값을 먼저 정해야 하는 새 행의 초안을 연다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool OpenForAdd
        (
            CollectionSession source, CollectionColumn column, object value,
            CollectionDraft baseDraft, Action<int> onApplied
        )
        {
            return OpenWithValue(Create(source, null, column, baseDraft ?? CollectionDraft.ForNewRow(), onApplied), value);
        }

        private static bool OpenWithValue(CollectionDraftWindow window, object value)
        {
            if (window == null)
            {
                return false;
            }

            if (!window.InitializeFromValue(value))
            {
                Debug.LogWarning("[CollectionDraft] 이 값은 초안 편집을 지원하지 않습니다.");
                window.Close();
                return false;
            }

            window.ShowEditor();
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 위치와 독립 SerializedObject를 캡처한다.
        /// </summary>
        // ------------------------------------------------------------
        private static CollectionDraftWindow Create
        (
            CollectionSession source, int? sourceIndex, CollectionColumn column,
            CollectionDraft baseDraft, Action<int> onApplied
        )
        {
            if (source == null || column == null || source.IsReadOnly)
            {
                return null;
            }

            var targetObject = new SerializedObject(source.SerializedObject.targetObject);
            CollectionSession session;
            SerializedPropertyLocator locator;
            SerializedPropertySnapshot originalSnapshot = null;
            try
            {
                session = source.Reopen(targetObject);
                var property = sourceIndex.HasValue
                    ? session.Source.GetCell(sourceIndex.Value, column)
                    : session.Source.GetProperty();
                locator = new SerializedPropertyLocator(property);
                if (sourceIndex.HasValue && !SerializedPropertySnapshot.TryCreate(property, out originalSnapshot, out var reason))
                {
                    throw new InvalidOperationException(reason);
                }
            }
            catch (Exception exception)
            {
                targetObject.Dispose();
                Debug.LogWarning($"[CollectionDraft] 원본 필드 캡처 실패: {exception.Message}");
                return null;
            }

            var window = CollectionEditWindowCoordinator.Acquire<CollectionDraftWindow>();
            window.ReleaseEditSession();
            window.session = session;
            window.column = column;
            window.sourceIndex = sourceIndex;
            window.baseDraft = baseDraft;
            window.onApplied = onApplied;
            window.locator = locator;
            window.originalSnapshot = originalSnapshot;
            window.editBuffer = CreateInstance<EditBuffer>();
            window.editBuffer.hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable;
            window.editBufferObject = new SerializedObject(window.editBuffer);
            return window;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 셀을 다시 열 때 적용 전 편집 내용을 보존할지 판단한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool IsSameTarget(CollectionSession source, int index, CollectionColumn target)
        {
            return isShown && session != null && locator != null
                && session.SerializedObject.targetObject == source.SerializedObject.targetObject
                && session.Source.Path == source.Source.Path
                && sourceIndex == index
                && column?.ID == target.ID
                && TryResolveOriginal(out _, out _);
        }

        private void ShowEditor()
        {
            var action = sourceIndex.HasValue ? "수정" : "추가";
            titleContent = new GUIContent($"{session.Source.DisplayName} · {column.Header} {action}");
            minSize = new Vector2(420f, 260f);
            CollectionEditWindowCoordinator.Activate<CollectionDraftWindow>();
            if (isShown)
            {
                CreateGUI();
                Focus();
                return;
            }

            isShown = true;
            ShowUtility();
        }

    #endregion

    #region 편집 버퍼

        // ------------------------------------------------------------
        /// <summary>
        /// 셀의 현재 값을 편집 버퍼에 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool InitializeFromProperty(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            if (column.IsManagedReference)
            {
                return InitializeFromValue(CollectionCellDrawers.Resolve(column).ValueCreator?.CreateEditCopy(property));
            }

            if (property.propertyType == SerializedPropertyType.Enum && column.ValueType?.IsEnum == true)
            {
                enumEditValue = (Enum)Enum.ToObject(column.ValueType, property.intValue);
                return true;
            }

            editFieldName = GetBufferField(column.ValueType);
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
        /// 새로 만든 값을 편집 버퍼에 넣는다.
        /// </summary>
        // ------------------------------------------------------------
        private bool InitializeFromValue(object value)
        {
            if (value == null)
            {
                return false;
            }

            if (column.IsManagedReference)
            {
                editBuffer.Reference = value;
                editFieldName = nameof(EditBuffer.Reference);
                editBufferObject.Update();
                return true;
            }

            if (value is Enum enumValue)
            {
                enumEditValue = enumValue;
                return true;
            }

            editFieldName = GetBufferField(value.GetType());
            if (editFieldName == null)
            {
                return false;
            }

            var destination = editBufferObject.FindProperty(editFieldName);
            if (value is UnityEngine.Object unityObject)
            {
                destination.objectReferenceValue = unityObject;
            }
            else
            {
                destination.boxedValue = value;
            }

            editBufferObject.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static string GetBufferField(Type type)
        {
            if (type == null)
            {
                return null;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return nameof(EditBuffer.Object);
            }

            return type == typeof(string) ? nameof(EditBuffer.Text)
                : type == typeof(int) ? nameof(EditBuffer.Integer)
                : type == typeof(long) ? nameof(EditBuffer.Long)
                : type == typeof(float) ? nameof(EditBuffer.Float)
                : type == typeof(double) ? nameof(EditBuffer.Double)
                : type == typeof(bool) ? nameof(EditBuffer.Boolean)
                : type == typeof(Color) ? nameof(EditBuffer.Color)
                : type == typeof(Vector2) ? nameof(EditBuffer.Vector2)
                : type == typeof(Vector3) ? nameof(EditBuffer.Vector3)
                : type == typeof(Vector4) ? nameof(EditBuffer.Vector4)
                : type == typeof(Vector2Int) ? nameof(EditBuffer.Vector2Int)
                : type == typeof(Vector3Int) ? nameof(EditBuffer.Vector3Int)
                : type == typeof(Quaternion) ? nameof(EditBuffer.Quaternion)
                : type == typeof(Rect) ? nameof(EditBuffer.Rect)
                : type == typeof(Bounds) ? nameof(EditBuffer.Bounds)
                : null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 임시 편집 값을 열의 실제 형식으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private object GetEditBufferValue()
        {
            return enumEditValue ?? SerializedPropertyHelper.GetValue(editBufferObject.FindProperty(editFieldName));
        }

        private CollectionDraft BuildDraft(object candidate)
        {
            var draft = sourceIndex.HasValue ? CollectionDraft.ForRow(sourceIndex.Value) : baseDraft.Clone();
            return draft.Set(column.ID, candidate);
        }

    #endregion

    #region 수명

        private void OnEnable()
        {
            Undo.undoRedoPerformed += Close;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비모달 창으로 돌아올 때 외부 변경에 따른 적용 가능 여부를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnFocus()
        {
            if (applyButton != null)
            {
                RefreshValidation();
            }
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Close;
            isShown = false;
            ReleaseEditSession();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 대상으로 전환할 때 이전 임시 버퍼와 바인딩을 폐기한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseEditSession()
        {
            UnregisterRootCallbacks();
            ReleaseEditorLayout();
            applyButton = null;
            editBufferObject?.Dispose();
            editBufferObject = null;
            session?.SerializedObject.Dispose();
            session = null;
            if (editBuffer != null)
            {
                DestroyImmediate(editBuffer);
                editBuffer = null;
            }

            column = null;
            baseDraft = null;
            locator = null;
            originalSnapshot = null;
            enumEditValue = null;
            editFieldName = null;
            onApplied = null;
        }

    #endregion

    #region 표시와 검증

        private void CreateGUI()
        {
            UnregisterRootCallbacks();
            ReleaseEditorLayout();
            if (editBufferObject == null || session == null)
            {
                rootVisualElement.Add(new HelpBox("인스펙터에서 편집창을 다시 여세요.", HelpBoxMessageType.Info));
                return;
            }

            var scroll = BuildEditorLayout(column.Header + " 편집", "적용하면 변경됩니다.");
            if (enumEditValue != null)
            {
                var enumField = new EnumField(column.Header, enumEditValue);
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
                scroll.Add(new PropertyField(property.Copy(), column.Header));
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
        /// 편집 대상과 적용 전 원본 값이 유지되는지 검사한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool TryResolveOriginal(out SerializedProperty property, out string reason)
        {
            property = null;
            reason = "편집 대상을 찾을 수 없습니다.";
            if (locator == null || session == null
                || !locator.TryResolve(session.SerializedObject, out property, out reason))
            {
                return false;
            }

            if (originalSnapshot == null || originalSnapshot.Matches(property))
            {
                return true;
            }

            property = null;
            reason = "원본 값이 바뀌었습니다. 편집창을 다시 여세요.";
            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원본을 고치지 않고 임시 값만 검증해 적용 가능 여부를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshValidation()
        {
            if (applyButton == null || session == null)
            {
                return;
            }

            if (!TryResolveOriginal(out _, out var reason))
            {
                applyButton.SetEnabled(false);
                ShowDiagnostics
                (
                    new[]
                    {
                        new CollectionCellDiagnostic("collection.target.stale", DiagnosticSeverity.Error, reason, column.ID),
                    }
                );
                return;
            }

            session.Observe();
            var result = session.Validate(BuildDraft(GetEditBufferValue()));
            applyButton.SetEnabled(result.CanApply);
            ShowDiagnostics(result.Diagnostics);
        }

    #endregion

    #region 확정

        // ------------------------------------------------------------
        /// <summary>
        /// 검증을 통과한 임시 값만 한 번의 Undo로 원본에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Apply()
        {
            RefreshValidation();
            if (applyButton == null || !applyButton.enabledSelf)
            {
                return;
            }

            editBufferObject.ApplyModifiedPropertiesWithoutUndo();
            var draft = BuildDraft(GetEditBufferValue());

            // 기존 행의 키·순서가 바뀌면 행 경로가 바뀔 수 있으므로 값 상세 창을 먼저 닫는다.
            if (sourceIndex.HasValue)
            {
                CollectionDetailEditors.Close(session);
            }

            var result = session.Apply
            (
                sourceIndex.HasValue ? CollectionEdit.CommitDraft(draft) : CollectionEdit.AddRow(draft)
            );
            if (!result.Succeeded)
            {
                var message = string.IsNullOrEmpty(result.Reason) ? "변경사항을 적용하지 못했습니다." : result.Reason;
                ShowDiagnostics
                (
                    new[]
                    {
                        new CollectionCellDiagnostic("collection.edit.failed", DiagnosticSeverity.Error, message, column.ID),
                    }
                );
                return;
            }

            var callback = onApplied;
            Close();
            if (result.RowIndex >= 0)
            {
                callback?.Invoke(result.RowIndex);
            }
        }

    #endregion

    }
}
