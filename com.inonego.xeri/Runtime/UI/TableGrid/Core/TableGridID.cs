/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridID.cs
수정일 : 2026-11-22

# 설명
TableGrid의 행·열 ID 검사를 한 곳에서 한다(행, 셀 주소, 선택, 요청이 같은 규칙과 문구를 쓴다).

# 특이사항, 제약사항
ID는 비어 있거나 공백뿐일 수 없다. 같은 ID인지는 서수(Ordinal) 비교다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 행·열 ID 검사.
    /// </summary>
    // ============================================================
    internal static class TableGridID
    {

    #region 검사

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 비어 있거나 공백뿐인 ID를 거부하고 그대로 돌려준다.
        /// <br/> subject는 문구의 대상 이름이다(행, 열, 대상 행 등).
        /// </summary>
        // ------------------------------------------------------------
        public static string Require(string id, string parameterName, string subject)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException($"{subject} ID는 비어 있거나 공백일 수 없습니다.", parameterName);
            }

            return id;
        }

    #endregion

    }
}
