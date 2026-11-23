/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridInputCellView.cs
수정일 : 2026-11-24

# 설명
Inline 편집 열의 셀 안 입력칸(값 변경을 알리는 UI Toolkit 요소: INotifyValueChanged)을 그리는 셀 뷰다.
입력칸을 만들고, 행이 바뀌면 값을 알림 없이 보이고, 사용자가 값을 바꾸면 셀 호스트(context.Host)로
Replace를 요청한다. 값 변경이 곧 확정인 입력칸(bool·enum·오브젝트 선택 등)은 Replace 뒤에 Commit도 요청한다.
입력칸 종류는 만들기 함수가 정하고, 값을 보이는 방식이 다르면 Show를 재정의한다.
글자·숫자 입력칸은 Inline(Immediate) 열이면 입력을 마칠 때(Enter·포커스 이탈)만 요청하고,
Inline(Confirm) 열이면 입력마다 보류 값을 갱신한다(확정은 표가 한다).

# 특이사항, 제약사항
셀 뷰는 상태를 두지 않으므로 셀 호스트는 입력칸 요소의 userData에 둔다. 하위 형식은 userData를 쓰지 않는다.
고칠 수 없는 셀(context.CanEdit가 false)은 입력칸을 비활성으로 보인다.
Escape로 보류 값을 버리면 표가 같은 값으로 다시 연결하므로(Bind) 입력칸은 원래 값으로 돌아온다.
기본 입력 셀 뷰(string·int·long·float·double·bool·enum)는 등록소가 미리 담고 있다(TableGridDefaultInputCellViews).
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
    /// Inline 열의 셀 안 입력칸을 그리는 셀 뷰.
    /// </summary>
    // ============================================================
    public class TableGridInputCellView<TField, TValue> : TableGridCellView<TValue>
    where TField : VisualElement, INotifyValueChanged<TValue>
    {

    #region 내부 타입

        // ============================================================
        /// <summary>
        /// 입력칸 요소가 가진 현재 셀 호스트.
        /// </summary>
        // ============================================================
        private sealed class HostRef
        {
            public ITableGridCellHost Host;
        }

    #endregion

    #region 필드

        private readonly Func<TField> create;
        private readonly bool commitOnChange;

    #endregion

    #region 생성자

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 뷰를 만든다. create는 입력칸을 만든다.
        /// <br/> commitOnChange이면 값이 바뀔 때마다 바로 확정을 요청한다(고르는 입력칸).
        /// <br/> 아니면 Inline 열의 반영 시점(Immediate·Confirm)을 따른다(글자·숫자 입력칸).
        /// </summary>
        // --------------------------------------------------------------------------------
        public TableGridInputCellView(Func<TField> create, bool commitOnChange)
        {
            this.create = create ?? throw new ArgumentNullException(nameof(create));
            this.commitOnChange = commitOnChange;
        }

    #endregion

    #region 입력칸

        // ------------------------------------------------------------
        /// <summary>
        /// 입력칸을 만들고 값 변경을 셀 호스트로 잇는다.
        /// </summary>
        // ------------------------------------------------------------
        public sealed override VisualElement MakeCell()
        {
            var field = create();
            var hostRef = new HostRef();
            field.userData = hostRef;
            field.RegisterValueChangedCallback<TValue>
            (
                evt =>
                {
                    var host = hostRef.Host;
                    if (host == null)
                    {
                        return;
                    }

                    host.Replace(evt.newValue);
                    if (commitOnChange)
                    {
                        host.Commit();
                    }
                }
            );

            return field;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 입력칸에 값을 알림 없이 보이고 셀 호스트를 잇는다.
        /// </summary>
        // ------------------------------------------------------------
        public sealed override void Bind(VisualElement element, TValue value, in TableGridCellContext context)
        {
            // 값을 보이는 동안 일어난 변경 알림이 셀 호스트로 새지 않게 호스트는 보인 뒤에 잇는다.
            var field = (TField)element;
            var hostRef = (HostRef)field.userData;
            hostRef.Host = null;
            field.SetEnabled(context.CanEdit);

            // 즉시 반영 열은 입력을 마칠 때 한 번만 요청하고, 확정 열은 입력마다 보류 값을 갱신한다.
            if (field is TextInputBaseField<TValue> text)
            {
                text.textEdition.isDelayed = context.Column?.Edit.InlineCommit == TableGridInlineCommit.Immediate;
            }

            Show(field, value, in context);
            hostRef.Host = context.Host;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 호스트 연결을 끊는다.
        /// </summary>
        // ------------------------------------------------------------
        public sealed override void Unbind(VisualElement element)
        {
            ((HostRef)element.userData).Host = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 값을 알림 없이 입력칸에 보인다. 입력칸이 값 형식 정보를 필요로 하면
        /// <br/> (enum 형식, 오브젝트 형식) 재정의해서 context.ValueType으로 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void Show(TField field, TValue value, in TableGridCellContext context)
        {
            field.SetValueWithoutNotify(value);
        }

    #endregion

    }
}
