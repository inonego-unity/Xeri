/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridCellEditor.cs
수정일 : 2026-11-23

# 설명
Editor 편집 방식에서 쓰는 셀 편집기 계약과 기본 입력칸 편집기를 정의한다.
표는 Begin이 만든 요소를 편집기 자리에 놓고, 확정할 때 TryGetValue로 값을 읽어 값 규칙으로 검증한 뒤
EditRequested로 요청한다. 값을 읽지 못하거나 검증에 실패하면 편집기를 열어 둔 채 오류를 보인다.

# 특이사항, 제약사항
편집기는 상태를 두지 않는다(열의 모든 셀이 공유한다). 편집 중 상태는 Begin이 만든 요소에 둔다.
편집기 위치(Placement)는 편집기가 정한다. 지금 Popup은 InCell과 같이 셀 안에 놓는다.
비제네릭 기반의 박싱 경로(internal)는 편집 경계에서만 쓴다.
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
    /// 등록소·열이 다루는 셀 편집기의 비제네릭 면.
    /// </summary>
    // ============================================================
    public abstract class TableGridCellEditor
    {

    #region 편집기

        // ------------------------------------------------------------
        /// <summary>
        /// 편집기가 다루는 값 형식.
        /// </summary>
        // ------------------------------------------------------------
        public abstract Type ValueType { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집기를 놓을 자리. 기본은 셀 안이다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual TableGridEditorPlacement Placement => TableGridEditorPlacement.InCell;

    #endregion

    #region 박싱 경로

        internal abstract VisualElement BeginBoxed(object value, in TableGridCellContext context, string initialText);

        internal abstract bool TryGetValueBoxed(VisualElement editor, out object value, out string error);

        internal abstract void EndBoxed(VisualElement editor);

    #endregion

    }

    // ============================================================
    /// <summary>
    /// TValue 값을 고치는 셀 편집기.
    /// </summary>
    // ============================================================
    public abstract class TableGridCellEditor<TValue> : TableGridCellEditor
    {

    #region 편집기

        // ------------------------------------------------------------
        /// <summary>
        /// 편집기가 다루는 값 형식(TValue).
        /// </summary>
        // ------------------------------------------------------------
        public sealed override Type ValueType => typeof(TValue);

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 편집기 요소를 만든다. initialText는 글자를 쳐서 편집을 시작했을 때 그 글자이고,
        /// <br/> Enter·더블클릭으로 시작했으면 null이다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public abstract VisualElement Begin(TValue value, in TableGridCellContext context, string initialText);

        // ------------------------------------------------------------
        /// <summary>
        /// 편집기에서 값을 읽는다. 읽지 못하면 false와 이유를 돌려준다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract bool TryGetValue(VisualElement editor, out TValue value, out string error);

        // ------------------------------------------------------------
        /// <summary>
        /// 편집이 끝나 편집기 요소를 뗄 때 부른다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void End(VisualElement editor)
        {
            // NONE
        }

    #endregion

    #region 박싱 경로

        internal sealed override VisualElement BeginBoxed(object value, in TableGridCellContext context, string initialText)
        {
            return Begin(TableGridBoxing.Unbox<TValue>(value), in context, initialText);
        }

        internal sealed override bool TryGetValueBoxed(VisualElement editor, out object value, out string error)
        {
            var read = TryGetValue(editor, out var typed, out error);
            value = typed;
            return read;
        }

        internal sealed override void EndBoxed(VisualElement editor)
        {
            End(editor);
        }

    #endregion

    }

    // ======================================================================
    /// <summary>
    /// <br/> 값 규칙의 글자로 시작하고 TryParse로 읽는 기본 입력칸 편집기.
    /// <br/> 글자를 읽을 수 있는 값 규칙의 Editor 열에 쓴다.
    /// </summary>
    // ======================================================================
    internal sealed class TableGridTextCellEditor<TValue> : TableGridCellEditor<TValue>
    {

    #region 필드

        private readonly TableGridValueFormat<TValue> format;

    #endregion

    #region 편집기

        // ------------------------------------------------------------
        /// <summary>
        /// 값 규칙으로 입력칸 편집기를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridTextCellEditor(TableGridValueFormat<TValue> format)
        {
            this.format = format ?? throw new ArgumentNullException(nameof(format));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 입력칸을 값 글자(또는 입력한 글자)로 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement Begin(TValue value, in TableGridCellContext context, string initialText)
        {
            return new TextField
            {
                value = initialText ?? format.ToText(value) ?? string.Empty,
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 입력칸 글자를 값 규칙으로 읽는다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool TryGetValue(VisualElement editor, out TValue value, out string error)
        {
            var text = (editor as TextField)?.value ?? string.Empty;
            return format.TryParse(text, out value, out error);
        }

    #endregion

    }
}
