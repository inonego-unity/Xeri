/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridPromptPanel.cs
수정일 : 2026-11-23

# 설명
입력칸 하나짜리 팝오버 화면 요소(빠른 이동 Ctrl+G, 위치로 이동). 입력하고 Enter 또는 버튼으로 실행한다.
실행하지 못하면 창을 닫지 않고 아래에 이유를 보이고, 입력을 고치면 처음 안내로 되돌린다.

# 특이사항, 제약사항
입력 해석과 이동은 호출자가 넘긴 함수가 한다. 함수는 성공하면 null, 실패하면 보일 문구를 돌려준다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 입력칸 하나짜리 팝오버 화면 요소.
    /// </summary>
    // ============================================================
    internal sealed class TableGridPromptPanel : TableGridPopoverPanel
    {

    #region 필드

        private TextField field;
        private Label message;
        private string hint;
        private Func<string, string> submit;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 입력을 처리했을 때(submit이 성공했을 때) 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action Submitted;

    #endregion

    #region 생성과 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 입력 팝오버 요소를 만든다. 내용은 Build로 채운다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridPromptPanel()
        {
            // NONE
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 제목·단축키·안내를 지정해 입력칸 하나짜리 창을 만든다(빠른 이동, 위치로 이동).
        /// <br/> submit은 입력을 받아 처리하고 실패하면 보일 문구를 돌려준다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void Build
        (
            string title, string shortcut, string placeholder, string hint, Func<string, string> submit
        )
        {
            this.submit = submit ?? throw new ArgumentNullException(nameof(submit));
            this.hint = hint;
            ResetContent();
            AddHeader(title, shortcut);

            var row = new VisualElement();
            row.AddToClassList("xeri-table-popover__range");
            field = CreateTextField(string.Empty, placeholder, Submit);
            field.RegisterValueChangedCallback(HandleFieldChanged);
            row.Add(field);

            var go = CreateAction("이동", null, false, Submit);
            go.AddToClassList("xeri-table-popover__button");
            go.AddToClassList("xeri-table-popover__button--primary");
            go.style.flexShrink = 0f;
            row.Add(go);
            AddSized(row, TableGridPopoverMetrics.FIELD_ROW_HEIGHT);

            message = new Label(hint);
            message.AddToClassList("xeri-table-popover__hint");
            AddSized(message, TableGridPopoverMetrics.HINT_HEIGHT);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 입력칸에 포커스를 주고 글자를 모두 고른다.
        /// </summary>
        // ------------------------------------------------------------
        public override void FocusFirst()
        {
            field.Focus();
            field.SelectAll();
        }

    #endregion

    #region 실행

        private void Submit()
        {
            var error = submit(field.value.Trim());
            if (error == null)
            {
                Submitted?.Invoke();
                return;
            }

            message.text = error;
            message.AddToClassList("xeri-table-popover__hint--error");
        }

        // 입력을 고치면 오류 문구를 지우고 처음 안내로 되돌린다.
        private void HandleFieldChanged(ChangeEvent<string> evt)
        {
            message.text = hint;
            message.RemoveFromClassList("xeri-table-popover__hint--error");
        }

    #endregion

    }
}
