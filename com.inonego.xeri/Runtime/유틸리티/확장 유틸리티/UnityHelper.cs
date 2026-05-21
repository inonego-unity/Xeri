/* BLOCK_HEADER_BEGIN =======================================================================
파일명: UnityHelper.cs
수정일: 2026-05-20

# 설명
UnityEngine Object 관련 확장 메서드 모음.
GameObject 컴포넌트 조회 및 생성 보조 기능을 제공한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// UnityEngine Object 관련 확장 메서드 정적 헬퍼.
    /// </summary>
    // ============================================================
    public static class UnityHelper
    {

    #region Component

        // ------------------------------------------------------------
        /// <summary>
        /// 컴포넌트를 가져오고, 없으면 추가해서 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();

            if (component == null)
            {
                component = go.AddComponent<T>();
            }

            return component;
        }

    #endregion

    }
}
