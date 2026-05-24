/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriWindowCanvas.cs
수정일 : 2026-05-23

# 설명
Xeri 커스텀 윈도우 패널을 배치하는 UITK 작업 공간.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Window
{
    // ============================================================
    /// <summary>
    /// 여러 XeriWindowPanel을 배치하는 UITK host.
    /// </summary>
    // ============================================================
    public sealed class XeriWindowCanvas : VisualElement
    {

    #region 필드

        private const string CANVAS_UXML_PATH = "XeriUI/Window/XeriWindowCanvas";
        private const string CANVAS_USS_PATH  = "XeriUI/Window/XeriWindowCanvas";

        // ------------------------------------------------------------
        /// <summary>
        /// Canvas 최상위 element.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement Root => this;

        // ------------------------------------------------------------
        /// <summary>
        /// Window panel이 배치되는 layer.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement WindowLayer => windowLayer;

        private readonly VisualElement windowLayer = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Canvas에 연결된 window registry.
        /// </summary>
        // ------------------------------------------------------------
        public IXeriWindowRegistry Registry => registry;

        private readonly IXeriWindowRegistry registry = null;

        private readonly IXeriWindowDragFactory dragFactory = null;
        private readonly Dictionary<XeriWindowHandle, WindowBinding> bindings = new();

    #endregion

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// Window view와 controller 연결 정보를 보관한다.
        /// </summary>
        // ============================================================
        private sealed class WindowBinding
        {
            public XeriWindowHandle Handle = null;
            public XeriWindowPanel Panel = null;
            public XeriWindowController Controller = null;
            public XeriWindowControlManipulator ControlManipulator = null;
            public XeriWindowResizeManipulator ResizeManipulator = null;
            public XeriWindowTitleBarManipulator TitleBarManipulator = null;
        }

    #endregion

    #region 생성자

        public XeriWindowCanvas() : this(null, null) {}

        // ------------------------------------------------------------
        /// <summary>
        /// Xeri window canvas를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriWindowCanvas
        (
            IXeriWindowRegistry registry,
            IXeriWindowDragFactory dragFactory = null
        ) : base()
        {
            name = "xeri-window-canvas";
            AddToClassList("xeri-window-canvas");
            ApplyDefaultLayout();
            LoadStyleSheet();

            this.registry    = registry ?? new XeriWindowRegistry();
            this.dragFactory = dragFactory ?? new XeriWindowDragFactory();

            windowLayer = CreateWindowLayer();

            ApplyWindowLayerLayout();

            hierarchy.Add(windowLayer);

            BindRegistry();
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 정보로 window panel과 controller를 생성해 canvas에 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriWindowHandle AddWindow
        (
            string id,
            string title,
            VisualElement view,
            Vector2 pos,
            Vector2 size,
            XeriWindowOptions? options = null
        )
        {
            var windowOptions = options ?? XeriWindowOptions.Default();
            var record = new XeriWindowRecord
            {
                ID = id ?? string.Empty,
                Title = title ?? id ?? string.Empty,
                Pos = pos,
                Size = size,
                NormalPos = pos,
                NormalSize = size,
                StackLayer = windowOptions.StackLayer,
            };

            return AddWindow(record, view, windowOptions);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 저장 가능한 record를 기반으로 window panel과 controller를 생성해 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriWindowHandle AddWindow
        (
            XeriWindowRecord record,
            VisualElement view,
            XeriWindowOptions? options = null
        )
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (registry.TryGetHandle(record.ID, out var exists))
            {
                return exists;
            }

            if (options.HasValue)
            {
                record.StackLayer = options.Value.StackLayer;
            }

            var panel = CreatePanel(record, view, options);
            var driver = new UITKWindowDriver(panel)
            {
                Pos = record.Pos,
                Size = record.Size,
                State = record.State,
            };

            var controller = new XeriWindowController(driver, options);
            var handle = registry.Register(record.ID, controller, record);
            var binding = CreateBinding(handle, panel, controller);

            bindings[handle] = binding;
            windowLayer.Add(panel);
            registry.Focus(handle);
            ApplyWindowOrder();

            return handle;
        }

    #endregion

    #region 내부 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Window panel을 생성하고 content와 option을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private static XeriWindowPanel CreatePanel
        (
            XeriWindowRecord record,
            VisualElement view,
            XeriWindowOptions? options
        )
        {
            var panel = new XeriWindowPanel();
            panel.AttachView(view);
            panel.ApplyOptions(options ?? XeriWindowOptions.Default());
            panel.ApplyTheme(record.ThemeID);

            var titleLabel = panel.Q<Label>("title-label");
            if (titleLabel != null)
            {
                titleLabel.text = record.Title ?? string.Empty;
            }

            return panel;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Canvas USS를 로드해 현재 element에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void LoadStyleSheet()
        {
            var styleSheet = Resources.Load<StyleSheet>(CANVAS_USS_PATH);

            if (styleSheet == null)
            {
                throw new InvalidOperationException($"XeriWindowCanvas USS를 로드할 수 없습니다. Path: {CANVAS_USS_PATH}");
            }

            styleSheets.Add(styleSheet);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Canvas가 부모 영역을 즉시 채우도록 기본 layout을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyDefaultLayout()
        {
            style.position = Position.Absolute;
            style.left = 0f;
            style.top = 0f;
            style.right = 0f;
            style.bottom = 0f;
            style.flexGrow = 1f;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Window layer가 canvas 영역 안에서 배치되도록 기본 layout을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyWindowLayerLayout()
        {
            windowLayer.style.position = Position.Absolute;
            windowLayer.style.left = 0f;
            windowLayer.style.top = 0f;
            windowLayer.style.right = 0f;
            windowLayer.style.bottom = 0f;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Canvas UXML에서 window layer를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement CreateWindowLayer()
        {
            var template = Resources.Load<VisualTreeAsset>(CANVAS_UXML_PATH);

            if (template == null)
            {
                throw new InvalidOperationException($"XeriWindowCanvas UXML을 로드할 수 없습니다. Path: {CANVAS_UXML_PATH}");
            }

            var tree = template.CloneTree();

            var windowLayer = tree.Q<VisualElement>("window-layer");

            if (windowLayer == null)
            {
                throw new InvalidOperationException("XeriWindowCanvas UXML에 window-layer가 없습니다.");
            }

            windowLayer.RemoveFromHierarchy();

            return windowLayer;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Panel에 control, resize, titlebar drag 연결을 붙인다.
        /// </summary>
        // ------------------------------------------------------------
        private WindowBinding CreateBinding
        (
            XeriWindowHandle handle,
            XeriWindowPanel panel,
            XeriWindowController controller
        )
        {
            var binding = new WindowBinding
            {
                Handle = handle,
                Panel = panel,
                Controller = controller,
                ControlManipulator = new XeriWindowControlManipulator(panel, controller),
                ResizeManipulator = new XeriWindowResizeManipulator(panel, controller),
                TitleBarManipulator = dragFactory.CreateTitleBarDrag(panel, controller),
            };

            binding.ControlManipulator.Attach();
            binding.ResizeManipulator.Attach();
            binding.TitleBarManipulator.Attach();

            panel.RegisterCallback<PointerDownEvent>
            (
                _ => registry.Focus(handle),
                TrickleDown.TrickleDown
            );

            return binding;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Registry 변경을 canvas 표시 갱신으로 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void BindRegistry()
        {
            registry.OnOrderChange += OnRegistryOrderChange;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Registry focus order를 window layer z-order에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyWindowOrder()
        {
            var ordered = registry.Records;
            var panels = new List<XeriWindowPanel>();

            foreach (var record in ordered)
            {
                if (!registry.TryGetHandle(record.ID, out var handle)) continue;
                if (!bindings.TryGetValue(handle, out var binding)) continue;

                panels.Add(binding.Panel);
            }

            foreach (var panel in panels)
            {
                panel.RemoveFromHierarchy();
            }

            foreach (var panel in panels)
            {
                windowLayer.Add(panel);
            }
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// Registry order 변경을 window layer z-order에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnRegistryOrderChange(object sender, EventArgs e)
        {
            ApplyWindowOrder();
        }

    #endregion

    }
}
