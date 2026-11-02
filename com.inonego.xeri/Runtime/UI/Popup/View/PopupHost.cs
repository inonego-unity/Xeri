/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PopupHost.cs
수정일 : 2026-10-28

# 설명
앵커에 붙는 팝업(컨텍스트 메뉴·피커 드롭다운 등)이 공유하는 호스트.
소유 패널 입력 차단, 닫기 정책, 좌표 변환과 솔버 호출을 한 곳에서 맡는다.

# 특이사항, 제약사항
팝업이 실제로 그려지는 방식만 IPopupDisplay가 다르며 Editor와 Runtime이 같은 호스트를 쓴다.
호출자가 Close를 부르면 닫기 콜백을 부르지 않고, backdrop 클릭·크기 변경·소유 요소 분리·표시 구현이
요청한 닫기에서는 호스트를 닫은 뒤 닫기 콜백을 한 번 부른다.
모든 배치는 팝업 좌표계에서 이뤄지고, 소유 패널 좌표는 ToPopupPosition·ToPopupRect로만 변환한다.
backdrop은 PopupResources가 UXML·USS로 만든다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 앵커 팝업이 공유하는 입력 차단·닫기·배치 호스트.
    /// </summary>
    // ============================================================
    internal sealed class PopupHost
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> Editor 컨텍스트에서 팝업 표시 구현을 만드는 함수.
        /// <br/> 인자는 소유 요소, 월드 좌표의 앵커, 닫기 요청 콜백이다.
        /// </summary>
        // ------------------------------------------------------------
        public static Func<VisualElement, Rect, Action, IPopupDisplay> EditorDisplayFactory
        {
            get;
            set;
        }

        private readonly VisualElement owner;
        private readonly VisualElement root;
        private readonly VisualElement backdrop;
        private readonly IPopupPlacementSolver placementSolver;
        private IPopupDisplay display;
        private Action onClosed;
        private bool isClosed;

        // ------------------------------------------------------------
        /// <summary>
        /// 팝업을 놓을 수 있는 팝업 좌표계의 영역.
        /// </summary>
        // ------------------------------------------------------------
        public Rect WorkArea => display.WorkArea;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 소유 패널을 덮는 backdrop을 만들어 호스트를 연다.
        /// </summary>
        // ------------------------------------------------------------
        private PopupHost
        (
            VisualElement owner, VisualElement root,
            Action onClosed, IPopupPlacementSolver placementSolver
        )
        {
            this.owner = owner;
            this.root = root;
            this.onClosed = onClosed;
            this.placementSolver = placementSolver ?? PopupPlacementSolver.Default;

            owner.RegisterCallback<DetachFromPanelEvent>(HandleOwnerDetached);
            root.RegisterCallback<GeometryChangedEvent>(HandleRootGeometryChanged);

            // 팝업이 열려 있는 동안 아래 요소가 클릭·휠·호버를 받지 않도록 패널 전체를 덮는다.
            backdrop = PopupResources.CreateBackdrop();
            backdrop.RegisterCallback<PointerDownEvent>(HandleBackdropPointerDown);
            root.Add(backdrop);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 소유 요소의 패널에 팝업 호스트를 연다.
        /// <br/> runtimeWorkArea는 Runtime 패널의 사용 영역(패널 루트 좌표)이고
        /// <br/> 생략하면 패널 전체를 쓴다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static PopupHost Open
        (
            VisualElement owner, Rect worldAnchor,
            Func<VisualElement, Rect> runtimeWorkArea, Action onClosed,
            IPopupPlacementSolver placementSolver = null
        )
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            var root = owner.panel?.visualTree
                ?? throw new InvalidOperationException
                (
                    "팝업 소유 요소가 UI 패널에 연결되어 있지 않습니다."
                );
            var host = new PopupHost(owner, root, onClosed, placementSolver);

            try
            {
                host.display = CreateDisplay
                (
                    owner, root, worldAnchor, runtimeWorkArea, host.Dismiss
                );
            }
            catch
            {
                host.Close();
                throw;
            }

            return host;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 패널 종류에 맞는 팝업 표시 구현을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private static IPopupDisplay CreateDisplay
        (
            VisualElement owner, VisualElement root, Rect worldAnchor,
            Func<VisualElement, Rect> runtimeWorkArea, Action dismiss
        )
        {
            if (root.panel.contextType != ContextType.Editor)
            {
                return new RuntimePopupDisplay(owner, root, runtimeWorkArea);
            }

            var factory = EditorDisplayFactory
                ?? throw new InvalidOperationException
                (
                    "Editor 팝업 표시 구현 생성 함수가 등록되지 않았습니다."
                );

            return factory(owner, worldAnchor, dismiss)
                ?? throw new InvalidOperationException
                (
                    "Editor 팝업 표시 구현을 생성할 수 없습니다."
                );
        }

    #endregion

    #region 좌표 변환과 배치

        // ------------------------------------------------------------
        /// <summary>
        /// 소유 패널의 월드 좌표를 팝업 좌표계로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 ToPopupPosition(Vector2 worldPosition)
        {
            return display.ToPopupPosition(worldPosition);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 소유 패널의 월드 좌표 영역을 팝업 좌표계의 영역으로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Rect ToPopupRect(Rect worldRect)
        {
            var min = ToPopupPosition(worldRect.min);
            var max = ToPopupPosition(worldRect.max);

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 팝업 좌표계의 앵커 영역을 기준으로 작업 영역 안에 팝업을 배치한다.
        /// <br/> 배치할 수 없으면 예외를 던진다.
        /// </summary>
        // ----------------------------------------------------------------------
        public PopupPlacementResult Solve
        (
            Rect anchorBounds, Vector2 popupSize, PopupPlacementOptions options
        )
        {
            var result = placementSolver.Solve
            (
                WorkArea, anchorBounds, popupSize, options
            );

            if (!result.Succeeded)
            {
                throw new InvalidOperationException
                (
                    $"팝업을 배치할 수 없습니다: {result.FailureReason}"
                );
            }

            return result;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 깊이의 팝업이 실제로 놓인 경계를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Rect GetBounds(int depth)
        {
            return display.GetBounds(depth);
        }

    #endregion

    #region 팝업 단계와 닫기

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 경계에 팝업 단계를 열고 내용을 담을 컨테이너를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement OpenLevel(int depth, Rect bounds)
        {
            return display.OpenLevel(depth, bounds);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 깊이 이상에 해당하는 팝업 단계를 모두 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloseFrom(int depth)
        {
            if (!isClosed)
            {
                display.CloseFrom(depth);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 팝업과 backdrop을 닫고 닫기 콜백은 부르지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public void Close()
        {
            if (isClosed)
            {
                return;
            }

            isClosed = true;
            onClosed = null;
            owner.UnregisterCallback<DetachFromPanelEvent>(HandleOwnerDetached);
            root.UnregisterCallback<GeometryChangedEvent>(HandleRootGeometryChanged);
            backdrop.UnregisterCallback<PointerDownEvent>(HandleBackdropPointerDown);
            display?.Close();
            backdrop.RemoveFromHierarchy();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 호스트가 스스로 닫아야 하는 상황에서 닫고 호출자에게 알린다.
        /// </summary>
        // ------------------------------------------------------------
        private void Dismiss()
        {
            if (isClosed)
            {
                return;
            }

            var callback = onClosed;
            Close();
            callback?.Invoke();
        }

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 팝업 바깥을 누르면 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleBackdropPointerDown(PointerDownEvent evt)
        {
            Dismiss();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 소유 요소가 패널에서 떨어지면 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleOwnerDetached(DetachFromPanelEvent evt)
        {
            Dismiss();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 패널 크기가 바뀌면 잘못된 위치에 남지 않도록 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleRootGeometryChanged(GeometryChangedEvent evt)
        {
            if (!Mathf.Approximately(evt.oldRect.width, evt.newRect.width)
                || !Mathf.Approximately(evt.oldRect.height, evt.newRect.height))
            {
                Dismiss();
            }
        }

    #endregion

    }
}
