/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : RevisionMark.cs
수정일 : 2026-11-25

# 설명
캡처한 소유자와 그 순간의 Revision을 함께 보관하고, 소유자의 현재 번호가 캡처한 번호와 같은지 확인한다.
캡처해 둔 선택·표시·검증 결과가 아직 현재 상태에서 나온 것인지 판단하는 소비 규약을 한 곳에 둔다.

# 특이사항, 제약사항
소유자 참조를 함께 들고 있어서 소유자가 다르면 번호가 같아도 현재가 아니다.
아무것도 캡처하지 않은 default 값은 소유자가 null이라 현재가 아니다.
바뀐 적 없는 소유자에서 None을 캡처하면 그 소유자가 바뀌기 전까지 현재로 판정된다.
비교는 같음만 쓰며 순서 비교와 번호를 올리는 일은 하지 않는다. 할당은 없다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 캡처한 소유자와 번호. 소유자의 번호가 달라지면 현재가 아니다.
    /// </summary>
    // ============================================================
    public readonly struct RevisionMark
    {

    #region 필드

        private readonly IRevisioned owner;
        private readonly Revision revision;

    #endregion

    #region 생성자

        private RevisionMark(IRevisioned owner, Revision revision)
        {
            this.owner = owner;
            this.revision = revision;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 소유자와 그 순간의 Revision을 캡처한다.
        /// </summary>
        // ------------------------------------------------------------
        public static RevisionMark Capture(IRevisioned owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            return new RevisionMark(owner, owner.Revision);
        }

    #endregion

    #region 판정

        // ------------------------------------------------------------
        /// <summary>
        /// 캡처한 소유자의 현재 Revision이 캡처한 값과 같은지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsCurrent => owner != null && owner.Revision == revision;

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Mark를 지정한 소유자에서 캡처했는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsCapturedFrom(IRevisioned other)
        {
            return owner != null && ReferenceEquals(owner, other);
        }

    #endregion

    }
}
