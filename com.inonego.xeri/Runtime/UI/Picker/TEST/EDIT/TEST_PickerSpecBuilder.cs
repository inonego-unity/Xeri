/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PickerSpecBuilder.cs
수정일 : 2026-06-07

# 설명
PickerSpec builder가 entry, column, tag, filter 계약을 구성하는지 검증한다.

# 테스트 구성
 B: Builder
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NUnit;
using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.Picker;

namespace inonego.Xeri.TEST.UI._Picker
{
   // ============================================================
   /// <summary>
   /// PickerSpec builder 테스트 클래스.
   /// </summary>
   // ============================================================
   public class TEST_PickerSpecBuilder
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
         public int Score;
         public string Status;
         public object Icon;
      }

   #endregion

   #region B-1: Builder

      // ------------------------------------------------------------
      /// <summary>
      /// builder는 spec과 picker entry 표시 계약을 구성한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_PickerSpecBuilder_Build_Entry_Column_Tag_생성()
      {
         var spec = PickerSpec<Entry, string>
            .Create("항목 선택")
            .Value(entry => entry.ID)
            .Label(entry => entry.Name)
            .Desc(entry => $"점수 {entry.Score}")
            .Tag("상태", entry => entry.Status)
            .DefaultPreviewTags("정보", "경로")
            .Column("이름", entry => entry.Name)
            .Column("점수", entry => entry.Score)
            .Column("아이콘", entry => entry.Icon, 48f, sortable: false)
            .Build();

         var sourceEntry = new Entry { ID = "S-1001", Name = "ALPHA", Score = 18, Status = "활성", Icon = new object() };
         var pickerEntry = spec.CreateEntry(sourceEntry);

         Assert.AreEqual("항목 선택", spec.Title);
         Assert.IsTrue(spec.ViewOptions.ShowPreview);
         Assert.AreEqual("S-1001", pickerEntry.Value);
         Assert.AreEqual("ALPHA", pickerEntry.Label);
         Assert.AreEqual(3, pickerEntry.Columns.Count);
         Assert.IsFalse(spec.Columns[2].Sortable);
         Assert.That(spec.DefaultPreviewTags, Is.EqualTo(new[] { "정보", "경로" }));
         Assert.AreEqual("활성", pickerEntry.Tags.Single().Value);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// builder 기본 preview tag는 도메인 label을 갖지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_PickerSpecBuilder_Build_DefaultPreviewTags_기본값_비어있음()
      {
         var spec = PickerSpec<Entry, string>
            .Create("항목 선택")
            .Value(entry => entry.ID)
            .Build();

         Assert.IsEmpty(spec.DefaultPreviewTags);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// builder preview 옵션은 spec 표시 계약에 반영된다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_PickerSpecBuilder_Build_Preview_표시여부_설정()
      {
         var spec = PickerSpec<Entry, string>
            .Create("항목 선택")
            .Value(entry => entry.ID)
            .Preview(false)
            .Build();

         Assert.IsFalse(spec.ViewOptions.ShowPreview);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// collection picker facade는 기본 preview를 표시하지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_PickerSpecBuilder_CollectionPicker_Preview_기본값_꺼짐()
      {
         var listSpec = ListPicker
            .Spec<string>("List 선택")
            .Build();
         var dictionarySpec = DictionaryPicker
            .Spec<string, int>("Dictionary 선택")
            .Build();

         Assert.IsFalse(listSpec.ViewOptions.ShowPreview);
         Assert.IsFalse(dictionarySpec.ViewOptions.ShowPreview);
      }

   #endregion

   }
}
