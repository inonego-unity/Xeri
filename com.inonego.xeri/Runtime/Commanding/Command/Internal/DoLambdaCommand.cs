/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DoLambdaCommand.cs
수정일 : 2026-09-15

# 설명
DoSession의 Action 기반 간편 실행 API를 지원하는 내부 Command를 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Commanding
{
    // ============================================================
    /// <summary>
    /// Action으로 실행과 실행 취소 동작을 전달받는 내부 Command.
    /// </summary>
    // ============================================================
    internal class DoLambdaCommand : IDoCommand
    {

    #region 필드

        private Action doAction = null;
        private Action undoAction = null;
        private Func<bool> canUndo = null;
        private string desc = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 실행 취소 가능 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanUndo => canUndo?.Invoke() ?? true;

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
        /// Action 기반 Command를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public DoLambdaCommand
        (
            Action doAction,
            Action undoAction,
            string desc,
            Func<bool> canUndo = null
        ) : base()
        {
            this.doAction = doAction;
            this.undoAction = undoAction;
            this.canUndo = canUndo;
            this.desc = desc;
        }

    #endregion

    #region 실행

        // ------------------------------------------------------------
        /// <summary>
        /// 전달받은 실행 Action을 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Do() => doAction();

        // ------------------------------------------------------------
        /// <summary>
        /// 전달받은 실행 취소 Action을 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Undo() => undoAction();

    #endregion

    }
}