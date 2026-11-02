/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PaginationBar.cs
수정일 : 2026-10-28

# 설명
IReadOnlyPaginator의 결과 범위, 현재·전체 페이지 수와 이동 가능 상태를 표시한다.
페이지 이동 명령을 공통 요청으로 전달하고, 가용 폭에 따라 표시 영역을 배치한다.
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
    /// 결과 범위와 페이지 이동 컨트롤을 제공한다.
    /// </summary>
    // ============================================================
    internal sealed class PaginationBar : VisualElement
    {

    #region 필드

        private readonly IReadOnlyPaginator paginator;
        private readonly Action<PageNavigationRequest> navigate;
        private readonly Label countLabel;
        private readonly VisualElement controls;
        private readonly IntegerField pageField;
        private readonly IntegerField pageCountField;
        private readonly Button firstButton;
        private readonly Button prevButton;
        private readonly Button nextButton;
        private readonly Button lastButton;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Paginator 상태와 이동 명령을 연결해 컨트롤을 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PaginationBar
        (
            IReadOnlyPaginator paginator,
            Action<PageNavigationRequest> navigate
        ) : base()
        {
            this.paginator = paginator ?? throw new ArgumentNullException(nameof(paginator));
            this.navigate = navigate ?? throw new ArgumentNullException(nameof(navigate));

            AddToClassList("xeri-pagination");
            countLabel = new Label
            {
                name = "count-label",
            };
            countLabel.AddToClassList("xeri-pagination__count-label");

            controls = new VisualElement();
            controls.AddToClassList("xeri-pagination__controls");

            firstButton = CreateButton
            (
                "first-page-button",
                "chevrons-left",
                () => this.navigate(new PageNavigationRequest(PageNavigationKind.First))
            );
            prevButton = CreateButton
            (
                "prev-page-button",
                "chevron-left",
                () => this.navigate(new PageNavigationRequest(PageNavigationKind.Previous))
            );

            pageField = new IntegerField
            {
                name = "page-field",
                isDelayed = true,
                tooltip = "페이지 번호를 입력하여 이동",
            };
            pageField.AddToClassList("xeri-pagination__page-field");
            pageField.RegisterValueChangedCallback(HandlePageChanged);
            pageField.RegisterCallback<KeyDownEvent>(HandlePageKeyDown);

            pageCountField = new IntegerField
            {
                name = "page-count-field",
                isReadOnly = true,
                tooltip = "전체 페이지 수",
            };
            pageCountField.AddToClassList("xeri-pagination__page-field");
            pageCountField.AddToClassList("xeri-pagination__page-field--readonly");

            var separator = new Label("/");
            separator.AddToClassList("xeri-pagination__page-separator");

            var page = new VisualElement();
            page.AddToClassList("xeri-pagination__page");
            page.Add(pageField);
            page.Add(separator);
            page.Add(pageCountField);

            nextButton = CreateButton
            (
                "next-page-button",
                "chevron-right",
                () => this.navigate(new PageNavigationRequest(PageNavigationKind.Next))
            );
            lastButton = CreateButton
            (
                "last-page-button",
                "chevrons-right",
                () => this.navigate(new PageNavigationRequest(PageNavigationKind.Last))
            );

            controls.Add(firstButton);
            controls.Add(prevButton);
            controls.Add(page);
            controls.Add(nextButton);
            controls.Add(lastButton);

            Add(controls);
            Add(countLabel);

            RegisterCallback<GeometryChangedEvent>(HandleGeometryChanged);
            controls.RegisterCallback<GeometryChangedEvent>(HandleGeometryChanged);
            RegisterCallback<AttachToPanelEvent>(HandleAttached);
            RegisterCallback<DetachFromPanelEvent>(HandleDetached);

            Refresh();
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 결과 범위, 페이지 수와 이동 가능 상태를 화면에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Refresh()
        {
            var range = paginator.Range;
            var begin = range.IsEmpty ? 0 : range.BeginIndex + 1;
            var end = range.EndIndex;

            countLabel.text = $"{begin}-{end} / 전체 {paginator.TotalCount}개";
            pageField.SetValueWithoutNotify(paginator.PageNumber);
            pageField.SetEnabled(paginator.PageCount > 0);
            pageCountField.SetValueWithoutNotify(paginator.PageCount);

            UpdateLayoutMode(resolvedStyle.width);

            firstButton.SetEnabled(paginator.CanMoveFirst);
            prevButton.SetEnabled(paginator.CanMovePrev);
            nextButton.SetEnabled(paginator.CanMoveNext);
            lastButton.SetEnabled(paginator.CanMoveLast);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 버튼과 결과 텍스트의 실제 너비에 따라 줄 배치를 결정한다.
        /// </summary>
        // ------------------------------------------------------------
        private void UpdateLayoutMode(float width)
        {
            if (float.IsNaN(width) || width <= 0f) return;

            var controlsWidth = controls.resolvedStyle.width;
            if (float.IsNaN(controlsWidth) || controlsWidth <= 0f) return;

            var countWidth = countLabel.MeasureTextSize
            (
                countLabel.text,
                0f, VisualElement.MeasureMode.Undefined,
                0f, VisualElement.MeasureMode.Undefined
            ).x;
            var requiredWidth = controlsWidth + countWidth + countLabel.resolvedStyle.paddingRight;
            EnableInClassList("xeri-pagination--compact", width < requiredWidth);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 아이콘과 이동 콜백을 가진 페이지 버튼을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static Button CreateButton
        (
            string name,
            string iconName,
            Action onClick
        )
        {
            var button = new Button(onClick)
            {
                name = name,
                iconImage = XeriIcon.LoadBackground(iconName),
            };

            button.AddToClassList("xeri-pagination__button");

            return button;
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// 숫자 입력을 페이지 인덱스로 변환해 호스트에 이동을 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePageChanged(ChangeEvent<int> evt)
        {
            if (paginator.PageCount > 0)
            {
                var pageNumber = Math.Max(1, Math.Min(evt.newValue, paginator.PageCount));
                navigate(new PageNavigationRequest(PageNavigationKind.Direct, pageNumber - 1));
            }

            Refresh();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Esc 입력을 취소하고 실제 현재 페이지 번호를 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePageKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape) return;

            pageField.SetValueWithoutNotify(paginator.PageNumber);
            pageField.Blur();
            evt.StopImmediatePropagation();
            evt.PreventDefault();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 페이지네이터나 이동 버튼 그룹의 실제 너비가 달라지면 배치를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateLayoutMode(resolvedStyle.width);
        }

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
