/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridRowSet.cs
수정일 : 2026-11-22

# 설명
표가 보이는 소스 결과의 행을 동기로 조회한다. 위치의 행, ID의 위치, 행 수를 소스에 묻는다.
사용자에게 보이는 번호·개수(행 번호, 전체 N행, 검색 결과 수, 번호로 이동)는 데이터 행만 센다.
위치(선택·스크롤·페이지)는 그룹 머리 행을 포함한 결과 순서다.

# 특이사항, 제약사항
행 수는 소스를 지정할 때 읽어 두며, 호스트가 소스 결과를 바꾸면 SetSource(또는 TableGrid.Refresh)로 다시 지정한다.
소스가 없으면 행이 없다.
데이터 행이 아닌 행(그룹 머리 등)의 위치는 처음 물을 때 결과를 한 번 훑어 기억하고, 소스를 다시 지정하면 버린다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 소스 결과의 행을 동기로 조회한다.
    /// </summary>
    // ============================================================
    internal sealed class TableGridRowSet
    {

    #region 필드

        private ITableGridSource source;

        // 데이터 행이 아닌 행의 위치(오름차순). null이면 아직 세지 않았다.
        private List<int> nonDataPositions;

        // ------------------------------------------------------------
        /// <summary>
        /// 화면이 다룰 행 수. 소스가 없으면 0이다.
        /// </summary>
        // ------------------------------------------------------------
        public int RowCount { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 소스가 있고 행이 하나도 없는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsEmpty => source != null && RowCount == 0;

        // ------------------------------------------------------------
        /// <summary>
        /// 데이터 행 수(그룹 머리 등은 세지 않는다).
        /// </summary>
        // ------------------------------------------------------------
        public int DataRowCount => CountDataRowsBefore(RowCount);

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 소스나 그 결과가 바뀌었을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action Changed;

    #endregion

    #region 소스 연결

        // ----------------------------------------------------------------------
        /// <summary>
        /// 소스를 지정하고 행 수를 다시 읽은 뒤 알린다. 같은 소스여도 다시 읽는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void SetSource(ITableGridSource newSource)
        {
            source = newSource;
            RowCount = source?.Count ?? 0;
            nonDataPositions = null;
            Changed?.Invoke();
        }

    #endregion

    #region 행 조회

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 범위 안 위치의 행을 반환한다. 동기 소스는 범위 안의 행을 늘 주므로
        /// <br/> 범위 밖이거나 소스가 null을 주면 계약 위반으로 예외를 낸다.
        /// </summary>
        // ----------------------------------------------------------------------
        public TableGridRow GetRow(int index)
        {
            if (!Contains(index))
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "행 위치가 소스 결과 범위 밖입니다.");
            }

            return source.GetRow(index) ?? throw new InvalidOperationException($"소스가 위치 {index}의 행을 주지 않았습니다.");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 위치의 행을 찾는다. 위치가 범위 밖이면 false다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetRow(int index, out TableGridRow row)
        {
            row = Contains(index) ? GetRow(index) : null;
            return row != null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 위치의 데이터 행을 찾는다. 범위 밖이거나 데이터 행이 아니면 false다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetDataRow(int index, out TableGridRow row)
        {
            if (TryGetRow(index, out row) && row.IsData)
            {
                return true;
            }

            row = null;
            return false;
        }

        private bool Contains(int index)
        {
            return source != null && index >= 0 && index < RowCount;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 ID의 위치를 찾는다. 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int IndexOf(string rowID)
        {
            if (source == null || rowID == null)
            {
                return -1;
            }

            var index = source.IndexOf(rowID);
            return index < RowCount ? index : -1;
        }

    #endregion

    #region 데이터 행 번호

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 위치 index 앞(index 미포함)에 있는 데이터 행 수.
        /// <br/> index는 0 ~ RowCount로 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        public int CountDataRowsBefore(int index)
        {
            index = Math.Clamp(index, 0, RowCount);
            var positions = EnsureNonDataPositions();

            // 앞에 있는 비데이터 행 수는 정렬된 위치 목록에서 index보다 작은 것의 수다.
            var found = positions.BinarySearch(index);
            var nonDataBefore = found >= 0 ? found : ~found;
            return index - nonDataBefore;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 이미 읽은 위치 index의 행 row로 데이터 행 번호(1부터)를 구한다.
        /// <br/> 데이터 행이 아니면 0이다. 행을 다시 읽지 않는다(본문 연결).
        /// </summary>
        // ----------------------------------------------------------------------
        public int GetDataRowNumber(int index, TableGridRow row)
        {
            return row.IsData ? CountDataRowsBefore(index) + 1 : 0;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// number번째(1부터) 데이터 행의 위치. 범위 밖이면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int FindDataRowByNumber(int number)
        {
            if (number < 1 || number > DataRowCount)
            {
                return -1;
            }

            // 앞쪽 비데이터 행마다 한 칸씩 밀린다.
            var index = number - 1;
            foreach (var position in EnsureNonDataPositions())
            {
                if (position > index)
                {
                    break;
                }

                index++;
            }

            return index;
        }

        private List<int> EnsureNonDataPositions()
        {
            if (nonDataPositions != null)
            {
                return nonDataPositions;
            }

            // 다 센 뒤에만 기억한다(소스가 중간에 예외를 내면 다음에 처음부터 다시 센다).
            var positions = new List<int>();
            for (var index = 0; index < RowCount; index++)
            {
                if (!GetRow(index).IsData)
                {
                    positions.Add(index);
                }
            }

            nonDataPositions = positions;
            return positions;
        }

    #endregion

    }
}
