/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerFilterSpec.cs
수정일 : 2026-10-31

# 설명
Picker filter chip과 실제 필터 조건을 함께 보관하는 모델.
========================================================================= BLOCK_HEADER_END */

using System;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.UI.Picker
{
    // ============================================================
    /// <summary>
    /// Picker filter chip과 실제 필터 조건을 함께 보관하는 모델.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class PickerFilterSpec<TEntry, TValue>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// filter 식별자.
        /// </summary>
        // ------------------------------------------------------------
        public readonly string ID;

        // ------------------------------------------------------------
        /// <summary>
        /// filter 표시 라벨.
        /// </summary>
        // ------------------------------------------------------------
        public readonly string Label;

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 활성 상태.
        /// </summary>
        // ------------------------------------------------------------
        public readonly bool DefaultEnabled;

        private readonly IFilter<PickerEntry<TEntry, TValue>> filter;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// filter 정의를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PickerFilterSpec
        (
            string id,
            string label,
            bool defaultEnabled,
            IFilter<PickerEntry<TEntry, TValue>> filter
        )
        {
            ID             = id ?? string.Empty;
            Label          = label ?? string.Empty;
            DefaultEnabled = defaultEnabled;
            this.filter    = filter ?? new PredicateFilter<PickerEntry<TEntry, TValue>>(_ => true);
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// entry가 filter 조건에 맞는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsMatch(PickerEntry<TEntry, TValue> entry)
        {
            return filter.IsMatch(entry);
        }

    #endregion

    }
}
