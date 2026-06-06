/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerView.cs
수정일 : 2026-06-07

# 설명
Picker session을 표시하고 Resources 기반 UXML/USS를 적용하는 root VisualElement.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker session을 표시하는 root VisualElement.
   /// </summary>
   // ============================================================
   public sealed class PickerView<TEntry, TValue> : VisualElement
   {

   #region 필드

      private readonly PickerSession<TEntry, TValue> session;
      private readonly PickerPreviewBinder<TEntry, TValue> previewBinder;
      private readonly PickerToolbarBinder<TEntry, TValue> toolbarBinder;
      private readonly PickerTableBinder<TEntry, TValue> tableBinder;
      private readonly PickerFooterBinder<TEntry, TValue> footerBinder;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// PickerView를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerView(PickerSession<TEntry, TValue> session) : this
      (
         session,
         PickerViewResourceLoader.LoadLayout(),
         PickerViewResourceLoader.LoadThemeStyle(),
         PickerViewResourceLoader.LoadViewStyle()
      )
      {

      }

      // ------------------------------------------------------------
      /// <summary>
      /// 테스트 또는 특수 host에서 명시 asset으로 PickerView를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerView
      (
         PickerSession<TEntry, TValue> session,
         VisualTreeAsset layout,
         StyleSheet themeStyle,
         StyleSheet viewStyle
      ) : base()
      {
         this.session = session ?? throw new ArgumentNullException(nameof(session));

         focusable = true;
         AddToClassList("xeri-picker-host");
         if (layout == null)
         {
            throw new ArgumentNullException(nameof(layout));
         }

         layout.CloneTree(this);

         if (themeStyle != null)
         {
            styleSheets.Add(themeStyle);
         }

         if (viewStyle != null)
         {
            styleSheets.Add(viewStyle);
         }

         ApplyPreviewVisibility();

         if (session.ShowPreview)
         {
            previewBinder = new PickerPreviewBinder<TEntry, TValue>(session, this);
         }

         toolbarBinder = new PickerToolbarBinder<TEntry, TValue>(session, this);
         tableBinder   = new PickerTableBinder<TEntry, TValue>(session, this);
         footerBinder  = new PickerFooterBinder<TEntry, TValue>(session, this);

         session.Changed += HandleSessionChanged;
         RegisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);
         RegisterCallback<AttachToPanelEvent>(HandleAttached);
         RegisterCallback<DetachFromPanelEvent>(HandleDetached);

         Refresh();
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// 모든 binder 표시 상태를 갱신한다.
      /// </summary>
      // ------------------------------------------------------------
      private void Refresh()
      {
         previewBinder?.Refresh();
         toolbarBinder?.Refresh();
         tableBinder?.Refresh();
         footerBinder?.Refresh();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// spec 옵션에 따라 preview pane을 layout에서 제외한다.
      /// </summary>
      // ------------------------------------------------------------
      private void ApplyPreviewVisibility()
      {
         var previewPane = this.Q<VisualElement>("preview-pane");
         if (previewPane == null) return;

         previewPane.style.display = session.ShowPreview ? DisplayStyle.Flex : DisplayStyle.None;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// session 변경을 UI에 반영한다.
      /// </summary>
      // ------------------------------------------------------------
      private void HandleSessionChanged(object sender, EventArgs e)
      {
         Refresh();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// keyboard shortcut을 session 명령으로 연결한다.
      /// </summary>
      // ------------------------------------------------------------
      private void HandleKeyDown(KeyDownEvent evt)
      {
         if (evt.keyCode == KeyCode.Escape)
         {
            session.ClearCurrentEntry();
            StopShortcutEvent(evt);
         }
         else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
         {
            session.ConfirmCurrent();
            StopShortcutEvent(evt);
         }
         else if (evt.keyCode == KeyCode.UpArrow)
         {
            StopShortcutEvent(evt);
            session.MoveSelectionPrev();
         }
         else if (evt.keyCode == KeyCode.DownArrow)
         {
            StopShortcutEvent(evt);
            session.MoveSelectionNext();
         }
         else if (evt.keyCode == KeyCode.LeftArrow && !IsTextInputEvent(evt))
         {
            session.MovePrev();
            StopShortcutEvent(evt);
         }
         else if (evt.keyCode == KeyCode.RightArrow && !IsTextInputEvent(evt))
         {
            session.MoveNext();
            StopShortcutEvent(evt);
         }
      }

      // ------------------------------------------------------------
      /// <summary>
      /// root shortcut이 소유한 key event를 내부 control 기본 처리로 넘기지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      private static void StopShortcutEvent(KeyDownEvent evt)
      {
         evt.StopPropagation();
         evt.StopImmediatePropagation();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 텍스트 입력 내부의 좌우 커서 이동은 picker shortcut으로 처리하지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      private static bool IsTextInputEvent(KeyDownEvent evt)
      {
         if (evt.target is TextField) return true;

         var element = evt.target as VisualElement;
         while (element != null)
         {
            if (element is TextField) return true;

            element = element.parent;
         }

         return false;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// picker가 열린 직후에도 keyboard shortcut을 받을 수 있도록 root focus를 확보한다.
      /// </summary>
      // ------------------------------------------------------------
      private void HandleAttached(AttachToPanelEvent evt)
      {
         schedule.Execute(Focus);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// view가 panel에서 분리될 때 session 구독을 해제한다.
      /// </summary>
      // ------------------------------------------------------------
      private void HandleDetached(DetachFromPanelEvent evt)
      {
         session.Changed -= HandleSessionChanged;
      }

   #endregion

   }
}
