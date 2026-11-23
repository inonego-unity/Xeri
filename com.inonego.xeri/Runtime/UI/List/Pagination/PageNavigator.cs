/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PageNavigator.cs
수정일 : 2026-11-23

# 설명
IReadOnlyPaginator의 현재·전체 페이지 수와 이동 가능 상태를 보여 주고, 사용자의 페이지 이동(처음·이전·번호 입력·다음·마지막)을
공통 요청(PageNavigationRequest)으로 전달하는 화면 요소.

# 특이사항, 제약사항
줄을 차지하는 막대가 아니라 가로로 놓이는 컨트롤 묶음이라, 놓을 자리(예: TableGrid 상태 줄 가운데 슬롯)는 호스트가 정한다.
결과 범위·전체 개수 같은 글자는 보이지 않는다(호스트의 상태 표시가 맡는다). 이동 요청만 올리고 실제 이동은 호스트가 한다.
모양은 자기 요소에 붙이는 Resources 시트(Xeri/UI/List/PageNavigator)가 정한다.
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
    /// 페이지 이동 컨트롤(처음·이전·번호·다음·마지막).
    /// </summary>
    // ============================================================
    internal sealed class PageNavigator : VisualElement
    {

    #region 필드

        private const string STYLE_PATH = "Xeri/UI/List/PageNavigator";

        private readonly IReadOnlyPaginator paginator;
        private readonly Action<PageNavigationRequest> navigate;
        private readonly IntegerField pageField;
        private readonly IntegerField pageCountField;
        private readonly Button firstButton;
        private readonly Button prevButton;
        private readonly Button nextButton;
        private readonly Button lastButton;

    #endregion

    #region 생성자

        // --------------------------------------------------------------------------------
        /// <summary>
        /// Paginator 상태와 이동 명령을 연결해 컨트롤을 구성한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public PageNavigator
        (
            IReadOnlyPaginator paginator,
            Action<PageNavigationRequest> navigate
        ) : base()
        {
            this.paginator = paginator ?? throw new ArgumentNullException(nameof(paginator));
            this.navigate = navigate ?? throw new ArgumentNullException(nameof(navigate));

            AddToClassList("xeri-page-navigator");

            // 모양은 놓이는 호스트와 상관없이 자기 시트로 정한다.
            // 다른 Core UI 요소처럼 필수 시트가 없으면 조용히 무너지지 않게 바로 알린다.
            styleSheets.Add(XeriUIResources.Require<StyleSheet>(STYLE_PATH, "PageNavigator"));

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
            pageField.AddToClassList("xeri-page-navigator__page-field");
            pageField.RegisterValueChangedCallback(HandlePageChanged);
            pageField.RegisterCallback<KeyDownEvent>(HandlePageKeyDown);

            pageCountField = new IntegerField
            {
                name = "page-count-field",
                isReadOnly = true,
                tooltip = "전체 페이지 수",
            };
            pageCountField.AddToClassList("xeri-page-navigator__page-field");
            pageCountField.AddToClassList("xeri-page-navigator__page-field--readonly");

            var separator = new Label("/");
            separator.AddToClassList("xeri-page-navigator__page-separator");

            var page = new VisualElement();
            page.AddToClassList("xeri-page-navigator__page");
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

            Add(firstButton);
            Add(prevButton);
            Add(page);
            Add(nextButton);
            Add(lastButton);

            RegisterCallback<AttachToPanelEvent>(HandleAttached);
            RegisterCallback<DetachFromPanelEvent>(HandleDetached);

            Refresh();
        }

    #endregion

    #region 메서드

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 현재·전체 페이지 수와 이동 가능 상태를 화면에 반영한다.
        /// <br/> Paginator 값이 바뀌면 저절로 부른다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Refresh()
        {
            pageField.SetValueWithoutNotify(paginator.PageNumber);
            pageField.SetEnabled(paginator.PageCount > 0);
            pageCountField.SetValueWithoutNotify(paginator.PageCount);

            firstButton.SetEnabled(paginator.CanMoveFirst);
            prevButton.SetEnabled(paginator.CanMovePrev);
            nextButton.SetEnabled(paginator.CanMoveNext);
            lastButton.SetEnabled(paginator.CanMoveLast);
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

            button.AddToClassList("xeri-page-navigator__button");
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
        /// paginator 상태 변경을 화면에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePaginatorChanged(object sender, EventArgs e)
        {
            Refresh();
        }

    #endregion

    }
}
