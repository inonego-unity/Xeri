/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridBoxing.cs
수정일 : 2026-11-22

# 설명
셀 뷰·편집기·값 규칙의 박싱 경로(object)에서 형식 있는 값을 꺼내는 공용 규칙.

# 특이사항, 제약사항
null은 형식의 기본값으로 꺼낸다(값이 없는 셀).
다른 형식의 값은 열 값 형식과 셀 규칙이 어긋난 구성 오류이므로 조용히 기본값으로 바꾸지 않고 예외를 던진다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 박싱 경로 값 꺼내기.
    /// </summary>
    // ============================================================
    internal static class TableGridBoxing
    {

    #region 꺼내기

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 박싱된 값을 T로 꺼낸다. null이면 T의 기본값이다.
        /// <br/> T가 아닌 값이면 예외를 던진다.
        /// </summary>
        // ------------------------------------------------------------
        public static T Unbox<T>(object value)
        {
            if (value is T typed)
            {
                return typed;
            }

            if (value == null)
            {
                return default;
            }

            throw new InvalidOperationException
            (
                $"셀 값 형식이 맞지 않습니다: {value.GetType().Name} 값을 {typeof(T).Name}로 받을 수 없습니다."
            );
        }

    #endregion

    }
}
