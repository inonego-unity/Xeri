/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridObjectCellView.cs
수정일 : 2026-11-24

# 설명
UnityEngine.Object 값 열(Inline 편집)의 셀 안 오브젝트 선택 입력칸(ObjectField) 셀 뷰다.
[TableGridInlineCellView]로 UnityEngine.Object와 그 하위 형식 열에 자동 등록한다.

# 특이사항, 제약사항
Editor 어셈블리(inonego.Xeri.Editor)에서만 컴파일된다(ObjectField가 UnityEditor.UIElements에 있다).
오브젝트 선택은 값이 바뀌는 즉시 확정을 요청한다. 선택 가능한 형식은 context.ValueType이 정하고,
UnityEngine.Object가 아닌 형식이면 UnityEngine.Object 전체에서 고른다.
씬 오브젝트 허용 여부는 ObjectField 기본값(허용)을 따른다. 값을 받는 쪽이 제한하려면 연결 뒤에 요소의 allowSceneObjects를 바꾼다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TableGrid;

namespace inonego.Xeri.UI.TableGrid.Editor
{
    // ============================================================
    /// <summary>
    /// 오브젝트 선택 입력칸 셀 뷰.
    /// </summary>
    // ============================================================
    [TableGridInlineCellView(typeof(UnityEngine.Object), UseForChildren = true)]
    internal sealed class TableGridObjectCellView : TableGridInputCellView<ObjectField, UnityEngine.Object>
    {

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 오브젝트 선택 입력칸 셀 뷰를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridObjectCellView() : base(() => new ObjectField(), true)
        {
            // NONE
        }

    #endregion

    #region 입력칸

        // ------------------------------------------------------------
        /// <summary>
        /// 고를 수 있는 오브젝트 형식을 열 값 형식에 맞추고 값을 보인다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void Show(ObjectField field, UnityEngine.Object value, in TableGridCellContext context)
        {
            var valueType = context.ValueType;
            var objectType = valueType != null && typeof(UnityEngine.Object).IsAssignableFrom(valueType)
                ? valueType
                : typeof(UnityEngine.Object);

            // 형식이 그대로이면 건드리지 않는다. 바꾸면 입력칸이 보이던 값을 다시 검사한다.
            if (field.objectType != objectType)
            {
                field.objectType = objectType;
            }

            field.SetValueWithoutNotify(value);
        }

    #endregion

    }
}
