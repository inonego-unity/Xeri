/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionColumnSpec.cs
수정일 : 2026-10-31

# 설명
Collection Editor table column의 ID, 표시명, 기본·최소 폭과 초기 확장 가중치를 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// Collection Editor table column 정의.
    /// </summary>
    // ============================================================
    internal sealed class CollectionColumnSpec
    {

    #region 프로퍼티

        // ------------------------------------------------------------
        /// <summary>
        /// 관리 참조의 식별자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string ID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열의 헤더에 표시할 문자열을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Header { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열의 기본 너비를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public float Width { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열에 허용되는 최소 너비를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public float MinWidth { get; }

        // Order처럼 짧은 컬럼은 고정하고 데이터 컬럼만 나머지 폭을 나눠 갖는다.
        public bool Stretchable { get; }

        // 가변 열의 초기 폭 배분에 사용하는 상대 가중치.
        public float StretchWeight { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// CollectionColumnSpec 인스턴스를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionColumnSpec
        (
            string id,
            string header,
            float width = 160f,
            float minWidth = 80f,
            bool stretchable = true,
            float stretchWeight = 1f
        )
        {
            if (stretchable && (stretchWeight <= 0f || float.IsNaN(stretchWeight) || float.IsInfinity(stretchWeight)))
            {
                throw new ArgumentOutOfRangeException(nameof(stretchWeight));
            }

            ID = id ?? throw new ArgumentNullException(nameof(id));
            Header = header ?? string.Empty;
            Width = width;
            MinWidth = minWidth;
            Stretchable = stretchable;
            StretchWeight = stretchable ? stretchWeight : 0f;
        }

    #endregion

    }
}
