/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : RuntimePopupDisplay.cs
수정일 : 2026-10-28

# 설명
Runtime 패널 안에서 Popup 단계를 프레임 요소로 표시하는 구현.

# 특이사항, 제약사항
팝업 좌표계는 패널 루트의 로컬 좌표이고, 작업 영역은 지정한 함수의 영역이나 패널 전체다.
프레임은 입력을 받지 않고 내용만 입력을 받으므로 프레임 밖을 누르면 backdrop이 처리한다.
레이어와 프레임은 PopupResources가 UXML·USS로 만들고 이 파일은 위치·크기만 지정한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// Runtime 패널 안에서 Popup 단계를 표시하는 구현.
    /// </summary>
    // ============================================================
    internal sealed class RuntimePopupDisplay : IPopupDisplay
    {

    #region 필드

        private readonly VisualElement owner;
        private readonly VisualElement root;
        private readonly Func<VisualElement, Rect> workAreaProvider;
        private readonly VisualElement layer;
        private readonly List<VisualElement> frames = new();
        private readonly List<Rect> frameBounds = new();

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 팝업을 놓을 수 있는 패널 루트 좌표의 영역.
        /// <br/> Safe Area처럼 패널 일부만 쓰는 호스트가 지정한 영역이 있으면 그 영역이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public Rect WorkArea
        {
            get
            {
                return workAreaProvider != null
                    ? workAreaProvider(owner)
                    : new Rect(0f, 0f, root.resolvedStyle.width, root.resolvedStyle.height);
            }
        }

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 패널 루트를 덮는 프레임 레이어를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public RuntimePopupDisplay
        (
            VisualElement owner, VisualElement root,
            Func<VisualElement, Rect> workAreaProvider
        )
        {
            this.owner = owner;
            this.root = root;
            this.workAreaProvider = workAreaProvider;

            layer = PopupResources.CreateLayer();
            root.Add(layer);
        }

    #endregion

    #region 좌표와 팝업 단계

        // ------------------------------------------------------------
        /// <summary>
        /// 소유 패널의 월드 좌표를 패널 루트의 로컬 좌표로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 ToPopupPosition(Vector2 worldPosition)
        {
            return root.WorldToLocal(worldPosition);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 요청한 경계가 그대로 놓이므로 열 때의 경계를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Rect GetBounds(int depth)
        {
            return frameBounds[depth];
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 경계에 프레임을 만들어 내용을 담을 컨테이너로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement OpenLevel(int depth, Rect bounds)
        {
            if (depth != frames.Count)
            {
                throw new InvalidOperationException("팝업 단계의 생성 순서가 올바르지 않습니다.");
            }

            var frame = PopupResources.CreateFrame();
            frame.style.left = bounds.x;
            frame.style.top = bounds.y;
            frame.style.width = bounds.width;
            frame.style.height = bounds.height;
            layer.Add(frame);
            frames.Add(frame);
            frameBounds.Add(bounds);

            return frame;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 깊이 이상의 프레임을 역순으로 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloseFrom(int depth)
        {
            for (var index = frames.Count - 1; index >= depth; index--)
            {
                frames[index].RemoveFromHierarchy();
                frames.RemoveAt(index);
                frameBounds.RemoveAt(index);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 프레임과 프레임 레이어를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Close()
        {
            CloseFrom(0);
            layer.RemoveFromHierarchy();
        }

    #endregion

    }
}
