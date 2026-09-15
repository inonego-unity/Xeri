/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DoGroupCommand.cs
수정일 : 2026-09-15

# 설명
여러 IDoCommand를 하나의 Undo 단위로 묶는 내부 Composite Command를 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Commanding
{
    // ============================================================
    /// <summary>
    /// 여러 Command를 하나의 실행 및 Undo 단위로 묶는 내부 Command.
    /// </summary>
    // ============================================================
    [Serializable]
    internal class DoGroupCommand : IDoCommand
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 그룹에 포함된 Command 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<IDoCommand> Commands => commands;

        [SerializeReference]
        private List<IDoCommand> commands = null;

        [SerializeField]
        private string desc = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 하위 Command가 Undo 가능한지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanUndo => commands.TrueForAll(command => command.CanUndo);

        // ------------------------------------------------------------
        /// <summary>
        /// 그룹 설명.
        /// </summary>
        // ------------------------------------------------------------
        public string Desc => desc;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 하위 Command와 설명으로 그룹 Command를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public DoGroupCommand
        (
            List<IDoCommand> commands,
            string desc
        ) : base()
        {
            this.commands = commands;
            this.desc = desc;
        }

    #endregion

    #region 실행

        // ------------------------------------------------------------
        /// <summary>
        /// 하위 Command를 정방향으로 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Do()
        {
            foreach (var command in commands)
            {
                command.Do();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 하위 Command를 역순으로 실행 취소한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Undo()
        {
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                commands[i].Undo();
            }
        }

    #endregion

    }
}