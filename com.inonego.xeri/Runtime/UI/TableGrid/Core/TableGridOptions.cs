/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridOptions.cs
수정일 : 2026-11-22

# 설명
TableGrid의 구성(권한·도구 수준·선택 방식), 조회, 화면, 테마 옵션과 사용처별 프리셋을 정의한다.

# 특이사항, 제약사항
기능을 하나씩 켜고 끄지 않는다. 데이터를 바꾸는 동작은 Access, 열 메뉴·필터 편집 같은 작업공간 도구는 Tools 하나로 열고 닫고,
나머지 능력은 TableGrid.Capabilities가 이 두 축과 연결된 이벤트·열 설정에서 파생한다.
옵션은 변경 가능하며 사용 시점에 Validate로 값 범위를 확인한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// TableGrid의 구성·조회·표시·테마 옵션 묶음이다.
    /// </summary>
    // ============================================================
    public sealed class TableGridOptions
    {

    #region 구성

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 데이터를 바꾸는 동작(편집·붙여넣기·지우기·잘라내기·행 삭제·행 이동)을 허용하는지 여부.
        /// </summary>
        // --------------------------------------------------------------------------------
        public TableGridAccess Access { get; set; } = TableGridAccess.ReadOnly;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 목록 수준(헤더 클릭 정렬)인지 작업공간 수준(열 메뉴·필터 편집·열 패널·빠른 이동)인지.
        /// </summary>
        // --------------------------------------------------------------------------------
        public TableGridTools Tools { get; set; } = TableGridTools.List;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 선택 방식. Cell은 셀 범위, MultiRow는 여러 행, Row는 한 행을 고른다.
        /// <br/> Workspace에서 Cell이면 헤더로 열을 고르고 옮길 수도 있다.
        /// </summary>
        // ----------------------------------------------------------------------
        public TableGridSelectionMode Selection { get; set; } = TableGridSelectionMode.Row;

    #endregion

    #region 표시 모드

        // ------------------------------------------------------------
        /// <summary>
        /// 행을 넘겨 보는 방식. 기본은 Scroll이다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridNavigation Navigation { get; set; } = TableGridNavigation.Scroll;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Paged일 때 한 페이지 위치 수(그룹 머리 행 포함).
        /// <br/> 0이면 보이는 높이에 들어가는 행 수가 한 페이지다(페이지 안 스크롤 없음).
        /// <br/> N이면 높이가 모자랄 때 페이지 안에서 스크롤하고 남으면 비워 둔다.
        /// </summary>
        // ----------------------------------------------------------------------
        public int PageSize { get; set; }

    #endregion

    #region 화면

        // ------------------------------------------------------------
        /// <summary>
        /// 만들어 재사용할 행 요소 수의 상한.
        /// </summary>
        // ------------------------------------------------------------
        public int MaxRenderedRows { get; set; } = 200;

        // ------------------------------------------------------------
        /// <summary>
        /// 보이는 범위 위아래로 미리 그릴 행 수.
        /// </summary>
        // ------------------------------------------------------------
        public int OverscanRows { get; set; } = 8;

        // ------------------------------------------------------------
        /// <summary>
        /// 행 높이(UI px).
        /// </summary>
        // ------------------------------------------------------------
        public float RowHeight { get; set; } = 32f;

        // ------------------------------------------------------------
        /// <summary>
        /// 헤더 줄을 보일지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool ShowHeader { get; set; } = true;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 앞의 선택 칸(행 번호)을 보일지 여부.
        /// <br/> null이면 Selection이 MultiRow·Cell일 때 보인다.
        /// <br/> 칸에는 표시 순서(1부터)의 행 번호가 보이고, 누르거나 끌면 행을 고른다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool? ShowRowSelector { get; set; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 아래 상태 줄을 보일지 여부. null이면 Tools가 Workspace일 때 보인다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool? ShowStatusBar { get; set; }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 행 앞의 끌기 손잡이 열을 보일지 여부. null이면 행 이동을 요청할 수 있을 때 보인다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool? ShowReorderHandle { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 행이 없을 때 본문 가운데에 보일 글자.
        /// </summary>
        // ------------------------------------------------------------
        public string EmptyText { get; set; } = "표시할 행이 없습니다";

        // ------------------------------------------------------------
        /// <summary>
        /// 검색·필터 결과 행이 없을 때 보일 글자.
        /// </summary>
        // ------------------------------------------------------------
        public string NoResultText { get; set; } = "조건에 맞는 행이 없습니다";

    #endregion

    #region 테마

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 테마. Editor는 Unity 에디터 색을 쓰며 Editor 패널에서만 적용되고,
        /// <br/> Player 패널에서는 Default로 바뀐다.
        /// </summary>
        // ----------------------------------------------------------------------
        public TableGridTheme Theme { get; set; } = TableGridTheme.Default;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// Default 테마의 다크·라이트. Auto는 Editor 패널이면 Unity 스킨, Player 패널이면 다크다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public TableGridColorMode ColorMode { get; set; } = TableGridColorMode.Auto;

    #endregion

    #region 파생 값

        internal bool IsEditable => Access == TableGridAccess.Editable;

        internal bool IsWorkspace => Tools == TableGridTools.Workspace;

        internal bool IsPaged => Navigation == TableGridNavigation.Paged;

        internal bool AllowsCellRange => Selection == TableGridSelectionMode.Cell;

        internal bool AllowsRowSelection => Selection == TableGridSelectionMode.MultiRow || Selection == TableGridSelectionMode.Cell;

        internal bool AllowsColumnSelection => IsWorkspace && AllowsCellRange;

        internal bool ShowsRowSelector => ShowRowSelector ?? AllowsRowSelection;

        internal bool ShowsStatusBar => ShowStatusBar ?? IsWorkspace;

    #endregion

    #region 프리셋

        // ------------------------------------------------------------
        /// <summary>
        /// Picker용 프리셋. 읽기 전용, 목록 수준, 한 행 선택, Editor 테마.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridOptions Picker()
        {
            return new TableGridOptions
            {
                Access = TableGridAccess.ReadOnly,
                Tools = TableGridTools.List,
                Selection = TableGridSelectionMode.Row,
                Theme = TableGridTheme.Editor,
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> Inspector(Collection Editor)용 프리셋.
        /// <br/> 편집 가능, 목록 수준, 한 행 선택, Editor 테마.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridOptions Inspector()
        {
            return new TableGridOptions
            {
                Access = TableGridAccess.Editable,
                Tools = TableGridTools.List,
                Selection = TableGridSelectionMode.Row,
                Theme = TableGridTheme.Editor,
            };
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 데이터 작업공간용 프리셋. 작업공간 도구, 셀 범위 선택, Default 테마.
        /// </summary>
        // ----------------------------------------------------------------------
        public static TableGridOptions Workspace(TableGridAccess access)
        {
            return new TableGridOptions
            {
                Access = access,
                Tools = TableGridTools.Workspace,
                Selection = TableGridSelectionMode.Cell,
                Theme = TableGridTheme.Default,
            };
        }

    #endregion

    #region 옵션 검증

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 수치와 enum 옵션이 유효한지 검사한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Validate()
        {
            ValidateEnum(typeof(TableGridAccess), Access, nameof(Access));
            ValidateEnum(typeof(TableGridTools), Tools, nameof(Tools));
            ValidateEnum(typeof(TableGridSelectionMode), Selection, nameof(Selection));
            ValidateEnum(typeof(TableGridTheme), Theme, nameof(Theme));
            ValidateEnum(typeof(TableGridColorMode), ColorMode, nameof(ColorMode));
            ValidateEnum(typeof(TableGridNavigation), Navigation, nameof(Navigation));
            ValidateNonNegative(PageSize, nameof(PageSize));
            ValidatePositive(MaxRenderedRows, nameof(MaxRenderedRows));
            ValidateNonNegative(OverscanRows, nameof(OverscanRows));

            if (float.IsNaN(RowHeight) || float.IsInfinity(RowHeight) || RowHeight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(RowHeight), "행 높이는 양의 유한한 값이어야 합니다.");
            }
        }

        private static void ValidateEnum(Type enumType, object value, string parameterName)
        {
            if (!Enum.IsDefined(enumType, value))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static void ValidatePositive(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "양수여야 합니다.");
            }
        }

        private static void ValidateNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "음수일 수 없습니다.");
            }
        }

    #endregion

    }
}
