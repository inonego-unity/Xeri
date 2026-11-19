/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ValidationGroupItemCell.cs
수정일 : 2026-11-19

# 설명
수동 검증 씬에서 ValidationGroupItem을 색 견본·이름·값 개수·대상 오브젝트로 그리는 셀 뷰 예시.
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
    // ======================================================================
    /// <summary>
    /// ValidationGroupItem 셀을 색 견본과 이름, 개수 배지로 그리는 셀 뷰.
    /// </summary>
    // ======================================================================
    [CustomCollectionCell(typeof(ValidationGroupItem))]
    internal sealed class ValidationGroupItemCell : CollectionCellView
    {

    #region 셀 UI

        // ------------------------------------------------------------
        /// <summary>
        /// 색 견본, 이름, 개수 배지, 대상 이름을 가진 셀 UI를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement MakeCell()
        {
            var row = CreateRow();
            var tint = new ColorSwatch();
            tint.name = "tint";
            tint.style.marginRight = 6f;
            row.Add(tint);

            var name = CreateText("name");
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(name);

            // 값 개수는 자릿수가 바뀌어도 뒤 글자 위치가 흔들리지 않도록 고정 폭으로 둔다.
            var count = new Badge();
            count.name = "count";
            count.Sizing = BadgeSizing.Fixed;
            count.style.marginLeft = 6f;
            row.Add(count);

            var target = CreateText("target");
            target.style.flexGrow = 1f;
            target.style.marginLeft = 6f;
            target.style.opacity = 0.7f;
            row.Add(target);
            return row;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 행의 색·이름·값 개수·대상 오브젝트를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public override void BindCell(VisualElement element, SerializedProperty property)
        {
            var name = property.FindPropertyRelative(nameof(ValidationGroupItem.Name)).stringValue;
            var values = property.FindPropertyRelative(nameof(ValidationGroupItem.Values));
            var target = property.FindPropertyRelative(nameof(ValidationGroupItem.Target)).objectReferenceValue;
            var tint = property.FindPropertyRelative(nameof(ValidationGroupItem.Tint)).colorValue;

            element.Q<ColorSwatch>("tint").Color = tint;
            element.Q<Label>("name").text = string.IsNullOrEmpty(name) ? "(이름 없음)" : name;
            element.Q<Badge>("count").text = $"{values.arraySize} values";
            element.Q<Label>("target").text = target != null ? target.name : string.Empty;
        }

    #endregion

    #region 검색

        // ------------------------------------------------------------
        /// <summary>
        /// 이름과 대상 오브젝트 이름으로 검색되게 한다.
        /// </summary>
        // ------------------------------------------------------------
        public override string GetSearchText(SerializedProperty property)
        {
            var name = property.FindPropertyRelative(nameof(ValidationGroupItem.Name)).stringValue;
            var target = property.FindPropertyRelative(nameof(ValidationGroupItem.Target)).objectReferenceValue;
            return target != null ? $"{name} {target.name}" : name;
        }

    #endregion

    }
}
