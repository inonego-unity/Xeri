/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionValueEditorWindow.cs
수정일 : 2026-11-19

# 설명
셀이나 행을 독립 SerializedObject와 UI Toolkit PropertyField로 상세 표시한다.
일반 모드는 고친 값을 바로 확정하고 읽기 전용 모드는 입력만 막는다.

# 특이사항, 제약사항
값 확정은 창이 다시 연 세션의 CommitBound로 하며 다른 편집기에는 세션 통지로 전달된다.
Undo/Redo나 원본 행이 사라지면 오래된 경로를 고치지 않도록 창을 닫는다.
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
    /// 셀·행 값의 상세 편집창.
    /// </summary>
    // ============================================================
    internal sealed class CollectionValueEditorWindow : CollectionCellEditorWindowBase
    {

    #region 필드

        private CollectionSession session;
        private UnityEngine.Object target;
        private string collectionPath;
        private string itemPath;
        private SerializedPropertyLocator locator;
        private bool commitQueued;
        private bool isShown;
        private bool isReadOnly;

    #endregion

    #region 창 열기와 닫기

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 셀은 기존 창을 재사용하고 독립 바인딩으로 상세 편집한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Open(CollectionSession source, SerializedProperty itemProperty, bool isReadOnly = false)
        {
            var target = itemProperty.serializedObject.targetObject;
            var existing = CollectionEditWindowCoordinator.Find<CollectionValueEditorWindow>();
            if (existing != null && existing.isShown && existing.isReadOnly == isReadOnly
                && existing.target == target
                && existing.collectionPath == source.Source.Path
                && existing.itemPath == itemProperty.propertyPath
                && existing.EnsureCurrent())
            {
                CollectionEditWindowCoordinator.Activate<CollectionValueEditorWindow>();
                existing.Focus();
                return;
            }

            var serializedObject = new SerializedObject(target);
            CollectionSession session;
            SerializedPropertyLocator locator;
            try
            {
                session = source.Reopen(serializedObject);
                locator = new SerializedPropertyLocator(serializedObject.FindProperty(itemProperty.propertyPath));
            }
            catch
            {
                serializedObject.Dispose();
                throw;
            }

            var window = CollectionEditWindowCoordinator.Acquire<CollectionValueEditorWindow>();
            window.ReleaseEditSession();
            window.session = session;
            window.target = target;
            window.collectionPath = source.Source.Path;
            window.itemPath = itemProperty.propertyPath;
            window.locator = locator;
            window.isReadOnly = isReadOnly;

            var rowNumber = source.Source.TryLocate(itemProperty.propertyPath, out var rowIndex, out _) ? rowIndex + 1 : 0;
            var suffix = isReadOnly ? " · 상세 보기" : string.Empty;
            window.titleContent = new GUIContent($"{source.Source.DisplayName} · {rowNumber} · {itemProperty.displayName}{suffix}");
            window.minSize = new Vector2(420f, 280f);
            CollectionEditWindowCoordinator.Activate<CollectionValueEditorWindow>();
            if (window.isShown)
            {
                window.CreateGUI();
                window.Focus();
                return;
            }

            window.isShown = true;
            window.ShowUtility();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션과 그 하위 컬렉션을 가리키는 상세 창을 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        public static void CloseForCollection(UnityEngine.Object target, string propertyPath)
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<CollectionValueEditorWindow>())
            {
                if (window.target == target
                    && SerializedPropertyHelper.IsSameOrDescendantPath(window.collectionPath, propertyPath))
                {
                    window.Close();
                }
            }
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += Close;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Close;
            isShown = false;
            ReleaseEditSession();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 대상 전환 시 이전 바인딩과 세션을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseEditSession()
        {
            UnregisterRootCallbacks();
            ReleaseEditorLayout();
            session?.SerializedObject.Dispose();
            session = null;
            locator = null;
            commitQueued = false;
            isReadOnly = false;
            target = null;
            itemPath = null;
            collectionPath = null;
        }

    #endregion

    #region 화면 구성과 입력

        private void CreateGUI()
        {
            // 루트 VisualElement는 CreateGUI 재호출 시 재사용되므로 콜백을 먼저 해제한다.
            UnregisterRootCallbacks();
            ReleaseEditorLayout();
            if (session == null)
            {
                rootVisualElement.Add(new HelpBox("인스펙터에서 상세 편집을 다시 여세요.", HelpBoxMessageType.Info));
                return;
            }

            if (!locator.TryResolve(session.SerializedObject, out var property, out _))
            {
                Close();
                return;
            }

            var scroll = BuildEditorLayout
            (
                property.displayName + (isReadOnly ? " · 상세 보기" : " · 상세 편집"),
                target.name + " · " + session.Source.DisplayName
            );
            property.isExpanded = true;
            var field = new PropertyField(property.Copy(), property.displayName);
            scroll.Add(field);
            field.BindProperty(property.Copy());
            field.SetEnabled(!isReadOnly);

            // SerializedObject 단위 추적은 중첩 SerializeReference 변경도 잡으며 PropertyField 이벤트와 한 번의 확정으로 합친다.
            if (!isReadOnly)
            {
                field.TrackSerializedObjectValue(session.SerializedObject, _ => QueueCommit());
                rootVisualElement.RegisterCallback<SerializedPropertyChangeEvent>(HandleEdited);
            }

            rootVisualElement.Q<Button>("editor-apply").RemoveFromHierarchy();
            rootVisualElement.Q<Label>("editor-footnote").text = isReadOnly ? "읽기 전용입니다." : "수정 내용이 즉시 반영됩니다.";
            var closeButton = rootVisualElement.Q<Button>("editor-cancel");
            closeButton.text = "닫기";
            closeButton.clicked += Close;

            rootVisualElement.RegisterCallback<PointerDownEvent>(HandlePointerDown, TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);
            RefreshValidation();
        }

        private void UnregisterRootCallbacks()
        {
            rootVisualElement.UnregisterCallback<SerializedPropertyChangeEvent>(HandleEdited);
            rootVisualElement.UnregisterCallback<PointerDownEvent>(HandlePointerDown, TrickleDown.TrickleDown);
            rootVisualElement.UnregisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 오래된 요소에 입력이 닿기 전에 창을 닫아 잘못된 편집을 막는다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePointerDown(PointerDownEvent evt)
        {
            if (!EnsureCurrent())
            {
                evt.StopImmediatePropagation();
            }
        }

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
        /// 원본 요소가 사라졌으면 창을 닫고 false를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool EnsureCurrent()
        {
            if (session != null && locator != null && locator.TryResolve(session.SerializedObject, out _, out _))
            {
                return true;
            }

            Close();
            return false;
        }

    #endregion

    #region 변경 확정과 진단

        private void HandleEdited(SerializedPropertyChangeEvent evt)
        {
            if (session != null && SerializedPropertyHelper.IsSameOrDescendantPath(evt.changedProperty.propertyPath, itemPath))
            {
                QueueCommit();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 입력에서 생긴 여러 변경 이벤트를 한 번의 확정으로 합친다.
        /// </summary>
        // ------------------------------------------------------------
        private void QueueCommit()
        {
            if (isReadOnly || commitQueued || session == null)
            {
                return;
            }

            commitQueued = true;
            rootVisualElement.schedule.Execute(CommitValueEdit).ExecuteLater(0);
        }

        private void CommitValueEdit()
        {
            commitQueued = false;
            if (isReadOnly || session == null)
            {
                return;
            }

            if (!locator.TryResolve(session.SerializedObject, out var property, out _))
            {
                Close();
                return;
            }

            session.Apply(CollectionEdit.CommitBound(property.propertyPath));

            // 정규화로 같은 경로의 속성 객체가 바뀌었을 수 있으므로 위치를 다시 잡는다.
            property = session.SerializedObject.FindProperty(itemPath);
            if (property != null)
            {
                locator.Refresh(property);
            }

            RefreshValidation();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 상세 편집 대상 셀에 속한 진단만 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshValidation()
        {
            if (session == null)
            {
                return;
            }

            if (!session.Source.TryLocate(itemPath, out var rowIndex, out var column))
            {
                ShowDiagnostics(Array.Empty<CollectionCellDiagnostic>());
                return;
            }

            var messages = new List<CollectionCellDiagnostic>();
            foreach (var diagnostic in session.Validate().GetDiagnostics(rowIndex))
            {
                if (column == null || diagnostic.ColumnID == null || diagnostic.ColumnID == column.ID)
                {
                    messages.Add(diagnostic);
                }
            }

            ShowDiagnostics(messages);
        }

    #endregion

    }
}
