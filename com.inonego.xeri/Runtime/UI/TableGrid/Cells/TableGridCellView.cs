/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridCellView.cs
수정일 : 2026-11-24

# 설명
셀 값을 화면에 보이는 셀 뷰 계약을 정의한다. 셀 뷰는 기본 셀 뷰(값 규칙 글자 Label, 선택형 칩)만 대체하고
데이터를 바꾸지 않는다. 값을 받아 그리는 TableGridCellView<TValue>와,
값 없이 행 항목·문맥으로 그리는 TableGridItemCellView 둘 중 하나를 상속한다.

# 특이사항, 제약사항
셀 뷰는 상태를 두지 않는다(열의 모든 셀이 공유한다). 셀마다 다른 상태는 MakeCell이 만든 요소에 둔다.
검색·복사 글자는 셀 뷰가 아니라 값 규칙이 낸다.
Inline 편집 열의 셀 뷰는 셀 안 컨트롤을 두고 context.Host로 값 변경을 요청한다(기본 입력 셀 뷰는 TableGridInputCellView).
비제네릭 기반의 박싱 경로 BindBoxed는 기반 형식에 등록한 셀 뷰를 하위 형식 열에 쓸 때와, 호스트가 셀 뷰를 자기 셀 안에 얹을 때 쓴다.
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
    /// 등록소·열이 다루는 셀 뷰의 비제네릭 면.
    /// </summary>
    // ============================================================
    public abstract class TableGridCellView
    {

    #region 셀 뷰

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 뷰가 받는 값 형식. 값 없는 셀 뷰는 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract Type ValueType { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 내용 요소를 만든다. 셀 요소마다 한 번 부른다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract VisualElement MakeCell();

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 내용이 행을 떠날 때 연결을 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void Unbind(VisualElement element)
        {
            // NONE
        }

    #endregion

    #region 박싱 경로

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 값을 object로 받아 셀 내용에 연결한다. 값 형식을 컴파일 시점에 모르는 호출자
        /// <br/> (기반 형식에 등록한 셀 뷰를 쓰는 열, 셀 뷰를 자기 셀 안에 얹는 호스트)가 쓴다.
        /// <br/> 값 없는 셀 뷰는 값을 무시한다. 값이 셀 뷰의 값 형식이 아니면 예외를 던진다.
        /// </summary>
        // ----------------------------------------------------------------------
        public abstract void BindBoxed(VisualElement element, object value, in TableGridCellContext context);

        internal abstract float? MeasureBoxed(object value, in TableGridCellContext context);

    #endregion

    }

    // ============================================================
    /// <summary>
    /// TValue 값을 받아 그리는 셀 뷰.
    /// </summary>
    // ============================================================
    public abstract class TableGridCellView<TValue> : TableGridCellView
    {

    #region 셀 뷰

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 뷰가 받는 값 형식(TValue).
        /// </summary>
        // ------------------------------------------------------------
        public sealed override Type ValueType => typeof(TValue);

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 내용에 값을 연결한다. 행이 바뀔 때마다 부른다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract void Bind(VisualElement element, TValue value, in TableGridCellContext context);

        // ------------------------------------------------------------
        /// <summary>
        /// 열 내용 맞춤에 쓸 폭. null이면 값 규칙 글자 폭으로 잰다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual float? MeasureWidth(TValue value, in TableGridCellContext context)
        {
            return null;
        }

    #endregion

    #region 박싱 경로

        public sealed override void BindBoxed(VisualElement element, object value, in TableGridCellContext context)
        {
            Bind(element, TableGridBoxing.Unbox<TValue>(value), in context);
        }

        internal sealed override float? MeasureBoxed(object value, in TableGridCellContext context)
        {
            return MeasureWidth(TableGridBoxing.Unbox<TValue>(value), in context);
        }

    #endregion

    }

    // ================================================================================
    /// <summary>
    /// <br/> 값 없이 행 항목·문맥으로 그리는 셀 뷰.
    /// <br/> SerializedProperty Inline 셀, 장식 열처럼 열 값보다 행 항목을 직접 다룰 때 쓴다.
    /// </summary>
    // ================================================================================
    public abstract class TableGridItemCellView : TableGridCellView
    {

    #region 셀 뷰

        // ------------------------------------------------------------
        /// <summary>
        /// 값을 받지 않으므로 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public sealed override Type ValueType => null;

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 내용에 행 문맥을 연결한다. 행이 바뀔 때마다 부른다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract void Bind(VisualElement element, in TableGridCellContext context);

        // ------------------------------------------------------------
        /// <summary>
        /// 열 내용 맞춤에 쓸 폭. null이면 값 규칙 글자 폭으로 잰다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual float? MeasureWidth(in TableGridCellContext context)
        {
            return null;
        }

    #endregion

    #region 박싱 경로

        public sealed override void BindBoxed(VisualElement element, object value, in TableGridCellContext context)
        {
            Bind(element, in context);
        }

        internal sealed override float? MeasureBoxed(object value, in TableGridCellContext context)
        {
            return MeasureWidth(in context);
        }

    #endregion

    }
}
