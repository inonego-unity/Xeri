/* BLOCK_HEADER_BEGIN =======================================================================
파일명: BootstrapperModuleAsset.cs
수정일: 2026-05-20

# 설명
BootStrapper 초기화 단계에서 실행되는 ScriptableObject 기반 모듈 베이스 클래스.
모듈별 설정값을 에셋에 직렬화하고 Init에서 초기화 로직을 수행한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Bootstrapper
{
    // ============================================================
    /// <summary>
    /// BootStrapper에서 순차 실행되는 초기화 모듈 에셋.
    /// </summary>
    // ============================================================
    public abstract class BootstrapperModuleAsset : ScriptableObject
    {

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// BootStrapper 초기화 중 수행할 모듈 로직을 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract Awaitable Init();

    #endregion

    }
}
