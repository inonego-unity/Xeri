/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : FocusController.cs
수정일 : 2026-07-29

# 설명
Screen별 마지막 Focus와 기본·대체 선택을 관리하고 새로 노출된 Screen에 복원한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Screen Focus 기록과 복원을 소유하는 Controller.
    /// </summary>
    // ============================================================
    public sealed class FocusController
    {
    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// Screen별 마지막 선택과 기본 Focus를 보관한다.
        /// </summary>
        // ============================================================
        private sealed class Record
        {
            public object Last = null;
            public object Default = null;
        }

    #endregion

    #region 필드

        private readonly IFocusDriver driver = null;
        private readonly Dictionary<ScreenSession, Record> records =
            new Dictionary<ScreenSession, Record>();

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Focus backend를 사용하는 Controller를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public FocusController(IFocusDriver driver) : base()
        {
            this.driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Screen Focus 기록을 만들고 해당 Screen의 유효한 대상을 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Activate
        (
            ScreenSession session,
            object defaultFocus
        )
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (!records.TryGetValue(session, out var record))
            {
                record = new Record();
                records.Add(session, record);
            }

            record.Default = defaultFocus;
            var target = Resolve(record);
            driver.Select(target);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Screen이 가려지기 전에 현재 선택을 마지막 Focus로 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Cover(ScreenSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (!records.TryGetValue(session, out var record))
            {
                record = new Record();
                records.Add(session, record);
            }

            if (driver.IsValid(driver.Current))
            {
                record.Last = driver.Current;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 제거된 Screen의 Focus 기록을 폐기한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Remove(ScreenSession session)
        {
            if (session == null) return;

            records.Remove(session);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 새로 노출된 Screen의 마지막·기본·대체 Focus 순서로 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Restore(ScreenSession session)
        {
            if (session == null)
            {
                driver.Select(driver.FindFallback());
                return;
            }

            if (!records.TryGetValue(session, out var record))
            {
                record = new Record();
                records.Add(session, record);
            }

            driver.Select(Resolve(record));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 Screen Focus 기록을 제거하고 대체 Focus를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear()
        {
            records.Clear();
            driver.Select(driver.FindFallback());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막·기본·대체 순서로 유효한 Focus 대상을 결정한다.
        /// </summary>
        // ------------------------------------------------------------
        private object Resolve(Record record)
        {
            if (driver.IsValid(record.Last))
            {
                return record.Last;
            }

            if (driver.IsValid(record.Default))
            {
                return record.Default;
            }

            return driver.FindFallback();
        }

    #endregion

    }
}
