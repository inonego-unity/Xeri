/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKDropResolver.cs
수정일 : 2026-05-22

# 설명
UI Toolkit Panel 좌표와 VisualElement 등록 정보로 현재 드롭존을 찾는다.
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
    /// UI Toolkit 드롭 대상 결정자.
    /// </summary>
    // ============================================================
    public sealed class UITKDropResolver : IDropResolver
    {

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// 등록된 VisualElement 와 DropZone 연결 정보.
        /// </summary>
        // ============================================================
        [Serializable]
        private sealed class Entry
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 드롭 영역 VisualElement.
            /// </summary>
            // ------------------------------------------------------------
            public VisualElement Target
            {
                get => target;
                set => target = value;
            }

            private VisualElement target = null;

            // ------------------------------------------------------------
            /// <summary>
            /// 연결된 Core DropZone.
            /// </summary>
            // ------------------------------------------------------------
            public DropZone DropZone
            {
                get => dropZone;
                set => dropZone = value;
            }

            private DropZone dropZone = null;
        }

    #endregion

    #region 필드

        private readonly VisualElement root;
        private readonly List<Entry> entries = new();
        private readonly Dictionary<VisualElement, DropZone> dropZones = new();

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UI Toolkit 드롭 대상 결정자를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public UITKDropResolver(VisualElement root) : base()
        {
            this.root = root;
        }

    #endregion

    #region 등록

        // ------------------------------------------------------------
        /// <summary>
        /// VisualElement 와 DropZone 연결을 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Register(VisualElement target, DropZone dropZone)
        {
            if (target == null) return;
            if (dropZone == null) return;

            dropZones[target] = dropZone;
            entries.RemoveAll(x => x.Target == target);
            entries.Add
            (
                new Entry
                {
                    Target   = target,
                    DropZone = dropZone,
                }
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// VisualElement 와 DropZone 연결을 등록 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Unregister(VisualElement target)
        {
            if (target == null) return;

            dropZones.Remove(target);
            entries.RemoveAll(x => x.Target == target);
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 입력 위치에서 가장 먼저 감지되는 DropZone을 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        public DropZone Resolve(InputPoint input, Draggable draggable)
        {
            var pickedDropZone = ResolveByPick(input);
            if (pickedDropZone != null)
            {
                return pickedDropZone;
            }

            return ResolveByWorldBound(input);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Panel Pick 결과와 부모 체인에서 DropZone을 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private DropZone ResolveByPick(InputPoint input)
        {
            if (root == null) return null;
            if (root.panel == null) return null;

            var picked = root.panel.Pick(input.Pos);
            while (picked != null)
            {
                if (dropZones.TryGetValue(picked, out DropZone dropZone))
                {
                    return dropZone;
                }

                picked = picked.parent;
            }

            return null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 등록된 VisualElement 의 worldBound 로 DropZone을 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private DropZone ResolveByWorldBound(InputPoint input)
        {
            for (var i = entries.Count - 1; i >= 0; --i)
            {
                var entry = entries[i];
                if (entry.Target == null) continue;
                if (!entry.Target.worldBound.Contains(input.Pos)) continue;

                return entry.DropZone;
            }

            return null;
        }

    #endregion

    }
}
