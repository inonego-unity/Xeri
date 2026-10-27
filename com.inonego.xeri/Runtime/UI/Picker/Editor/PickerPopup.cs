/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerPopup.cs
수정일 : 2026-10-28

# 설명
앵커 요소에 붙는 Picker 팝업을 공통 팝업 호스트로 연다.

# 특이사항, 제약사항
배치 좌표 변환·입력 차단·닫기는 PopupHost가 맡고, 이 파일은 PickerView 생성과 취소 처리,
배치 정책(아래 우선·뒤집기·최소 높이)만 정한다.
확정 선택 없이 닫히면 취소 callback을 호출한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.Picker;

namespace inonego.Xeri.Editor.Picker
{
    // ============================================================
    /// <summary>
    /// 앵커 요소에 붙는 Picker 팝업의 진입점.
    /// </summary>
    // ============================================================
    internal static class PickerPopup
    {

    #region 상수

        private const string popupClass = "xeri-picker-host--popup";
        private const float minimumHeight = 180f;
        private static readonly Vector2 initialSize = new(290f, 280f);

    #endregion

    #region 열기

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Picker를 앵커 요소 아래(부족하면 위)에 붙는 팝업으로 연다.
        /// <br/> 선택을 확정하거나 포커스를 잃어 닫히면 팝업을 정리하고 취소를 처리한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal static void Open<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            TValue currentValue,
            Action<TValue> onSelected,
            Action onCanceled,
            int pageSize,
            VisualElement anchor
        )
        {
            if (anchor == null)
            {
                throw new ArgumentNullException(nameof(anchor));
            }

            var bridge = new PickerWindowBridge<TEntry, TValue>
            (
                spec,
                entries,
                currentValue,
                onSelected,
                onCanceled,
                pageSize
            );
            PopupHost host = null;

            void Close()
            {
                host?.Close();
                bridge.CancelIfNotCompleted();
            }

            try
            {
                host = PopupHost.Open(anchor, anchor.worldBound, null, Close);

                var view = bridge.CreateView(Close);
                view.AddToClassList(popupClass);

                var result = host.Solve
                (
                    host.ToPopupRect(anchor.worldBound), initialSize, CreateOptions()
                );
                host.OpenLevel(0, result.Bounds).Add(view);
            }
            catch
            {
                Close();
                throw;
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 앵커 아래를 우선하고 부족하면 위로 열며 화면 오른쪽을 넘으면 앵커 오른쪽 끝에 맞춘다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static PopupPlacementOptions CreateOptions()
        {
            return new PopupPlacementOptions
            (
                PopupPlacementDirection.Below,
                minMainSize: minimumHeight,
                allowAnchorOverlap: true,
                allowAlignmentFlip: true
            );
        }

    #endregion

    }
}
