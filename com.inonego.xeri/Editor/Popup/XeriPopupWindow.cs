/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriPopupWindow.cs
수정일 : 2026-10-28

# 설명
Editor에서 앵커 팝업(컨텍스트 메뉴·피커 드롭다운 등)의 각 단계를 프레임 없는 팝업 창으로 표시한다.
공통 표시 구현에서 창 집합과 포커스·종료 수명주기를 관리하고, 내용은 호출자가 컨테이너에 채운다.

# 특이사항, 제약사항
배치 영역은 EditorWorkArea가 구하는 작업 표시줄을 제외한 모니터 작업 영역이다.
OS가 팝업 창을 작업 영역 안으로 밀어 넣으므로 전체 모니터 경계를 쓰면 팝업 위치가 어긋난다.
패널 월드 좌표의 원점은 탭 줄을 포함한 호스트 뷰의 좌상단이다. 도킹된 창은 콘텐츠 루트가 그 안쪽에 있다.
창의 화면 위치는 소유 창과 팝업 창 모두 EditorWindow.position으로 읽는다.
Unity는 ShowPopup과 같은 업데이트 안에서는 position을 (0, 0)으로 보고하므로 그동안은 요청한 경계를 쓴다.
Windows에서는 모니터마다 배율이 달라 논리 좌표가 이어지지 않으므로 기준 요소의 좌표는
EditorWorkArea.ToPopupSpace로 팝업을 놓을 모니터의 좌표로 바꾼다.
소유 패널의 입력 차단과 바깥 클릭 종료는 PopupHost가 맡고, 이 파일은 창과 포커스만 관리한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.Editor
{
    // ============================================================
    /// <summary>
    /// 앵커 팝업의 한 단계를 표시하는 Editor 전용 팝업 창.
    /// </summary>
    // ============================================================
    [InitializeOnLoad]
    internal sealed class XeriPopupWindow : EditorWindow
    {

    #region 팝업 표시 구현

        // ============================================================
        /// <summary>
        /// 팝업 전체에 속한 팝업 창과 포커스 전환을 관리한다.
        /// </summary>
        // ============================================================
        private sealed class Display : IPopupDisplay
        {

        #region 상태

            public Rect WorkArea { get; }

            private readonly Vector2 screenOffset;
            private readonly float ownerScale;
            private readonly List<XeriPopupWindow> windows = new();
            private int updateTick;
            private Action dismiss;
            private bool isClosed;
            private bool focusCheckQueued;
            private bool hasPopupFocus;

        #endregion

        #region 표시 구현 생성

            // ------------------------------------------------------------
            /// <summary>
            /// 표시 구현의 좌표·창 소유권 및 포커스 감시를 초기화한다.
            /// </summary>
            // ------------------------------------------------------------
            public Display
            (
                Vector2 screenOffset, float ownerScale, Rect workArea, Action dismiss
            )
            {
                this.screenOffset = screenOffset;
                this.ownerScale = ownerScale;
                WorkArea = workArea;
                this.dismiss = dismiss;
                EditorApplication.update += MonitorFocus;
            }

        #endregion

        #region 좌표 변환 및 팝업 수명주기

            // ------------------------------------------------------------
            /// <summary>
            /// 소유 패널의 월드 좌표를 팝업 창을 놓는 모니터의 화면 좌표로 변환한다.
            /// </summary>
            // ------------------------------------------------------------
            public Vector2 ToPopupPosition(Vector2 worldPosition)
            {
                return EditorWorkArea.ToPopupSpace(screenOffset, worldPosition, ownerScale);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 지정한 깊이의 팝업 창이 실제로 놓인 경계를 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public Rect GetBounds(int depth)
            {
                return windows[depth].GetBounds(updateTick);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 지정한 경계에 팝업 창을 열고 내용을 담을 컨테이너를 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public VisualElement OpenLevel(int depth, Rect bounds)
            {
                if (isClosed || depth != windows.Count)
                {
                    throw new InvalidOperationException("팝업 창의 생성 순서가 올바르지 않습니다.");
                }

                var window = ScriptableObject.CreateInstance<XeriPopupWindow>();
                window.display = this;
                window.minSize = Vector2.one;
                window.position = bounds;
                window.requestedBounds = bounds;
                window.openedTick = updateTick;
                windows.Add(window);

                try
                {
                    window.ShowPopup();

                    if (depth == 0)
                    {
                        window.Focus();
                    }

                    return window.rootVisualElement;
                }
                catch
                {
                    windows.RemoveAt(depth);
                    window.display = null;
                    window.Close();
                    throw;
                }
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 지정한 깊이부터 모든 팝업 창을 역순으로 닫고 필요한 포커스를 복구한다.
            /// </summary>
            // ------------------------------------------------------------
            public void CloseFrom(int depth)
            {
                var focused = EditorWindow.focusedWindow;
                var restoreFocus = false;

                for (var index = windows.Count - 1; index >= depth; index--)
                {
                    var window = windows[index];
                    restoreFocus |= window == focused;
                    windows.RemoveAt(index);
                    window.display = null;
                    window.Close();
                }

                if (restoreFocus && !isClosed && windows.Count > 0)
                {
                    windows[windows.Count - 1].Focus();
                }
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 표시 구현이 소유한 모든 팝업 창과 닫기 요청 콜백을 정리한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Close()
            {
                if (isClosed)
                {
                    return;
                }

                isClosed = true;
                dismiss = null;
                EditorApplication.update -= MonitorFocus;
                EditorApplication.delayCall -= CheckFocus;
                CloseFrom(0);
            }

        #endregion

        #region 포커스 관리

            // ------------------------------------------------------------
            /// <summary>
            /// 팝업 창의 포커스를 추적해 다른 에디터 창으로 이동하면 닫기를 요청한다.
            /// </summary>
            // ------------------------------------------------------------
            private void MonitorFocus()
            {
                updateTick++;

                if (isClosed)
                {
                    return;
                }

                if (ContainsWindow(EditorWindow.focusedWindow))
                {
                    hasPopupFocus = true;
                }
                else if (hasPopupFocus)
                {
                    dismiss?.Invoke();
                }
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 팝업 포커스 이탈을 통지받아 지연된 외부 포커스 확인을 예약한다.
            /// </summary>
            // ------------------------------------------------------------
            public void NotifyFocusLost()
            {
                if (isClosed || focusCheckQueued)
                {
                    return;
                }

                focusCheckQueued = true;
                EditorApplication.delayCall += CheckFocus;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 포커스가 팝업의 어느 창에도 없을 때만 닫기를 요청한다.
            /// </summary>
            // ------------------------------------------------------------
            private void CheckFocus()
            {
                focusCheckQueued = false;

                if (isClosed)
                {
                    return;
                }

                if (!ContainsWindow(EditorWindow.focusedWindow))
                {
                    dismiss?.Invoke();
                }
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 포커스가 이 표시 구현의 팝업 창에 있는지 확인한다.
            /// </summary>
            // ------------------------------------------------------------
            private bool ContainsWindow(EditorWindow focused)
            {
                return focused != null
                    && windows.Contains(focused as XeriPopupWindow);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Unity가 팝업 창을 외부에서 제거한 경우 닫기를 요청한다.
            /// </summary>
            // ------------------------------------------------------------
            public void NotifyDestroyed(XeriPopupWindow window)
            {
                if (!isClosed && windows.Contains(window))
                {
                    dismiss?.Invoke();
                }
            }

        #endregion

        }

    #endregion

    #region 필드

        private Display display;
        private Rect requestedBounds;
        private int openedTick;

    #endregion

    #region 경계

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 이 창이 실제로 놓인 경계를 반환한다.
        /// <br/> Unity는 ShowPopup과 같은 업데이트 안에서는 위치를 (0, 0)으로 보고하므로
        /// <br/> 그동안은 요청한 경계를 쓴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private Rect GetBounds(int updateTick)
        {
            return updateTick > openedTick ? position : requestedBounds;
        }

    #endregion

    #region 등록 및 생성

        // ------------------------------------------------------------
        /// <summary>
        /// Editor 전용 팝업 표시 구현 생성 함수를 공용 호스트에 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        static XeriPopupWindow()
        {
            PopupHost.EditorDisplayFactory = CreateDisplay;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 소유 Editor 창을 기준으로 팝업 표시 구현을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static IPopupDisplay CreateDisplay
        (
            VisualElement owner, Rect worldAnchor, Action dismiss
        )
        {
            var window = FindOwnerWindow(owner);

            if (window == null)
            {
                return null;
            }

            // 패널 월드 좌표의 원점은 탭 줄을 포함한 호스트 뷰의 좌상단이다.
            // 창 콘텐츠 루트는 그 안쪽으로 밀려 있으므로 루트 기준 변환 없이 창 위치를 더한다.
            var screenOffset = window.position.position;
            var ownerScale = GetOwnerScale(owner);
            var anchor = EditorWorkArea.ToPopupSpace
            (
                screenOffset, new Vector2(worldAnchor.xMin, worldAnchor.yMax), ownerScale
            );
            var area = EditorWorkArea.Get(anchor);

            return new Display
            (
                screenOffset, ownerScale, area, dismiss
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 소유 패널이 그려지는 모니터의 배율(point당 픽셀 수)을 구한다.
        /// <br/> 패널에서 읽을 수 없으면 현재 GUI의 배율을 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        private static float GetOwnerScale(VisualElement owner)
        {
            var panel = owner.panel;

            for (var type = panel?.GetType(); type != null; type = type.BaseType)
            {
                var property = type.GetProperty
                (
                    "scaledPixelsPerPoint",
                    BindingFlags.Instance | BindingFlags.Public
                        | BindingFlags.NonPublic | BindingFlags.DeclaredOnly
                );

                if (property?.GetValue(panel) is float scale && scale > 0f)
                {
                    return scale;
                }
            }

            return EditorGUIUtility.pixelsPerPoint;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 소유 요소와 같은 UI 패널을 사용하는 EditorWindow를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private static EditorWindow FindOwnerWindow(VisualElement owner)
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window != null && window.rootVisualElement.panel == owner.panel)
                {
                    return window;
                }
            }

            return null;
        }

    #endregion

    #region Unity 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 포커스 이동이 팝업 내부 이동인지 표시 구현에서 판정하게 한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnLostFocus()
        {
            display?.NotifyFocusLost();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 의도하지 않게 창이 파괴되면 팝업 호스트도 닫도록 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDestroy()
        {
            display?.NotifyDestroyed(this);
            display = null;
        }

    #endregion

    }
}
