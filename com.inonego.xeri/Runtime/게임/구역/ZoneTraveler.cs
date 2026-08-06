/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ZoneTraveler.cs
수정일 : 2026-08-05

# 설명
Zone Graph 안을 이동하는 Actor가 Zone Link 통과를 요청할 수 있게 한다.

# 제약사항
이동 입력·좌표 보정·NavMesh 이동은 소유하지 않고, Link 경계를 넘은 사실만 Zone Graph에 전달한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// 플레이어 또는 다른 이동 Actor가 현재 Zone Graph의 이동을 요청하는 부착형 Component다.
    /// </summary>
    // ============================================================
    public sealed class ZoneTraveler : MonoBehaviour
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Actor가 이동할 공간 수명을 소유하는 Zone Graph다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private ZoneGraphRuntime zoneGraph = null;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 Link를 통한 Zone 이동을 현재 Zone Graph에 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryTraverse(ZoneLink link)
        {
            if (zoneGraph == null)
            {
                return false;
            }

            return zoneGraph.TryTraverse(link);
        }

    #endregion
    }
}
