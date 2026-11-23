/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridSearchField.cs
수정일 : 2026-11-23

# 설명
표의 검색창. 검색 아이콘·글자 입력칸(안내 글자)·지우기 버튼과 선택적인 결과 수 글자로 이뤄진다.
호스트가 원하는 곳(표 위 도구 줄 등)에 놓고 Grid를 지정하면 입력이 Grid.SearchText로 들어간다.

# 특이사항, 제약사항
검색 실행은 표의 소스가 맡는다. 호스트가 직접 거르면 SearchesGrid를 끄고 SearchTextChanged를 받아 처리한다
(이때 Grid는 테마와 결과 수에만 쓴다).
표 밖에 놓이므로 테마·모양 USS를 직접 붙이고 테마 변수 클래스(xeri-table-search)를 가진다.
테마 수정 클래스는 자기 패널과 따르는 표의 옵션으로 정한다(표가 없으면 기본 옵션).
키 입력은 가로채지 않는다. 위·아래·Enter·Escape는 호스트가 정한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 표 검색창.
    /// </summary>
    // ============================================================
    [UxmlElement]
    public partial class TableGridSearchField : VisualElement
    {

    #region 상수

        // ------------------------------------------------------------
        /// <summary>
        /// 검색창 루트 요소의 USS 클래스.
        /// </summary>
        // ------------------------------------------------------------
        public static readonly string UssClassName = "xeri-table-search";

        private const string HAS_TEXT_CLASS = "xeri-table-search--has-text";
        private const string FOCUSED_CLASS = "xeri-table-search--focused";
        private const string COUNTING_CLASS = "xeri-table-search--counting";
        private const string DEFAULT_PLACEHOLDER = "검색";

    #endregion

    #region 필드

        private readonly TextField textField;
        private readonly VisualElement clearButton;
        private readonly Label countLabel;

        // ------------------------------------------------------------
        /// <summary>
        /// 따르는 표(테마·결과 수·검색 대상). null이면 입력만 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGrid Grid
        {
            get => grid;
            set
            {
                if (grid == value)
                {
                    return;
                }

                Unsubscribe();
                grid = value;
                if (panel != null)
                {
                    Subscribe();
                }

                if (grid != null)
                {
                    SyncGridSearch();
                }

                TableGridTheming.Apply(this, grid?.Options);
                RefreshCount();
            }
        }
        private TableGrid grid;

        // ------------------------------------------------------------
        /// <summary>
        /// 입력한 검색 글자. 지정하면 입력한 것처럼 반영하고 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public string Text
        {
            get => textField.value ?? string.Empty;
            set => textField.value = value ?? string.Empty;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비어 있을 때 보일 안내 글자.
        /// </summary>
        // ------------------------------------------------------------
        [UxmlAttribute]
        public string Placeholder
        {
            get => textField.textEdition.placeholder;
            set => textField.textEdition.placeholder = value ?? string.Empty;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 검색 중에 표의 결과 행 수를 검색창 끝에 보일지 여부.
        /// </summary>
        // ------------------------------------------------------------
        [UxmlAttribute]
        public bool ShowResultCount
        {
            get => showResultCount;
            set
            {
                showResultCount = value;
                RefreshCount();
            }
        }
        private bool showResultCount;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> true면 입력을 Grid.SearchText로 보낸다(기본).
        /// <br/> 호스트가 직접 거르면 false로 두고 SearchTextChanged를 받아 처리한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UxmlAttribute]
        public bool SearchesGrid
        {
            get => searchesGrid;
            set
            {
                if (searchesGrid == value)
                {
                    return;
                }

                searchesGrid = value;
                SyncGridSearch();
            }
        }
        private bool searchesGrid = true;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 입력·지우기·Text 지정으로 검색 글자가 바뀌었을 때 알린다.
        /// <br/> SearchesGrid면 Grid.SearchText를 설정한 뒤 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler SearchTextChanged;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 검색창을 만든다. Grid를 지정하면 표를 검색한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSearchField()
        {
            AddToClassList(UssClassName);
            styleSheets.Add(TableGridResources.Theme);
            styleSheets.Add(TableGridResources.Style);

            var icon = new VisualElement();
            icon.AddToClassList("xeri-table-search__icon");
            Add(icon);

            textField = new TextField
            {
                name = "search-text",
            };
            textField.AddToClassList("xeri-table-search__input");
            textField.textEdition.placeholder = DEFAULT_PLACEHOLDER;
            textField.RegisterValueChangedCallback(HandleTextChanged);
            textField.RegisterCallback<FocusInEvent>(_ => AddToClassList(FOCUSED_CLASS));
            textField.RegisterCallback<FocusOutEvent>(_ => RemoveFromClassList(FOCUSED_CLASS));
            Add(textField);

            countLabel = new Label
            {
                name = "search-count",
            };
            countLabel.AddToClassList("xeri-table-search__count");
            Add(countLabel);

            // 지우기 버튼은 Xeri Core 아이콘(x)을 쓰고 지운 뒤 입력칸에 포커스를 돌려준다.
            clearButton = new VisualElement
            {
                name = "search-clear",
                tooltip = "검색 지우기",
            };
            clearButton.AddToClassList("xeri-table-search__clear");
            clearButton.AddManipulator(new Clickable(ClearText));
            Add(clearButton);

            RegisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
            RefreshTextState();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표를 지정해 검색창을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSearchField(TableGrid grid) : this()
        {
            Grid = grid;
        }

    #endregion

    #region 검색 글자

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 검색 글자를 바꾸고 SearchesGrid면 표 검색에도 반영한다.
        /// <br/> SearchTextChanged는 알리지 않는다(호스트가 자기 상태를 직접 맞출 때 쓴다).
        /// </summary>
        // --------------------------------------------------------------------------------
        public void SetTextWithoutNotify(string text)
        {
            textField.SetValueWithoutNotify(text ?? string.Empty);
            RefreshAfterTextChange();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 검색 글자를 지우고 입력칸에 포커스를 둔다.
        /// </summary>
        // ------------------------------------------------------------
        public void ClearText()
        {
            Text = string.Empty;
            textField.Focus();
        }

        private void HandleTextChanged(ChangeEvent<string> evt)
        {
            RefreshAfterTextChange();
            SearchTextChanged?.Invoke(this, EventArgs.Empty);
        }

        // 검색 글자가 바뀐 뒤 표 검색·입력칸 상태·결과 수를 맞춘다.
        private void RefreshAfterTextChange()
        {
            ApplyGridSearch();
            RefreshTextState();
            RefreshCount();
        }

        private void ApplyGridSearch()
        {
            if (searchesGrid && grid != null)
            {
                grid.SearchText = Text;
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 표를 검색할 때 입력한 글자는 표로 보내고, 비었으면 표의 검색 글자를 보인다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void SyncGridSearch()
        {
            if (!searchesGrid || grid == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(Text))
            {
                textField.SetValueWithoutNotify(grid.SearchText);
                RefreshTextState();
                return;
            }

            grid.SearchText = Text;
        }

        private void RefreshTextState()
        {
            EnableInClassList(HAS_TEXT_CLASS, !string.IsNullOrEmpty(Text));
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 검색 중이고 결과 수를 보이게 했으면 표의 데이터 행 수를 보인다(그룹 머리 행은 세지 않는다).
        /// </summary>
        // --------------------------------------------------------------------------------
        private void RefreshCount()
        {
            var counting = showResultCount && grid != null && !string.IsNullOrWhiteSpace(Text);
            countLabel.text = counting ? $"{grid.DataRowCount}개" : string.Empty;
            EnableInClassList(COUNTING_CLASS, counting);
        }

    #endregion

    #region 테마

        private void HandleAttachToPanel(AttachToPanelEvent evt)
        {
            // 떨어져 있는 동안 바뀐 결과 수를 놓쳤을 수 있으므로 다시 구독하고 맞춘다.
            Subscribe();
            RefreshCount();

            // 패널이 바뀌면 Editor·Player 판정이 달라질 수 있으므로 다시 정한다.
            TableGridTheming.Apply(this, grid?.Options);
        }

        private void HandleDetachFromPanel(DetachFromPanelEvent evt)
        {
            Unsubscribe();
        }

        // 표의 행 변경은 패널에 붙어 있는 동안만 받는다(떨어진 검색창이 표에 붙들려 있지 않게 한다).
        private void Subscribe()
        {
            if (grid != null)
            {
                grid.RowsChanged -= RefreshCount;
                grid.RowsChanged += RefreshCount;
            }
        }

        private void Unsubscribe()
        {
            if (grid != null)
            {
                grid.RowsChanged -= RefreshCount;
            }
        }

    #endregion

    }
}
