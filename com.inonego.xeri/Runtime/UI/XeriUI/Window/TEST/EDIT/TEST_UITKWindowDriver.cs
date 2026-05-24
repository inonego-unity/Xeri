/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_UITKWindowDriver.cs
수정일 : 2026-05-23

# 설명
UITKWindowDriver style 반영 테스트.

# 테스트 구성
 B: Bounds 반영
 S: State 반영
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UIElements;

using NUnit.Framework;

using inonego.Xeri.UI.Window;

namespace inonego.Xeri.TEST.UI._Window
{
    // ============================================================
    /// <summary>
    /// UITKWindowDriver 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_UITKWindowDriver
    {

    #region B-1: Bounds

        // ------------------------------------------------------------
        /// <summary>
        /// Pos와 Size 설정은 target style에 반영된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UITKWindowDriver_Pos_Size_Target_Style_반영()
        {
            var target = new VisualElement();
            var driver = new UITKWindowDriver(target);

            driver.Pos = new Vector2(10f, 20f);
            driver.Size = new Vector2(300f, 240f);

            Assert.AreEqual(10f, target.style.left.value.value);
            Assert.AreEqual(20f, target.style.top.value.value);
            Assert.AreEqual(300f, target.style.width.value.value);
            Assert.AreEqual(240f, target.style.height.value.value);
        }

    #endregion

    #region S-1: State

        // ------------------------------------------------------------
        /// <summary>
        /// Minimized와 Closed 상태는 target을 숨긴다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UITKWindowDriver_State_Minimized_Closed_Display_None()
        {
            var target = new VisualElement();
            var driver = new UITKWindowDriver(target);

            driver.State = XeriWindowState.Minimized;
            Assert.AreEqual(DisplayStyle.None, target.style.display.value);

            driver.State = XeriWindowState.Normal;
            Assert.AreEqual(DisplayStyle.Flex, target.style.display.value);

            driver.State = XeriWindowState.Closed;
            Assert.AreEqual(DisplayStyle.None, target.style.display.value);
        }

    #endregion

    #region S-2: Maximize Restore

        // ------------------------------------------------------------
        /// <summary>
        /// Maximized에서 Normal로 돌아오면 이전 위치와 크기를 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UITKWindowDriver_Maximized_Normal_이전_Bounds_복원()
        {
            var target = new VisualElement();
            var driver = new UITKWindowDriver(target);

            driver.Pos = new Vector2(10f, 20f);
            driver.Size = new Vector2(300f, 240f);
            driver.State = XeriWindowState.Maximized;
            driver.Pos = new Vector2(0f, 0f);
            driver.Size = new Vector2(600f, 480f);
            driver.State = XeriWindowState.Normal;

            Assert.AreEqual(new Vector2(10f, 20f), driver.Pos);
            Assert.AreEqual(new Vector2(300f, 240f), driver.Size);
            Assert.AreEqual(10f, target.style.left.value.value);
            Assert.AreEqual(20f, target.style.top.value.value);
            Assert.AreEqual(300f, target.style.width.value.value);
            Assert.AreEqual(240f, target.style.height.value.value);
        }

    #endregion

    }
}
