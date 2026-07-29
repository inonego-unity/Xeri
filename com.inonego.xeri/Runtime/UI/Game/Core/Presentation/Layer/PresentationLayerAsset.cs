/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationLayerAsset.cs
수정일 : 2026-07-29

# 설명
stable string ID와 Canvas 배치 정책으로 게임 UI Layer 구성을 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// 게임 UI Layer의 직렬화 구성.
    /// </summary>
    // ============================================================
    [CreateAssetMenu
    (
        fileName = "Presentation Layer",
        menuName = "Xeri/UI/Game/Presentation Layer"
    )]
    public sealed class PresentationLayerAsset : ScriptableObject
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Layer를 식별하는 stable string ID.
        /// </summary>
        // ------------------------------------------------------------
        public string ID => id;

        [SerializeField]
        private string id = "";

        // ------------------------------------------------------------
        /// <summary>
        /// Layer의 Canvas 구성 방식.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationLayerMode Mode => mode;

        [SerializeField]
        private PresentationLayerMode mode = PresentationLayerMode.Shared;

        // ------------------------------------------------------------
        /// <summary>
        /// 공유 Canvas sibling 또는 독립 Canvas sorting 순서.
        /// </summary>
        // ------------------------------------------------------------
        public int Order => order;

        [SerializeField]
        private int order = 0;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Layer 구성이 Runtime 등록에 유효한지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException("Presentation Layer ID가 비어 있습니다.");
            }
        }

    #endregion

    }
}
