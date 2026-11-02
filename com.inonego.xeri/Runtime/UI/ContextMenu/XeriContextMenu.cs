/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriContextMenu.cs
수정일 : 2026-10-28

# 설명
Resources의 UXML·USS와 Xeri Pool을 활용해 메뉴 단계를 재사용한다.
런타임 패널과 Editor의 독립 팝업 창에서 공통 입력·배치 동작을 제공한다.

# 특이사항, 제약사항
입력 차단·바깥 클릭 닫기·좌표 변환·팝업 표면은 PopupHost가 맡으며 Editor와 Runtime이 같은 방식이다.
루트와 하위 메뉴의 위치·방향은 모두 PopupPlacementSolver가 계산하며 이 파일은 정책(선호 방향·간격·여백)만 정한다.
모든 배치 좌표는 호스트가 변환한 Y-down 팝업 좌표계이고, 배치 영역은 호스트가 제공하는 작업 영역이다.
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

    #region 내부 타입

        // ============================================================
        /// <summary>
        /// 중첩 메뉴 한 단계의 UI와 선택·입력 상태를 보관한다.
        /// </summary>
        // ============================================================
        private sealed class MenuLevel
        {

        #region 상태 및 요소

            public XeriContextMenu Menu;
            public readonly VisualElement Element;
            public readonly ScrollView Scroll;
            public Button ParentButton;
            public Button ActiveButton;
            public Rect Bounds;
            public PopupPlacementDirection Direction;
            public int Depth;
            public readonly List<Button> Buttons = new();
            public readonly Dictionary<Button, XeriContextMenuItem> Items = new();
            public readonly List<MenuActionView> Actions = new();
            public readonly List<Label> Labels = new();
            public readonly List<VisualElement> Separators = new();

        #endregion

        #region 생성 및 수명주기

            // ------------------------------------------------------------
            /// <summary>
            /// 메뉴 단계의 UXML 요소와 입력 이벤트를 초기화한다.
            /// </summary>
            // ------------------------------------------------------------
            public MenuLevel()
            {
                var layout = levelLayout.Instantiate();
                Element = layout.Q<VisualElement>("context-menu-level");
                Scroll = layout.Q<ScrollView>("context-menu-scroll");

                if (Element == null || Scroll == null)
                {
                    throw new InvalidOperationException("컨텍스트 메뉴 레벨 요소를 찾을 수 없습니다.");
                }

                Element.RemoveFromHierarchy();
                Element.styleSheets.Add(menuStyle);
                Element.focusable = true;
                Element.RegisterCallback<PointerDownEvent>(HandlePointerDown);
                Element.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
                Element.RegisterCallback<KeyDownEvent>
                (
                    HandleKeyDown, TrickleDown.TrickleDown
                );
                Element.RegisterCallback<NavigationSubmitEvent>
                (
                    HandleSubmit, TrickleDown.TrickleDown
                );
                Scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                Scroll.verticalScroller.valueChanged += HandleScrollChanged;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 재사용할 메뉴 단계에 소유자와 표시 상태를 연결한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Bind(XeriContextMenu menu, int depth, Button parentButton)
            {
                Menu = menu;
                Depth = depth;
                ParentButton = parentButton;

                // 색과 글꼴은 USS 클래스로, 배치에 쓰는 치수는 스타일 값을 직접 적용한다.
                var metrics = menu.metrics;
                Element.EnableInClassList("xeri-context-menu--light", menu.useLightTheme);
                Element.EnableInClassList
                (
                    "xeri-context-menu--expanded",
                    menu.style.Density == XeriContextMenuDensity.Expanded
                );
                Element.style.width = metrics.Width;
                Element.style.minWidth = metrics.Width;
                Element.style.paddingTop = metrics.VerticalPadding;
                Element.style.paddingBottom = metrics.VerticalPadding;
                Element.style.borderTopWidth = metrics.BorderWidth;
                Element.style.borderBottomWidth = metrics.BorderWidth;
                Element.style.borderLeftWidth = metrics.BorderWidth;
                Element.style.borderRightWidth = metrics.BorderWidth;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 메뉴 단계의 선택·스크롤·소유 상태를 초기화한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Unbind()
            {
                Element.RemoveFromHierarchy();
                Element.RemoveFromClassList("xeri-context-menu--light");
                Element.RemoveFromClassList("xeri-context-menu--expanded");
                ActiveButton?.RemoveFromClassList("xeri-context-menu__action--active");
                Menu = null;
                ActiveButton = null;
                ParentButton = null;
                Scroll.scrollOffset = Vector2.zero;
                Depth = 0;
                Bounds = default;
                Direction = PopupPlacementDirection.Right;
            }

        #endregion

        #region 이벤트 처리

            // ------------------------------------------------------------
            /// <summary>
            /// 명령 행 이외를 클릭하면 하위 메뉴와 선택 상태를 정리한다.
            /// </summary>
            // ------------------------------------------------------------
            private void HandlePointerDown(PointerDownEvent evt)
            {
                if (Menu == null)
                {
                    return;
                }

                if (evt.target is not Button button || !Items.ContainsKey(button))
                {
                    Menu.CloseLevelsFrom(Depth + 1);
                    SetActive(this, null, false);
                }

                evt.StopPropagation();
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 하위 메뉴에 포인터가 진입하면 부모의 연결된 항목을 활성화한다.
            /// </summary>
            // ------------------------------------------------------------
            private void HandlePointerEnter(PointerEnterEvent evt)
            {
                if (Menu != null && Depth > 0)
                {
                    var parent = Menu.levels[Depth - 1];
                    SetActive(parent, ParentButton, false);
                }
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 라벨·구분선 진입 시 선택과 하위 메뉴를 정리한다.
            /// </summary>
            // ------------------------------------------------------------
            public void HandleNonActionPointerEnter(PointerEnterEvent evt)
            {
                Menu?.HandleItemPointerEnter(this, null);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 메뉴 단계의 키 입력을 컨텍스트 메뉴의 탐색 처리기로 전달한다.
            /// </summary>
            // ------------------------------------------------------------
            private void HandleKeyDown(KeyDownEvent evt)
            {
                Menu?.HandleMenuKeyDown(evt, this);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 메뉴의 선택 확정 입력을 전달한다.
            /// </summary>
            // ------------------------------------------------------------
            private void HandleSubmit(NavigationSubmitEvent evt)
            {
                Menu?.HandleMenuSubmit(evt, this);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 부모 메뉴가 스크롤되면 항목 위치가 달라진 하위 메뉴를 닫는다.
            /// </summary>
            // ------------------------------------------------------------
            private void HandleScrollChanged(float value)
            {
                if (Menu != null && Menu.levels.Count > Depth + 1)
                {
                    Menu.CloseLevelsFrom(Depth + 1);
                }
            }

        #endregion

        }

        // ============================================================
        /// <summary>
        /// UXML로 만든 명령 행을 메뉴 사이에서 안전하게 재사용한다.
        /// </summary>
        // ============================================================
        private sealed class MenuActionView
        {

        #region 상태 및 요소

            public readonly Button Button;
            public readonly Image Icon;
            public readonly Label Check;
            public readonly Label Label;
            public readonly Label Shortcut;
            public MenuLevel Level;
            public XeriContextMenuItem Item;

        #endregion

        #region 생성 및 수명주기

            // ------------------------------------------------------------
            /// <summary>
            /// 메뉴 항목의 UXML 요소와 이벤트를 초기화한다.
            /// </summary>
            // ------------------------------------------------------------
            public MenuActionView()
            {
                var layout = actionLayout.Instantiate();
                Button = layout.Q<Button>("context-menu-action");
                Icon = layout.Q<Image>("context-menu-icon");
                Check = layout.Q<Label>("context-menu-check");
                Label = layout.Q<Label>("context-menu-label");
                Shortcut = layout.Q<Label>("context-menu-shortcut");

                if (Button == null || Icon == null || Check == null
                    || Label == null || Shortcut == null)
                {
                    throw new InvalidOperationException("컨텍스트 메뉴 항목 요소를 찾을 수 없습니다.");
                }

                Button.RemoveFromHierarchy();
                Icon.pickingMode = PickingMode.Ignore;
                Check.pickingMode = PickingMode.Ignore;
                Label.pickingMode = PickingMode.Ignore;
                Shortcut.pickingMode = PickingMode.Ignore;
                Button.clicked += HandleClick;
                Button.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
                Button.RegisterCallback<FocusInEvent>(HandleFocusIn);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 명령 항목을 행 UI에 연결하고 활성 여부를 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public bool Bind(MenuLevel level, XeriContextMenuItem item)
            {
                var isSubmenu = item.Kind == XeriContextMenuItem.ItemKind.Submenu;
                var enabled = item.Enabled && (!isSubmenu || item.Children.Count > 0);
                Level = level;
                Item = item;
                Button.style.height = level.Menu.metrics.ActionHeight;
                Button.style.minHeight = level.Menu.metrics.ActionHeight;
                Button.SetEnabled(enabled);
                Label.text = item.Label;
                Shortcut.text = isSubmenu ? "▸" : item.Shortcut ?? string.Empty;

                if (item.Icon != null)
                {
                    Icon.image = item.Icon;
                    Icon.scaleMode = ScaleMode.ScaleToFit;
                    Icon.style.display = DisplayStyle.Flex;
                }
                else if (item.IsChecked)
                {
                    Check.style.display = DisplayStyle.Flex;
                }

                if (isSubmenu)
                {
                    Shortcut.AddToClassList("xeri-context-menu__shortcut--submenu");
                }
                else if (string.IsNullOrEmpty(item.Shortcut))
                {
                    Shortcut.style.display = DisplayStyle.None;
                }

                return enabled;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 명령 행의 데이터와 표시 상태를 해제해 풀에 반환할 수 있게 한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Unbind()
            {
                Button.RemoveFromHierarchy();
                Button.RemoveFromClassList("xeri-context-menu__action--active");
                Button.SetEnabled(true);
                Icon.image = null;
                Icon.style.display = DisplayStyle.None;
                Check.style.display = DisplayStyle.None;
                Label.text = string.Empty;
                Shortcut.text = string.Empty;
                Shortcut.style.display = DisplayStyle.Flex;
                Shortcut.RemoveFromClassList("xeri-context-menu__shortcut--submenu");
                Level = null;
                Item = null;
            }

        #endregion

        #region 이벤트 처리

            // ------------------------------------------------------------
            /// <summary>
            /// 항목을 실행하거나 해당 하위 메뉴를 전환한다.
            /// </summary>
            // ------------------------------------------------------------
            private void HandleClick()
            {
                var menu = Level?.Menu;
                var item = Item;

                if (menu == null || item == null)
                {
                    return;
                }

                if (item.Kind == XeriContextMenuItem.ItemKind.Submenu)
                {
                    menu.ToggleSubmenu(Level, Button, item);
                    return;
                }

                menu.Close();
                menu.owner.Focus();
                item.Execute?.Invoke();
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 포인터가 들어온 명령 행을 선택하고 필요하면 하위 메뉴를 연다.
            /// </summary>
            // ------------------------------------------------------------
            private void HandlePointerEnter(PointerEnterEvent evt)
            {
                Level?.Menu?.HandleItemPointerEnter(Level, Button);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 포커스를 받은 명령 행을 현재 메뉴 단계의 활성 항목으로 반영한다.
            /// </summary>
            // ------------------------------------------------------------
            private void HandleFocusIn(FocusInEvent evt)
            {
                Level?.Menu?.HandleItemFocusIn(Level, Button);
            }

        #endregion

        }

    #endregion

    #region 상수 및 필드

        // 화면 가장자리 여백과 서브메뉴 간격은 밀도와 무관한 배치 정책이다.
        private const string resourceRoot = "XeriUI/ContextMenu/";
        private const float menuMargin = 4f;
        private const float submenuGap = 1f;

        private static WeakReference<XeriContextMenu> activeMenu;
        private static readonly Pool<MenuLevel> levelPool = new();
        private static readonly Pool<MenuActionView> actionPool = new();
        private static readonly Pool<Label> labelPool = new();
        private static readonly Pool<VisualElement> separatorPool = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 스타일을 지정하지 않고 만든 메뉴가 쓰는 기본 스타일.
        /// </summary>
        // ------------------------------------------------------------
        public static XeriContextMenuStyle DefaultStyle { get; set; } = XeriContextMenuStyle.Compact;

        private static VisualTreeAsset levelLayout;
        private static VisualTreeAsset actionLayout;
        private static StyleSheet menuStyle;

        private readonly VisualElement owner;
        private readonly XeriContextMenuStyle style;
        private readonly XeriContextMenuMetrics metrics;
        private readonly bool useLightTheme;
        private readonly Action<KeyDownEvent> onUnhandledKeyDown;
        private readonly List<XeriContextMenuItem> entries = new();
        private readonly List<MenuLevel> levels = new();
        private PopupHost host;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 런타임 패널에서 메뉴를 놓을 수 있는 영역을 패널 루트 좌표로 반환하는 함수.
        /// <br/> Safe Area처럼 패널 일부만 쓰는 호스트가 지정하며 패널 안의 영역을 반환해야 한다.
        /// <br/> 지정하지 않으면 패널 루트 전체를 쓰고 Editor에서는 모니터 작업 영역을 쓴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public Func<VisualElement, Rect> RuntimeWorkAreaProvider { get; set; }

    #endregion

    #region 생성과 리소스

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
            this.style = style ?? DefaultStyle;
            metrics = this.style.Metrics;
            useLightTheme = this.style.Theme == XeriContextMenuTheme.Auto
                ? GetDefaultLightTheme()
                : this.style.Theme == XeriContextMenuTheme.Light;

            EnsureResourcesLoaded();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UXML·USS 리소스를 메뉴 인스턴스 전체에서 공유한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void EnsureResourcesLoaded()
        {
            if (levelLayout == null)
            {
                levelLayout = LoadResource<VisualTreeAsset>("XeriContextMenu");
            }

            if (actionLayout == null)
            {
                actionLayout = LoadResource<VisualTreeAsset>("XeriContextMenuAction");
            }

            if (menuStyle == null)
            {
                menuStyle = LoadResource<StyleSheet>("XeriContextMenuStyle");
            }
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

        // ------------------------------------------------------------
        /// <summary>
        /// 필수 UI 리소스를 Resources에서 로드한다.
        /// </summary>
        // ------------------------------------------------------------
        private static T LoadResource<T>(string name)
        where T : UnityEngine.Object
        {
            var path = $"{resourceRoot}{name}";
            var asset = Resources.Load<T>(path);

            if (asset == null)
            {
                throw new InvalidOperationException($"컨텍스트 메뉴 리소스를 찾을 수 없습니다: {path}");
            }

            return asset;
        }

    #endregion

    #region 항목 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 메뉴 항목을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear()
        {
            entries.Clear();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실행할 명령과 표시 정보를 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddAction
        (
            string label, Action execute, bool enabled = true,
            string shortcut = null, bool isChecked = false, Texture2D icon = null
        )
        {
            entries.Add
            (
                XeriContextMenuItem.CreateAction
                (
                    label, execute, enabled, shortcut, isChecked, icon
                )
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택할 수 없는 텍스트 라벨을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddLabel(string label)
        {
            entries.Add(XeriContextMenuItem.CreateLabel(label));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 명령을 중첩할 하위 메뉴를 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriContextMenuItem AddSubmenu(string label)
        {
            var item = XeriContextMenuItem.CreateSubmenu(label);
            entries.Add(item);
            return item;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 항목 사이에 구분선을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddSeparator()
        {
            entries.Add(XeriContextMenuItem.CreateSeparator());
        }

    #endregion

    #region 열기와 종료

        // ------------------------------------------------------------
        /// <summary>
        /// 기준 요소의 아래쪽에 붙여 메뉴를 열고 공간이 부족하면 위쪽에 연다.
        /// </summary>
        // ------------------------------------------------------------
        public void OpenBelow()
        {
            OpenRoot(owner.worldBound);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 월드 좌표를 루트 메뉴의 기준점으로 사용해 연다.
        /// </summary>
        // ------------------------------------------------------------
        public void Open(Vector2 worldPosition)
        {
            OpenRoot(new Rect(worldPosition, Vector2.zero));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 월드 좌표의 영역 또는 점을 기준으로 루트 메뉴를 연다.
        /// </summary>
        // ------------------------------------------------------------
        private void OpenRoot(Rect worldAnchor)
        {
            Close();
            var root = owner.panel?.visualTree;

            if (root == null || entries.Count == 0)
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
                var level = CreateLevel(entries, 0, null, anchorBounds);
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
        private MenuLevel CreateLevel
        (
            IReadOnlyList<XeriContextMenuItem> items, int depth,
            Button parentButton, Rect anchorBounds, float crossOffset = 0f
        )
        {
            var level = levelPool.Acquire();
            level.Bind(this, depth, parentButton);

            try
            {
                var contentHeight = metrics.ChromeHeight;

                foreach (var item in items)
                {
                    if (item.Kind == XeriContextMenuItem.ItemKind.Separator)
                    {
                        var separator = separatorPool.Acquire();
                        separator.AddToClassList("xeri-context-menu__separator");
                        separator.style.marginTop = metrics.SeparatorMargin;
                        separator.style.marginBottom = metrics.SeparatorMargin;
                        separator.RegisterCallback<PointerEnterEvent>
                        (
                            level.HandleNonActionPointerEnter
                        );
                        level.Scroll.Add(separator);
                        level.Separators.Add(separator);
                        contentHeight += metrics.SeparatorHeight;
                    }
                    else if (item.Kind == XeriContextMenuItem.ItemKind.Label)
                    {
                        var label = labelPool.Acquire();
                        label.text = item.Label;
                        label.AddToClassList("xeri-context-menu__label-row");
                        label.style.height = metrics.ActionHeight;
                        label.style.minHeight = metrics.ActionHeight;
                        label.RegisterCallback<PointerEnterEvent>
                        (
                            level.HandleNonActionPointerEnter
                        );
                        level.Scroll.Add(label);
                        level.Labels.Add(label);
                        contentHeight += metrics.ActionHeight;
                    }
                    else
                    {
                        AppendAction(level, item);
                        contentHeight += metrics.ActionHeight;
                    }
                }

                PlaceLevel(level, contentHeight, anchorBounds, crossOffset);
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

    #region 메뉴 배치

        // ----------------------------------------------------------------------
        /// <summary>
        /// 공통 Popup 배치 계산기로 메뉴 단계의 화면 경계와 하위 메뉴 방향을 정한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void PlaceLevel
        (
            MenuLevel level, float contentHeight, Rect anchorBounds, float crossOffset
        )
        {
            var isRoot = level.Depth == 0;
            var options = isRoot
                ? CreateRootPlacementOptions()
                : CreateSubmenuPlacementOptions(levels[level.Depth - 1].Direction, crossOffset);
            var result = host.Solve
            (
                anchorBounds, new Vector2(metrics.Width, contentHeight), options
            );

            // 루트는 좌우의 남은 공간으로, 하위 메뉴는 실제 선택된 방향으로 다음 단계의 선호 방향을 정한다.
            var bounds = result.Bounds;
            level.Direction = isRoot
                ? GetRootChildDirection(host.WorkArea, bounds) : result.Direction;

            // 높이를 줄였을 때만 스크롤을 허용한다. 줄이지 않았다면 내용이 들어가므로 배율에 따른
            // 픽셀 반올림 오차로 스크롤바가 생기지 않게 숨긴다.
            level.Scroll.verticalScrollerVisibility = result.WasResized
                ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
            level.Element.style.height = bounds.height;
            level.Bounds = bounds;
            level.Element.style.left = 0f;
            level.Element.style.top = 0f;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 루트 메뉴는 기준 영역 아래를 우선하고 부족하면 위로 열며 높이는 마지막에만 줄인다.
        /// <br/> 왼쪽 정렬이 화면을 넘으면 오른쪽 모서리를 기준 영역에 맞춰 연다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private PopupPlacementOptions CreateRootPlacementOptions()
        {
            return new PopupPlacementOptions
            (
                PopupPlacementDirection.Below,
                padding: menuMargin,
                minMainSize: metrics.ActionHeight + metrics.ChromeHeight,
                allowAnchorOverlap: true,
                allowAlignmentFlip: true
            );
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 하위 메뉴는 부모가 열린 쪽을 우선하고 항목 높이에 맞춰 부모 옆에 붙인다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static PopupPlacementOptions CreateSubmenuPlacementOptions
        (
            PopupPlacementDirection parentDirection, float crossOffset
        )
        {
            // 메뉴 폭은 고정이므로 폭을 줄이지 않고, 양쪽이 모자라면 부모와 덜 겹치는 쪽에 놓는다.
            return new PopupPlacementOptions
            (
                parentDirection,
                gap: submenuGap,
                padding: menuMargin,
                allowMainShrink: false,
                allowAnchorOverlap: true,
                crossOffset: crossOffset
            );
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 루트 메뉴 좌우의 남은 공간을 비교해 하위 메뉴가 우선 열릴 방향을 구한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private PopupPlacementDirection GetRootChildDirection(Rect area, Rect rootBounds)
        {
            var rightSpace = area.xMax - rootBounds.xMax;
            var leftSpace = rootBounds.xMin - area.xMin;

            return rightSpace >= metrics.Width || rightSpace >= leftSpace
                ? PopupPlacementDirection.Right : PopupPlacementDirection.Left;
        }

    #endregion

    #region 메뉴 항목 연결 및 반환

        // ------------------------------------------------------------
        /// <summary>
        /// 명령 행의 표시 상태를 갱신하고 해당 메뉴에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void AppendAction(MenuLevel level, XeriContextMenuItem item)
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
        private static void ReleaseLevel(MenuLevel level)
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
        private void HandleItemPointerEnter(MenuLevel level, Button button)
        {
            if (levels.Count > level.Depth + 1
                && levels[level.Depth + 1].ParentButton == button)
            {
                SetActive(level, button, false);
                return;
            }

            SelectHoveredItem(level, button);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 포인터가 가리키는 항목을 선택하고 하위 메뉴를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void SelectHoveredItem(MenuLevel level, Button button)
        {
            if (button == null || !level.Items.TryGetValue(button, out var item))
            {
                SetActive(level, null, false);
                CloseLevelsFrom(level.Depth + 1);
                return;
            }

            SetActive(level, button, true);

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
        private void ToggleSubmenu
        (
            MenuLevel parent, Button button, XeriContextMenuItem item
        )
        {
            if (levels.Count > parent.Depth + 1
                && levels[parent.Depth + 1].ParentButton == button)
            {
                CloseLevelsFrom(parent.Depth + 1);
                SetActive(parent, button, true);
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
            MenuLevel parent, Button button, XeriContextMenuItem item, bool focusFirst
        )
        {
            if (levels.Count > parent.Depth + 1
                && levels[parent.Depth + 1].ParentButton == button)
            {
                if (focusFirst)
                {
                    FocusFirst(levels[parent.Depth + 1]);
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
                - metrics.VerticalPadding;
            CloseLevelsFrom(parent.Depth + 1);
            var child = CreateLevel
            (
                item.Children, parent.Depth + 1, button, parent.Bounds, crossOffset
            );

            if (focusFirst)
            {
                FocusFirst(child);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 깊이부터 열린 하위 메뉴를 모두 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        private void CloseLevelsFrom(int depth)
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

    #region 키보드 및 포커스

        // ------------------------------------------------------------
        /// <summary>
        /// 포커스가 옮겨진 항목에 속하지 않는 하위 메뉴를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleItemFocusIn(MenuLevel level, Button button)
        {
            SetActive(level, button, false);

            if (levels.Count > level.Depth + 1
                && levels[level.Depth + 1].ParentButton != button)
            {
                CloseLevelsFrom(level.Depth + 1);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 항목의 선택 색상과 키보드 포커스를 별도로 관리한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void SetActive(MenuLevel level, Button button, bool focus)
        {
            if (level.ActiveButton != button)
            {
                level.ActiveButton?.RemoveFromClassList("xeri-context-menu__action--active");
                level.ActiveButton = button;
                button?.AddToClassList("xeri-context-menu__action--active");
            }

            if (focus)
            {
                button?.Focus();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키보드로 하위 메뉴를 열 때 첫 번째 활성 항목을 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void FocusFirst(MenuLevel level)
        {
            if (level.Buttons.Count > 0)
            {
                SetActive(level, level.Buttons[0], true);
            }
            else
            {
                level.Element.Focus();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 계층 사이의 키보드 탐색과 닫기 동작을 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleMenuKeyDown(KeyDownEvent evt, MenuLevel level)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                Close();
                owner.Focus();
                StopKeyEvent(evt);
                return;
            }

            if (evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.DownArrow
                || evt.keyCode == KeyCode.Tab)
            {
                if (level.Buttons.Count > 0)
                {
                    var index = level.Buttons.IndexOf(level.ActiveButton);
                    var step = evt.keyCode == KeyCode.UpArrow
                        || (evt.keyCode == KeyCode.Tab && evt.shiftKey) ? -1 : 1;
                    var next = index < 0
                        ? (step > 0 ? 0 : level.Buttons.Count - 1)
                        : (index + step + level.Buttons.Count) % level.Buttons.Count;
                    var button = level.Buttons[next];
                    SetActive(level, button, true);
                    level.Scroll.ScrollTo(button);
                    CloseLevelsFrom(level.Depth + 1);
                }

                StopKeyEvent(evt);
                return;
            }

            if ((evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter
                    || evt.keyCode == KeyCode.Space)
                && evt.target is Button submitButton
                && level.Items.TryGetValue(submitButton, out var submitItem)
                && submitItem.Kind == XeriContextMenuItem.ItemKind.Submenu)
            {
                OpenSubmenu(level, submitButton, submitItem, true);
                StopKeyEvent(evt);
                return;
            }

            if (evt.keyCode == KeyCode.RightArrow
                && evt.target is Button rightButton
                && level.Items.TryGetValue(rightButton, out var rightItem)
                && rightItem.Kind == XeriContextMenuItem.ItemKind.Submenu)
            {
                OpenSubmenu(level, rightButton, rightItem, true);
                StopKeyEvent(evt);
                return;
            }

            if (evt.keyCode == KeyCode.LeftArrow && level.ParentButton != null)
            {
                var parent = levels[level.Depth - 1];
                var parentButton = level.ParentButton;
                CloseLevelsFrom(level.Depth);
                SetActive(parent, parentButton, true);
                StopKeyEvent(evt);
                return;
            }

            onUnhandledKeyDown?.Invoke(evt);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키보드·게임패드 확인 입력으로 하위 메뉴를 연다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleMenuSubmit(NavigationSubmitEvent evt, MenuLevel level)
        {
            if (evt.target is Button button
                && level.Items.TryGetValue(button, out var item)
                && item.Kind == XeriContextMenuItem.ItemKind.Submenu)
            {
                OpenSubmenu(level, button, item, true);
                evt.StopPropagation();
                evt.StopImmediatePropagation();
            }
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
