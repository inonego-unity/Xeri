/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_XeriWindowCanvas.cs
수정일 : 2026-05-23

# 설명
XeriWindowCanvas 기본 host 동작 테스트.

# 테스트 구성
 C: Canvas 구성
 W: Window 등록
 R: Root 확장
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UIElements;

using NUnit.Framework;

using inonego.Xeri.UI.Window;

namespace inonego.Xeri.TEST.UI._Window
{
    // ============================================================
    /// <summary>
    /// XeriWindowCanvas 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_XeriWindowCanvas
    {

    #region C-1: 기본 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 생성된 canvas가 root와 window layer를 제공한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowCanvas_Construct_Layer_제공()
        {
            var canvas = new XeriWindowCanvas();

            Assert.AreSame(canvas, canvas.Root);
            Assert.IsNotNull(canvas.WindowLayer);
        }

    #endregion

    #region W-1: Window 등록

        // ------------------------------------------------------------
        /// <summary>
        /// AddWindow가 panel, controller, registry record를 함께 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowCanvas_AddWindow_Panel_Controller_Record_생성()
        {
            var canvas = new XeriWindowCanvas();
            var view = new Label("Content");

            var handle = canvas.AddWindow
            (
                "inventory",
                "Inventory",
                view,
                new Vector2(10f, 20f),
                new Vector2(240f, 160f)
            );

            Assert.IsTrue(canvas.Registry.Contains(handle));
            Assert.AreEqual(1, canvas.WindowLayer.childCount);

            var panel = canvas.WindowLayer[0] as XeriWindowPanel;

            Assert.IsNotNull(panel);
            Assert.AreSame(view, panel.ContentSlot[0]);
        }

    #endregion

    #region W-2: 중복 ID

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 ID를 다시 등록하면 기존 handle을 반환하고 panel을 추가하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowCanvas_AddWindow_중복ID_Panel_추가안함()
        {
            var canvas = new XeriWindowCanvas();

            var first = canvas.AddWindow
            (
                "inventory",
                "Inventory",
                new Label("Content"),
                Vector2.zero,
                new Vector2(240f, 160f)
            );
            var second = canvas.AddWindow
            (
                "inventory",
                "Inventory Again",
                new Label("Content"),
                Vector2.zero,
                new Vector2(240f, 160f)
            );

            Assert.AreSame(first, second);
            Assert.AreEqual(1, canvas.WindowLayer.childCount);
        }

    #endregion

    #region W-3: Window Order

        // ------------------------------------------------------------
        /// <summary>
        /// Registry order가 바뀌면 WindowLayer의 실제 panel 순서도 갱신된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowCanvas_BringToFront_WindowLayer_Order_갱신()
        {
            var canvas = new XeriWindowCanvas();
            var first = canvas.AddWindow
            (
                "first",
                "First",
                new Label("First"),
                Vector2.zero,
                new Vector2(240f, 160f)
            );
            canvas.AddWindow
            (
                "second",
                "Second",
                new Label("Second"),
                Vector2.zero,
                new Vector2(240f, 160f)
            );

            canvas.Registry.BringToFront(first);

            Assert.AreEqual("Second", GetPanelTitle(canvas.WindowLayer[0]));
            Assert.AreEqual("First", GetPanelTitle(canvas.WindowLayer[1]));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// AlwaysOnTop window는 Normal window focus 이후에도 WindowLayer 앞쪽을 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowCanvas_AlwaysOnTop_WindowLayer_Order_유지()
        {
            var options = XeriWindowOptions.Default();
            options.StackLayer = XeriWindowStackLayer.AlwaysOnTop;

            var canvas = new XeriWindowCanvas();
            var normal = canvas.AddWindow
            (
                "normal",
                "Normal",
                new Label("Normal"),
                Vector2.zero,
                new Vector2(240f, 160f)
            );
            var top = canvas.AddWindow
            (
                "top",
                "Top",
                new Label("Top"),
                Vector2.zero,
                new Vector2(240f, 160f),
                options
            );

            canvas.Registry.Focus(normal);

            Assert.AreEqual("Normal", GetPanelTitle(canvas.WindowLayer[0]));
            Assert.AreEqual("Top", GetPanelTitle(canvas.WindowLayer[1]));
            Assert.IsTrue(canvas.Registry.Contains(top));
        }

    #endregion

    #region R-1: Root 확장

        // ------------------------------------------------------------
        /// <summary>
        /// Root에 외부 UI를 추가해도 WindowLayer order 적용과 섞이지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowCanvas_Root에_외부_UI를_붙여도_Window_Order_유지()
        {
            var canvas = new XeriWindowCanvas();
            var external = new Label("External");
            var first = canvas.AddWindow
            (
                "first",
                "First",
                new Label("First"),
                Vector2.zero,
                new Vector2(240f, 160f)
            );
            canvas.AddWindow
            (
                "second",
                "Second",
                new Label("Second"),
                Vector2.zero,
                new Vector2(240f, 160f)
            );

            canvas.Root.Add(external);
            canvas.Registry.BringToFront(first);

            Assert.AreSame(external, canvas.Root[1]);
            Assert.AreEqual(2, canvas.WindowLayer.childCount);
            Assert.AreEqual("Second", GetPanelTitle(canvas.WindowLayer[0]));
            Assert.AreEqual("First", GetPanelTitle(canvas.WindowLayer[1]));
        }

    #endregion

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// Panel title label text를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static string GetPanelTitle(VisualElement element)
        {
            return ((XeriWindowPanel)element).Q<Label>("title-label").text;
        }


    #endregion

    }
}
