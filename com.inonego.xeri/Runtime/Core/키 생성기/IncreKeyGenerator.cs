/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IncreKeyGenerator.cs
수정일 : 2026-05-01

# 설명
단조 증가 ulong 키 생성기. Generate() 호출마다 1씩 증가한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 단조 증가 ulong 키 생성기.
    /// </summary>
    // ============================================================
    [Serializable]
    public class IncreKeyGenerator : IKeyGenerator<ulong>
    {

    #region 필드

        [SerializeField]
        private ulong current = 0;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 값을 반환하고 1 증가시킨다.
        /// </summary>
        // ------------------------------------------------------------
        public ulong Generate() => current++;

    #endregion

    }
}
