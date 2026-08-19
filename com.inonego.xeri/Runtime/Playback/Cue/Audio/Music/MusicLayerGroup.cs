/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MusicLayerGroup.cs
수정일 : 2026-08-19

# 설명
같은 음악 Timeline에서 동기 재생할 Music Layer 구성을 정의한다.

# 적용 범위
재생 상태와 Gameplay 의미는 저장하지 않고 동기 재생할 Layer 구성만 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// 같은 Timeline에서 함께 재생할 Music Layer Group.
    /// </summary>
    // ============================================================
    [CreateAssetMenu(menuName = "Xeri/Playback/Music Layer Group", fileName = "MusicLayerGroup")]
    public sealed class MusicLayerGroup : ScriptableObject
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Group에 포함된 Music Layer 수.
        /// </summary>
        // ------------------------------------------------------------
        public int LayerCount => layers != null ? layers.Length : 0;

        [SerializeField]
        private MusicLayer[] layers = Array.Empty<MusicLayer>();

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 인덱스의 Music Layer를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public MusicLayer GetLayer(int index)
        {
            if (index < 0 || index >= LayerCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return layers[index];
        }

    #endregion

    }
}
