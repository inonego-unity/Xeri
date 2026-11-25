/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridEditState.cs
수정일 : 2026-11-23

# 설명
편집 상태. 지금 편집 중인 셀과 편집 전 값, 붙여넣기에서 거부된 셀(잠깐 강조할 대상),
마지막 복사의 값 묶음과 그 복사가 잘라내기인지(붙여넣기 전까지 원본을 흐리게 표시)를 보관한다.

# 특이사항, 제약사항
셀은 행·열 ID로 가리키므로 가상화로 행 요소가 바뀌어도 같은 셀을 가리킨다.
거부 표시는 세대 번호로 묶어, 새 붙여넣기가 오면 이전 표시를 지우는 예약이 새 표시를 지우지 않게 한다.
잘라내기는 복사와 같고 표시만 더한다. 같은 표에 붙여넣으면 값을 옮기고(잘라낸 행은 잘라낸 행 삽입으로 행째 옮길 수도 있다), 한 번 옮기면 표시를 지운다.
UI Toolkit에 의존하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 편집 상태.
    /// </summary>
    // ============================================================
    internal sealed class TableGridEditState
    {

    #region 필드

        private const int MAX_REJECTED_CELLS = 256;

        private readonly HashSet<TableGridCellAddress> rejected = new();
        private readonly HashSet<string> cutRowSet = new(StringComparer.Ordinal);
        private readonly HashSet<string> cutColumnSet = new(StringComparer.Ordinal);

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 중인 셀. 편집 중이 아니면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridCellAddress? Editing { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집을 시작할 때의 셀 값.
        /// </summary>
        // ------------------------------------------------------------
        public object OriginalValue { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 거부 표시 묶음의 세대. 새로 표시하면 바뀐다.
        /// </summary>
        // ------------------------------------------------------------
        public Revision RejectedGeneration { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 거부 표시할 셀이 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasRejected => rejected.Count > 0;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 마지막 복사의 값 묶음. 표 안에서 붙여넣을 때 글자가 같으면 이 값을 쓴다.
        /// <br/> 복사한 적이 없으면 null이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public TableGridClipboardBundle LastCopied { get; private set; }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 마지막 복사가 아직 옮기지 않은 잘라내기인지 여부.
        /// <br/> 같은 표에 붙여넣으면 값을 옮긴다. 잘라낸 행은 잘라낸 행 삽입으로 행째 옮길 수도 있다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool IsCut { get; private set; }

    #endregion

    #region 편집 중인 셀

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 편집을 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Begin(TableGridCellAddress address, object originalValue)
        {
            Editing = address;
            OriginalValue = originalValue;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 편집을 끝낸다.
        /// </summary>
        // ------------------------------------------------------------
        public void End()
        {
            Editing = null;
            OriginalValue = null;
        }

    #endregion

    #region 거부 표시

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 거부된 셀을 새 묶음으로 표시하고 세대를 반환한다.
        /// <br/> 너무 많은 셀은 앞쪽 일부만 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public Revision MarkRejected(IEnumerable<TableGridCellAddress> cells)
        {
            rejected.Clear();
            foreach (var cell in cells)
            {
                if (rejected.Count >= MAX_REJECTED_CELLS)
                {
                    break;
                }

                rejected.Add(cell);
            }

            RejectedGeneration = RejectedGeneration.Inc();

            return RejectedGeneration;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 세대의 거부 표시를 지운다. 그사이 새로 표시했으면 false다.
        /// </summary>
        // ------------------------------------------------------------
        public bool ClearRejected(Revision generation)
        {
            if (generation != RejectedGeneration || rejected.Count == 0)
            {
                return false;
            }

            rejected.Clear();
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀이 거부 표시 대상인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsRejected(string rowID, string columnID)
        {
            return rejected.Count > 0 && rejected.Contains(new TableGridCellAddress(rowID, columnID));
        }

    #endregion

    #region 복사와 잘라내기

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 마지막 복사를 바꾼다. cut이면 잘라내기로 표시하고,
        /// <br/> 아니면 앞의 잘라내기 표시를 지운다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetCopied(TableGridClipboardBundle bundle, bool cut)
        {
            ClearCut();
            LastCopied = bundle ?? throw new ArgumentNullException(nameof(bundle));
            if (!cut)
            {
                return;
            }

            IsCut = true;
            cutRowSet.UnionWith(bundle.SourceRowIDs);
            cutColumnSet.UnionWith(bundle.SourceColumnIDs);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 시스템 클립보드 글자를 값 묶음으로 읽는다.
        /// <br/> 마지막 복사의 TSV와 같으면 그 값 묶음을, 아니면 글자만 가진 묶음을 돌려준다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridClipboardBundle ResolveClip(string text)
        {
            return LastCopied != null && LastCopied.Matches(text)
                ? LastCopied
                : TableGridClipboardBundle.FromText(text);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 잘라내기 표시를 지운다. 마지막 복사는 그대로 둔다.
        /// <br/> 지울 것이 있었으면 true다.
        /// </summary>
        // ------------------------------------------------------------
        public bool ClearCut()
        {
            var had = IsCut;
            IsCut = false;
            cutRowSet.Clear();
            cutColumnSet.Clear();
            return had;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 선택에서 잘라낸 행인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsCutRow(string rowID)
        {
            return IsCut && LastCopied.SourceKind == TableGridSelectionKind.Row && rowID != null && cutRowSet.Contains(rowID);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀·열 선택에서 잘라낸 셀인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsCutCell(string rowID, string columnID)
        {
            if (!IsCut || LastCopied.SourceKind == TableGridSelectionKind.Row || rowID == null || columnID == null)
            {
                return false;
            }

            return cutRowSet.Contains(rowID) && cutColumnSet.Contains(columnID);
        }

    #endregion

    }
}
