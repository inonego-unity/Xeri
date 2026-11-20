/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ValidationRowCell.cs
수정일 : 2026-11-19

# 설명
수동 검증 씬에서 Columns 없이 쓴 ValidationRow를 이름·수량·비율·종류로 그리는 셀 뷰 예시.

# 특이사항, 제약사항
Columns로 필드를 나눈 목록은 셀 형식이 필드 형식이므로 이 뷰를 쓰지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;
using inonego.Xeri.Serializable.Editor;
using inonego.Xeri.UI;

namespace inonego.Xeri.Serializable.Validation.Editor
{
    // ============================================================
    /// <summary>
    /// ValidationRow 셀을 이름, 수량×비율, 종류 배지로 그리는 셀 뷰.
    /// </summary>
    // ============================================================
    [CustomCollectionCell(typeof(ValidationRow))]
    internal sealed class ValidationRowCell : CollectionCellView
    {

    #region 셀 UI

        // ------------------------------------------------------------
        /// <summary>
        /// 이름, 수량, 종류 배지를 가진 셀 UI를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement MakeCell()
        {
            var row = CreateRow();
            var label = CreateText("label");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(label);

            var amount = CreateText("amount");
            amount.style.marginLeft = 6f;
            amount.style.opacity = 0.7f;
            row.Add(amount);

            var kind = new Badge();
            kind.name = "kind";
            kind.style.marginLeft = 6f;
            row.Add(kind);
            return row;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 행의 이름·수량·비율·종류를 표시하고 사용하지 않는 행은 흐리게 보인다.
        /// </summary>
        // ----------------------------------------------------------------------
        public override void BindCell(VisualElement element, SerializedProperty property)
        {
            var label = property.FindPropertyRelative(nameof(ValidationRow.Label)).stringValue;
            var amount = property.FindPropertyRelative(nameof(ValidationRow.Amount)).intValue;
            var ratio = property.FindPropertyRelative(nameof(ValidationRow.Ratio)).floatValue;
            var enabled = property.FindPropertyRelative(nameof(ValidationRow.Enabled)).boolValue;
            var kind = (ValidationKind)property.FindPropertyRelative(nameof(ValidationRow.Kind)).intValue;

            element.Q<Label>("label").text = string.IsNullOrEmpty(label) ? "(이름 없음)" : label;
            element.Q<Label>("amount").text = $"{amount} × {ratio:0.##}";
            element.Q<Badge>("kind").text = kind.ToString();
            element.style.opacity = enabled ? 1f : 0.45f;
            element.tooltip = enabled ? null : "사용 안 함";
        }

    #endregion

    #region 검색

        // ------------------------------------------------------------
        /// <summary>
        /// 이름과 종류 이름으로 검색되게 한다.
        /// </summary>
        // ------------------------------------------------------------
        public override string GetSearchText(SerializedProperty property)
        {
            var label = property.FindPropertyRelative(nameof(ValidationRow.Label)).stringValue;
            var kind = (ValidationKind)property.FindPropertyRelative(nameof(ValidationRow.Kind)).intValue;
            return $"{label} {kind}";
        }

    #endregion

    }
}
