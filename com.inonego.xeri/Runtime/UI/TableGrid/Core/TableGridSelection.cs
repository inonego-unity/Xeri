/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridSelection.cs
수정일 : 2026-11-25

# 설명
셀·행·열 선택 주소와 변경 이벤트 인수를 정의한다.

# 특이사항, 제약사항
행·열 ID 컬렉션은 생성 시 복사되어 선택 스냅샷이 외부 변경에 노출되지 않는다.
종류마다 쓰는 값만 담는다. 셀은 시작·끝 주소, 행은 행 ID, 열은 열 ID만 가진다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 안정적인 행·열 ID로 셀을 식별하는 값 타입이다.
    /// </summary>
    // ============================================================
    public readonly struct TableGridCellAddress : IEquatable<TableGridCellAddress>
    {

    #region 셀 주소

        // ------------------------------------------------------------
        /// <summary>
        /// 행 ID와 열 ID로 셀 주소를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridCellAddress(string rowID, string columnID)
        {
            RowID = TableGridID.Require(rowID, nameof(rowID), "행");
            ColumnID = TableGridID.Require(columnID, nameof(columnID), "열");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀이 속한 행 ID.
        /// </summary>
        // ------------------------------------------------------------
        public string RowID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀이 속한 열 ID.
        /// </summary>
        // ------------------------------------------------------------
        public string ColumnID { get; }

        internal bool IsValid => !string.IsNullOrWhiteSpace(RowID) && !string.IsNullOrWhiteSpace(ColumnID);

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 셀 주소와 행·열 ID가 같은지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Equals(TableGridCellAddress other)
        {
            return string.Equals(RowID, other.RowID, StringComparison.Ordinal)
                && string.Equals(ColumnID, other.ColumnID, StringComparison.Ordinal);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 객체가 같은 셀 주소인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Equals(object obj)
        {
            return obj is TableGridCellAddress other && Equals(other);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 ID와 열 ID로 해시 코드를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override int GetHashCode()
        {
            unchecked
            {
                int rowHash = RowID == null ? 0 : StringComparer.Ordinal.GetHashCode(RowID);
                int columnHash = ColumnID == null ? 0 : StringComparer.Ordinal.GetHashCode(ColumnID);
                return (rowHash * 397) ^ columnHash;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 두 셀 주소가 같은지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator ==(TableGridCellAddress left, TableGridCellAddress right)
        {
            return left.Equals(right);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 두 셀 주소가 다른지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator !=(TableGridCellAddress left, TableGridCellAddress right)
        {
            return !left.Equals(right);
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 변경 불가능한 셀 범위, 행 또는 열 선택 스냅샷이다.
    /// </summary>
    // ============================================================
    public sealed class TableGridSelection
    {

    #region 선택 상태

        // ------------------------------------------------------------
        /// <summary>
        /// 선택이 없는 빈 선택 스냅샷.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridSelection None => none;

        private static readonly TableGridSelection none = new TableGridSelection();

        private TableGridSelection()
        {
            Kind = TableGridSelectionKind.None;
            RowIDs = Array.Empty<string>();
            ColumnIDs = Array.Empty<string>();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 선택 종류와 주소, 행·열 ID 목록으로 선택 스냅샷을 만든다.
        /// <br/> 셀은 주소만, 행은 행 ID만, 열은 열 ID만 가질 수 있다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSelection
        (
            TableGridSelectionKind kind,
            TableGridCellAddress anchor,
            TableGridCellAddress focus,
            IReadOnlyList<string> rowIDs,
            IReadOnlyList<string> columnIDs = null
        )
        {
            if (!kind.IsDefined())
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (kind != TableGridSelectionKind.Column && columnIDs != null && columnIDs.Count > 0)
            {
                throw new ArgumentException("열 ID는 열 선택만 가질 수 있습니다.", nameof(columnIDs));
            }

            if (kind == TableGridSelectionKind.None)
            {
                if (rowIDs != null && rowIDs.Count > 0)
                {
                    throw new ArgumentException("빈 선택은 행 ID를 가질 수 없습니다.", nameof(rowIDs));
                }

                Kind = kind;
                RowIDs = Array.Empty<string>();
                ColumnIDs = Array.Empty<string>();
                return;
            }

            if (kind == TableGridSelectionKind.Row && rowIDs == null)
            {
                throw new ArgumentNullException(nameof(rowIDs));
            }

            if (kind == TableGridSelectionKind.Row && (anchor.IsValid || focus.IsValid))
            {
                throw new ArgumentException("행 선택은 셀 주소를 가질 수 없습니다.");
            }

            if (kind == TableGridSelectionKind.Cell && (!anchor.IsValid || !focus.IsValid))
            {
                throw new ArgumentException("셀 선택에는 올바른 시작·끝 셀 주소가 필요합니다.");
            }

            if (kind == TableGridSelectionKind.Cell && rowIDs != null && rowIDs.Count > 0)
            {
                throw new ArgumentException("셀 선택은 행 ID를 가질 수 없습니다.", nameof(rowIDs));
            }

            if (kind == TableGridSelectionKind.Column)
            {
                ValidateColumnSelection(anchor, focus, rowIDs, columnIDs);
            }

            Kind = kind;
            Anchor = anchor;
            Focus = focus;
            RowIDs = CopyIDs(rowIDs, nameof(rowIDs), "행");
            ColumnIDs = CopyIDs(columnIDs, nameof(columnIDs), "열");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택이 가리키는 단위 종류.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSelectionKind Kind { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 범위 선택의 시작 셀 주소.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridCellAddress Anchor { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 범위 선택의 현재 끝 셀 주소.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridCellAddress Focus { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택된 행 ID 목록의 복사본.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<string> RowIDs { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택된 열 ID 목록의 복사본.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<string> ColumnIDs { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 한 셀만 가리키는 선택을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridSelection SingleCell(TableGridCellAddress address)
        {
            return new TableGridSelection(TableGridSelectionKind.Cell, address, address, null);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 두 셀 사이의 범위 선택을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridSelection CellRange(TableGridCellAddress anchor, TableGridCellAddress focus)
        {
            return new TableGridSelection(TableGridSelectionKind.Cell, anchor, focus, null);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 행 ID 목록으로 행 선택을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridSelection Rows(IReadOnlyList<string> rowIDs)
        {
            if (rowIDs == null)
            {
                throw new ArgumentNullException(nameof(rowIDs));
            }

            return new TableGridSelection(TableGridSelectionKind.Row, default, default, rowIDs);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정한 열 ID 목록으로 열 선택을 만든다.
        /// <br/> 열 선택은 그 열의 모든 행을 가리키며 결과 행이 바뀌어도 뜻이 같다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static TableGridSelection Columns(IReadOnlyList<string> columnIDs)
        {
            if (columnIDs == null)
            {
                throw new ArgumentNullException(nameof(columnIDs));
            }

            return new TableGridSelection(TableGridSelectionKind.Column, default, default, null, columnIDs);
        }

        private static void ValidateColumnSelection
        (
            TableGridCellAddress anchor, TableGridCellAddress focus,
            IReadOnlyList<string> rowIDs, IReadOnlyList<string> columnIDs
        )
        {
            if (columnIDs == null)
            {
                throw new ArgumentNullException(nameof(columnIDs));
            }

            if (rowIDs != null && rowIDs.Count > 0)
            {
                throw new ArgumentException("열 선택은 행 ID를 가질 수 없습니다.", nameof(rowIDs));
            }

            if (anchor.IsValid || focus.IsValid)
            {
                throw new ArgumentException("열 선택은 셀 주소를 가질 수 없습니다.");
            }
        }

        private static IReadOnlyList<string> CopyIDs(IReadOnlyList<string> ids, string parameterName, string label)
        {
            if (ids == null || ids.Count == 0)
            {
                return Array.Empty<string>();
            }

            string[] copy = new string[ids.Count];
            HashSet<string> uniqueIDs = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < ids.Count; index++)
            {
                string id = TableGridID.Require(ids[index], parameterName, label);
                if (!uniqueIDs.Add(id))
                {
                    throw new ArgumentException($"{label} ID가 겹칩니다: {id}", parameterName);
                }

                copy[index] = id;
            }

            return Array.AsReadOnly(copy);
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 이전 선택과 새 선택을 전달하는 변경 이벤트 인수다.
    /// </summary>
    // ============================================================
    public sealed class TableGridSelectionChanged : EventArgs
    {

    #region 변경 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 이전 선택과 새 선택으로 이벤트 인수를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSelectionChanged(TableGridSelection previous, TableGridSelection current)
        {
            Previous = previous ?? throw new ArgumentNullException(nameof(previous));
            Current = current ?? throw new ArgumentNullException(nameof(current));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 변경 전 선택.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSelection Previous { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 변경 후 선택.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSelection Current { get; }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 행 확정(더블클릭·Enter)을 알리는 이벤트 데이터.
    /// </summary>
    // ============================================================
    public sealed class TableGridRowActivated : EventArgs
    {

    #region 확정 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 확정한 행으로 이벤트 데이터를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridRowActivated(string rowID, string columnID)
        {
            RowID = rowID ?? throw new ArgumentNullException(nameof(rowID));
            ColumnID = columnID;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 확정한 행 ID.
        /// </summary>
        // ------------------------------------------------------------
        public string RowID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 확정할 때의 활성 열 ID. 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public string ColumnID { get; }

    #endregion

    }
}
