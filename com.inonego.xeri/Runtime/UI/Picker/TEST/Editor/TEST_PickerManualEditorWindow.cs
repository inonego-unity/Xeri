/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PickerManualEditorWindow.cs
수정일 : 2026-08-04

# 설명
Picker EditorWindow 선택 UI를 직접 조작해 확인하는 수동 Editor 테스트.
기본 Picker의 preview overflow와 column layout, ListPicker, DictionaryPicker의 선택과 취소 처리를 직접 확인한다.

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
         public string InternalMeta;
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
            .DefaultPreviewTags("preview-name ellipsis", "column layout", "hidden column", "search policy")
            .Column
            (
               "이름",
               entry => entry.Name,
               PickerColumnOptions.Flexible(width: 220f, minWidth: 120f, overflow: PickerColumnOverflow.Ellipsis)
            )
            .Column
            (
               "점수",
               entry => entry.Score,
               PickerColumnOptions.Fixed(width: 64f, alignment: PickerColumnAlignment.Right)
            )
            .Column
            (
               "긴 코드",
               entry => entry.Code,
               PickerColumnOptions.Flexible(width: 180f, minWidth: 100f, searchable: false)
            )
            .Column
            (
               "상태",
               entry => entry.Status,
               PickerColumnOptions.Fixed(width: 84f, sortable: false)
            )
            .Column
            (
               "출처",
               entry => entry.Source,
               PickerColumnOptions.Fixed(width: 90f, alignment: PickerColumnAlignment.Center)
            )
            .Column
            (
               "internal-meta",
               "숨김 메타",
               entry => entry.InternalMeta,
               PickerColumnOptions.Fixed(width: 120f, searchable: false, visibility: PickerColumnVisibility.Hidden)
            )
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
            CreateEntry
            (
               "sample://very/long/current/value/path/that/should/stay/inside/preview-sub-label/selected-delta",
               "EXTREMELY_LONG_PREVIEW_NAME_THAT_SHOULD_NOT_PUSH_THE_SELECT_BUTTON_OR_BREAK_THE_PREVIEW_ROW_DELTA_SAMPLE",
               24,
               "CODE-1024-VERY-LONG-COLUMN-VALUE-THAT-SHOULD-STAY-INSIDE-THE-CODE-COLUMN",
               "활성",
               "외부",
               "hidden-meta-delta",
               CreateLongDesc()
            ),
            CreateEntry("항목-001", "ALPHA_SHORT_REFERENCE", 18, "코드-1001", "잠김", "데이터", "hidden-meta-alpha", "ALPHA 항목의 예시 미리보기입니다."),
            CreateEntry("항목-002", "BETA_FIXED_COLUMN_SAMPLE", 20, "코드-1012", "활성", "외부", "hidden-meta-beta", "BETA 항목의 예시 미리보기입니다."),
            CreateEntry("항목-003", "GAMMA_FLEXIBLE_COLUMN_SAMPLE", 22, "코드-1023", "활성", "사전", "hidden-meta-gamma", "GAMMA 항목의 예시 미리보기입니다."),
            CreateEntry("항목-005", "EPSILON_LOCKED_SAMPLE", 16, "코드-1025", "잠김", "데이터", "hidden-meta-epsilon", "EPSILON 항목의 예시 미리보기입니다."),
            CreateEntry("항목-006", "ZETA_SEARCH_SAMPLE", 21, "코드-1026", "활성", "사전", "hidden-meta-zeta", "ZETA 항목의 예시 미리보기입니다."),
            CreateEntry("항목-007", "ETA_EXTERNAL_SAMPLE", 28, "코드-1027", "활성", "외부", "hidden-meta-eta", "ETA 항목의 예시 미리보기입니다."),
            CreateEntry("항목-008", "THETA_DISABLED_SAMPLE", 14, "코드-1028", "잠김", "데이터", "hidden-meta-theta", "THETA 항목의 예시 미리보기입니다."),
            CreateEntry("항목-009", "IOTA_PAGE_SAMPLE", 19, "코드-1029", "활성", "사전", "hidden-meta-iota", "IOTA 항목의 예시 미리보기입니다."),
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
         return "이 항목은 preview name ellipsis 검증용 데이터입니다.\n" +
                "두 번째 줄은 desc 영역이 여러 줄을 표시하는지 확인합니다.\n" +
                "세 번째 줄은 선택 버튼과 table 영역이 밀리지 않는지 확인합니다.\n" +
                "네 번째 줄은 desc ScrollView가 자기 영역 안에서만 처리되는지 확인합니다.\n" +
                "다섯 번째 줄은 좁은 창에서도 레이아웃이 유지되는지 확인합니다.";
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
         string internalMeta,
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
            InternalMeta = internalMeta,
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
         window = Picker.Show
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
         window = Picker.ShowList
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
         window = Picker.ShowDictionary
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
