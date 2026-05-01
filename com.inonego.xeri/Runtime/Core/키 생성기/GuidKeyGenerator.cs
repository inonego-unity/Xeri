/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GuidKeyGenerator.cs
수정일 : 2026-05-01

# 설명
GUID 기반 string 키 생성기.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// GUID 기반 string 키 생성기.
    /// </summary>
    // ============================================================
    [Serializable]
    public class GuidKeyGenerator : IKeyGenerator<string>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 새로운 GUID 문자열을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Generate() => Guid.NewGuid().ToString();
    }
}
