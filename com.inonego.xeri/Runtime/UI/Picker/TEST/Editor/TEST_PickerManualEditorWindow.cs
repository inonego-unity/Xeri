/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PickerManualEditorWindow.cs
수정일 : 2026-06-07

# 설명
Picker EditorWindow 선택 UI를 직접 조작해 확인하는 수동 Editor 테스트.
기본 Picker, ListPicker, DictionaryPicker의 선택과 취소 처리를 직접 확인한다.

# 테스트 구성
 M: 수동 확인

# 특이사항
[Explicit] 과 [Category("Manual")] 로 일반 테스트 실행에서 멈추지 않게 분리한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using NUnit;
using NUnit.Framework;

using inonego.Xeri.Editor;
using inonego.Xeri.Editor.Picker;
using inonego.Xeri.UI;
using inonego.Xeri.UI.Picker;

namespace inonego.Xeri.TEST.UI._Picker
{
   // ============================================================
   /// <summary>
   /// Picker 수동 EditorWindow 테스트 클래스.
   /// </summary>
   // ============================================================
   public class TEST_PickerManualEditorWindow
   {

   #region 내부 데이터

      // ============================================================
      /// <summary>
      /// 수동 확인용 picker entry.
      /// </summary>
      // ============================================================
      private sealed class Entry
      {
         public string ID;
         public string Name;
         public int Score;
         public string Code;
         public string Status;
         public string Source;
         public string Desc;
         public Texture2D Thumbnail;
      }

   #endregion

   #region 필드

      private const string CanceledID = "__CANCELED__";

      private static readonly PickerSpec<Entry, string> Spec =
         PickerSpec<Entry, string>
            .Create("선택 샘플")
            .Value(entry => entry.ID)
            .Label(entry => entry.Name)
            .Desc(entry => entry.Desc)
            .Image(entry => entry.Thumbnail)
            .Tag("점수", entry => entry.Score.ToString())
            .Tag("출처", entry => entry.Source)
            .Tag("상태", entry => entry.Status)
            .DefaultPreviewTags("요약", "원본", "참조", "검증")
            .Column("이름", entry => entry.Name, 190f)
            .Column("점수", entry => entry.Score, 70f)
            .Column("코드", entry => entry.Code, 130f)
            .Column("상태", entry => entry.Status, 100f)
            .Column("출처", entry => entry.Source, 130f)
            .FilterByEntry("active", "활성", false, entry => entry.Status == "활성")
            .FilterByEntry("external", "외부", false, entry => entry.Source == "외부")
            .FilterByEntry("valid", "유효", false, entry => entry.Status != "잠김")
            .Build();

      private PickerWindow window = null;
      private string selectedID = null;

   #endregion

   #region 헬퍼

      // ------------------------------------------------------------
      /// <summary>
      /// 샘플 entry 목록을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      private static IReadOnlyList<Entry> CreateEntries()
      {
         return new[]
         {
            CreateEntry("항목-001", "ALPHA", 18, "코드-1001", "잠김", "데이터", "ALPHA 항목의 예시 미리보기입니다."),
            CreateEntry("항목-002", "BETA", 20, "코드-1012", "활성", "외부", "BETA 항목의 예시 미리보기입니다."),
            CreateEntry("항목-003", "GAMMA", 22, "코드-1023", "활성", "사전", "GAMMA 항목의 예시 미리보기입니다."),
            CreateEntry("항목-004", "DELTA", 24, "코드-1024", "활성", "외부", CreateLongDesc()),
            CreateEntry("항목-005", "EPSILON", 16, "코드-1025", "잠김", "데이터", "EPSILON 항목의 예시 미리보기입니다."),
            CreateEntry("항목-006", "ZETA", 21, "코드-1026", "활성", "사전", "ZETA 항목의 예시 미리보기입니다."),
            CreateEntry("항목-007", "ETA", 28, "코드-1027", "활성", "외부", "ETA 항목의 예시 미리보기입니다."),
            CreateEntry("항목-008", "THETA", 14, "코드-1028", "잠김", "데이터", "THETA 항목의 예시 미리보기입니다."),
            CreateEntry("항목-009", "IOTA", 19, "코드-1029", "활성", "사전", "IOTA 항목의 예시 미리보기입니다."),
         };
      }

      // ------------------------------------------------------------
      /// <summary>
      /// ListPicker 수동 확인용 문자열 목록을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      private static IReadOnlyList<string> CreateListEntries()
      {
         return new[]
         {
            "ALPHA",
            "BETA",
            "GAMMA",
            "DELTA",
            "EPSILON",
            "ZETA",
            "ETA",
            "THETA",
            "IOTA",
         };
      }

      // ------------------------------------------------------------
      /// <summary>
      /// DictionaryPicker 수동 확인용 dictionary를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      private static IReadOnlyDictionary<string, int> CreateDictionaryEntries()
      {
         return new Dictionary<string, int>
         {
            { "ALPHA", 18 },
            { "BETA", 20 },
            { "GAMMA", 22 },
            { "DELTA", 24 },
            { "EPSILON", 16 },
            { "ZETA", 21 },
            { "ETA", 28 },
            { "THETA", 14 },
            { "IOTA", 19 },
         };
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 설명 스크롤 동작을 눈으로 확인할 수 있는 긴 설명을 만든다.
      /// </summary>
      // ------------------------------------------------------------
      private static string CreateLongDesc()
      {
         return "DELTA 항목은 설명 스크롤 테스트를 위한 긴 데이터입니다.\n" +
                "첫 번째 줄은 일반 요약, 두 번째 줄은 원본 경로나 외부 API 응답 요약을 가정합니다.\n" +
                "세 번째 줄부터는 preview desc 영역 안에서만 세로 스크롤되어야 합니다.\n" +
                "테이블, 태그, 선택 버튼, footer 영역은 설명이 길어져도 밀려 내려가면 안 됩니다.\n" +
                "마지막 줄이 보이면 desc ScrollView의 스크롤 범위가 정상입니다.";
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 샘플 entry를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      private static Entry CreateEntry
      (
         string id,
         string name,
         int score,
         string code,
         string status,
         string source,
         string desc
      )
      {
         return new Entry
         {
            ID = id,
            Name = name,
            Score = score,
            Code = code,
            Status = status,
            Source = source,
            Desc = desc,
            Thumbnail = null,
         };
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 선택 완료 callback이 호출될 때까지 에디터 업데이트를 넘긴다.
      /// </summary>
      // ------------------------------------------------------------
      private IEnumerator WaitForSelectionComplete()
      {
         while (string.IsNullOrEmpty(selectedID))
         {
            yield return null;
         }

         if (selectedID == CanceledID)
         {
            Assert.Fail("선택 없이 Picker를 닫아 취소 처리되었습니다.");
         }

         Assert.IsFalse(string.IsNullOrEmpty(selectedID), "항목 선택 완료 시 selectedID가 설정되어야 합니다.");
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 선택 없이 닫힌 picker를 수동 테스트 실패로 기록한다.
      /// </summary>
      // ------------------------------------------------------------
      private void MarkCanceled()
      {
         selectedID = CanceledID;
      }

   #endregion

   #region 픽스처

      // ------------------------------------------------------------
      /// <summary>
      /// 열려 있는 수동 테스트 창을 정리한다.
      /// </summary>
      // ------------------------------------------------------------
      [TearDown]
      public void TearDown()
      {
         if (window != null)
         {
            window.Close();
            window = null;
         }
      }

   #endregion

   #region M-1: Picker 수동 확인

      // ----------------------------------------------------------------------
      /// <summary>
      /// Picker EditorWindow 선택 UI를 직접 확인한다.
      /// </summary>
      // ----------------------------------------------------------------------
      [Explicit]
      [Category("Manual")]
      [UnityEngine.TestTools.UnityTest]
      public IEnumerator TEST_PickerManualEditorWindow_선택UI_수동확인()
      {
         window = PickerWindow.Show
         (
            Spec,
            CreateEntries(),
            currentValue: null,
            onSelected: value => selectedID = value,
            onCanceled: MarkCanceled
         );

         yield return WaitForSelectionComplete();
      }

      // ----------------------------------------------------------------------
      /// <summary>
      /// ListPicker EditorWindow 선택 UI를 직접 확인한다.
      /// </summary>
      // ----------------------------------------------------------------------
      [Explicit]
      [Category("Manual")]
      [UnityEngine.TestTools.UnityTest]
      public IEnumerator TEST_PickerManualEditorWindow_ListPicker_수동확인()
      {
         window = PickerWindow.ShowList
         (
            "List 선택 샘플",
            CreateListEntries(),
            currentValue: null,
            onSelected: value => selectedID = value,
            onCanceled: MarkCanceled
         );

         yield return WaitForSelectionComplete();
      }

      // ----------------------------------------------------------------------
      /// <summary>
      /// DictionaryPicker EditorWindow 선택 UI를 직접 확인한다.
      /// </summary>
      // ----------------------------------------------------------------------
      [Explicit]
      [Category("Manual")]
      [UnityEngine.TestTools.UnityTest]
      public IEnumerator TEST_PickerManualEditorWindow_DictionaryPicker_수동확인()
      {
         window = PickerWindow.ShowDictionary
         (
            "Dictionary 선택 샘플",
            CreateDictionaryEntries(),
            currentKey: null,
            onSelected: value => selectedID = value,
            onCanceled: MarkCanceled
         );

         yield return WaitForSelectionComplete();
      }

   #endregion

   }
}
