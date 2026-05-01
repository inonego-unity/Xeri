/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Pool.cs
수정일 : 2026-05-01

# 설명
UnityEngine.Object가 아닌 일반 클래스에 사용 가능한 풀.
new() 제약이 있는 참조 타입에 대해 new T()로 오브젝트를 생성한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace inonego.Xeri.Pool
{

    // ===================================================================
    /// <summary>
    /// UnityEngine.Object가 아닌 일반적인 클래스에 사용 가능한 풀입니다.
    /// </summary>
    // ===================================================================
    [Serializable]
    public class Pool<T> : PoolBase<T> where T : class, new()
    {

    #region 내부 구현용 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 새로운 오브젝트를 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected override T AcquireNew()
        {
            return new T();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 새로운 오브젝트를 비동기로 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected override async Awaitable<T> AcquireNewAsync()
        {
            return await Task.FromResult(new T());
        }

    #endregion

    }
}
