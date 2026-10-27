/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_XeriContextMenuStyle.cs
수정일 : 2026-10-28

# 설명
컨텍스트 메뉴 스타일 프리셋, 기본 스타일과 밀도별 치수가 공개 계약을 지키는지 검증한다.

=========================================================================

# 테스트 구성
 E: 프리셋과 기본 스타일
 M: 밀도별 치수
========================================================================= BLOCK_HEADER_END */

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.TEST.UI._ContextMenu
{
    // ============================================================
    /// <summary>
    /// 컨텍스트 메뉴 스타일과 치수 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_XeriContextMenuStyle
    {

    #region E-1: 프리셋과 기본 스타일

        // ------------------------------------------------------------
        /// <summary>
        /// 기본값 구조체는 Editor 스킨을 따르는 Compact 밀도다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenuStyle_default_Auto테마_Compact밀도()
        {
            var style = default(XeriContextMenuStyle);

            Assert.AreEqual(XeriContextMenuTheme.Auto, style.Theme);
            Assert.AreEqual(XeriContextMenuDensity.Compact, style.Density);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 프리셋은 밀도만 다르고 테마는 Editor 스킨을 따른다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenuStyle_프리셋_밀도만_다름()
        {
            Assert.AreEqual(XeriContextMenuDensity.Compact, XeriContextMenuStyle.Compact.Density);
            Assert.AreEqual(XeriContextMenuDensity.Expanded, XeriContextMenuStyle.Expanded.Density);
            Assert.AreEqual(XeriContextMenuTheme.Auto, XeriContextMenuStyle.Compact.Theme);
            Assert.AreEqual(XeriContextMenuTheme.Auto, XeriContextMenuStyle.Expanded.Theme);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 스타일을 지정하지 않은 메뉴가 쓰는 기본 스타일은 Compact다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenu_DefaultStyle_Compact()
        {
            Assert.AreEqual(XeriContextMenuDensity.Compact, XeriContextMenu.DefaultStyle.Density);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 스타일의 치수는 지정한 밀도의 치수와 같다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenuStyle_Metrics_밀도의_치수와_일치()
        {
            var style = new XeriContextMenuStyle
            (
                XeriContextMenuTheme.Light, XeriContextMenuDensity.Expanded
            );
            var expected = XeriContextMenuMetrics.Of(XeriContextMenuDensity.Expanded);

            Assert.AreEqual(expected.Width, style.Metrics.Width);
            Assert.AreEqual(expected.ActionHeight, style.Metrics.ActionHeight);
            Assert.AreEqual(expected.ChromeHeight, style.Metrics.ChromeHeight);
            Assert.AreEqual(XeriContextMenuTheme.Light, style.Theme);
        }

    #endregion

    #region M-1: 밀도별 치수

        // ------------------------------------------------------------
        /// <summary>
        /// Expanded 밀도는 이전 기본 메뉴의 치수를 그대로 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenuMetrics_Expanded_이전기본치수_유지()
        {
            var metrics = XeriContextMenuMetrics.Of(XeriContextMenuDensity.Expanded);

            Assert.AreEqual(268f, metrics.Width);
            Assert.AreEqual(29f, metrics.ActionHeight);
            Assert.AreEqual(11f, metrics.SeparatorHeight);
            Assert.AreEqual(10f, metrics.ChromeHeight);
            Assert.AreEqual(5f, metrics.SeparatorMargin);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Compact 밀도는 모든 치수가 Expanded보다 작다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenuMetrics_Compact_모든치수_Expanded보다_작음()
        {
            var expanded = XeriContextMenuMetrics.Of(XeriContextMenuDensity.Expanded);
            var compact = XeriContextMenuMetrics.Of(XeriContextMenuDensity.Compact);

            Assert.Less(compact.Width, expanded.Width);
            Assert.Less(compact.ActionHeight, expanded.ActionHeight);
            Assert.Less(compact.SeparatorHeight, expanded.SeparatorHeight);
            Assert.Less(compact.ChromeHeight, expanded.ChromeHeight);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구분선 행의 높이는 구분선 두께와 위아래 여백의 합과 같다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriContextMenuMetrics_구분선높이_두께와_여백의_합()
        {
            var densities = new[]
            {
                XeriContextMenuDensity.Compact,
                XeriContextMenuDensity.Expanded,
            };

            foreach (var density in densities)
            {
                var metrics = XeriContextMenuMetrics.Of(density);

                Assert.AreEqual
                (
                    metrics.SeparatorHeight,
                    XeriContextMenuMetrics.SeparatorLineHeight + metrics.SeparatorMargin * 2f,
                    0.001f,
                    density.ToString()
                );
            }
        }

    #endregion

    }
}
