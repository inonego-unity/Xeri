/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_XeriWindowController.cs
수정일 : 2026-05-23

# 설명
Xeri 커스텀 윈도우 controller와 core 옵션 테스트.

# 테스트 구성
 O: 옵션 기본값
 M: 이동과 크기 변경
 S: 상태 명령
 C: 취소 가능한 요청
 E: 이벤트
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.UI.Window;

namespace inonego.Xeri.TEST.UI._Window
{
    // ============================================================
    /// <summary>
    /// Xeri 커스텀 윈도우 controller 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_XeriWindowController
    {

    #region 헬퍼

        // ============================================================
        /// <summary>
        /// 테스트용 윈도우 driver.
        /// </summary>
        // ============================================================
        private sealed class TestWindowDriver : IXeriWindowDriver
        {
            public Vector2 Pos { get; set; } = new Vector2(10f, 20f);
            public Vector2 Size { get; set; } = new Vector2(200f, 120f);
            public XeriWindowState State { get; set; } = XeriWindowState.Normal;
        }

    #endregion

    #region O-1: 기본 옵션

        // ------------------------------------------------------------
        /// <summary>
        /// XeriWindowOptions 기본값은 모든 기본 상호작용을 활성화한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowOptions_Default_기본_상호작용_활성화()
        {
            var options = XeriWindowOptions.Default();

            Assert.IsTrue(options.CanMove);
            Assert.IsTrue(options.CanResize);
            Assert.IsTrue(options.CanMinimize);
            Assert.IsTrue(options.CanMaximize);
            Assert.IsTrue(options.CanClose);
            Assert.IsTrue(options.CanFocus);
            Assert.IsTrue(options.CanTitleBarDoubleClickMaximize);
            Assert.IsFalse(options.HideDisabledButtons);
            Assert.AreEqual(new Vector2(120f, 80f), options.MinSize);
        }

    #endregion

    #region M-1: Move

        // ------------------------------------------------------------
        /// <summary>
        /// Move는 driver 위치를 변경한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowController_Move_Driver_Pos_변경()
        {
            var driver = new TestWindowDriver();
            var controller = new XeriWindowController(driver);

            controller.Move(new Vector2(30f, 40f));

            Assert.AreEqual(new Vector2(30f, 40f), driver.Pos);
        }

    #endregion

    #region M-2: Resize

        // ------------------------------------------------------------
        /// <summary>
        /// Resize는 옵션의 최소/최대 크기 범위로 driver 크기를 보정한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowController_Resize_Min_Max_범위로_Size_보정()
        {
            var driver = new TestWindowDriver();
            var options = XeriWindowOptions.Default();
            options.MinSize = new Vector2(100f, 80f);
            options.MaxSize = new Vector2(300f, 240f);

            var controller = new XeriWindowController(driver, options);

            controller.Resize(new Vector2(40f, 400f));

            Assert.AreEqual(new Vector2(100f, 240f), driver.Size);
        }

    #endregion

    #region S-1: State Command

        // ------------------------------------------------------------
        /// <summary>
        /// 상태 명령은 driver 상태를 전환한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowController_StateCommand_Driver_State_전환()
        {
            var driver = new TestWindowDriver();
            var controller = new XeriWindowController(driver);

            controller.Minimize();
            Assert.AreEqual(XeriWindowState.Minimized, driver.State);

            controller.ShowNormal();
            Assert.AreEqual(XeriWindowState.Normal, driver.State);

            controller.Maximize();
            Assert.AreEqual(XeriWindowState.Maximized, driver.State);

            controller.Close();
            Assert.AreEqual(XeriWindowState.Closed, driver.State);
        }

    #endregion

    #region S-2: Disabled Option

        // ------------------------------------------------------------
        /// <summary>
        /// 비활성화된 옵션의 명령은 driver를 변경하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowController_Disabled_Option_Driver_미변경()
        {
            var driver = new TestWindowDriver();
            var options = XeriWindowOptions.Default();
            options.CanMove = false;
            options.CanResize = false;
            options.CanMinimize = false;
            options.CanMaximize = false;
            options.CanClose = false;

            var controller = new XeriWindowController(driver, options);

            controller.Move(new Vector2(30f, 40f));
            controller.Resize(new Vector2(300f, 240f));
            controller.Minimize();
            controller.Maximize();
            controller.Close();

            Assert.AreEqual(new Vector2(10f, 20f), driver.Pos);
            Assert.AreEqual(new Vector2(200f, 120f), driver.Size);
            Assert.AreEqual(XeriWindowState.Normal, driver.State);
        }

    #endregion

    #region S-3: Closed State

        // ------------------------------------------------------------
        /// <summary>
        /// Closed 상태에서는 위치, 크기, 포커스 이벤트가 변경되지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowController_Closed_State_Move_Resize_Focus_무시()
        {
            var driver = new TestWindowDriver();
            var controller = new XeriWindowController(driver);
            var focusFired = false;

            controller.OnFocus += (_, _) => focusFired = true;
            controller.Close();
            controller.Move(new Vector2(30f, 40f));
            controller.Resize(new Vector2(300f, 240f));
            controller.Focus();

            Assert.AreEqual(new Vector2(10f, 20f), driver.Pos);
            Assert.AreEqual(new Vector2(200f, 120f), driver.Size);
            Assert.IsFalse(focusFired);
        }

    #endregion

    #region C-1: Close Cancel

        // ------------------------------------------------------------
        /// <summary>
        /// OnPreClose에서 Cancel을 설정하면 Close는 상태를 변경하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowController_OnPreClose_Cancel_Close_취소()
        {
            var driver = new TestWindowDriver();
            var controller = new XeriWindowController(driver);

            controller.OnPreClose += (_, e) => e.Cancel = true;

            controller.Close();

            Assert.AreEqual(XeriWindowState.Normal, driver.State);
        }

    #endregion

    #region C-2: State Cancel

        // ------------------------------------------------------------
        /// <summary>
        /// 상태 사전 이벤트에서 Cancel을 설정하면 상태 명령은 취소된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowController_PreState_Cancel_StateCommand_취소()
        {
            var driver = new TestWindowDriver();
            var controller = new XeriWindowController(driver);

            controller.OnPreMinimize += (_, e) => e.Cancel = true;
            controller.OnPreMaximize += (_, e) => e.Cancel = true;

            controller.Minimize();
            controller.Maximize();

            Assert.AreEqual(XeriWindowState.Normal, driver.State);
        }

    #endregion

    #region E-1: Value Change Event

        // ------------------------------------------------------------
        /// <summary>
        /// Move와 Resize는 값 변경 이벤트를 발화한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowController_Move_Resize_ValueChangeEvent_발화()
        {
            var driver = new TestWindowDriver();
            var controller = new XeriWindowController(driver);
            ValueChangeEventArgs<Vector2> posArgs = default;
            ValueChangeEventArgs<Vector2> sizeArgs = default;

            controller.OnPosChange += (_, e) => posArgs = e;
            controller.OnSizeChange += (_, e) => sizeArgs = e;

            controller.Move(new Vector2(30f, 40f));
            controller.Resize(new Vector2(300f, 240f));

            Assert.AreEqual(new Vector2(10f, 20f), posArgs.Previous);
            Assert.AreEqual(new Vector2(30f, 40f), posArgs.Current);
            Assert.AreEqual(new Vector2(200f, 120f), sizeArgs.Previous);
            Assert.AreEqual(new Vector2(300f, 240f), sizeArgs.Current);
        }

    #endregion

    #region E-2: State Change Event

        // ------------------------------------------------------------
        /// <summary>
        /// 상태 명령은 상태 변경 이벤트를 발화한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriWindowController_StateCommand_StateChangeEvent_발화()
        {
            var driver = new TestWindowDriver();
            var controller = new XeriWindowController(driver);
            ValueChangeEventArgs<XeriWindowState> stateArgs = default;

            controller.OnStateChange += (_, e) => stateArgs = e;

            controller.Minimize();

            Assert.AreEqual(XeriWindowState.Normal, stateArgs.Previous);
            Assert.AreEqual(XeriWindowState.Minimized, stateArgs.Current);
        }

    #endregion

    }
}
