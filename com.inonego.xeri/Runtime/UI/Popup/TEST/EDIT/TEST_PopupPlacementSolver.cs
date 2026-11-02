/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PopupPlacementSolver.cs
수정일 : 2026-10-28

# 설명
PopupPlacementSolver가 공개하는 방향 선택, 정렬, 보정과 실패 계약을 검증한다.

=========================================================================

# 테스트 구성
 E: 열림 방향 선택과 Flip·대체 방향
 P: 점 기준, 정렬, 간격, 여백, 교차축 오프셋과 정렬 반전
 S: 교차축 Shift, 열림 방향 크기 축소와 교차축 크기 상한
 O: 기준 영역 겹침 방지, 겹침 허용 정책과 가장 덜 겹치는 방향 선택
 X: 입력 검증과 명시적 실패
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.TEST.UI._PopupPlacement
{
    // ============================================================
    /// <summary>
    /// Popup 배치 계산기 공개 계약 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_PopupPlacementSolver
    {

    #region 헬퍼

        private static readonly Rect safeRect = new(0f, 0f, 800f, 600f);
        private static readonly Vector2 menuSize = new(268f, 200f);
        private static readonly IPopupPlacementSolver solver = PopupPlacementSolver.Default;

        // ------------------------------------------------------------
        /// <summary>
        /// 성공한 결과가 안전 영역 안에 포함되는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void AssertInside(Rect safe, Rect bounds)
        {
            var tolerance = PopupPlacementSolver.Tolerance;

            Assert.GreaterOrEqual(bounds.xMin, safe.xMin - tolerance);
            Assert.GreaterOrEqual(bounds.yMin, safe.yMin - tolerance);
            Assert.LessOrEqual(bounds.xMax, safe.xMax + tolerance);
            Assert.LessOrEqual(bounds.yMax, safe.yMax + tolerance);
        }

    #endregion

    #region E-1: 기준 영역 아래 배치

        // ----------------------------------------------------------------------
        /// <summary>
        /// 아래 공간이 충분하면 기준 영역 아래에 왼쪽을 맞춰 전체 크기로 배치한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_Below_공간충분_기준아래_전체크기()
        {
            var anchor = new Rect(100f, 50f, 80f, 30f);
            var result = solver.Solve
            (
                safeRect, anchor, menuSize,
                new PopupPlacementOptions(PopupPlacementDirection.Below)
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(PopupPlacementDirection.Below, result.Direction);
            Assert.AreEqual(new Rect(100f, 80f, 268f, 200f), result.Bounds);
            Assert.IsFalse(result.WasFlipped);
            Assert.IsFalse(result.WasShifted);
            Assert.IsFalse(result.WasResized);
            Assert.IsFalse(result.OverlapsAnchor);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 안전 영역 끝에 정확히 맞닿는 크기는 부동소수점 오차 안에서 아래에 배치된다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_Below_경계정확히맞음_오차허용_아래유지()
        {
            var anchor = new Rect(100f, 370f, 80f, 30.005f);
            var result = solver.Solve
            (
                safeRect, anchor, menuSize,
                new PopupPlacementOptions(PopupPlacementDirection.Below)
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(PopupPlacementDirection.Below, result.Direction);
            Assert.IsFalse(result.WasResized);
            AssertInside(safeRect, result.Bounds);
        }

    #endregion

    #region E-2: 위쪽 Flip

        // ----------------------------------------------------------------------
        /// <summary>
        /// 아래 공간이 부족하고 위가 충분하면 기준 영역 위에 전체 크기로 배치한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_Below_아래부족_위충분_Above로Flip()
        {
            var anchor = new Rect(100f, 500f, 80f, 30f);
            var result = solver.Solve
            (
                safeRect, anchor, menuSize,
                new PopupPlacementOptions(PopupPlacementDirection.Below)
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(PopupPlacementDirection.Above, result.Direction);
            Assert.AreEqual(new Rect(100f, 300f, 268f, 200f), result.Bounds);
            Assert.IsTrue(result.WasFlipped);
            Assert.IsFalse(result.WasResized);
            Assert.IsFalse(result.OverlapsAnchor);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Flip을 끄면 위가 충분해도 선호 방향의 남은 공간으로 크기를 줄여 배치한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_AllowFlip_False_아래유지_크기축소()
        {
            var anchor = new Rect(100f, 500f, 80f, 30f);
            var result = solver.Solve
            (
                safeRect, anchor, menuSize,
                new PopupPlacementOptions(PopupPlacementDirection.Below, allowFlip: false)
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(PopupPlacementDirection.Below, result.Direction);
            Assert.AreEqual(70f, result.Bounds.height, PopupPlacementSolver.Tolerance);
            Assert.IsTrue(result.WasResized);
            Assert.IsFalse(result.OverlapsAnchor);
        }

    #endregion

    #region E-3: 좌우 방향과 대체 방향

        // ------------------------------------------------------------
        /// <summary>
        /// 오른쪽 공간이 충분하면 기준 영역 오른쪽에 위쪽을 맞춰 배치한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_Right_공간충분_기준오른쪽_위쪽정렬()
        {
            var anchor = new Rect(100f, 100f, 80f, 30f);
            var result = solver.Solve
            (
                safeRect, anchor, new Vector2(200f, 100f),
                new PopupPlacementOptions(PopupPlacementDirection.Right)
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(PopupPlacementDirection.Right, result.Direction);
            Assert.AreEqual(new Rect(180f, 100f, 200f, 100f), result.Bounds);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 오른쪽 공간이 부족하면 왼쪽으로 Flip한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_Right_오른쪽부족_Left로Flip()
        {
            var anchor = new Rect(650f, 100f, 100f, 30f);
            var result = solver.Solve
            (
                safeRect, anchor, new Vector2(200f, 100f),
                new PopupPlacementOptions(PopupPlacementDirection.Right)
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(PopupPlacementDirection.Left, result.Direction);
            Assert.AreEqual(450f, result.Bounds.x, PopupPlacementSolver.Tolerance);
            Assert.IsTrue(result.WasFlipped);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 대체 방향은 반대 방향보다 먼저 평가되며 교차축은 안전 영역 안으로 이동한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_FallbackDirection_반대방향보다_먼저평가()
        {
            var anchor = new Rect(100f, 500f, 80f, 30f);
            var result = solver.Solve
            (
                safeRect, anchor, menuSize,
                new PopupPlacementOptions
                (
                    PopupPlacementDirection.Below,
                    fallbackDirection: PopupPlacementDirection.Right
                )
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(PopupPlacementDirection.Right, result.Direction);
            Assert.AreEqual(180f, result.Bounds.x, PopupPlacementSolver.Tolerance);
            Assert.AreEqual(400f, result.Bounds.y, PopupPlacementSolver.Tolerance);
            Assert.IsTrue(result.WasFlipped);
            Assert.IsTrue(result.WasShifted);
        }

    #endregion

    #region P-1: 점 기준

        // ------------------------------------------------------------
        /// <summary>
        /// 크기가 없는 점 기준은 공간이 충분하면 시작점이 점과 정확히 일치한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_PointAnchor_공간충분_시작점일치()
        {
            var point = new Rect(300f, 200f, 0f, 0f);
            var result = solver.Solve
            (
                safeRect, point, menuSize,
                new PopupPlacementOptions(PopupPlacementDirection.Below)
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(point.position, result.Bounds.position);
            Assert.IsFalse(result.OverlapsAnchor);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 점 아래 공간이 부족하면 점이 팝업의 아래 모서리가 되도록 위로 연다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_PointAnchor_아래부족_점이아래모서리()
        {
            var point = new Rect(300f, 550f, 0f, 0f);
            var result = solver.Solve
            (
                safeRect, point, menuSize,
                new PopupPlacementOptions(PopupPlacementDirection.Below)
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(PopupPlacementDirection.Above, result.Direction);
            Assert.AreEqual(300f, result.Bounds.xMin, PopupPlacementSolver.Tolerance);
            Assert.AreEqual(550f, result.Bounds.yMax, PopupPlacementSolver.Tolerance);
        }

    #endregion

    #region P-2: 정렬

        // ------------------------------------------------------------
        /// <summary>
        /// 교차축 정렬은 Start, Center, End에 따라 기준 영역과 맞춰진다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_Alignment_Start_Center_End_기준영역정렬()
        {
            var anchor = new Rect(400f, 50f, 80f, 30f);

            var start = solver.Solve
            (
                safeRect, anchor, menuSize,
                new PopupPlacementOptions(PopupPlacementDirection.Below, PopupPlacementAlignment.Start)
            );
            var center = solver.Solve
            (
                safeRect, anchor, menuSize,
                new PopupPlacementOptions(PopupPlacementDirection.Below, PopupPlacementAlignment.Center)
            );
            var end = solver.Solve
            (
                safeRect, anchor, menuSize,
                new PopupPlacementOptions(PopupPlacementDirection.Below, PopupPlacementAlignment.End)
            );

            Assert.AreEqual(400f, start.Bounds.xMin, PopupPlacementSolver.Tolerance);
            Assert.AreEqual(440f, center.Bounds.center.x, PopupPlacementSolver.Tolerance);
            Assert.AreEqual(480f, end.Bounds.xMax, PopupPlacementSolver.Tolerance);
        }

    #endregion

    #region P-3: 간격과 여백

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gap은 기준 영역과 Popup 사이를 벌리고 Padding은 안전 영역 안쪽에서 보장된다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_Gap_Padding_간격과_안쪽여백()
        {
            var anchor = new Rect(700f, 50f, 80f, 30f);
            var result = solver.Solve
            (
                safeRect, anchor, menuSize,
                new PopupPlacementOptions
                (
                    PopupPlacementDirection.Below, gap: 5f, padding: 4f
                )
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(85f, result.Bounds.yMin, PopupPlacementSolver.Tolerance);
            Assert.AreEqual(796f - 268f, result.Bounds.xMin, PopupPlacementSolver.Tolerance);
            Assert.AreEqual(796f, result.Bounds.xMax, PopupPlacementSolver.Tolerance);
        }

    #endregion

    #region P-4: 교차축 오프셋

        // ------------------------------------------------------------
        /// <summary>
        /// CrossOffset은 기준 영역을 피한 채 교차축 시작 위치만 이동한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_CrossOffset_기준영역옆_교차축시작위치이동()
        {
            var parent = new Rect(100f, 50f, 268f, 300f);
            var result = solver.Solve
            (
                safeRect, parent, menuSize,
                new PopupPlacementOptions
                (
                    PopupPlacementDirection.Right, gap: 1f, crossOffset: 120f
                )
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(new Rect(369f, 170f, 268f, 200f), result.Bounds);
            Assert.IsFalse(result.OverlapsAnchor);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// CrossOffset으로 밀려난 위치가 경계를 넘으면 안전 영역 안으로 이동한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_CrossOffset_경계초과_Shift()
        {
            var parent = new Rect(100f, 50f, 268f, 300f);
            var result = solver.Solve
            (
                safeRect, parent, menuSize,
                new PopupPlacementOptions
                (
                    PopupPlacementDirection.Right, crossOffset: 500f
                )
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(400f, result.Bounds.yMin, PopupPlacementSolver.Tolerance);
            Assert.IsTrue(result.WasShifted);
            AssertInside(safeRect, result.Bounds);
        }

    #endregion

    #region P-5: 교차축 정렬 반전

        // ----------------------------------------------------------------------
        /// <summary>
        /// 왼쪽 정렬이 경계를 넘으면 오른쪽 모서리를 기준 영역에 맞춰 Shift 없이 놓는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_AllowAlignmentFlip_Start초과_End로반전()
        {
            var anchor = new Rect(700f, 50f, 80f, 30f);
            var result = solver.Solve
            (
                safeRect, anchor, menuSize,
                new PopupPlacementOptions
                (
                    PopupPlacementDirection.Below, allowAlignmentFlip: true
                )
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(780f, result.Bounds.xMax, PopupPlacementSolver.Tolerance);
            Assert.AreEqual(80f, result.Bounds.yMin, PopupPlacementSolver.Tolerance);
            Assert.IsTrue(result.WasAlignmentFlipped);
            Assert.IsFalse(result.WasShifted);
            Assert.IsFalse(result.OverlapsAnchor);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 반전을 허용하지 않으면 기존처럼 안전 영역 안으로 Shift한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_AllowAlignmentFlip_False_Start초과_Shift유지()
        {
            var anchor = new Rect(700f, 50f, 80f, 30f);
            var result = solver.Solve
            (
                safeRect, anchor, menuSize,
                new PopupPlacementOptions(PopupPlacementDirection.Below)
            );

            Assert.AreEqual(800f, result.Bounds.xMax, PopupPlacementSolver.Tolerance);
            Assert.IsFalse(result.WasAlignmentFlipped);
            Assert.IsTrue(result.WasShifted);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 점 기준이 오른쪽 끝에 가까우면 점이 메뉴의 오른쪽 위 모서리가 된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_PointAnchor_오른쪽끝_점이오른쪽위모서리()
        {
            var point = new Rect(790f, 100f, 0f, 0f);
            var result = solver.Solve
            (
                safeRect, point, menuSize,
                new PopupPlacementOptions
                (
                    PopupPlacementDirection.Below, allowAlignmentFlip: true
                )
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(790f, result.Bounds.xMax, PopupPlacementSolver.Tolerance);
            Assert.AreEqual(100f, result.Bounds.yMin, PopupPlacementSolver.Tolerance);
            Assert.IsTrue(result.WasAlignmentFlipped);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 시작 정렬이 들어가면 반전하지 않고, 양쪽 모두 넘으면 Shift로 넘어간다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_AllowAlignmentFlip_들어가면유지_양쪽초과면Shift()
        {
            var options = new PopupPlacementOptions
            (
                PopupPlacementDirection.Below, allowAlignmentFlip: true
            );
            var narrow = new Rect(0f, 0f, 300f, 600f);

            var fits = solver.Solve
            (
                safeRect, new Rect(100f, 50f, 80f, 30f), menuSize, options
            );
            var both = solver.Solve
            (
                narrow, new Rect(140f, 50f, 20f, 30f), menuSize, options
            );

            Assert.AreEqual(100f, fits.Bounds.xMin, PopupPlacementSolver.Tolerance);
            Assert.IsFalse(fits.WasAlignmentFlipped);
            Assert.AreEqual(300f, both.Bounds.xMax, PopupPlacementSolver.Tolerance);
            Assert.IsFalse(both.WasAlignmentFlipped);
            Assert.IsTrue(both.WasShifted);
        }

    #endregion

    #region S-1: 교차축 Shift

        // ------------------------------------------------------------
        /// <summary>
        /// 오른쪽 경계를 넘는 Popup은 교차축으로 이동해 안전 영역 안에 놓인다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_Below_오른쪽경계초과_Shift()
        {
            var anchor = new Rect(700f, 50f, 80f, 30f);
            var result = solver.Solve
            (
                safeRect, anchor, menuSize,
                new PopupPlacementOptions(PopupPlacementDirection.Below)
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(PopupPlacementDirection.Below, result.Direction);
            Assert.AreEqual(800f, result.Bounds.xMax, PopupPlacementSolver.Tolerance);
            Assert.IsTrue(result.WasShifted);
            Assert.IsFalse(result.OverlapsAnchor);
        }

    #endregion

    #region S-2: 크기 축소

        // ----------------------------------------------------------------------
        /// <summary>
        /// 위아래 모두 부족하면 더 넓은 쪽 공간으로 길이를 줄이고 기준 영역을 가리지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_양쪽부족_넓은쪽으로_크기축소()
        {
            var anchor = new Rect(100f, 250f, 80f, 40f);
            var result = solver.Solve
            (
                safeRect, anchor, new Vector2(268f, 400f),
                new PopupPlacementOptions(PopupPlacementDirection.Below)
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(PopupPlacementDirection.Below, result.Direction);
            Assert.AreEqual(290f, result.Bounds.yMin, PopupPlacementSolver.Tolerance);
            Assert.AreEqual(310f, result.Bounds.height, PopupPlacementSolver.Tolerance);
            Assert.IsTrue(result.WasResized);
            Assert.IsFalse(result.OverlapsAnchor);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 안전 영역보다 큰 Popup은 교차축도 안전 영역 크기로 줄어든다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_안전영역보다큼_교차축_크기축소()
        {
            var anchor = new Rect(100f, 50f, 80f, 30f);
            var result = solver.Solve
            (
                safeRect, anchor, new Vector2(900f, 100f),
                new PopupPlacementOptions(PopupPlacementDirection.Below)
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0f, result.Bounds.xMin, PopupPlacementSolver.Tolerance);
            Assert.AreEqual(800f, result.Bounds.width, PopupPlacementSolver.Tolerance);
            Assert.IsTrue(result.WasResized);
            AssertInside(safeRect, result.Bounds);
        }

    #endregion

    #region S-3: 교차축 크기 상한

        // ------------------------------------------------------------
        /// <summary>
        /// 열림 방향 축소를 끄더라도 교차축은 항상 안전 영역 크기로 제한된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_AllowMainShrink_False_교차축은_안전영역크기로_제한()
        {
            var anchor = new Rect(100f, 50f, 80f, 30f);
            var result = solver.Solve
            (
                safeRect, anchor, new Vector2(900f, 100f),
                new PopupPlacementOptions(PopupPlacementDirection.Below, allowMainShrink: false)
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(800f, result.Bounds.width, PopupPlacementSolver.Tolerance);
            Assert.IsTrue(result.WasResized);
            AssertInside(safeRect, result.Bounds);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 서브메뉴처럼 옆으로 열리는 Popup이 화면보다 높으면 폭은 유지하고 높이만 제한한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_Right_화면보다높음_폭유지_높이제한()
        {
            var parent = new Rect(100f, 50f, 268f, 300f);
            var result = solver.Solve
            (
                safeRect, parent, new Vector2(268f, 700f),
                new PopupPlacementOptions
                (
                    PopupPlacementDirection.Right,
                    gap: 1f, allowMainShrink: false, allowAnchorOverlap: true
                )
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(PopupPlacementDirection.Right, result.Direction);
            Assert.AreEqual(369f, result.Bounds.xMin, PopupPlacementSolver.Tolerance);
            Assert.AreEqual(268f, result.Bounds.width, PopupPlacementSolver.Tolerance);
            Assert.AreEqual(600f, result.Bounds.height, PopupPlacementSolver.Tolerance);
            Assert.AreEqual(0f, result.Bounds.yMin, PopupPlacementSolver.Tolerance);
            Assert.IsFalse(result.OverlapsAnchor);
        }

    #endregion

    #region O-1: 겹침 방지

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 전체 크기가 들어가는 모든 세로 위치에서 안전 영역 안에 놓이고 기준 영역과 겹치지 않는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_기준위치전범위_안전영역포함_비겹침()
        {
            for (var y = 0f; y <= 570f; y += 10f)
            {
                var anchor = new Rect(120f, y, 90f, 30f);
                var result = solver.Solve
                (
                    safeRect, anchor, menuSize,
                    new PopupPlacementOptions(PopupPlacementDirection.Below)
                );

                Assert.IsTrue(result.Succeeded, $"y={y}");
                Assert.IsFalse(result.OverlapsAnchor, $"y={y}");
                Assert.IsFalse(result.WasResized, $"y={y}");
                AssertInside(safeRect, result.Bounds);
            }
        }

    #endregion

    #region O-2: 겹침 정책

        // ------------------------------------------------------------
        /// <summary>
        /// 피할 공간이 없고 겹침을 허용하지 않으면 NoSpace로 실패한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_공간없음_겹침불허_NoSpace()
        {
            var anchor = new Rect(0f, 0f, 800f, 600f);
            var result = solver.Solve
            (
                safeRect, anchor, menuSize,
                new PopupPlacementOptions(PopupPlacementDirection.Below)
            );

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(PopupPlacementFailure.NoSpace, result.FailureReason);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 겹침을 허용하면 선호 방향으로 안전 영역 안에 겹쳐 배치한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_공간없음_겹침허용_안전영역내_겹침()
        {
            var anchor = new Rect(0f, 0f, 800f, 600f);
            var result = solver.Solve
            (
                safeRect, anchor, menuSize,
                new PopupPlacementOptions
                (
                    PopupPlacementDirection.Below, allowAnchorOverlap: true
                )
            );

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.OverlapsAnchor);
            Assert.IsFalse(result.WasResized);
            AssertInside(safeRect, result.Bounds);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 남은 공간이 최소 길이보다 작으면 축소하지 않고 겹침 정책으로 넘어간다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_MinMainSize미만_축소거부_NoSpace()
        {
            var anchor = new Rect(100f, 250f, 80f, 40f);
            var result = solver.Solve
            (
                safeRect, anchor, new Vector2(268f, 400f),
                new PopupPlacementOptions
                (
                    PopupPlacementDirection.Below, minMainSize: 400f
                )
            );

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(PopupPlacementFailure.NoSpace, result.FailureReason);
        }

    #endregion

    #region O-3: 겹침 감수 시 가장 덜 겹치는 방향

        // ----------------------------------------------------------------------
        /// <summary>
        /// 위아래 모두 모자라고 축소도 거부되면 기준 영역과 덜 겹치는 위쪽을 고른다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_세로겹침감수_덜겹치는_Above선택()
        {
            var anchor = new Rect(100f, 100f, 80f, 460f);
            var result = solver.Solve
            (
                safeRect, anchor, new Vector2(268f, 300f),
                new PopupPlacementOptions
                (
                    PopupPlacementDirection.Below,
                    minMainSize: 150f, allowAnchorOverlap: true
                )
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(PopupPlacementDirection.Above, result.Direction);
            Assert.AreEqual(0f, result.Bounds.yMin, PopupPlacementSolver.Tolerance);
            Assert.IsTrue(result.WasFlipped);
            Assert.IsTrue(result.OverlapsAnchor);
            AssertInside(safeRect, result.Bounds);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 좌우 모두 폭이 모자라면 부모와 덜 겹치는 왼쪽으로 안전 영역 안에 놓는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_가로겹침감수_덜겹치는_Left선택()
        {
            var narrow = new Rect(0f, 0f, 600f, 600f);
            var parent = new Rect(182f, 0f, 268f, 300f);
            var result = solver.Solve
            (
                narrow, parent, menuSize,
                new PopupPlacementOptions
                (
                    PopupPlacementDirection.Right,
                    gap: 1f, allowMainShrink: false, allowAnchorOverlap: true
                )
            );

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(PopupPlacementDirection.Left, result.Direction);
            Assert.AreEqual(0f, result.Bounds.xMin, PopupPlacementSolver.Tolerance);
            Assert.AreEqual(268f, result.Bounds.width, PopupPlacementSolver.Tolerance);
            Assert.IsTrue(result.WasFlipped);
            AssertInside(narrow, result.Bounds);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 겹치는 정도가 같으면 선호 방향을 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_겹침동일_선호방향_유지()
        {
            var narrow = new Rect(0f, 0f, 600f, 600f);
            var parent = new Rect(166f, 0f, 268f, 300f);
            var options = new PopupPlacementOptions
            (
                PopupPlacementDirection.Right,
                allowMainShrink: false, allowAnchorOverlap: true
            );
            var leftOptions = new PopupPlacementOptions
            (
                PopupPlacementDirection.Left,
                allowMainShrink: false, allowAnchorOverlap: true
            );

            var right = solver.Solve(narrow, parent, menuSize, options);
            var left = solver.Solve(narrow, parent, menuSize, leftOptions);

            Assert.AreEqual(PopupPlacementDirection.Right, right.Direction);
            Assert.AreEqual(PopupPlacementDirection.Left, left.Direction);
            Assert.IsFalse(right.WasFlipped);
            Assert.IsFalse(left.WasFlipped);
        }

    #endregion

    #region X-1: 입력 검증

        // ----------------------------------------------------------------------
        /// <summary>
        /// 안전 영역이 비었거나 Padding으로 사라지면 InvalidInput으로 실패한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_안전영역없음_InvalidInput()
        {
            var anchor = new Rect(10f, 10f, 10f, 10f);
            var empty = solver.Solve
            (
                Rect.zero, anchor, menuSize,
                new PopupPlacementOptions(PopupPlacementDirection.Below)
            );
            var padded = solver.Solve
            (
                safeRect, anchor, menuSize,
                new PopupPlacementOptions(PopupPlacementDirection.Below, padding: 300f)
            );

            Assert.AreEqual(PopupPlacementFailure.InvalidInput, empty.FailureReason);
            Assert.AreEqual(PopupPlacementFailure.InvalidInput, padded.FailureReason);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 크기가 0 이하이거나 유한하지 않은 값은 InvalidInput으로 실패한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_잘못된크기와NaN_InvalidInput()
        {
            var anchor = new Rect(10f, 10f, 10f, 10f);
            var options = new PopupPlacementOptions(PopupPlacementDirection.Below);

            var zeroSize = solver.Solve(safeRect, anchor, Vector2.zero, options);
            var nanSize = solver.Solve
            (
                safeRect, anchor, new Vector2(float.NaN, 10f), options
            );
            var nanAnchor = solver.Solve
            (
                safeRect, new Rect(float.NaN, 0f, 0f, 0f), menuSize, options
            );

            Assert.AreEqual(PopupPlacementFailure.InvalidInput, zeroSize.FailureReason);
            Assert.AreEqual(PopupPlacementFailure.InvalidInput, nanSize.FailureReason);
            Assert.AreEqual(PopupPlacementFailure.InvalidInput, nanAnchor.FailureReason);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기본값 결과는 성공으로 취급되지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementResult_default_성공아님()
        {
            Assert.IsFalse(default(PopupPlacementResult).Succeeded);
        }

    #endregion

    #region X-2: 열림 방향 축소 불가

        // ----------------------------------------------------------------------
        /// <summary>
        /// 열림 방향 축소를 허용하지 않으면 안전 영역보다 긴 Popup은 NoSpace로 실패한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_AllowMainShrink_False_안전영역보다긺_NoSpace()
        {
            var anchor = new Rect(100f, 50f, 80f, 30f);
            var result = solver.Solve
            (
                safeRect, anchor, new Vector2(268f, 700f),
                new PopupPlacementOptions(PopupPlacementDirection.Below, allowMainShrink: false)
            );

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(PopupPlacementFailure.NoSpace, result.FailureReason);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 겹침을 허용해도 열림 방향으로 안전 영역보다 긴 후보는 제외되어 실패한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PopupPlacementSolver_AllowMainShrink_False_겹침허용_긴방향제외_NoSpace()
        {
            var anchor = new Rect(100f, 50f, 80f, 30f);
            var result = solver.Solve
            (
                safeRect, anchor, new Vector2(268f, 700f),
                new PopupPlacementOptions
                (
                    PopupPlacementDirection.Below,
                    allowMainShrink: false, allowAnchorOverlap: true
                )
            );

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(PopupPlacementFailure.NoSpace, result.FailureReason);
        }

    #endregion

    }
}
