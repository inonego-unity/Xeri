/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Revision.cs
수정일 : 2026-11-25

# 설명
상태가 바뀔 때마다 오르는 순서 있는 발급 번호를 값으로 정의한다.
같은 발급자 안에서는 번호가 클수록 나중 상태이며, 소유자는 revision = revision.Inc()로 다음 번호를 발급한다.

# 특이사항, 제약사항
0은 None(미발급)으로 예약하고 발급은 1부터 한다. 기본값(default)도 None이며 음수는 표현할 수 없다.
Inc는 checked라 넘치면 OverflowException을 던진다.
같음·순서 비교 연산자만 제공하고 덧셈·뺄셈과 정수 변환 연산자는 없다. 전송·저장용 원시 값은 ToUInt64·FromUInt64로만 연다.
번호는 인스턴스 수명 안에서만 의미가 있으며 직렬화 대상이 아니다. 다른 소유자나 재시작 전후의 값끼리는 비교하지 않는다.
번호를 올리는 시점과 복원 때의 동작은 소유자가 정하고, 이 값은 발급 번호의 표현과 비교 규칙만 맡는다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 상태가 바뀔 때마다 오르는 순서 있는 발급 번호.
    /// </summary>
    // ============================================================
    public readonly struct Revision : IEquatable<Revision>, IComparable<Revision>
    {

    #region 필드

        // 0은 None(미발급)이다. default(Revision)도 None이다.
        private readonly ulong value;

    #endregion

    #region 프로퍼티

        // ------------------------------------------------------------
        /// <summary>
        /// 아직 발급하지 않은 번호를 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public static readonly Revision None = default;

        // ------------------------------------------------------------
        /// <summary>
        /// 아직 발급하지 않은 번호인지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsNone => value == 0;

    #endregion

    #region 생성자

        private Revision(ulong value)
        {
            this.value = value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 전송·저장에서 읽은 원시 값으로 번호를 만든다. 0은 None이다.
        /// </summary>
        // ------------------------------------------------------------
        public static Revision FromUInt64(ulong value)
        {
            return new Revision(value);
        }

    #endregion

    #region 발급

        // ----------------------------------------------------------------------
        /// <summary>
        /// 다음 번호를 반환한다. None의 다음은 1이고 넘치면 OverflowException을 던진다.
        /// </summary>
        // ----------------------------------------------------------------------
        public Revision Inc()
        {
            return new Revision(checked(value + 1));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 전송·저장에 쓰는 원시 값을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public ulong ToUInt64()
        {
            return value;
        }

    #endregion

    #region 비교

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 번호인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Equals(Revision other) => value == other.value;

        // ------------------------------------------------------------
        /// <summary>
        /// 번호의 순서를 비교한다. 작으면 음수, 같으면 0, 크면 양수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int CompareTo(Revision other) => value.CompareTo(other.value);

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 Revision 번호인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Equals(object obj) => obj is Revision other && Equals(other);

        // ------------------------------------------------------------
        /// <summary>
        /// 번호의 해시 코드를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override int GetHashCode() => value.GetHashCode();

        // ------------------------------------------------------------
        /// <summary>
        /// 번호를 문자열로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToString() => value.ToString();

        // ------------------------------------------------------------
        /// <summary>
        /// 두 번호가 같은지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator ==(Revision left, Revision right) => left.value == right.value;

        // ------------------------------------------------------------
        /// <summary>
        /// 두 번호가 다른지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator !=(Revision left, Revision right) => left.value != right.value;

        // ------------------------------------------------------------
        /// <summary>
        /// left가 right보다 먼저 발급된 번호인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator <(Revision left, Revision right) => left.value < right.value;

        // ------------------------------------------------------------
        /// <summary>
        /// left가 right보다 나중에 발급된 번호인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator >(Revision left, Revision right) => left.value > right.value;

        // ------------------------------------------------------------
        /// <summary>
        /// left가 right와 같거나 먼저 발급된 번호인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator <=(Revision left, Revision right) => left.value <= right.value;

        // ------------------------------------------------------------
        /// <summary>
        /// left가 right와 같거나 나중에 발급된 번호인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator >=(Revision left, Revision right) => left.value >= right.value;

    #endregion

    }
}
