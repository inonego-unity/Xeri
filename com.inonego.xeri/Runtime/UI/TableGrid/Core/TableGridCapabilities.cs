/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridCapabilities.cs
수정일 : 2026-11-22

# 설명
TableGrid의 옵션(Access·Tools·Selection)과 연결된 요청 이벤트에서 파생한 능력을 읽기 전용으로 보인다.

# 특이사항, 제약사항
값은 TableGrid.Capabilities를 읽은 시점의 스냅샷이다. 옵션이나 이벤트 연결이 바뀌면 다시 읽는다.
열 단위 설정(Editable·Sortable 등)은 반영하지 않는다. 열마다 이 능력 안에서 다시 좁힌다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// TableGrid 옵션과 연결된 요청 이벤트에서 파생한 능력.
    /// </summary>
    // ============================================================
    public sealed class TableGridCapabilities
    {

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 파생한 능력 값으로 스냅샷을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridCapabilities
        (
            bool canEdit,
            bool canDeleteRows,
            bool canMoveRows,
            bool canSelectCellRange,
            bool canSelectRows,
            bool canSortFromHeader,
            bool hasColumnMenu,
            bool canQueryFromMenu,
            bool hasJump,
            bool canApplySortOrder = false,
            bool canSelectColumns = false,
            bool canAddRows = false
        )
        {
            CanEdit = canEdit;
            CanDeleteRows = canDeleteRows;
            CanMoveRows = canMoveRows;
            CanSelectCellRange = canSelectCellRange;
            CanSelectRows = canSelectRows;
            CanSortFromHeader = canSortFromHeader;
            HasColumnMenu = hasColumnMenu;
            CanQueryFromMenu = canQueryFromMenu;
            HasJump = hasJump;
            CanApplySortOrder = canApplySortOrder;
            CanSelectColumns = canSelectColumns;
            CanAddRows = canAddRows;
        }

    #endregion

    #region 능력

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 편집·붙여넣기·지우기·잘라낸 셀 옮기기를 요청할 수 있는지
        /// <br/> (Editable + EditRequested 연결).
        /// </summary>
        // ------------------------------------------------------------
        public bool CanEdit { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 행 삭제를 요청할 수 있는지(Editable + DeleteRowsRequested 연결).
        /// </summary>
        // ----------------------------------------------------------------------
        public bool CanDeleteRows { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 이동을 요청할 수 있는지(Editable + MoveRowsRequested 연결).
        /// <br/> 소스 CanReorder가 참이고 보기 정렬·그룹이 없을 때만 열린다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool CanMoveRows { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 범위를 고를 수 있는지(Selection = Cell).
        /// </summary>
        // ------------------------------------------------------------
        public bool CanSelectCellRange { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 행 선택 칸으로 여러 행을 고를 수 있는지(Selection = MultiRow·Cell).
        /// </summary>
        // ----------------------------------------------------------------------
        public bool CanSelectRows { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 헤더 클릭으로 정렬을 요청하는지(Tools = List + QueryRequested 연결).
        /// </summary>
        // ----------------------------------------------------------------------
        public bool CanSortFromHeader { get; }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 헤더 우클릭으로 열 메뉴(고정·숨기기·맞춤·열 표시)를 여는지(Tools = Workspace).
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool HasColumnMenu { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 열 메뉴에서 정렬·필터·그룹을 요청할 수 있는지
        /// <br/> (Tools = Workspace + QueryRequested 연결).
        /// </summary>
        // ------------------------------------------------------------
        public bool CanQueryFromMenu { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 빠른 이동(Ctrl+G)과 열 이름 포함 복사를 쓰는지(Tools = Workspace).
        /// </summary>
        // ----------------------------------------------------------------------
        public bool HasJump { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 보기 정렬 순서를 데이터에 적용해 달라고 요청할 수 있는지.
        /// <br/> Workspace 도구이고, 정렬 중이며, 행 이동 가능한 데이터일 때만 된다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool CanApplySortOrder { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 헤더로 열을 고르고(클릭·Ctrl·Shift·끌기·Ctrl+Space)
        /// <br/> 고른 열을 끌거나 Ctrl+Alt+←/→로 보기 순서를 옮길 수 있는지
        /// <br/> (Tools = Workspace + Selection = Cell).
        /// </summary>
        // ------------------------------------------------------------
        public bool CanSelectColumns { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 새 행을 넣어 달라고 요청할 수 있는지
        /// <br/> (Editable + AddRowRequested 연결). 우클릭 "위에·아래에 행 삽입"과
        /// <br/> "복사한 행 삽입"이 이 능력을 따른다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool CanAddRows { get; }

    #endregion

    }
}
