/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriContextMenu.cs
수정일 : 2026-11-23

# 설명
런타임 패널과 Editor의 독립 팝업 창에서 공통으로 쓰는 컨텍스트 메뉴의 공개 진입점이다.
메뉴 항목은 열 때 XeriContextMenuItem 목록으로 전달하며, 경로의 슬래시가 하위 메뉴가 된다.
열린 단계 스택과 포인터·키보드 탐색을 맡고, 단계·행 UI(XeriContextMenuLevel·Row)는 Xeri Pool로 재사용한다.

# 특이사항, 제약사항
입력 차단·바깥 클릭 닫기·좌표 변환·팝업 표면은 PopupHost가 맡으며 Editor와 Runtime이 같은 방식이다.
루트와 하위 메뉴의 위치·방향 정책은 XeriContextMenuPlacement가, UXML·USS 리소스는 XeriContextMenuResources가 맡는다.
메뉴 폭·행 높이 같은 치수는 XeriContextMenuStyle의 밀도(XeriContextMenuMetrics)에서만 정하고 요소에 직접 적용한다.
색·글꼴은 USS 클래스(기본은 Compact, 테마·Expanded는 수정 클래스)가 정하므로 USS에서 같은 치수를 다시 정하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Pool;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 런타임과 Editor의 드롭다운·우클릭에서 공유하는 컨텍스트 메뉴.
    /// </summary>
    // ============================================================
    public sealed class XeriContextMenu
    {

    #region 상수 및 필드

        private static WeakReference<XeriContextMenu> activeMenu;
        private static readonly Pool<XeriContextMenuLevel> levelPool = new();
        private static readonly Pool<XeriContextMenuRow> actionPool = new();
        private static readonly Pool<Label> labelPool = new();
        private static readonly Pool<VisualElement> separatorPool = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 스타일을 지정하지 않고 만든 메뉴가 쓰는 기본 스타일.
        /// </summary>
        // ------------------------------------------------------------
        public static XeriContextMenuStyle DefaultStyle { get; set; } = XeriContextMenuStyle.Compact;

        private readonly VisualElement owner;
        private readonly Action<KeyDownEvent> onUnhandledKeyDown;
        private readonly List<XeriContextMenuLevel> levels = new();
        private PopupHost host;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 런타임 패널에서 메뉴를 놓을 수 있는 영역을 패널 루트 좌표로 반환하는 함수.
        /// <br/> Safe Area처럼 패널 일부만 쓰는 호스트가 지정하며 패널 안의 영역을 반환해야 한다.
        /// <br/> 지정하지 않으면 패널 루트 전체를 쓰고 Editor에서는 모니터 작업 영역을 쓴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public Func<VisualElement, Rect> RuntimeWorkAreaProvider { get; set; }

        internal XeriContextMenuStyle Style { get; }
        internal XeriContextMenuMetrics Metrics { get; }
        internal bool UseLightTheme { get; }
        internal int LevelCount => levels.Count;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 메뉴의 표시 대상과 스타일(테마·밀도)을 지정한다.
        /// <br/> 스타일을 생략하면 DefaultStyle을 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriContextMenu
        (
            VisualElement owner,
            Action<KeyDownEvent> onUnhandledKeyDown = null,
            XeriContextMenuStyle? style = null
        )
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.onUnhandledKeyDown = onUnhandledKeyDown;
            Style = style ?? DefaultStyle;
            Metrics = Style.Metrics;
            UseLightTheme = Style.Theme == XeriContextMenuTheme.Auto
                ? GetDefaultLightTheme()
                : Style.Theme == XeriContextMenuTheme.Light;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Editor 스킨을 따르며 플레이어에서는 다크 테마를 기본으로 한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool GetDefaultLightTheme()
        {
#if UNITY_EDITOR
            return !UnityEditor.EditorGUIUtility.isProSkin;
#else
            return false;
#endif
        }

    #endregion

    #region 열기와 종료

        // ------------------------------------------------------------
        /// <summary>
        /// 기준 요소의 아래쪽에 붙여 메뉴를 열고 공간이 부족하면 위쪽에 연다.
        /// </summary>
        // ------------------------------------------------------------
        public void OpenBelow(IEnumerable<XeriContextMenuItem> items)
        {
            OpenRoot(owner.worldBound, items);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 월드 좌표를 루트 메뉴의 기준점으로 사용해 연다.
        /// </summary>
        // ------------------------------------------------------------
        public void Open(Vector2 worldPosition, IEnumerable<XeriContextMenuItem> items)
        {
            OpenRoot(new Rect(worldPosition, Vector2.zero), items);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 월드 좌표의 영역 또는 점을 기준으로 루트 메뉴를 연다.
        /// </summary>
        // ------------------------------------------------------------
        private void OpenRoot(Rect worldAnchor, IEnumerable<XeriContextMenuItem> items)
        {
            // 잘못된 경로는 패널 상태와 관계없이 호출한 쪽에서 바로 드러나게 먼저 푼다.
            var tree = XeriContextMenuItem.Resolve(items);
            Close();
            var root = owner.panel?.visualTree;

            if (root == null || tree.Count == 0)
            {
                return;
            }

            if (activeMenu != null && activeMenu.TryGetTarget(out var previous)
                && previous != this)
            {
                previous.Close();
            }

            activeMenu = new WeakReference<XeriContextMenu>(this);

            try
            {
                host = PopupHost.Open(owner, worldAnchor, RuntimeWorkAreaProvider, Close);
                var anchorBounds = host.ToPopupRect(worldAnchor);
                var level = CreateLevel(tree, 0, null, anchorBounds);
                level.Element.Focus();
            }
            catch
            {
                Close();
                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 하위 메뉴와 배경을 함께 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        public void Close()
        {
            CloseLevelsFrom(0);
            host?.Close();
            host = null;

            if (activeMenu != null && activeMenu.TryGetTarget(out var current)
                && current == this)
            {
                activeMenu = null;
            }
        }

    #endregion

    #region 메뉴 레벨 구성

        // ------------------------------------------------------------
        /// <summary>
        /// UXML로 메뉴 레벨을 만들고 항목과 화면 위치를 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        private XeriContextMenuLevel CreateLevel
        (
            IReadOnlyList<XeriContextMenuItem> items, int depth,
            Button parentButton, Rect anchorBounds, float crossOffset = 0f
        )
        {
            var level = levelPool.Acquire();
            level.Bind(this, depth, parentButton);

            try
            {
                var contentHeight = Metrics.ChromeHeight;

                foreach (var item in items)
                {
                    if (item.Kind == XeriContextMenuItem.ItemKind.Separator)
                    {
                        var separator = separatorPool.Acquire();
                        separator.AddToClassList("xeri-context-menu__separator");
                        separator.style.marginTop = Metrics.SeparatorMargin;
                        separator.style.marginBottom = Metrics.SeparatorMargin;
                        separator.RegisterCallback<PointerEnterEvent>
                        (
                            level.HandleNonActionPointerEnter
                        );
                        level.Scroll.Add(separator);
                        level.Separators.Add(separator);
                        contentHeight += Metrics.SeparatorHeight;
                    }
                    else if (item.Kind == XeriContextMenuItem.ItemKind.Label)
                    {
                        var label = labelPool.Acquire();
                        label.text = item.Text;
                        label.AddToClassList("xeri-context-menu__label-row");
                        label.style.height = Metrics.ActionHeight;
                        label.style.minHeight = Metrics.ActionHeight;
                        label.RegisterCallback<PointerEnterEvent>
                        (
                            level.HandleNonActionPointerEnter
                        );
                        level.Scroll.Add(label);
                        level.Labels.Add(label);
                        contentHeight += Metrics.ActionHeight;
                    }
                    else
                    {
                        AppendAction(level, item);
                        contentHeight += Metrics.ActionHeight;
                    }
                }

                var parent = depth > 0 ? levels[depth - 1] : null;
                XeriContextMenuPlacement.Place(host, Metrics, level, parent, contentHeight, anchorBounds, crossOffset);
                host.OpenLevel(depth, level.Bounds).Add(level.Element);
                levels.Add(level);
                return level;
            }
            catch
            {
                host?.CloseFrom(depth);
                ReleaseLevel(level);
                throw;
            }
        }

    #endregion

    #region 메뉴 항목 연결 및 반환

        // ------------------------------------------------------------
        /// <summary>
        /// 명령 행의 표시 상태를 갱신하고 해당 메뉴에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void AppendAction(XeriContextMenuLevel level, XeriContextMenuItem item)
        {
            var view = actionPool.Acquire();

            try
            {
                var enabled = view.Bind(level, item);
                level.Scroll.Add(view.Button);
                level.Actions.Add(view);

                if (enabled)
                {
                    level.Buttons.Add(view.Button);
                    level.Items.Add(view.Button, item);
                }
            }
            catch
            {
                view.Unbind();
                actionPool.Release(view);
                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 요소의 이벤트 상태와 연결을 정리하고 풀에 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ReleaseLevel(XeriContextMenuLevel level)
        {
            level.Unbind();

            foreach (var view in level.Actions)
            {
                view.Unbind();
                actionPool.Release(view);
            }

            foreach (var label in level.Labels)
            {
                label.UnregisterCallback<PointerEnterEvent>
                (
                    level.HandleNonActionPointerEnter
                );
                label.RemoveFromHierarchy();
                label.text = string.Empty;
                labelPool.Release(label);
            }

            foreach (var separator in level.Separators)
            {
                separator.UnregisterCallback<PointerEnterEvent>
                (
                    level.HandleNonActionPointerEnter
                );
                separator.RemoveFromHierarchy();
                separatorPool.Release(separator);
            }

            level.Actions.Clear();
            level.Labels.Clear();
            level.Separators.Clear();
            level.Buttons.Clear();
            level.Items.Clear();
            levelPool.Release(level);
        }

    #endregion

    #region 포인터 탐색

        // ------------------------------------------------------------
        /// <summary>
        /// 포인터가 이동한 항목을 즉시 활성화한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void HandleItemPointerEnter(XeriContextMenuLevel level, Button button)
        {
            if (levels.Count > level.Depth + 1
                && levels[level.Depth + 1].ParentButton == button)
            {
                level.SetActive(button, false);
                return;
            }

            SelectHoveredItem(level, button);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 포인터가 가리키는 항목을 선택하고 하위 메뉴를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void SelectHoveredItem(XeriContextMenuLevel level, Button button)
        {
            if (button == null || !level.Items.TryGetValue(button, out var item))
            {
                level.SetActive(null, false);
                CloseLevelsFrom(level.Depth + 1);
                return;
            }

            level.SetActive(button, true);

            if (item.Kind == XeriContextMenuItem.ItemKind.Submenu)
            {
                OpenSubmenu(level, button, item, false);
            }
            else
            {
                CloseLevelsFrom(level.Depth + 1);
            }
        }

    #endregion

    #region 하위 메뉴

        // ------------------------------------------------------------
        /// <summary>
        /// 부모 항목 클릭으로 해당 하위 메뉴 전체를 열거나 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        internal void ToggleSubmenu
        (
            XeriContextMenuLevel parent, Button button, XeriContextMenuItem item
        )
        {
            if (levels.Count > parent.Depth + 1
                && levels[parent.Depth + 1].ParentButton == button)
            {
                CloseLevelsFrom(parent.Depth + 1);
                parent.SetActive(button, true);
                return;
            }

            OpenSubmenu(parent, button, item, true);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 항목의 하위 메뉴를 열고 포커스를 선택적으로 이동한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OpenSubmenu
        (
            XeriContextMenuLevel parent, Button button, XeriContextMenuItem item, bool focusFirst
        )
        {
            if (levels.Count > parent.Depth + 1
                && levels[parent.Depth + 1].ParentButton == button)
            {
                if (focusFirst)
                {
                    levels[parent.Depth + 1].FocusFirst();
                }

                return;
            }

            var root = owner.panel?.visualTree;

            if (root == null || item.Children.Count == 0)
            {
                return;
            }

            // 부모 창이 OS에 의해 밀렸을 수 있으므로 하위 메뉴는 부모가 실제로 놓인 경계에 붙인다.
            parent.Bounds = host.GetBounds(parent.Depth);

            // 하위 메뉴의 윗선은 부모 항목의 윗선보다 안쪽 여백만큼 위에 맞춘다.
            var crossOffset = button.worldBound.y - parent.Element.worldBound.y
                - Metrics.VerticalPadding;
            CloseLevelsFrom(parent.Depth + 1);
            var child = CreateLevel
            (
                item.Children, parent.Depth + 1, button, parent.Bounds, crossOffset
            );

            if (focusFirst)
            {
                child.FocusFirst();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 깊이부터 열린 하위 메뉴를 모두 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        internal void CloseLevelsFrom(int depth)
        {
            for (var index = levels.Count - 1; index >= depth; index--)
            {
                var level = levels[index];
                levels.RemoveAt(index);
                ReleaseLevel(level);
            }

            host?.CloseFrom(depth);
        }

    #endregion

    #region 단계와 행 연결

        // ------------------------------------------------------------
        /// <summary>
        /// 단계 스택에서 지정한 깊이의 단계를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal XeriContextMenuLevel GetLevel(int depth)
        {
            return levels[depth];
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴를 연 요소로 포커스를 되돌린다.
        /// </summary>
        // ------------------------------------------------------------
        internal void FocusOwner()
        {
            owner.Focus();
        }

    #endregion

    #region 키보드 및 포커스

        // ------------------------------------------------------------
        /// <summary>
        /// 포커스가 옮겨진 항목에 속하지 않는 하위 메뉴를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void HandleItemFocusIn(XeriContextMenuLevel level, Button button)
        {
            level.SetActive(button, false);

            if (levels.Count > level.Depth + 1
                && levels[level.Depth + 1].ParentButton != button)
            {
                CloseLevelsFrom(level.Depth + 1);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 계층 사이의 키보드 탐색과 닫기 동작을 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void HandleMenuKeyDown(KeyDownEvent evt, XeriContextMenuLevel level)
        {
            var handled = true;

            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    Close();
                    owner.Focus();
                    break;

                case KeyCode.UpArrow:
                    MoveActive(level, -1);
                    break;

                case KeyCode.DownArrow:
                    MoveActive(level, 1);
                    break;

                case KeyCode.Tab:
                    MoveActive(level, evt.shiftKey ? -1 : 1);
                    break;

                // 오른쪽(열기)과 왼쪽(닫기)은 짝이며, 열 수 없거나 닫을 수 없으면 처리하지 않은 키로 넘긴다.
                case KeyCode.RightArrow:
                case KeyCode.Space:
                case var key when XeriShortcutKeys.IsSubmit(key):
                    handled = TryOpenSubmenu(level, evt.target);
                    break;

                case KeyCode.LeftArrow:
                    handled = TryCloseSubmenu(level);
                    break;

                default:
                    handled = false;
                    break;
            }

            if (handled)
            {
                StopKeyEvent(evt);
            }
            else
            {
                onUnhandledKeyDown?.Invoke(evt);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키보드·게임패드 확인 입력으로 하위 메뉴를 연다.
        /// </summary>
        // ------------------------------------------------------------
        internal void HandleMenuSubmit(NavigationSubmitEvent evt, XeriContextMenuLevel level)
        {
            if (TryOpenSubmenu(level, evt.target))
            {
                evt.StopPropagation();
                evt.StopImmediatePropagation();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 단계의 활성 항목을 step(-1 위, 1 아래)만큼 옮기고 끝에서는 반대편으로 돌린다.
        /// </summary>
        // ------------------------------------------------------------
        private void MoveActive(XeriContextMenuLevel level, int step)
        {
            if (level.Buttons.Count == 0)
            {
                return;
            }

            var index = level.Buttons.IndexOf(level.ActiveButton);
            var next = index < 0
                ? (step > 0 ? 0 : level.Buttons.Count - 1)
                : (index + step + level.Buttons.Count) % level.Buttons.Count;
            var button = level.Buttons[next];
            level.SetActive(button, true);
            level.Scroll.ScrollTo(button);
            CloseLevelsFrom(level.Depth + 1);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 입력을 받은 요소가 하위 메뉴 항목이면 하위 메뉴를 열고 true를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool TryOpenSubmenu(XeriContextMenuLevel level, IEventHandler target)
        {
            if (target is Button button
                && level.Items.TryGetValue(button, out var item)
                && item.Kind == XeriContextMenuItem.ItemKind.Submenu)
            {
                OpenSubmenu(level, button, item, true);
                return true;
            }

            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이 단계가 하위 메뉴이면 닫고 부모 항목으로 돌아가 true를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool TryCloseSubmenu(XeriContextMenuLevel level)
        {
            if (level.ParentButton == null)
            {
                return false;
            }

            var parent = levels[level.Depth - 1];
            var parentButton = level.ParentButton;
            CloseLevelsFrom(level.Depth);
            parent.SetActive(parentButton, true);
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴에서 처리한 키 입력이 상위 UI로 전달되지 않게 한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void StopKeyEvent(KeyDownEvent evt)
        {
            evt.StopPropagation();
            evt.StopImmediatePropagation();
        }

    #endregion

    }
}
