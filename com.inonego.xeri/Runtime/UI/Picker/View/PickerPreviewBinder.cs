/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerPreviewBinder.cs
수정일 : 2026-11-22

# 설명
Picker preview UXML 요소를 session 현재 entry에 바인딩한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Picker
{
    // ============================================================
    /// <summary>
    /// Picker preview UXML 요소를 session 현재 entry에 바인딩한다.
    /// </summary>
    // ============================================================
    internal sealed class PickerPreviewBinder<TEntry, TValue>
    {

    #region 필드

        private readonly PickerSession<TEntry, TValue> session;
        private readonly Image image;
        private readonly Label nameLabel;
        private readonly Label subLabel;
        private readonly ScrollView descScroll;
        private readonly Label descLabel;
        private readonly VisualElement tagContainer;
        private readonly Button selectButton;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// preview binder를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PickerPreviewBinder(PickerSession<TEntry, TValue> session, VisualElement root) : base()
        {
            this.session = session;
            image        = root.Q<Image>("preview-image");
            nameLabel    = root.Q<Label>("preview-name");
            subLabel     = root.Q<Label>("preview-sub-label");
            descScroll   = root.Q<ScrollView>("preview-desc-scroll");
            descLabel    = root.Q<Label>("preview-desc");
            tagContainer = root.Q<VisualElement>("preview-tags");
            selectButton = root.Q<Button>("select-button");

            selectButton.clicked += session.ConfirmCurrent;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 entry preview를 UI에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Refresh()
        {
            var entry = session.CurrentEntry;
            var preview = entry?.CreateDefaultPreview();

            image.image      = preview?.Image ?? PickerDefaultTextures.GrayChecker;
            nameLabel.text   = preview?.Name ?? "선택 없음";
            subLabel.text    = preview?.SubLabel ?? "목록에서 항목을 선택하세요. 더블 클릭하면 선택됩니다.";
            descLabel.text   = FormatDesc(entry, preview?.Desc);
            selectButton.SetEnabled(entry != null && entry.IsEnabled);
            selectButton.tooltip = entry != null && !entry.IsEnabled ? FormatDisabledReason(entry) : string.Empty;
            descScroll.scrollOffset = Vector2.zero;

            // 태그 글자는 여기서 한 번만 정리하고 빈 것은 담지 않는다. SetTags는 잇기만 한다.
            tagContainer.Clear();
            var tags = new List<string>();
            if (preview == null)
            {
                foreach (var tag in session.DefaultPreviewTags)
                {
                    AddTag(tags, CleanTagPart(tag));
                }
            }
            else
            {
                foreach (var tag in preview.Tags)
                {
                    AddTag(tags, FormatTag(tag));
                }
            }

            SetTags(tags);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 정리된 tag 글자를 " | "로 이어 preview tag row에 한 줄로 표시한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void SetTags(IReadOnlyList<string> tags)
        {
            if (tags.Count == 0) return;

            var label = new Label(string.Join(" | ", tags));
            label.AddToClassList("xeri-picker__tag");
            tagContainer.Add(label);
        }

        private static void AddTag(List<string> tags, string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                tags.Add(text);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 설명 글자. 선택할 수 없는 entry면 그 이유를 맨 앞 줄에 둔다.
        /// </summary>
        // ------------------------------------------------------------
        private static string FormatDesc(PickerEntry<TEntry, TValue> entry, string desc)
        {
            desc ??= string.Empty;
            if (entry == null || entry.IsEnabled) return desc;

            var reason = FormatDisabledReason(entry);
            return string.IsNullOrEmpty(desc) ? reason : $"{reason}\n\n{desc}";
        }

        private static string FormatDisabledReason(PickerEntry<TEntry, TValue> entry)
        {
            return string.IsNullOrEmpty(entry.DisabledReason)
                ? "선택할 수 없습니다."
                : $"선택할 수 없습니다: {entry.DisabledReason}";
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Preview tag 값을 UI 표시용 문자열로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static string FormatTag(PickerTag tag)
        {
            var label = CleanTagPart(tag.Label);
            var value = CleanTagPart(tag.Value);

            if (string.IsNullOrEmpty(label)) return value;
            if (string.IsNullOrEmpty(value)) return label;

            return $"{label} {value}";
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UI tag에 불필요한 구분 문자가 섞이지 않도록 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private static string CleanTagPart(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            return text
                .Replace(":", string.Empty)
                .Replace("<", string.Empty)
                .Replace(">", string.Empty)
                .Trim();
        }

    #endregion

    }
}
