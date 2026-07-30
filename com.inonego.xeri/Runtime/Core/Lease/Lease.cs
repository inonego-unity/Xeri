/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Lease.cs
수정일 : 2026-07-30

# 설명
값이 없는 일회 종료 책임과 값이 있는 일회 소유권을 전달하는 공통 Lease를 정의한다.

# 종료 계약
Release callback은 호출 전에 소비되며, callback 예외 이후에도 같은 Lease에서 다시 실행되지 않는다.

# 적용 범위
Lease는 일회 종료 책임만 표현한다.
진행 상태, 취소, 식별자와 복합 소유권이 필요한 작업은 해당 Domain Handle이 Lease를 소유한다.
동일 실행 컨텍스트의 중복 호출과 callback 재진입만 수렴시키며 Cross-thread 동기화는 제공하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// <br/> 동일 실행 컨텍스트에서 일회성 종료 책임을 전달하는 Lease.
    /// <br/> 진행 상태나 취소 같은 Domain 제어 계약은 표현하지 않는다.
    /// </summary>
    // ============================================================
    public class Lease : IDisposable
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Lease가 논리적으로 종료되었는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDisposed => release == null;

        private Action release = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 일회 종료 callback으로 Lease를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public Lease(Action release) : base()
        {
            this.release = release ?? throw new ArgumentNullException(nameof(release));
        }

    #endregion

    #region 메서드

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 종료 callback을 최대 한 번 실행한다.
        /// <br/> Callback 실행 전에 Lease를 종료하여 예외와 재진입에도 같은 작업을 다시 호출하지 않는다.
        /// <br/> Callback 실패는 소유권이 유지된 상태나 재시도 가능한 상태를 의미하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Release()
        {
            var release = this.release;
            if (release == null) return;

            // 불투명한 callback이 부분 실행 후 실패해도 같은 작업을 다시 호출하지 않는다.
            this.release = null;
            release();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Lease의 종료 책임을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            Release();
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 값과 일회성 종료 책임을 함께 전달하는 Lease.
    /// </summary>
    /// <typeparam name="T">Lease가 소유하는 값 타입.</typeparam>
    // ============================================================
    public sealed class Lease<T> : Lease
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Lease가 전달하는 값.
        /// </summary>
        // ------------------------------------------------------------
        public T Value { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 값과 일회 종료 callback으로 Lease를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public Lease(T value, Action release) : base(release)
        {
            Value = value;
        }

    #endregion

    }
}
