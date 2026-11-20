/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : AutoScrollSettings.cs
수정일 : 2026-11-20

# 설명
휠 클릭 자동 스크롤의 속도와 판정 기준. 기준점 둘레의 정지 거리, 거리당 속도, 최대 속도,
누른 채 끌기로 볼 거리, 가로·세로 허용을 정한다.

# 특이사항, 제약사항
값은 UI 픽셀과 초 단위다. 음수는 0으로 맞춘다. UGUI 인스펙터에서도 쓸 수 있게 직렬화한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.UI.AutoScroll
{
    // ============================================================
    /// <summary>
    /// 자동 스크롤 설정.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class AutoScrollSettings
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 기준점에서 이 거리 안이면 움직이지 않는다(px).
        /// </summary>
        // ------------------------------------------------------------
        public float DeadZone
        {
            get => deadZone;
            set
            {
                deadZone = Mathf.Max(0f, value);
            }
        }
        [SerializeField] private float deadZone = 10f;

        // ------------------------------------------------------------
        /// <summary>
        /// 정지 거리 밖으로 1px 벗어날 때마다 더하는 초속(px/s).
        /// </summary>
        // ------------------------------------------------------------
        public float SpeedPerPixel
        {
            get => speedPerPixel;
            set
            {
                speedPerPixel = Mathf.Max(0f, value);
            }
        }
        [SerializeField] private float speedPerPixel = 6f;

        // ------------------------------------------------------------
        /// <summary>
        /// 한 축의 최대 초속(px/s).
        /// </summary>
        // ------------------------------------------------------------
        public float MaxSpeed
        {
            get => maxSpeed;
            set
            {
                maxSpeed = Mathf.Max(0f, value);
            }
        }
        [SerializeField] private float maxSpeed = 4000f;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 버튼을 누른 채 이 거리 이상 움직였다가 떼면 그 자리에서 멈춘다(px).
        /// <br/> 그보다 적게 움직이고 떼면 다시 누를 때까지 계속 스크롤한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public float HoldDistance
        {
            get => holdDistance;
            set
            {
                holdDistance = Mathf.Max(0f, value);
            }
        }
        [SerializeField] private float holdDistance = 6f;

        // ------------------------------------------------------------
        /// <summary>
        /// 가로 스크롤 허용 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool Horizontal
        {
            get => horizontal;
            set
            {
                horizontal = value;
            }
        }
        [SerializeField] private bool horizontal = true;

        // ------------------------------------------------------------
        /// <summary>
        /// 세로 스크롤 허용 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool Vertical
        {
            get => vertical;
            set
            {
                vertical = value;
            }
        }
        [SerializeField] private bool vertical = true;

    #endregion

    }
}
