/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionHelper.cs
수정일 : 2026-05-03

# 설명
List·IReadOnlyList·IEnumerable을 대상으로 하는 컬렉션 확장 메서드 모음.
CheckInRange, ClearAndAddRange, Resize를 제공한다.
========================================================================= BLOCK_HEADER_END */

using System.Collections.Generic;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 컬렉션 확장 메서드 정적 헬퍼.
    /// </summary>
    // ============================================================
    public static class CollectionHelper
    {

    #region 검사

        // ------------------------------------------------------------
        /// <summary>
        /// 인덱스가 리스트의 유효 범위 내에 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool CheckInRange<T>(this IReadOnlyList<T> list, int index)
        {
            return 0 <= index && index < list.Count;
        }

    #endregion

    #region 조작

        // ------------------------------------------------------------
        /// <summary>
        /// 리스트를 비운 뒤 컬렉션의 모든 요소를 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void ClearAndAddRange<T>(this List<T> list, IEnumerable<T> collection)
        {
            list.Clear();
            list.AddRange(collection);
        }

        // -----------------------------------------------------------------------
        /// <summary>
        /// <br/> 리스트의 크기를 지정한 크기로 조정한다.
        /// <br/> 크기가 줄어드는 경우 뒤에서부터 제거하고, 늘어나는 경우 기본값으로 채운다.
        /// </summary>
        // -----------------------------------------------------------------------
        public static void Resize<T>(this List<T> list, int size, T defaultValue = default)
        {
            if (list.Count > size)
            {
                list.RemoveRange(size, list.Count - size);
            }
            else
            {
                while (list.Count < size)
                {
                    list.Add(defaultValue);
                }
            }
        }

    #endregion

    }
}
