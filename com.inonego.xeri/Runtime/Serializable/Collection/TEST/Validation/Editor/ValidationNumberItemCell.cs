/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ValidationNumberItemCell.cs
수정일 : 2026-11-19

# 설명
수동 검증 씬에서 ValidationNumberItem을 숫자와 형식 배지로 그리는 셀 뷰 예시.
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

namespace inonego.Xeri.Serializable.Validation.Editor
{
    // ============================================================
    /// <summary>
    /// ValidationNumberItem 셀을 숫자와 형식 배지로 그리는 셀 뷰.
    /// </summary>
    // ============================================================
    [CustomCollectionCell(typeof(ValidationNumberItem))]
    internal sealed class ValidationNumberItemCell : CollectionCellView
    {

    #region 셀 UI

        // ------------------------------------------------------------
        /// <summary>
        /// 숫자와 형식 배지를 가진 셀 UI를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement MakeCell()
        {
            var row = CreateRow();
            var value = CreateText("value");
            value.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(value);

            var type = new Badge("Number");
            type.Tone = BadgeTone.Accent;
            type.style.marginLeft = 6f;
            row.Add(type);
            return row;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 행의 숫자를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public override void BindCell(VisualElement element, SerializedProperty property)
        {
            var value = property.FindPropertyRelative(nameof(ValidationNumberItem.Value)).intValue;
            element.Q<Label>("value").text = value.ToString();
        }

    #endregion

    }
}
