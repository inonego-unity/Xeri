/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PickerEntry.cs
수정일 : 2026-06-17

# 설명
Picker entry의 표시 metadata, 검색 텍스트, null 표시 정책 테스트.

# 테스트 구성
 E: Entry metadata
 N: Null / empty normalization
========================================================================= BLOCK_HEADER_END */

using NUnit;
using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.Picker;

namespace inonego.Xeri.TEST.UI._Picker
{
   // ============================================================
   /// <summary>
   /// Picker entry core model 테스트 클래스.
   /// </summary>
   // ============================================================
   public class TEST_PickerEntry
   {

   #region 헬퍼

      // ============================================================
      /// <summary>
      /// 테스트용 원본 entry.
      /// </summary>
      // ============================================================
      private sealed class Entry
      {
         public string ID;
         public string Name;
      }

   #endregion

   #region E-1: Entry metadata

      // ------------------------------------------------------------
      /// <summary>
      /// 검색 제외 column 값은 entry 검색 문자열에 포함되지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_PickerEntry_SearchText_SearchableFalse_Column_제외()
      {
         var pickerEntry = new PickerEntry<Entry, string>
         (
            new Entry { ID = "S-1001", Name = "ALPHA" },
            "S-1001",
            "ALPHA",
            "항목 설명",
            null,
            null,
            new[]
            {
               new PickerColumnValue("visible", "표시", "VISIBLE-CODE", true),
               new PickerColumnValue("hidden", "숨김", "HIDDEN-CODE", false),
            },
            true,
            string.Empty
         );

         Assert.IsTrue(pickerEntry.SearchText.Contains("VISIBLE-CODE"));
         Assert.IsFalse(pickerEntry.SearchText.Contains("HIDDEN-CODE"));
      }

      // ------------------------------------------------------------
      /// <summary>
      /// entry metadata는 검색 문자열과 기본 preview에 반영된다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_PickerEntry_Metadata_SearchText_Preview_생성()
      {
         var sourceEntry = new Entry { ID = "S-1001", Name = "ALPHA" };
         var pickerEntry = new PickerEntry<Entry, string>
         (
            sourceEntry,
            "S-1001",
            "ALPHA",
            "항목 설명",
            null,
            new[] { new PickerTag("상태", "활성") },
            new[]
            {
               new PickerColumnValue("name", "이름", "ALPHA"),
               new PickerColumnValue("age", "점수", 18),
            },
            true,
            string.Empty
         );

         var preview = pickerEntry.CreateDefaultPreview();

         Assert.AreSame(sourceEntry, pickerEntry.Entry);
         Assert.AreEqual("S-1001", pickerEntry.Value);
         Assert.AreEqual("ALPHA", pickerEntry.Label);
         Assert.IsTrue(pickerEntry.SearchText.Contains("S-1001"));
         Assert.IsTrue(pickerEntry.SearchText.Contains("ALPHA"));
         Assert.IsTrue(pickerEntry.SearchText.Contains("활성"));
         Assert.IsTrue(pickerEntry.SearchText.Contains("18"));
         Assert.AreEqual("ALPHA", preview.Name);
         Assert.AreEqual("항목 설명", preview.Desc);
      }

   #endregion

   #region N-1: Null normalization

      // ------------------------------------------------------------
      /// <summary>
      /// null 표시 값은 빈 문자열로 정규화된다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_PickerEntry_NullDisplayValues_빈문자열_정규화()
      {
         var entry = new PickerEntry<Entry, string>
         (
            new Entry(),
            null,
            null,
            null,
            null,
            null,
            new[]
            {
               new PickerColumnValue("code", "코드", null),
            },
            true,
            null
         );

         Assert.AreEqual(string.Empty, entry.Label);
         Assert.AreEqual(string.Empty, entry.Desc);
         Assert.AreEqual(string.Empty, entry.DisabledReason);
         Assert.IsNull(entry.Columns[0].Value);
         Assert.AreEqual(string.Empty, entry.Columns[0].DisplayText);
         Assert.AreEqual(string.Empty, entry.SearchText);
         Assert.AreEqual(string.Empty, entry.CreateDefaultPreview().Name);
         Assert.AreEqual(string.Empty, entry.CreateDefaultPreview().Desc);
      }

   #endregion

   }
}
