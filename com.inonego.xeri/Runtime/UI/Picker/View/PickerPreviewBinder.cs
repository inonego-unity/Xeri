/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerPreviewBinder.cs
수정일 : 2026-06-07

# 설명
Picker preview UXML 요소를 session 현재 entry에 바인딩한다.
========================================================================= BLOCK_HEADER_END */

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
         descLabel.text   = preview?.Desc ?? "데이터 종류에 따라 이미지, 요약 정보, 참조, 검증 결과 등을 표시할 수 있습니다.";
         selectButton.SetEnabled(entry != null && entry.IsEnabled);
         descScroll.scrollOffset = Vector2.zero;

         tagContainer.Clear();
         if (preview == null)
         {
            SetTags(session.DefaultPreviewTags);
            return;
         }

         var tags = new List<string>();
         foreach (var tag in preview.Tags)
         {
            if (string.IsNullOrEmpty(tag.Label) && string.IsNullOrEmpty(tag.Value)) continue;

            tags.Add(FormatTag(tag));
         }

         SetTags(tags);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Preview tag row에 하나의 구분 문자열을 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      private void SetTags(IReadOnlyList<string> tags)
      {
         var text = JoinTags(tags);
         if (string.IsNullOrEmpty(text)) return;

         var label = new Label(text);
         label.AddToClassList("xeri-picker__tag");
         tagContainer.Add(label);
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
      /// 여러 tag를 목업과 같은 pipe 구분 문자열로 합친다.
      /// </summary>
      // ------------------------------------------------------------
      private static string JoinTags(IReadOnlyList<string> tags)
      {
         var visibleTags = new List<string>();
         foreach (var tag in tags)
         {
            var text = CleanTagPart(tag);
            if (string.IsNullOrEmpty(text)) continue;

            visibleTags.Add(text);
         }

         return string.Join(" | ", visibleTags);
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
