/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_AutoScrollManipulator.cs
수정일 : 2026-11-22

# 설명
휠 클릭 자동 스크롤 Manipulator가 실제 Editor 패널에서 붙인 요소 위에서만 시작하고,
고정 상태에서 다른 요소의 입력을 가로채지 않으며, 다른 스크롤 영역에서 새로 시작할 수 있는지 확인한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.AutoScroll;

namespace inonego.Xeri.TEST.UI._AutoScroll
{
    // ============================================================
    /// <summary>
    /// Editor 창에서 포인터 이벤트로 휠 클릭 자동 스크롤을 재현한다.
    /// </summary>
    // ============================================================
    [TestFixture]
    public sealed class TEST_AutoScrollManipulator
    {

    #region 내부 창

        // ============================================================
        /// <summary>
        /// 스크롤 영역 두 개와 그 사이 버튼을 둔 시험 창.
        /// </summary>
        // ============================================================
        private sealed class AutoScrollTestWindow : EditorWindow
        {
            // NONE
        }

    #endregion

    #region 필드

        private const int MIDDLE = 2;
        private const int LEFT = 0;
        private const string INDICATOR_CLASS = "xeri-auto-scroll-indicator";

        private AutoScrollTestWindow window;
        private ScrollView first;
        private ScrollView second;
        private Button middleButton;
        private UITKAutoScrollManipulator firstManipulator;
        private int buttonDownCount;

    #endregion

    #region 준비

        // ------------------------------------------------------------
        /// <summary>
        /// 시험 창을 열고 스크롤 영역 두 개에 Manipulator를 붙인다.
        /// </summary>
        // ------------------------------------------------------------
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            window = ScriptableObject.CreateInstance<AutoScrollTestWindow>();
            window.position = new Rect(100f, 100f, 400f, 600f);
            window.ShowUtility();

            var root = window.rootVisualElement;
            first = CreateScrollView("first");
            middleButton = new Button
            {
                text = "middle",
            };
            middleButton.style.height = 60f;
            middleButton.RegisterCallback<PointerDownEvent>(_ => buttonDownCount++, TrickleDown.TrickleDown);
            second = CreateScrollView("second");
            root.Add(first);
            root.Add(middleButton);
            root.Add(second);

            firstManipulator = new UITKAutoScrollManipulator(new UITKScrollViewTarget(first));
            first.AddManipulator(firstManipulator);
            second.AddManipulator(new UITKAutoScrollManipulator(new UITKScrollViewTarget(second)));

            for (var frame = 0; frame < 5; frame++)
            {
                window.Repaint();
                yield return null;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 시험 창을 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            if (window != null)
            {
                window.Close();
            }
        }

        private static ScrollView CreateScrollView(string name)
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical)
            {
                name = name,
            };
            scroll.style.height = 200f;
            var content = new VisualElement();
            content.style.height = 4000f;
            scroll.Add(content);

            return scroll;
        }

    #endregion

    #region 입력 보조

        private void Send(EventBase evt)
        {
            using (evt)
            {
                window.rootVisualElement.panel.visualTree.SendEvent(evt);
            }
        }

        private void Down(int button, Vector2 position)
        {
            var e = new Event
            {
                type = EventType.MouseDown,
                button = button,
                mousePosition = position,
                clickCount = 1,
            };
            Send(PointerDownEvent.GetPooled(e));
        }

        private void Up(int button, Vector2 position)
        {
            var e = new Event
            {
                type = EventType.MouseUp,
                button = button,
                mousePosition = position,
                clickCount = 1,
            };
            Send(PointerUpEvent.GetPooled(e));
        }

        private void Move(Vector2 position)
        {
            var e = new Event
            {
                type = EventType.MouseMove,
                mousePosition = position,
            };
            Send(PointerMoveEvent.GetPooled(e));
        }

        private bool IsIndicatorShown()
        {
            return window.rootVisualElement.panel.visualTree.Q(className: INDICATOR_CLASS) != null;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 자동 스크롤 주기가 실제 시간으로 돌도록 창을 다시 그리며 seconds초 동안 기다린다.
        /// </summary>
        // ----------------------------------------------------------------------
        private IEnumerator Scrolling(double seconds = 0.3)
        {
            var end = EditorApplication.timeSinceStartup + seconds;
            while (EditorApplication.timeSinceStartup < end)
            {
                window.Repaint();
                yield return null;
            }
        }

        private static IEnumerator Frames(int count)
        {
            for (var frame = 0; frame < count; frame++)
            {
                yield return null;
            }
        }

    #endregion

    #region 시험

        // ------------------------------------------------------------
        /// <summary>
        /// 붙인 요소 밖에서 가운데 버튼을 눌러도 시작하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator 붙인_요소_밖에서는_시작하지_않는다()
        {
            var outside = middleButton.worldBound.center;
            Down(MIDDLE, outside);
            Up(MIDDLE, outside);
            yield return Frames(2);

            Assert.IsFalse(IsIndicatorShown(), "요소 밖 가운데 클릭으로 자동 스크롤이 시작됐다");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 고정 상태에서 밖을 누르면 끝나고 그 누름은 원래 요소로 가며,
        /// <br/> 다른 스크롤 영역에서 가운데 버튼을 누르면 그쪽이 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator 고정_상태는_밖의_입력을_가로채지_않고_다른_영역에서_새로_시작한다()
        {
            var firstCenter = first.worldBound.center;
            Down(MIDDLE, firstCenter);
            Up(MIDDLE, firstCenter);
            yield return Frames(2);
            Assert.IsTrue(IsIndicatorShown(), "첫 영역 가운데 클릭으로 고정 상태가 되지 않았다");

            // 고정 상태에서 아래로 멀리 움직이면 첫 영역이 스크롤된다.
            Move(firstCenter + new Vector2(0f, 150f));
            yield return Scrolling();
            Assert.Greater(first.scrollOffset.y, 0f, "고정 상태에서 움직여도 첫 영역이 스크롤되지 않았다");

            // 밖(버튼)을 누르면 끝나고 버튼이 그 누름을 받는다.
            var before = buttonDownCount;
            Down(LEFT, middleButton.worldBound.center);
            Up(LEFT, middleButton.worldBound.center);
            yield return Frames(2);
            Assert.IsFalse(IsIndicatorShown(), "밖을 눌러도 자동 스크롤이 끝나지 않았다");
            Assert.AreEqual(before + 1, buttonDownCount, "밖의 누름이 버튼으로 가지 않았다");

            // 두 번째 영역에서 가운데 버튼을 누르면 그쪽이 시작하고, 움직이면 두 번째만 스크롤된다.
            var firstOffset = first.scrollOffset.y;
            var secondCenter = second.worldBound.center;
            Down(MIDDLE, secondCenter);
            Up(MIDDLE, secondCenter);
            yield return Frames(2);
            Assert.IsTrue(IsIndicatorShown(), "두 번째 영역에서 자동 스크롤이 시작되지 않았다");

            Move(secondCenter + new Vector2(0f, 150f));
            yield return Scrolling();
            Assert.Greater(second.scrollOffset.y, 0f, "두 번째 영역이 스크롤되지 않았다");
            Assert.AreEqual(firstOffset, first.scrollOffset.y, "두 번째를 움직이는데 첫 영역이 스크롤됐다");
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 고정 상태에서 다른 스크롤 영역을 가운데 버튼으로 바로 누르면 앞 영역은 끝나고 그쪽이 시작한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        [UnityTest]
        public IEnumerator 고정_상태에서_다른_영역을_누르면_그쪽으로_바뀐다()
        {
            var firstCenter = first.worldBound.center;
            Down(MIDDLE, firstCenter);
            Up(MIDDLE, firstCenter);
            yield return Frames(2);

            var secondCenter = second.worldBound.center;
            Down(MIDDLE, secondCenter);
            Up(MIDDLE, secondCenter);
            yield return Frames(2);

            var firstOffset = first.scrollOffset.y;
            Move(secondCenter + new Vector2(0f, 150f));
            yield return Scrolling();

            Assert.Greater(second.scrollOffset.y, 0f, "두 번째 영역으로 바뀌지 않았다");
            Assert.AreEqual(firstOffset, first.scrollOffset.y, "첫 영역이 계속 스크롤됐다");
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 움직일 내용이 없는 스크롤 영역에서는 시작하지 않고 누름을 그대로 넘긴다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator 움직일_내용이_없으면_시작하지_않고_누름을_넘긴다()
        {
            second.contentContainer[0].style.height = 50f;
            var received = 0;
            second.contentContainer[0].RegisterCallback<PointerDownEvent>(_ => received++);
            yield return Frames(3);

            var secondCenter = second.worldBound.center;
            Down(MIDDLE, new Vector2(secondCenter.x, second.worldBound.yMin + 20f));
            Up(MIDDLE, new Vector2(secondCenter.x, second.worldBound.yMin + 20f));
            yield return Frames(2);

            Assert.IsFalse(IsIndicatorShown(), "움직일 내용이 없는데 자동 스크롤이 시작됐다");
            Assert.AreEqual(1, received, "시작하지 않은 누름이 아래 요소로 가지 않았다");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 밖에서 Scroller.End로 끝내도 표시·포인터·패널 등록이 정리된다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator 밖에서_End로_끝내도_정리된다()
        {
            var firstCenter = first.worldBound.center;
            Down(MIDDLE, firstCenter);
            yield return Frames(2);
            Assert.IsTrue(first.HasPointerCapture(PointerId.mousePointerId), "누르는 동안 포인터를 잡지 않았다");

            firstManipulator.Scroller.End();
            yield return Frames(2);

            Assert.IsFalse(IsIndicatorShown(), "End 뒤에도 표식이 남았다");
            Assert.IsFalse(first.HasPointerCapture(PointerId.mousePointerId), "End 뒤에도 포인터를 잡고 있다");

            var before = buttonDownCount;
            Down(LEFT, middleButton.worldBound.center);
            Up(LEFT, middleButton.worldBound.center);
            yield return Frames(2);
            Assert.AreEqual(before + 1, buttonDownCount, "End 뒤 다른 요소의 누름이 가로채졌다");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 누른 채 끌면 누르는 동안 스크롤하고 떼면 끝난다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator 누른_채_끌면_떼는_순간_끝난다()
        {
            var firstCenter = first.worldBound.center;
            Down(MIDDLE, firstCenter);
            Move(firstCenter + new Vector2(0f, 150f));
            yield return Scrolling();
            Assert.Greater(first.scrollOffset.y, 0f, "누른 채 끌어도 스크롤되지 않았다");

            Up(MIDDLE, firstCenter + new Vector2(0f, 150f));
            yield return Frames(2);
            Assert.IsFalse(IsIndicatorShown(), "떼도 자동 스크롤이 끝나지 않았다");

            // 끌기 뒤에도 포인터를 놓아야 다른 클릭이 원래 요소로 가고 다른 영역에서 새로 시작한다.
            Assert.IsFalse(first.HasPointerCapture(PointerId.mousePointerId), "끌기를 끝낸 뒤에도 포인터를 잡고 있다");

            var before = buttonDownCount;
            Down(LEFT, middleButton.worldBound.center);
            Up(LEFT, middleButton.worldBound.center);
            yield return Frames(2);
            Assert.AreEqual(before + 1, buttonDownCount, "끌기 뒤 다른 요소의 누름이 가로채졌다");

            var secondCenter = second.worldBound.center;
            Down(MIDDLE, secondCenter);
            Up(MIDDLE, secondCenter);
            Move(secondCenter + new Vector2(0f, 150f));
            yield return Scrolling();
            Assert.Greater(second.scrollOffset.y, 0f, "끌기 뒤 다른 영역에서 새로 시작하지 않았다");
        }

    #endregion

    }
}
