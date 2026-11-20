/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : LayerHelper.cs
수정일 : 2026-05-02

# 설명
LayerMask와 layer 인덱스를 다루기 위한 확장 메서드 모음.
Contains, AddLayer, RemoveLayer, ToLayerMask를 제공한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// LayerMask 및 layer 인덱스 확장 메서드 정적 헬퍼.
    /// </summary>
    // ============================================================
    public static class LayerHelper
    {

    #region 포함 검사

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 레이어가 레이어 마스크에 포함되어 있는지 확인합니다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool Contains(this LayerMask mask, int layer)
        {
            return (mask & (1 << layer)) != 0;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 게임 오브젝트가 레이어 마스크에 포함되는지 확인합니다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool Contains(this LayerMask mask, GameObject obj)
        {
            return mask.Contains(obj.layer);
        }

    #endregion

    #region 추가 및 제거

        // ------------------------------------------------------------
        /// <summary>
        /// 레이어 마스크에 레이어를 추가합니다.
        /// </summary>
        // ------------------------------------------------------------
        public static LayerMask AddLayer(this LayerMask mask, int layer)
        {
            return mask | (1 << layer);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 레이어 마스크에서 레이어를 제거합니다.
        /// </summary>
        // ------------------------------------------------------------
        public static LayerMask RemoveLayer(this LayerMask mask, int layer)
        {
            return mask & ~(1 << layer);
        }

    #endregion

    #region 변환

        // ------------------------------------------------------------
        /// <summary>
        /// 단일 레이어 인덱스를 레이어 마스크로 변환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public static LayerMask ToLayerMask(this int layer)
        {
            return 1 << layer;
        }

    #endregion

    }
}
