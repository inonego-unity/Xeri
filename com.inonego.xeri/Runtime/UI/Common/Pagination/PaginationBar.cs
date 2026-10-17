/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PaginationBar.cs
수정일 : 2026-09-24

# 설명
IReadOnlyPaginator 상태와 first/prev/next/last control을 표시하는 공통 UI Toolkit pagination bar.
Picker와 Collection Editor가 동일한 페이지 UI와 이동 동작을 재사용할 수 있게 한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Utility;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 공통 pagination bar.
    /// </summary>
    // ============================================================
    internal sealed class PaginationBar : VisualElement
    {

    #region 필드

        private readonly IReadOnlyPaginator paginator;
        private readonly Label countLabel;
        private readonly Label pageLabel;
        private readonly Button firstButton;
        private readonly Button prevButton;
        private readonly Button nextButton;
        private readonly Button lastButton;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// pagination bar를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PaginationBar
        (
            IReadOnlyPaginator paginator,
            Action moveFirst,
            Action movePrev,
            Action moveNext,
            Action moveLast
        ) : base()
        {
            this.paginator = paginator ?? throw new ArgumentNullException(nameof(paginator));

            AddToClassList("xeri-pagination");
            countLabel = new Label
            {
                name = "count-label",
            };
            countLabel.AddToClassList("xeri-pagination__count-label");

            var controls = new VisualElement();
            controls.AddToClassList("xeri-pagination__controls");

            firstButton = CreateButton
            (
                "first-page-button",
                "◀◀",
                moveFirst ?? throw new ArgumentNullException(nameof(moveFirst))
            );
            prevButton = CreateButton
            (
                "prev-page-button",
                "◀",
                movePrev ?? throw new ArgumentNullException(nameof(movePrev))
            );

            pageLabel = new Label
            {
                name = "page-label",
            };
            pageLabel.AddToClassList("xeri-pagination__page-label");

            nextButton = CreateButton
            (
                "next-page-button",
                "▶",
                moveNext ?? throw new ArgumentNullException(nameof(moveNext))
            );
            lastButton = CreateButton
            (
                "last-page-button",
                "▶▶",
                moveLast ?? throw new ArgumentNullException(nameof(moveLast))
            );

            controls.Add(firstButton);
            controls.Add(prevButton);
            controls.Add(pageLabel);
            controls.Add(nextButton);
            controls.Add(lastButton);

            Add(countLabel);
            Add(controls);

            RegisterCallback<AttachToPanelEvent>(HandleAttached);
            RegisterCallback<DetachFromPanelEvent>(HandleDetached);

            Refresh();
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 paginator 상태를 pagination UI에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Refresh()
        {
            var range = paginator.Range;
            var begin = range.IsEmpty ? 0 : range.BeginIndex + 1;
            var end = range.EndIndex;

            countLabel.text = $"{begin}-{end} / 전체 {paginator.TotalCount}개";
            pageLabel.text = $"{paginator.PageNumber}/{paginator.PageCount}";

            firstButton.SetEnabled(paginator.CanMoveFirst);
            prevButton.SetEnabled(paginator.CanMovePrev);
            nextButton.SetEnabled(paginator.CanMoveNext);
            lastButton.SetEnabled(paginator.CanMoveLast);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 공통 pagination 이동 버튼을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static Button CreateButton
        (
            string name,
            string text,
            Action onClick
        )
        {
            var button = new Button(onClick)
            {
                name = name,
                text = text,
            };

            button.AddToClassList("xeri-pagination__button");

            return button;
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// panel 연결 시 paginator 변경 구독과 표시 동기화를 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleAttached(AttachToPanelEvent evt)
        {
            paginator.OnChange -= HandlePaginatorChanged;
            paginator.OnChange += HandlePaginatorChanged;
            Refresh();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// panel 분리 시 paginator 변경 구독을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleDetached(DetachFromPanelEvent evt)
        {
            paginator.OnChange -= HandlePaginatorChanged;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// paginator 상태 변경을 pagination UI에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePaginatorChanged(object sender, EventArgs e)
        {
            Refresh();
        }

    #endregion

    }
}
