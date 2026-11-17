/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : BootstrapperModuleAsset.cs
수정일 : 2026-11-18

# 설명
Unity Bootstrapper의 Initial Scene 전후 초기화 계약을 정의한다.
Application의 일반 초기화 모듈과 달리 Scene phase를 필수로 갖는다.
에디터에서는 모듈 에셋의 값이 바뀌면 OnChange로 알려 설정 UI가 상태를 다시 읽게 한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Application;

namespace inonego.Xeri.Bootstrapper
{
    // ============================================================
    /// <summary>
    /// Bootstrapper Module이 실행되는 Initial Scene 기준 phase.
    /// </summary>
    // ============================================================
    public enum BootstrapperModulePhase
    {
        BeforeInitialScene = 0,
        AfterInitialScene = 1,
    }

    // ============================================================
    /// <summary>
    /// Scene phase에 속한 Bootstrapper Module Asset.
    /// </summary>
    // ============================================================
    public abstract class BootstrapperModuleAsset : ScriptableObject
    {

    #region 실행 계약

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Module이 실행될 Bootstrap phase를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract BootstrapperModulePhase Phase { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Scene phase에서 Application 소유 자원을 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract Task StartAsync(ApplicationContext context, CancellationToken cancellationToken);

    #endregion

    #region 변경 알림

    #if UNITY_EDITOR

        // ------------------------------------------------------------
        /// <summary>
        /// 모듈 에셋의 값이 Inspector 편집, 실행 취소, 에셋 로드로 바뀌면 발생한다. 에디터에서만 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public static event Action<BootstrapperModuleAsset> OnChange = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 값이 바뀌면 변경을 알린다. 파생 형식이 재정의하면 base를 호출해야 한다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnValidate()
        {
            OnChange?.Invoke(this);
        }

    #endif

    #endregion

    }
}
