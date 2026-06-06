/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerSelectionEventArgs.cs
수정일 : 2026-06-06

# 설명
Picker 선택 확정 이벤트에 전달할 entry, value 정보.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker 선택 확정 이벤트 인자.
   /// </summary>
   // ============================================================
   [Serializable]
   public sealed class PickerSelectionEventArgs<TEntry, TValue> : EventArgs
   {

   #region 필드

      // ------------------------------------------------------------
      /// <summary>
      /// 선택된 picker entry.
      /// </summary>
      // ------------------------------------------------------------
      public readonly PickerEntry<TEntry, TValue> Entry;

      // ------------------------------------------------------------
      /// <summary>
      /// 선택된 값.
      /// </summary>
      // ------------------------------------------------------------
      public readonly TValue Value;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// 선택 이벤트 인자를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSelectionEventArgs(PickerEntry<TEntry, TValue> entry) : base()
      {
         Entry = entry;
         Value = entry == null ? default : entry.Value;
      }

   #endregion

   }
}
