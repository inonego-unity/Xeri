/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerSession.cs
수정일 : 2026-11-22

# 설명
Picker 실행 단위의 상태: spec으로 만든 표시 항목, filter chip 켜짐 상태, 현재 항목, 확정 선택.
검색·정렬·스크롤·키보드 선택 이동은 목록 표(TableGrid)가 맡고, 표 연결은 PickerTableGridBinder가 한다.

# 특이사항, 제약사항
UI 없이 동작한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.UI.Picker
{
    // ============================================================
    /// <summary>
    /// Picker 실행 단위의 상태를 관리한다.
    /// </summary>
    // ============================================================
    public sealed class PickerSession<TEntry, TValue>
    {

    #region 필드

        private readonly PickerSpec<TEntry, TValue> spec;
        private readonly Action<TValue> onSelected;
        private readonly Dictionary<string, bool> filterStates = new();

        // ------------------------------------------------------------
        /// <summary>
        /// spec으로 만든 전체 표시 항목(원본 순서).
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<PickerEntry<TEntry, TValue>> Entries { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 강조된 entry.
        /// </summary>
        // ------------------------------------------------------------
        public PickerEntry<TEntry, TValue> CurrentEntry { get; private set; } = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 켜진 filter chip이 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        internal bool HasActiveFilter
        {
            get
            {
                foreach (var enabled in filterStates.Values)
                {
                    if (enabled) return true;
                }

                return false;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// table column 정의 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<PickerColumnSpec<TEntry>> Columns => spec.Columns;

        // ------------------------------------------------------------
        /// <summary>
        /// filter 정의 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<PickerFilterSpec<TEntry, TValue>> Filters => spec.Filters;

        // ------------------------------------------------------------
        /// <summary>
        /// view 전용 표시 옵션.
        /// </summary>
        // ------------------------------------------------------------
        public PickerViewOptions ViewOptions => spec.ViewOptions;

        // ------------------------------------------------------------
        /// <summary>
        /// preview 영역 표시 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool ShowPreview => ViewOptions.ShowPreview;

        // ------------------------------------------------------------
        /// <summary>
        /// 선택 없음 preview에 표시할 기본 tag label 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<string> DefaultPreviewTags => spec.DefaultPreviewTags;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 entry나 filter 상태가 바뀔 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler Changed = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 선택이 확정될 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<PickerSelectionEventArgs<TEntry, TValue>> Confirmed = null;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// filter chip 상태가 바뀌어 목록을 다시 걸러야 할 때 발생한다(Changed보다 먼저).
        /// </summary>
        // --------------------------------------------------------------------------------
        internal event Action FiltersChanged = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Picker session을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PickerSession
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            TValue currentValue,
            Action<TValue> onSelected
        ) : base()
        {
            this.spec = spec ?? throw new ArgumentNullException(nameof(spec));
            this.onSelected = onSelected ?? (_ => { });
            Entries = spec.CreateEntries(entries);

            foreach (var filter in spec.Filters)
            {
                filterStates[filter.ID] = filter.DefaultEnabled;
            }

            CurrentEntry = FindEntryByValue(currentValue);
        }

    #endregion

    #region 필터

        // ------------------------------------------------------------
        /// <summary>
        /// filter 활성 상태를 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetFilterEnabled(string filterID, bool enabled)
        {
            filterStates[filterID ?? string.Empty] = enabled;
            FiltersChanged?.Invoke();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// filter 활성 상태를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool GetFilterEnabled(string filterID)
        {
            return filterStates.TryGetValue(filterID ?? string.Empty, out var enabled) && enabled;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 켜진 모든 filter를 통과하는지 확인한다(목록 표 소스의 필터).
        /// </summary>
        // ------------------------------------------------------------
        internal bool MatchesFilters(PickerEntry<TEntry, TValue> entry)
        {
            foreach (var filter in spec.Filters)
            {
                if (!filterStates.TryGetValue(filter.ID, out var enabled) || !enabled) continue;

                if (!filter.IsMatch(entry))
                {
                    return false;
                }
            }

            return true;
        }

    #endregion

    #region 선택

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 entry를 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetCurrentEntry(PickerEntry<TEntry, TValue> entry)
        {
            // 표가 현재 선택을 다시 반영해도 상태가 같으면 View 갱신을 재진입시키지 않는다.
            if (ReferenceEquals(CurrentEntry, entry)) return;

            CurrentEntry = entry;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 선택을 비운다.
        /// </summary>
        // ------------------------------------------------------------
        public void ClearCurrentEntry()
        {
            SetCurrentEntry(null);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 entry를 확정 선택한다. 비활성 entry는 무시한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Confirm(PickerEntry<TEntry, TValue> entry)
        {
            if (entry == null || !entry.IsEnabled) return;

            CurrentEntry = entry;
            onSelected(entry.Value);
            Confirmed?.Invoke(this, new PickerSelectionEventArgs<TEntry, TValue>(entry));
            Changed?.Invoke(this, EventArgs.Empty);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 entry를 확정 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ConfirmCurrent()
        {
            Confirm(CurrentEntry);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 선택값과 같은 값을 가진 entry를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private PickerEntry<TEntry, TValue> FindEntryByValue(TValue value)
        {
            foreach (var entry in Entries)
            {
                if (EqualityComparer<TValue>.Default.Equals(entry.Value, value))
                {
                    return entry;
                }
            }

            return null;
        }

    #endregion

    }
}
