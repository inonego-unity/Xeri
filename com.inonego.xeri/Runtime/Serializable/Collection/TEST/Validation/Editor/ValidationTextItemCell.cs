/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ValidationTextItemCell.cs
수정일 : 2026-11-19

# 설명
수동 검증 씬에서 셀 뷰 API를 확인하는 예시.
필드가 셋이라 기본 요약이 타입 이름으로 보이는 ValidationTextItem을 텍스트·종류·사용 여부로 그린다.
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
    /// ValidationTextItem 셀을 텍스트와 종류 배지로 그리는 셀 뷰.
    /// </summary>
    // ============================================================
    [CustomCollectionCell(typeof(ValidationTextItem))]
    internal sealed class ValidationTextItemCell : CollectionCellView
    {

    #region 셀 UI

        // ------------------------------------------------------------
        /// <summary>
        /// 텍스트와 종류 배지를 가진 셀 UI를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement MakeCell()
        {
            var row = CreateRow();
            var text = CreateText("text");
            text.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(text);

            var kind = new Badge();
            kind.name = "kind";
            kind.style.marginLeft = 6f;
            row.Add(kind);
            return row;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 행의 텍스트·종류를 표시하고 사용하지 않는 항목은 흐리게 보인다.
        /// </summary>
        // ------------------------------------------------------------
        public override void BindCell(VisualElement element, SerializedProperty property)
        {
            var text = property.FindPropertyRelative(nameof(ValidationTextItem.Text)).stringValue;
            var kind = (ValidationKind)property.FindPropertyRelative(nameof(ValidationTextItem.Kind)).intValue;
            var enabled = property.FindPropertyRelative(nameof(ValidationTextItem.Enabled)).boolValue;

            element.Q<Label>("text").text = string.IsNullOrEmpty(text) ? "(빈 텍스트)" : text;
            element.Q<Badge>("kind").text = kind.ToString();
            element.style.opacity = enabled ? 1f : 0.45f;
            element.tooltip = enabled ? null : "사용 안 함";
        }

    #endregion

    #region 검색

        // ------------------------------------------------------------
        /// <summary>
        /// 텍스트와 종류 이름으로 검색되게 한다.
        /// </summary>
        // ------------------------------------------------------------
        public override string GetSearchText(SerializedProperty property)
        {
            var text = property.FindPropertyRelative(nameof(ValidationTextItem.Text)).stringValue;
            var kind = (ValidationKind)property.FindPropertyRelative(nameof(ValidationTextItem.Kind)).intValue;
            return $"{text} {kind}";
        }

    #endregion

    }
}
