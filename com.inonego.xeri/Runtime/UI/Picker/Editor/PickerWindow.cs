/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerWindow.cs
수정일 : 2026-06-07

# 설명
PickerView를 Unity Editor 모달 창으로 표시하는 shell.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.Picker;

namespace inonego.Xeri.Editor.Picker
{
   // ============================================================
   /// <summary>
   /// PickerView를 Unity Editor 모달 창으로 표시하는 shell.
   /// </summary>
   // ============================================================
   public sealed class PickerWindow : EditorWindow
   {

   #region 필드

      private const int DefaultPageSize = 8;
      private IPickerWindowBridge bridge = null;

   #endregion

   #region 생성

   #region Show

      // ------------------------------------------------------------
      /// <summary>
      /// Picker modal window를 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerWindow Show<TEntry, TValue>
      (
         PickerSpec<TEntry, TValue> spec,
         IReadOnlyList<TEntry> entries,
         TValue currentValue,
         Action<TValue> onSelected,
         int pageSize = DefaultPageSize
      )
      {
         return Show(spec, entries, currentValue, onSelected, null, pageSize);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 취소 callback을 포함해 Picker modal window를 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerWindow Show<TEntry, TValue>
      (
         PickerSpec<TEntry, TValue> spec,
         IReadOnlyList<TEntry> entries,
         TValue currentValue,
         Action<TValue> onSelected,
         Action onCanceled,
         int pageSize = DefaultPageSize
      )
      {
         var window = CreateInstance<PickerWindow>();
         window.bridge = new PickerWindowBridge<TEntry, TValue>
         (
            spec,
            entries,
            currentValue,
            onSelected,
            onCanceled,
            pageSize
         );
         window.titleContent = new GUIContent(window.bridge.Title);
         window.minSize = new Vector2(640f, 560f);
         window.position = new Rect(100f, 100f, 680f, 620f);
         window.ShowModalUtility();

         return window;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 선택값 없이 Picker modal window를 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerWindow Show<TEntry, TValue>
      (
         PickerSpec<TEntry, TValue> spec,
         IReadOnlyList<TEntry> entries,
         Action<TValue> onSelected,
         int pageSize = DefaultPageSize
      )
      {
         return Show
         (
            spec,
            entries,
            default,
            onSelected,
            pageSize
         );
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 선택값 없이 취소 callback을 포함해 Picker modal window를 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerWindow Show<TEntry, TValue>
      (
         PickerSpec<TEntry, TValue> spec,
         IReadOnlyList<TEntry> entries,
         Action<TValue> onSelected,
         Action onCanceled,
         int pageSize = DefaultPageSize
      )
      {
         return Show
         (
            spec,
            entries,
            default,
            onSelected,
            onCanceled,
            pageSize
         );
      }

   #endregion

   #region ShowList

      // ------------------------------------------------------------
      /// <summary>
      /// list entry 자체를 선택값으로 반환하는 modal window를 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerWindow ShowList<TEntry>
      (
         string title,
         IReadOnlyList<TEntry> entries,
         TEntry currentValue,
         Action<TEntry> onSelected,
         int pageSize = DefaultPageSize
      )
      {
         return ShowList(title, entries, currentValue, onSelected, null, pageSize);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 취소 callback을 포함해 list entry 자체를 선택값으로 반환하는 modal window를 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerWindow ShowList<TEntry>
      (
         string title,
         IReadOnlyList<TEntry> entries,
         TEntry currentValue,
         Action<TEntry> onSelected,
         Action onCanceled,
         int pageSize = DefaultPageSize
      )
      {
         var spec = ListPicker.Spec<TEntry>(title).Build();

         return Show(spec, entries, currentValue, onSelected, onCanceled, pageSize);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 선택값 없이 list picker modal window를 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerWindow ShowList<TEntry>
      (
         string title,
         IReadOnlyList<TEntry> entries,
         Action<TEntry> onSelected,
         int pageSize = DefaultPageSize
      )
      {
         return ShowList(title, entries, default, onSelected, pageSize);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 선택값 없이 취소 callback을 포함해 list picker modal window를 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerWindow ShowList<TEntry>
      (
         string title,
         IReadOnlyList<TEntry> entries,
         Action<TEntry> onSelected,
         Action onCanceled,
         int pageSize = DefaultPageSize
      )
      {
         return ShowList(title, entries, default, onSelected, onCanceled, pageSize);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// list entry에서 선택값을 추출하는 modal window를 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerWindow ShowList<TEntry, TValue>
      (
         string title,
         IReadOnlyList<TEntry> entries,
         Func<TEntry, TValue> valueGetter,
         TValue currentValue,
         Action<TValue> onSelected,
         int pageSize = DefaultPageSize
      )
      {
         return ShowList(title, entries, valueGetter, currentValue, onSelected, null, pageSize);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 취소 callback을 포함해 list entry에서 선택값을 추출하는 modal window를 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerWindow ShowList<TEntry, TValue>
      (
         string title,
         IReadOnlyList<TEntry> entries,
         Func<TEntry, TValue> valueGetter,
         TValue currentValue,
         Action<TValue> onSelected,
         Action onCanceled,
         int pageSize = DefaultPageSize
      )
      {
         var spec = ListPicker.Spec(title, valueGetter).Build();

         return Show(spec, entries, currentValue, onSelected, onCanceled, pageSize);
      }

   #endregion

   #region ShowDictionary

      // ------------------------------------------------------------
      /// <summary>
      /// dictionary picker modal window를 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerWindow ShowDictionary<TKey, TValue>
      (
         string title,
         IReadOnlyDictionary<TKey, TValue> dictionary,
         TKey currentKey,
         Action<TKey> onSelected,
         int pageSize = DefaultPageSize
      )
      {
         return ShowDictionary(title, dictionary, currentKey, onSelected, null, pageSize);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 취소 callback을 포함해 dictionary picker modal window를 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerWindow ShowDictionary<TKey, TValue>
      (
         string title,
         IReadOnlyDictionary<TKey, TValue> dictionary,
         TKey currentKey,
         Action<TKey> onSelected,
         Action onCanceled,
         int pageSize = DefaultPageSize
      )
      {
         var spec    = DictionaryPicker.Spec<TKey, TValue>(title).Build();
         var entries = DictionaryPicker.Entries(dictionary);

         return Show(spec, entries, currentKey, onSelected, onCanceled, pageSize);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 선택값 없이 dictionary picker modal window를 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerWindow ShowDictionary<TKey, TValue>
      (
         string title,
         IReadOnlyDictionary<TKey, TValue> dictionary,
         Action<TKey> onSelected,
         int pageSize = DefaultPageSize
      )
      {
         return ShowDictionary(title, dictionary, default, onSelected, pageSize);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 선택값 없이 취소 callback을 포함해 dictionary picker modal window를 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerWindow ShowDictionary<TKey, TValue>
      (
         string title,
         IReadOnlyDictionary<TKey, TValue> dictionary,
         Action<TKey> onSelected,
         Action onCanceled,
         int pageSize = DefaultPageSize
      )
      {
         return ShowDictionary(title, dictionary, default, onSelected, onCanceled, pageSize);
      }

   #endregion

   #endregion

   #region Unity 이벤트

      // ------------------------------------------------------------
      /// <summary>
      /// EditorWindow UI를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      private void CreateGUI()
      {
         rootVisualElement.Clear();
         rootVisualElement.Add(bridge.CreateView(Close));
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 확정 선택 없이 닫힌 modal window를 취소로 처리한다.
      /// </summary>
      // ------------------------------------------------------------
      private void OnDestroy()
      {
         bridge?.CancelIfNotCompleted();
         bridge = null;
      }

   #endregion

   }
}
