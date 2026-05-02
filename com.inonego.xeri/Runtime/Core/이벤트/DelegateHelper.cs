/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DelegateHelper.cs
수정일 : 2026-05-02

# 설명
델리게이트 조작 헬퍼. invocation list 복제 등 Delegate 관련 유틸리티 메서드를 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 델리게이트 조작용 정적 헬퍼.
    /// </summary>
    // ============================================================
    public static class DelegateHelper
    {

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 델리게이트의 invocation list를 복제하여 대상에 할당한다.
        /// 원본이 null이면 대상도 null로 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void CloneFrom<T>(ref T target, in T source) where T : Delegate
        {
            if (source == null)
            {
                target = null;

                return;
            }

            target = (T)Delegate.Combine(source.GetInvocationList());
        }

    #endregion

    }
}
