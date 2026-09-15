/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DoPropertyCommand.cs
수정일 : 2026-09-15

# 설명
getter와 setter를 이용해 프로퍼티 변경과 이전 값 복원을 수행하는 Command를 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Commanding
{
    // ============================================================
    /// <summary>
    /// 프로퍼티 값을 변경하고 생성 시점의 값으로 되돌리는 Command.
    /// </summary>
    // ============================================================
    public class DoPropertyCommand<TTarget, TValue> : IDoCommand
    {

    #region 필드

        private TTarget target;
        private Func<TTarget, TValue> getter;
        private Action<TTarget, TValue> setter;
        private TValue oldValue;
        private TValue newValue;
        private string desc;

        // ------------------------------------------------------------
        /// <summary>
        /// target이 존재해 실행 취소할 수 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanUndo => target != null;

        // ------------------------------------------------------------
        /// <summary>
        /// 명령 설명.
        /// </summary>
        // ------------------------------------------------------------
        public string Desc => desc;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 값을 캡처하고 새 값으로 변경할 Command를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public DoPropertyCommand
        (
            TTarget target,
            Func<TTarget, TValue> getter,
            Action<TTarget, TValue> setter,
            TValue newValue,
            string desc
        ) : base()
        {
            this.target = target;
            this.getter = getter;
            this.setter = setter;
            oldValue = getter(target);
            this.newValue = newValue;
            this.desc = desc;
        }

    #endregion

    #region 실행

        // ------------------------------------------------------------
        /// <summary>
        /// 새 값을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Do() => setter(target, newValue);

        // ------------------------------------------------------------
        /// <summary>
        /// 생성 시점에 캡처한 값을 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Undo() => setter(target, oldValue);

    #endregion

    }
}