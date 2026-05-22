/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EditorAssetDropManipulator.cs
수정일 : 2026-05-22

# 설명
UnityEditor DragAndDrop 으로 Editor UI Toolkit 요소에 Asset/Object drop 처리를 붙인다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.DragDrop.Editor
{
    // ============================================================
    /// <summary>
    /// Editor Asset Drop Manipulator.
    /// </summary>
    // ============================================================
    public sealed class EditorAssetDropManipulator : Manipulator
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 허용할 Object 타입.
        /// </summary>
        // ------------------------------------------------------------
        public Type AcceptedType
        {
            get => acceptedType;
            set => acceptedType = value;
        }

        private Type acceptedType = null;

        private readonly List<UnityEngine.Object> acceptedObjects = new();

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Editor Object drop 완료 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<EditorAssetDropManipulator, IReadOnlyList<UnityEngine.Object>> OnDropDone = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Editor Asset Drop Manipulator 를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public EditorAssetDropManipulator(Type acceptedType = null) : base()
        {
            this.acceptedType = acceptedType;
        }

    #endregion

    #region 콜백 등록

        // ------------------------------------------------------------
        /// <summary>
        /// target VisualElement 에 Editor DragAndDrop 이벤트를 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            target.RegisterCallback<DragPerformEvent>(OnDragPerform);
            target.RegisterCallback<DragLeaveEvent>  (OnDragLeave);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// target VisualElement 에서 Editor DragAndDrop 이벤트를 등록 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
            target.UnregisterCallback<DragPerformEvent>(OnDragPerform);
            target.UnregisterCallback<DragLeaveEvent>  (OnDragLeave);
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 중인 Editor Object 의 허용 여부를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDragUpdated(DragUpdatedEvent eventData)
        {
            CollectAcceptedObjects();

            DragAndDrop.visualMode = acceptedObjects.Count > 0
                ? DragAndDropVisualMode.Copy
                : DragAndDropVisualMode.Rejected;

            eventData.StopPropagation();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Editor Object drop 을 수락하고 이벤트로 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDragPerform(DragPerformEvent eventData)
        {
            CollectAcceptedObjects();
            if (acceptedObjects.Count == 0)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                eventData.StopPropagation();
                return;
            }

            DragAndDrop.AcceptDrag();
            OnDropDone?.Invoke(this, acceptedObjects);
            eventData.StopPropagation();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Editor drag 이탈 시 임시 Object 목록을 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDragLeave(DragLeaveEvent eventData)
        {
            acceptedObjects.Clear();
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 DragAndDrop 참조 중 허용 타입에 맞는 Object를 수집한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CollectAcceptedObjects()
        {
            acceptedObjects.Clear();

            var type = acceptedType ?? typeof(UnityEngine.Object);
            foreach (var objectReference in DragAndDrop.objectReferences)
            {
                if (objectReference == null) continue;
                if (!type.IsInstanceOfType(objectReference)) continue;

                acceptedObjects.Add(objectReference);
            }
        }

    #endregion

    }
}
