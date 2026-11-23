/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_XeriContextMenu.cs
수정일 : 2026-11-23

# 설명
컨텍스트 메뉴 항목 경로를 하위 메뉴 트리로 풀어내는 규칙 테스트.

# 테스트 구성
 H: 하위 메뉴 경로 해석
 S: 구분선 위치와 정리
 E: 잘못된 경로와 이름 충돌
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.TEST.UI._ContextMenu
{
    // ============================================================
    /// <summary>
    /// XeriContextMenu 항목 경로 해석 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_XeriContextMenu
    {

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// 경로만 가진 명령 항목을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private static XeriContextMenuItem Command(string path)
        {
            return XeriContextMenuItem.Command(path, () => { });
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이미 나뉜 경로 조각만 가진 명령 항목을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private static XeriContextMenuItem Command(string[] path)
        {
            return XeriContextMenuItem.Command(path, () => { });
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 풀어낸 항목 트리를 비교하기 쉬운 문자열로 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        private static string Describe(IReadOnlyList<XeriContextMenuItem> items)
        {
            var parts = new List<string>(items.Count);
            foreach (var item in items)
            {
                switch (item.Kind)
                {
                    case XeriContextMenuItem.ItemKind.Separator:
                        parts.Add("-");
                        break;

                    case XeriContextMenuItem.ItemKind.Submenu:
                        parts.Add($"{item.Text}[{Describe(item.Children)}]");
                        break;

                    default:
                        parts.Add(item.Text);
                        break;
                }
            }

            return string.Join(",", parts);
        }

    #endregion

    #region H-1: 하위 메뉴 경로 해석

        // ------------------------------------------------------------
        /// <summary>
        /// 슬래시 경로는 하위 메뉴가 되고 처음 나온 위치에 놓인다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenu_경로는_하위메뉴로_처음나온위치에_놓인다()
        {
            var items = new[]
            {
                Command("A"),
                Command("키/추가"),
                Command("B"),
                Command("키/제거"),
            };

            var tree = XeriContextMenuItem.Resolve(items);

            Assert.AreEqual("A,키[추가,제거],B", Describe(tree));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 여러 단계 경로는 단계마다 하위 메뉴를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenu_여러단계경로는_중첩된_하위메뉴를_만든다()
        {
            var items = new[]
            {
                Command("가/나/다"),
                Command("가/라"),
            };

            var tree = XeriContextMenuItem.Resolve(items);

            Assert.AreEqual("가[나[다],라]", Describe(tree));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 항목 목록을 다시 풀어도 같은 결과이며 입력 항목은 바뀌지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenu_같은목록을_다시풀어도_결과가같다()
        {
            var items = new[]
            {
                Command("가/나"),
                Command("다"),
            };

            var first = Describe(XeriContextMenuItem.Resolve(items));
            var second = Describe(XeriContextMenuItem.Resolve(items));

            Assert.AreEqual("가[나],다", first);
            Assert.AreEqual(first, second);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 조각 경로의 이름은 슬래시나 빈 글자가 있어도 하위 메뉴로 풀지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenu_조각경로의_이름은_슬래시가_있어도_풀리지않는다()
        {
            var items = new[]
            {
                Command(new[] { "N/A" }),
                Command(new[] { "A" }),
                Command(new[] { "A/B" }),
                Command(new[] { "/" }),
                Command(new[] { "" }),
            };

            var tree = XeriContextMenuItem.Resolve(items);

            Assert.AreEqual("N/A,A,A/B,/,", Describe(tree));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 조각 경로의 앞쪽 조각은 문자열 경로와 같은 하위 메뉴가 된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenu_조각경로는_문자열경로와_같은_하위메뉴에_놓인다()
        {
            var items = new[]
            {
                Command(new[] { "형식", "N/A" }),
                Command("형식/문자"),
            };

            var tree = XeriContextMenuItem.Resolve(items);

            Assert.AreEqual("형식[N/A,문자]", Describe(tree));
        }

    #endregion

    #region S-1: 구분선 위치와 정리

        // ------------------------------------------------------------
        /// <summary>
        /// 구분선은 경로가 가리키는 단계의 목록 순서 위치에 놓인다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenu_구분선은_지정한_단계의_순서위치에_놓인다()
        {
            var items = new[]
            {
                Command("A"),
                XeriContextMenuItem.Separator(),
                Command("키/추가"),
                XeriContextMenuItem.Separator("키"),
                Command("키/제거"),
            };

            var tree = XeriContextMenuItem.Resolve(items);

            Assert.AreEqual("A,-,키[추가,-,제거]", Describe(tree));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 각 단계의 맨 앞, 맨 뒤, 연속 구분선은 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenu_맨앞_맨뒤_연속구분선은_제거된다()
        {
            var items = new[]
            {
                XeriContextMenuItem.Separator(),
                Command("A"),
                XeriContextMenuItem.Separator(),
                XeriContextMenuItem.Separator(),
                Command("B"),
                XeriContextMenuItem.Separator(),
                Command("키/추가"),
                XeriContextMenuItem.Separator("키"),
            };

            var tree = XeriContextMenuItem.Resolve(items);

            Assert.AreEqual("A,-,B,-,키[추가]", Describe(tree));
        }

    #endregion

    #region E-1: 잘못된 경로와 이름 충돌

        // ------------------------------------------------------------
        /// <summary>
        /// 비어 있는 경로나 경로 조각은 허용하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [TestCase(null)]
        [TestCase("")]
        [TestCase("/A")]
        [TestCase("A/")]
        [TestCase("A//B")]
        public void TEST_XeriContextMenu_비어있는_경로조각은_예외(string path)
        {
            Assert.Throws<ArgumentException>(() => XeriContextMenuItem.Command(path, () => { }));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 조각 경로가 비었거나 하위 메뉴 조각이 비어 있으면 허용하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenu_비어있는_조각경로는_예외()
        {
            Assert.Throws<ArgumentException>(() => Command(Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() => Command(new[] { "", "A" }));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 명령 이름이 하위 메뉴 경로와 겹치면
        /// <br/> 어느 쪽이 먼저 나와도 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenu_명령과_하위메뉴이름이_겹치면_예외()
        {
            var commandFirst = new[]
            {
                Command("키"),
                Command("키/추가"),
            };
            var submenuFirst = new[]
            {
                Command("키/추가"),
                Command("키"),
            };

            Assert.Throws<InvalidOperationException>(() => XeriContextMenuItem.Resolve(commandFirst));
            Assert.Throws<InvalidOperationException>(() => XeriContextMenuItem.Resolve(submenuFirst));
        }

    #endregion

    }
}
