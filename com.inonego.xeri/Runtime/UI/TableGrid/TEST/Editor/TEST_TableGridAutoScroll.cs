/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_TableGridAutoScroll.cs
수정일 : 2026-11-22

# 설명
Editor 창에 TableGrid 두 개를 두고 휠 클릭 자동 스크롤이 누른 표에서만 시작하고,
고정 상태에서 다른 입력을 가로채지 않으며, 다른 표에서 새로 시작하는지 확인한다(Collection Inspector 배치와 같은 구성).
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
using inonego.Xeri.UI.TableGrid;

using TableGridView = inonego.Xeri.UI.TableGrid.TableGrid;

namespace inonego.Xeri.TEST.UI._TableGrid
{
    // ============================================================
    /// <summary>
    /// TableGrid 두 개에서 휠 클릭 자동 스크롤을 재현한다.
    /// </summary>
    // ============================================================
    [TestFixture]
    public sealed class TEST_TableGridAutoScroll
    {

    #region 내부 형식

        // ============================================================
        /// <summary>
        /// 표 두 개와 그 사이 버튼을 둔 시험 창.
        /// </summary>
        // ============================================================
        private sealed class TableGridAutoScrollTestWindow : EditorWindow
        {
            // NONE
        }

        private sealed class Item
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 행 ID.
            /// </summary>
            // ------------------------------------------------------------
            public string ID { get; set; }
        }

    #endregion

    #region 필드

        private const int MIDDLE = 2;
        private const int LEFT = 0;
        private const string INDICATOR_CLASS = "xeri-auto-scroll-indicator";

        private TableGridAutoScrollTestWindow window;
        private TableGridView firstGrid;
        private TableGridView secondGrid;
        private Button middleButton;
        private int buttonDownCount;
        private bool throughWindow;

    #endregion

    #region 준비

        // ------------------------------------------------------------
        /// <summary>
        /// 시험 창을 열고 Inspector 프리셋 표 두 개를 붙인다.
        /// </summary>
        // ------------------------------------------------------------
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // 같은 인스턴스로 시험이 이어지므로 입력 경로를 기본(직접 보내기)으로 되돌린다.
            throughWindow = false;
            window = ScriptableObject.CreateInstance<TableGridAutoScrollTestWindow>();
            window.position = new Rect(100f, 100f, 500f, 700f);
            window.ShowUtility();

            var root = window.rootVisualElement;
            firstGrid = CreateGrid("first");
            middleButton = new Button
            {
                text = "middle",
            };
            middleButton.style.height = 60f;
            middleButton.RegisterCallback<PointerDownEvent>(_ => buttonDownCount++, TrickleDown.TrickleDown);
            secondGrid = CreateGrid("second");
            root.Add(firstGrid);
            root.Add(middleButton);
            root.Add(secondGrid);

            for (var frame = 0; frame < 8; frame++)
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

        private static TableGridView CreateGrid(string name)
        {
            var grid = new TableGridView(TableGridOptions.Inspector())
            {
                name = name,
            };
            grid.style.height = 240f;
            grid.style.flexShrink = 0f;

            var columns = new TableGridColumn[]
            {
                new TableGridColumn<Item, string>("id", "ID", item => item.ID, TableGridFormats.Text),
            };
            grid.SetColumns(columns);

            var items = new List<Item>();
            for (var index = 0; index < 300; index++)
            {
                items.Add
                (
                    new Item
                    {
                        ID = $"{name}-{index}",
                    }
                );
            }

            grid.SetSource(new TableGridListSource<Item>(items, item => item.ID));
            return grid;
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

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> throughWindow이면 실제 입력처럼 창을 거쳐 보낸다.
        /// <br/> 창에서는 IMGUI 이벤트를 UI Toolkit 이벤트로 변환해 보낸다.
        /// <br/> 그 밖이면 패널에 포인터 이벤트를 바로 보낸다.
        /// </summary>
        // ------------------------------------------------------------
        private void SendMouse(Event e, Func<Event, EventBase> toPointer)
        {
            if (throughWindow)
            {
                window.SendEvent(e);
                return;
            }

            Send(toPointer(e));
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
            SendMouse(e, source => PointerDownEvent.GetPooled(source));
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
            SendMouse(e, source => PointerUpEvent.GetPooled(source));
        }

        private void Move(Vector2 position)
        {
            var e = new Event
            {
                type = EventType.MouseMove,
                mousePosition = position,
            };
            SendMouse(e, source => PointerMoveEvent.GetPooled(source));
        }

        private bool IsIndicatorShown()
        {
            return window.rootVisualElement.panel.visualTree.Q(className: INDICATOR_CLASS) != null;
        }

        private static float ScrollY(TableGridView grid)
        {
            return grid.Q<ScrollView>("scroll-pane").scrollOffset.y;
        }

        private static Vector2 BodyCenter(TableGridView grid)
        {
            return grid.Q<ScrollView>("scroll-pane").worldBound.center;
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

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 표 밖에서 가운데 버튼을 눌러도 어느 표도 시작하지 않는다(앞서 표를 눌러 포커스가 있어도).
        /// </summary>
        // --------------------------------------------------------------------------------
        [UnityTest]
        public IEnumerator 표_밖의_가운데_클릭은_포커스된_표를_시작하지_않는다()
        {
            return RunOutsideMiddleClick();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 입력 경로(창 → UI Toolkit)로 표 밖 가운데 클릭을 재현한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator 창_입력_표_밖의_가운데_클릭은_포커스된_표를_시작하지_않는다()
        {
            throughWindow = true;
            return RunOutsideMiddleClick();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 고정 상태에서 밖을 누르면 끝나고 그 누름은 원래 요소로 가며,
        /// <br/> 다른 표에서 가운데 버튼을 누르면 그 표만 스크롤된다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator 고정_상태는_밖의_입력을_가로채지_않고_다른_표에서_새로_시작한다()
        {
            return RunLatchedSwitch();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 입력 경로(창 → UI Toolkit)로 고정 상태 전환을 재현한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator 창_입력_고정_상태는_밖의_입력을_가로채지_않고_다른_표에서_새로_시작한다()
        {
            throughWindow = true;
            return RunLatchedSwitch();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 실제 입력 경로로 누른 채 끌고 뗀 뒤 포인터가 풀려,
        /// <br/> 다른 클릭이 원래 요소로 가고 다른 표에서 새로 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator 창_입력_끌기를_끝내면_포인터를_놓아_다른_표에서_새로_시작한다()
        {
            throughWindow = true;
            var firstCenter = BodyCenter(firstGrid);
            Down(MIDDLE, firstCenter);
            Move(firstCenter + new Vector2(0f, 150f));
            yield return Scrolling();
            Up(MIDDLE, firstCenter + new Vector2(0f, 150f));
            yield return Frames(2);

            var body = firstGrid.Q<ScrollView>("scroll-pane").parent;
            Assert.IsFalse(body.HasPointerCapture(PointerId.mousePointerId), "끌기를 끝낸 뒤에도 첫 표가 포인터를 잡고 있다");

            var before = buttonDownCount;
            Down(LEFT, middleButton.worldBound.center);
            Up(LEFT, middleButton.worldBound.center);
            yield return Frames(2);
            Assert.AreEqual(before + 1, buttonDownCount, "끌기 뒤 다른 요소의 누름이 가로채졌다");

            var firstOffset = ScrollY(firstGrid);
            var secondCenter = BodyCenter(secondGrid);
            Down(MIDDLE, secondCenter);
            Up(MIDDLE, secondCenter);
            Move(secondCenter + new Vector2(0f, 150f));
            yield return Scrolling();
            Assert.Greater(ScrollY(secondGrid), 0f, "끌기 뒤 두 번째 표에서 새로 시작하지 않았다");
            Assert.AreEqual(firstOffset, ScrollY(firstGrid), "끌기 뒤 첫 표가 다시 스크롤됐다");
        }

        private IEnumerator RunOutsideMiddleClick()
        {
            var firstCenter = BodyCenter(firstGrid);
            Down(LEFT, firstCenter);
            Up(LEFT, firstCenter);
            yield return Frames(2);

            var outside = middleButton.worldBound.center;
            Down(MIDDLE, outside);
            Up(MIDDLE, outside);
            Move(outside + new Vector2(0f, 150f));
            yield return Scrolling();

            Assert.IsFalse(IsIndicatorShown(), "표 밖 가운데 클릭으로 자동 스크롤이 시작됐다");
            Assert.AreEqual(0f, ScrollY(firstGrid), "표 밖 가운데 클릭으로 첫 표가 스크롤됐다");
        }

        private IEnumerator RunLatchedSwitch()
        {
            var firstCenter = BodyCenter(firstGrid);
            Down(MIDDLE, firstCenter);
            Up(MIDDLE, firstCenter);
            yield return Frames(2);
            Assert.IsTrue(IsIndicatorShown(), "첫 표 가운데 클릭으로 고정 상태가 되지 않았다");

            Move(firstCenter + new Vector2(0f, 150f));
            yield return Scrolling();
            Assert.Greater(ScrollY(firstGrid), 0f, "고정 상태에서 움직여도 첫 표가 스크롤되지 않았다");

            var before = buttonDownCount;
            Down(LEFT, middleButton.worldBound.center);
            Up(LEFT, middleButton.worldBound.center);
            yield return Frames(2);
            Assert.IsFalse(IsIndicatorShown(), "밖을 눌러도 자동 스크롤이 끝나지 않았다");
            Assert.AreEqual(before + 1, buttonDownCount, "밖의 누름이 버튼으로 가지 않았다");

            var firstOffset = ScrollY(firstGrid);
            var secondCenter = BodyCenter(secondGrid);
            Down(MIDDLE, secondCenter);
            Up(MIDDLE, secondCenter);
            yield return Frames(2);
            Assert.IsTrue(IsIndicatorShown(), "두 번째 표에서 자동 스크롤이 시작되지 않았다");

            Move(secondCenter + new Vector2(0f, 150f));
            yield return Scrolling();
            Assert.Greater(ScrollY(secondGrid), 0f, "두 번째 표가 스크롤되지 않았다");
            Assert.AreEqual(firstOffset, ScrollY(firstGrid), "두 번째를 움직이는데 첫 표가 스크롤됐다");
        }

    #endregion

    }
}
