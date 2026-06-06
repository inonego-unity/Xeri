/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKDropZoneManipulator.cs
수정일 : 2026-05-22

# 설명
UI Toolkit VisualElement 를 Core DropZone으로 등록하는 Manipulator.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.DragDrop
{
    // ============================================================
    /// <summary>
    /// UI Toolkit 드롭존 Manipulator.
    /// </summary>
    // ============================================================
    public sealed class UITKDropZoneManipulator : Manipulator
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 허용 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanDrop
        {
            get => canDrop;
            set
            {
                canDrop = value;
                if (dropZone != null)
                {
                    dropZone.CanDrop = value;
                }
            }
        }

        private bool canDrop = true;

        // ------------------------------------------------------------
        /// <summary>
        /// Core 드롭 영역 상태.
        /// </summary>
        // ------------------------------------------------------------
        public DropZone DropZone => dropZone;

        private DropZone dropZone = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 드롭 조율자.
        /// </summary>
        // ------------------------------------------------------------
        public DragDropCoordinator Coordinator
        {
            get => coordinator ?? DragDropCoordinator.Default;
            set => coordinator = value;
        }

        private DragDropCoordinator coordinator = null;

        // ------------------------------------------------------------
        /// <summary>
        /// UI Toolkit 드롭 대상 결정자.
        /// </summary>
        // ------------------------------------------------------------
        public UITKDropResolver DropResolver
        {
            get => dropResolver;
            set => dropResolver = value;
        }

        private UITKDropResolver dropResolver = null;

        private readonly List<IDropRule> dropRules = new();

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 대상이 드롭 영역에 진입할 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event DropEventHandler OnDropEnter = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 대상이 드롭 영역에서 이탈할 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event DropEventHandler OnDropExit = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 대상이 드롭 영역에 드롭될 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event DropEventHandler OnDropDone = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UI Toolkit 드롭존 Manipulator 를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public UITKDropZoneManipulator
        (
            DragDropCoordinator coordinator = null,
            UITKDropResolver dropResolver = null
        ) : base()
        {
            this.coordinator  = coordinator;
            this.dropResolver = dropResolver;
        }

    #endregion

    #region 드롭 규칙

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 규칙을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddDropRule(IDropRule dropRule)
        {
            if (dropRule == null) return;
            if (dropRules.Contains(dropRule)) return;

            dropRules.Add(dropRule);
            dropZone?.AddDropRule(dropRule);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 등록된 드롭 규칙을 모두 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ClearDropRules()
        {
            dropRules.Clear();
            dropZone?.ClearDropRules();
        }

    #endregion

    #region 콜백 등록

        // ------------------------------------------------------------
        /// <summary>
        /// target VisualElement 를 DropZone으로 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void RegisterCallbacksOnTarget()
        {
            EnsureRuntimeObjects();

            Coordinator.Register(dropZone);
            dropResolver?.Register(target, dropZone);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// target VisualElement 의 DropZone 등록을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void UnregisterCallbacksFromTarget()
        {
            if (dropZone == null) return;

            Coordinator.Unregister(dropZone);
            dropResolver?.Unregister(target);
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Core DropZone이 생성되어 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private void EnsureRuntimeObjects()
        {
            if (dropZone != null) return;

            dropZone = new DropZone(target)
            {
                CanDrop = canDrop,
            };
            dropZone.OnDropEnter += InvokeDropEnter;
            dropZone.OnDropExit  += InvokeDropExit;
            dropZone.OnDropDone  += InvokeDropDone;

            foreach (var dropRule in dropRules)
            {
                dropZone.AddDropRule(dropRule);
            }
        }

    #endregion

    #region 이벤트 호출

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 진입 이벤트를 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void InvokeDropEnter(DropZone sender, DropEventArgs e)
        {
            OnDropEnter?.Invoke(sender, e);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 이탈 이벤트를 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void InvokeDropExit(DropZone sender, DropEventArgs e)
        {
            OnDropExit?.Invoke(sender, e);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 완료 이벤트를 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void InvokeDropDone(DropZone sender, DropEventArgs e)
        {
            OnDropDone?.Invoke(sender, e);
        }

    #endregion

    }
}
