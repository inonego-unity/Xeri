/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DoSession.cs
수정일 : 2026-09-15

# 설명
Command Pattern 기반으로 실행 이력과 Undo/Redo를 관리하는 독립 세션을 정의한다.
그룹 실행, lambda helper, history 조회와 변경 이벤트를 제공한다.

# 특이사항, 제약사항
새 Do는 Redo history를 비우며, CanUndo가 false인 최상단 Command는 Undo barrier로 동작한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Commanding
{
    // ======================================================================
    /// <summary>
    /// Command 실행 이력을 관리하고 Undo/Redo를 제공하는 독립 세션.
    /// </summary>
    // ======================================================================
    public class DoSession : IDoSession
    {

    #region 상태

        private readonly List<IDoCommand> undoStack = new();
        private readonly List<IDoCommand> redoStack = new();

        private bool isGrouping = false;
        private List<IDoCommand> groupBuffer = null;
        private string groupDesc = null;
        private int maxSize = 100;

        // ------------------------------------------------------------
        /// <summary>
        /// 최대 history 크기. 초과 시 가장 오래된 항목부터 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public int MaxSize
        {
            get => maxSize;
            set => maxSize = value;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Undo stack이 있고 최상단 Command가 Undo 가능한지 여부.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool CanUndo => undoStack.Count > 0 && undoStack[undoStack.Count - 1].CanUndo;

        // ------------------------------------------------------------
        /// <summary>
        /// Redo 가능한 Command가 존재하는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanRedo => redoStack.Count > 0;

        // ------------------------------------------------------------
        /// <summary>
        /// Undo stack 항목 수.
        /// </summary>
        // ------------------------------------------------------------
        public int UndoCount => undoStack.Count;

        // ------------------------------------------------------------
        /// <summary>
        /// Redo stack 항목 수.
        /// </summary>
        // ------------------------------------------------------------
        public int RedoCount => redoStack.Count;

        // ------------------------------------------------------------
        /// <summary>
        /// 다음 Undo 대상. 비어 있으면 null.
        /// </summary>
        // ------------------------------------------------------------
        public IDoCommand PeekUndo => undoStack.Count > 0 ? undoStack[undoStack.Count - 1] : null;

        // ------------------------------------------------------------
        /// <summary>
        /// 다음 Redo 대상. 비어 있으면 null.
        /// </summary>
        // ------------------------------------------------------------
        public IDoCommand PeekRedo => redoStack.Count > 0 ? redoStack[redoStack.Count - 1] : null;

        // ------------------------------------------------------------
        /// <summary>
        /// Undo history. 높은 index가 더 최근 항목이다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<IDoCommand> UndoHistory => undoStack;

        // ------------------------------------------------------------
        /// <summary>
        /// Redo history. 높은 index가 더 최근 항목이다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<IDoCommand> RedoHistory => redoStack;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Do 실행 후 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<IDoCommand> OnDo = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Undo 실행 후 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<IDoCommand> OnUndo = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Redo 실행 후 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<IDoCommand> OnRedo = null;

        // ------------------------------------------------------------
        /// <summary>
        /// history가 변경될 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action OnChange = null;

    #endregion

    #region 실행

        // ------------------------------------------------------------
        /// <summary>
        /// Command를 실행하고 Undo history에 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Do(IDoCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command is null.");
            }

            // 그룹 중에는 개별 Command를 즉시 실행하되 history 반영은 그룹 종료까지 보류한다.
            if (isGrouping)
            {
                command.Do();
                groupBuffer.Add(command);
                return;
            }

            // 새 실행은 기존 Redo 분기를 폐기하고 최신 Undo 항목으로 기록한다.
            command.Do();
            undoStack.Add(command);
            redoStack.Clear();

            // 설정된 크기를 넘는 가장 오래된 Undo 항목부터 제거한다.
            while (undoStack.Count > MaxSize)
            {
                undoStack.RemoveAt(0);
            }

            OnDo?.Invoke(command);
            OnChange?.Invoke();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Action 쌍으로 간단한 Command를 생성해 실행한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Do
        (
            Action doAction,
            Action undoAction,
            string desc,
            Func<bool> canUndo = null
        )
        {
            Do(new DoLambdaCommand(doAction, undoAction, desc, canUndo));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 마지막 Command를 실행 취소한다. 최상단이 Undo 불가이면 barrier로 중단한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool Undo()
        {
            if (undoStack.Count == 0)
            {
                return false;
            }

            var command = undoStack[undoStack.Count - 1];

            // Undo 불가 Command는 history를 유지한 채 그 이전 항목으로의 이동을 막는다.
            if (!command.CanUndo)
            {
                return false;
            }

            undoStack.RemoveAt(undoStack.Count - 1);
            command.Undo();
            redoStack.Add(command);

            OnUndo?.Invoke(command);
            OnChange?.Invoke();

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막으로 실행 취소한 Command를 다시 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Redo()
        {
            if (redoStack.Count == 0)
            {
                return false;
            }

            var command = redoStack[redoStack.Count - 1];

            redoStack.RemoveAt(redoStack.Count - 1);
            command.Do();
            undoStack.Add(command);

            OnRedo?.Invoke(command);
            OnChange?.Invoke();

            return true;
        }

    #endregion

    #region 그룹 실행

        // ------------------------------------------------------------
        /// <summary>
        /// 이후 Do 호출을 하나의 Undo 단위로 묶기 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        public void BeginGroup(string desc)
        {
            if (isGrouping)
            {
                throw new InvalidOperationException("A group is already in progress.");
            }

            isGrouping = true;
            groupBuffer = new List<IDoCommand>();
            groupDesc = desc;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 누적된 Command를 하나의 그룹 Command로 묶어 history에 기록한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void EndGroup()
        {
            if (!isGrouping)
            {
                throw new InvalidOperationException("No group is in progress.");
            }

            isGrouping = false;

            // 비어 있지 않은 그룹만 하나의 Undo 단위로 기록한다.
            if (groupBuffer.Count > 0)
            {
                var group = new DoGroupCommand(groupBuffer, groupDesc);

                undoStack.Add(group);
                redoStack.Clear();

                while (undoStack.Count > MaxSize)
                {
                    undoStack.RemoveAt(0);
                }

                OnDo?.Invoke(group);
                OnChange?.Invoke();
            }

            groupBuffer = null;
            groupDesc = null;
        }

    #endregion

    #region History 관리

        // ------------------------------------------------------------
        /// <summary>
        /// Undo와 Redo history를 모두 비운다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();

            OnChange?.Invoke();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Undo history만 비운다.
        /// </summary>
        // ------------------------------------------------------------
        public void ClearUndo()
        {
            undoStack.Clear();

            OnChange?.Invoke();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Redo history만 비운다.
        /// </summary>
        // ------------------------------------------------------------
        public void ClearRedo()
        {
            redoStack.Clear();

            OnChange?.Invoke();
        }

    #endregion

    }
}
