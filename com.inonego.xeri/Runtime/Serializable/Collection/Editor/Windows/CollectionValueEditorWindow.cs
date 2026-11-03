/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionValueEditorWindow.cs
수정일 : 2026-11-03

# 설명
Cell을 독립 SerializedObject와 UI Toolkit PropertyField로 상세 표시한다.
일반 모드는 live 편집하고 ReadOnly 모드는 입력만 차단하며 Undo·stale 경로의 수명을 관리한다.
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

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 복합 cell의 문맥·Unity Inspector를 이용하는 Value 상세 편집창.
    /// </summary>
    // ============================================================
    internal sealed class CollectionValueEditorWindow : CollectionCellEditorWindowBase
    {

    #region 필드

        private ICollectionEditorModel model;
        private UnityEngine.Object target;
        private string collectionPropertyPath;
        private string itemPropertyPath;
        private SerializedObject serializedObject;
        private SerializedPropertyLocator locator;
        private bool commitQueued;
        private bool isShown;
        private bool isReadOnly;

    #endregion

    #region 생성과 종료

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 cell은 기존 창을 재사용하고 독립 binding으로 상세 편집한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Open
        (
            ICollectionEditorModel sourceModel, SerializedProperty itemProperty,
            bool isReadOnly = false
        )
        {
            var target = itemProperty.serializedObject.targetObject;
            var existing = CollectionEditWindowCoordinator.Find<CollectionValueEditorWindow>();
            if (existing != null && existing.isShown && existing.isReadOnly == isReadOnly
                && existing.target == target
                && existing.collectionPropertyPath == sourceModel.CollectionPropertyPath
                && existing.itemPropertyPath == itemProperty.propertyPath
                && existing.locator != null && existing.serializedObject != null
                && existing.locator.TryResolve(existing.serializedObject, out _, out _))
            {
                CollectionEditWindowCoordinator.Activate<CollectionValueEditorWindow>();
                existing.Focus();
                return;
            }

            var serializedObject = new SerializedObject(target);
            ICollectionEditorModel model;
            SerializedPropertyLocator locator;
            try
            {
                model = sourceModel.CreateFor
                (
                    serializedObject.FindProperty(sourceModel.CollectionPropertyPath)
                );
                locator = new SerializedPropertyLocator
                (
                    serializedObject.FindProperty(itemProperty.propertyPath)
                );
            }
            catch
            {
                serializedObject.Dispose();
                throw;
            }

            var window = CollectionEditWindowCoordinator.Acquire<CollectionValueEditorWindow>();
            window.ReleaseEditSession();
            window.target = target;
            window.collectionPropertyPath = sourceModel.CollectionPropertyPath;
            window.itemPropertyPath = itemProperty.propertyPath;
            window.serializedObject = serializedObject;
            window.model = model;
            window.locator = locator;
            window.isReadOnly = isReadOnly;
            var rowIndex = -1;
            for (var index = 0; index < sourceModel.Count; index++)
            {
                var rowPath = sourceModel.GetRowProperty(index).propertyPath;
                if (SerializedPropertyHelper.IsSameOrDescendantPath(itemProperty.propertyPath, rowPath))
                {
                    rowIndex = index;
                    break;
                }
            }

            var suffix = isReadOnly ? " · 상세 보기" : string.Empty;
            window.titleContent = new GUIContent($"{sourceModel.Label} · {rowIndex + 1} · {itemProperty.displayName}{suffix}");
            window.minSize = new Vector2(420f, 280f);
            CollectionEditWindowCoordinator.Activate<CollectionValueEditorWindow>();
            if (window.isShown)
            {
                window.CreateGUI();
                window.Focus();
            }
            else
            {
                window.isShown = true;
                window.ShowUtility();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// collection과 그 하위 collection을 가리키는 상세 창을 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        public static void CloseForCollection(UnityEngine.Object target, string propertyPath)
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<CollectionValueEditorWindow>())
            {
                if (window.target == target && SerializedPropertyHelper.IsSameOrDescendantPath
                (
                    window.collectionPropertyPath, propertyPath
                ))
                {
                    window.Close();
                }
            }

        }

        // ------------------------------------------------------------
        /// <summary>
        /// Undo는 위치 기반 상세 편집을 확정할 수 없으므로 안전하게 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnEnable()
        {
            Undo.undoRedoPerformed += Close;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 독립 binding과 Undo 구독을 함께 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Close;
            isShown = false;
            ReleaseEditSession();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 대상 전환 시 이전 바인딩을 해제하고 변경된 필드 상태를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseEditSession()
        {
            UnregisterRootCallbacks();
            ReleaseEditorLayout();
            serializedObject?.Dispose();
            serializedObject = null;

            locator = null;
            commitQueued = false;
            isReadOnly = false;
            model = null;
            target = null;
            itemPropertyPath = null;
            collectionPropertyPath = null;
        }

    #endregion

    #region 화면 구성과 입력

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 기본 Inspector 필드를 스크롤 영역에 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CreateGUI()
        {
            // Root VisualElement는 CreateGUI 재호출 시 재사용되므로 콜백을 먼저 해제한다.
            UnregisterRootCallbacks();
            ReleaseEditorLayout();
            if (model == null || serializedObject == null)
            {
                rootVisualElement.Add(new HelpBox("인스펙터에서 상세 편집을 다시 여세요.", HelpBoxMessageType.Info));
                return;
            }

            if (!locator.TryResolve(serializedObject, out var property, out _))
            {
                Close();
                return;
            }

            var scroll = BuildEditorLayout
            (
                property.displayName + (isReadOnly ? " · 상세 보기" : " · 상세 편집"),
                target.name + " · " + model.Label
            );
            property.isExpanded = true;
            var field = new PropertyField(property.Copy(), property.displayName);
            scroll.Add(field);
            field.BindProperty(property.Copy());
            field.SetEnabled(!isReadOnly);
            // SerializedObject 단위 추적은 중첩 SerializeReference 변경도 포착한다.
            // PropertyField 이벤트와 한 번의 Commit으로 병합한다.
            if (!isReadOnly)
            {
                field.TrackSerializedObjectValue(serializedObject, HandleTrackedValue);
            }

            // Value는 실시간 확정하므로 단계별 적용 버튼만 제거한다.
            rootVisualElement.Q<Button>("editor-apply").RemoveFromHierarchy();
            var footnote = rootVisualElement.Q<Label>("editor-footnote");
            footnote.text = isReadOnly ? "읽기 전용입니다." : "수정 내용이 즉시 반영됩니다.";

            var closeButton = rootVisualElement.Q<Button>("editor-cancel");
            closeButton.text = "닫기";
            closeButton.clicked += Close;

            // UI Toolkit PropertyField의 변경 이벤트로 live edit를 확정한다.
            if (!isReadOnly)
            {
                rootVisualElement.RegisterCallback<SerializedPropertyChangeEvent>(HandleEdited);
            }

            rootVisualElement.RegisterCallback<PointerDownEvent>
            (
                HandlePointerDown,
                TrickleDown.TrickleDown
            );
            rootVisualElement.RegisterCallback<KeyDownEvent>
            (
                HandleKeyDown,
                TrickleDown.TrickleDown
            );

            RefreshValidation();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 창 재구성 또는 종료 시 root에 연결한 이벤트를 명시적으로 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        private void UnregisterRootCallbacks()
        {
            rootVisualElement.UnregisterCallback<SerializedPropertyChangeEvent>(HandleEdited);
            rootVisualElement.UnregisterCallback<PointerDownEvent>
            (
                HandlePointerDown,
                TrickleDown.TrickleDown
            );
            rootVisualElement.UnregisterCallback<KeyDownEvent>
            (
                HandleKeyDown,
                TrickleDown.TrickleDown
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 오래된 요소에 입력이 도달하기 전에 창을 닫아 잘못된 편집을 차단한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePointerDown(PointerDownEvent evt)
        {
            if (!EnsureCurrent())
            {
                evt.StopImmediatePropagation();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Escape 및 대상 소멸 시 상세 창을 안전하게 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape || !EnsureCurrent())
            {
                Close();
                evt.StopImmediatePropagation();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 변경으로 원본 요소가 사라지면 입력 전에 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        private bool EnsureCurrent()
        {
            if (serializedObject != null && locator.TryResolve(serializedObject, out _, out _))
            {
                return true;
            }

            Close();
            return false;
        }

    #endregion

    #region 변경 확정과 진단

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 대상의 변경만 원래 collection commit으로 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleEdited(SerializedPropertyChangeEvent evt)
        {
            if (serializedObject == null || locator == null)
            {
                return;
            }

            var path = evt.changedProperty.propertyPath;
            if (!SerializedPropertyHelper.IsSameOrDescendantPath(path, itemPropertyPath))
            {
                return;
            }

            QueueCommit();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UI Toolkit의 중첩 필드 추적도 동일한 Commit 진입점에 합류한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleTrackedValue(SerializedObject _)
        {
            QueueCommit();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 UI 입력에서 발생한 여러 변경 이벤트를 하나의 Commit으로 합친다.
        /// </summary>
        // ------------------------------------------------------------
        private void QueueCommit()
        {
            if (isReadOnly || commitQueued || serializedObject == null)
            {
                return;
            }

            commitQueued = true;
            rootVisualElement.schedule.Execute(CommitValueEdit).ExecuteLater(0);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Value 변경을 구조 정규화 없이 모델에 확정하고 변경 통지를 발행한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CommitValueEdit()
        {
            commitQueued = false;
            if (isReadOnly || serializedObject == null || locator == null || model == null)
            {
                return;
            }

            if (!locator.TryResolve(serializedObject, out var property, out _))
            {
                Close();
                return;
            }

            model.CommitValueChange(property.propertyPath);
            if (serializedObject != null)
            {
                property = serializedObject.FindProperty(itemPropertyPath);
                if (property != null)
                {
                    locator.Refresh(property);
                }
            }

            RefreshValidation();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 상세 편집 대상 셀에 연결된 진단을 현재 상태로 갱신한다.
        /// <br/> 행 재정렬 및 참조 변경을 고려해 매번 셀 경로로 위치를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshValidation()
        {
            if (model == null || serializedObject == null)
            {
                return;
            }

            var rowIndex = -1;
            string columnID = null;
            for (var index = 0; index < model.Count && rowIndex < 0; index++)
            {
                foreach (var column in model.Columns)
                {
                    var cell = model.GetCellProperty(index, column.ID);
                    if (cell == null || cell.propertyPath != itemPropertyPath)
                    {
                        continue;
                    }

                    rowIndex = index;
                    columnID = column.ID;
                    break;
                }
            }

            if (rowIndex < 0)
            {
                ShowDiagnostics(Array.Empty<CollectionCellDiagnostic>());
                return;
            }

            var result = model.Validate(CollectionValidationContext.ForRow(rowIndex));
            var messages = new List<CollectionCellDiagnostic>();
            foreach (var diagnostic in result.Diagnostics)
            {
                if (diagnostic.ColumnID == null || diagnostic.ColumnID == columnID)
                {
                    messages.Add(diagnostic);
                }
            }

            ShowDiagnostics(messages);
        }

    #endregion

    }
}
