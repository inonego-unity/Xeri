/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerWindowBridge.cs
수정일 : 2026-06-07

# 설명
PickerWindow.Show 인자를 non-generic EditorWindow 생명주기에 연결하기 위한 bridge.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.Picker;

namespace inonego.Xeri.Editor.Picker
{
   // ============================================================
   /// <summary>
   /// Non-generic EditorWindow가 보관할 generic picker bridge 계약.
   /// </summary>
   // ============================================================
   internal interface IPickerWindowBridge
   {
      // ------------------------------------------------------------
      /// <summary>
      /// 창 제목.
      /// </summary>
      // ------------------------------------------------------------
      public string Title { get; }

      // ------------------------------------------------------------
      /// <summary>
      /// Picker view를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public VisualElement CreateView(Action closeWindow);

      // ------------------------------------------------------------
      /// <summary>
      /// 확정 선택 없이 window가 닫힌 경우 취소 처리를 실행한다.
      /// </summary>
      // ------------------------------------------------------------
      public void CancelIfNotCompleted();
   }

   // ============================================================
   /// <summary>
   /// Generic picker 인자를 EditorWindow 생명주기에 연결한다.
   /// </summary>
   // ============================================================
   internal sealed class PickerWindowBridge<TEntry, TValue> : IPickerWindowBridge
   {

   #region 필드

      private readonly PickerSpec<TEntry, TValue> spec;
      private readonly IReadOnlyList<TEntry> entries;
      private readonly TValue currentValue;
      private readonly Action<TValue> onSelected;
      private readonly Action onCanceled;
      private readonly int pageSize;
      private bool isCompleted = false;

      // ------------------------------------------------------------
      /// <summary>
      /// 창 제목.
      /// </summary>
      // ------------------------------------------------------------
      public string Title => spec.Title;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// bridge를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerWindowBridge
      (
         PickerSpec<TEntry, TValue> spec,
         IReadOnlyList<TEntry> entries,
         TValue currentValue,
         Action<TValue> onSelected,
         Action onCanceled,
         int pageSize
      ) : base()
      {
         this.spec         = spec ?? throw new ArgumentNullException(nameof(spec));
         this.entries      = entries ?? Array.Empty<TEntry>();
         this.currentValue = currentValue;
         this.onSelected   = onSelected ?? (_ => { });
         this.onCanceled   = onCanceled ?? (() => { });
         this.pageSize     = pageSize;
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// close callback까지 연결된 session을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      internal PickerSession<TEntry, TValue> CreateSession(Action closeWindow)
      {
         var session = new PickerSession<TEntry, TValue>(spec, entries, currentValue, HandleSelected, pageSize);
         session.Confirmed += (_, _) => closeWindow?.Invoke();

         return session;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 확정 선택 callback을 한 번만 완료 상태로 기록한다.
      /// </summary>
      // ------------------------------------------------------------
      private void HandleSelected(TValue value)
      {
         isCompleted = true;
         onSelected(value);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Picker view를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public VisualElement CreateView(Action closeWindow)
      {
         return new PickerView<TEntry, TValue>(CreateSession(closeWindow));
      }

      // ------------------------------------------------------------
      /// <summary>
      /// window 닫힘이 확정 선택 없이 발생하면 취소 callback을 호출한다.
      /// </summary>
      // ------------------------------------------------------------
      public void CancelIfNotCompleted()
      {
         if (isCompleted) return;

         isCompleted = true;
         onCanceled();
      }

   #endregion

   }
}
