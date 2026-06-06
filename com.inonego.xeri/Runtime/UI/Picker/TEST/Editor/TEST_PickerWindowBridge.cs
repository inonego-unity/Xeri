/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PickerWindowBridge.cs
수정일 : 2026-06-07

# 설명
PickerWindowBridge의 generic 인자 전달과 close/cancel callback wiring 테스트.

# 테스트 구성
 B: Bridge
========================================================================= BLOCK_HEADER_END */

using NUnit;
using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.Editor;
using inonego.Xeri.Editor.Picker;
using inonego.Xeri.UI;
using inonego.Xeri.UI.Picker;

namespace inonego.Xeri.TEST.UI._Picker
{
   // ============================================================
   /// <summary>
   /// PickerWindowBridge 테스트 클래스.
   /// </summary>
   // ============================================================
   public class TEST_PickerWindowBridge
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

   #region B-1: Bridge

      // ------------------------------------------------------------
      /// <summary>
      /// session 확정 선택은 선택 callback과 close callback을 발생시킨다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_PickerWindowBridge_CreateSession_Confirm_CloseCallback_호출()
      {
         var selectedValue = string.Empty;
         var closeCount = 0;
         var cancelCount = 0;
         var spec = PickerSpec<Entry, string>
            .Create("항목 선택")
            .Value(entry => entry.ID)
            .Label(entry => entry.Name)
            .Build();

         var entries = new[]
         {
            new Entry { ID = "1", Name = "ALPHA" },
         };

         var bridge = new PickerWindowBridge<Entry, string>
         (
            spec,
            entries,
            "1",
            value => selectedValue = value,
            () => cancelCount++,
            8
         );
         var session = bridge.CreateSession(() => closeCount++);

         session.ConfirmCurrent();

         Assert.AreEqual("1", selectedValue);
         Assert.AreEqual(1, closeCount);
         Assert.AreEqual(0, cancelCount);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 확정 선택 없이 닫힌 bridge는 취소 callback을 한 번 발생시킨다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_PickerWindowBridge_CancelIfNotCompleted_CancelCallback_호출()
      {
         var cancelCount = 0;
         var spec = PickerSpec<Entry, string>
            .Create("항목 선택")
            .Value(entry => entry.ID)
            .Label(entry => entry.Name)
            .Build();

         var entries = new[]
         {
            new Entry { ID = "1", Name = "ALPHA" },
         };

         var bridge = new PickerWindowBridge<Entry, string>
         (
            spec,
            entries,
            "1",
            _ => { },
            () => cancelCount++,
            8
         );

         bridge.CancelIfNotCompleted();
         bridge.CancelIfNotCompleted();

         Assert.AreEqual(1, cancelCount);
      }

   #endregion

   }
}
