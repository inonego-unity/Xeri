/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DoCollectionCommand.cs
수정일 : 2026-09-15

# 설명
ICollection<T>의 Add/Remove 동작을 실행하고 반대 동작으로 되돌리는 Command를 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Commanding
{
    // ============================================================
    /// <summary>
    /// ICollection의 Add 또는 Remove를 실행하는 Command.
    /// </summary>
    // ============================================================
    public class DoCollectionCommand<T> : IDoCommand
    {

    #region 필드

        private ICollection<T> collection;
        private T item;
        private bool isAdd;
        private string desc;

        // ------------------------------------------------------------
        /// <summary>
        /// collection이 존재해 실행 취소할 수 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanUndo => collection != null;

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
        /// 컬렉션 변경 Command를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private DoCollectionCommand
        (
            ICollection<T> collection,
            T item,
            bool isAdd,
            string desc
        ) : base()
        {
            this.collection = collection;
            this.item = item;
            this.isAdd = isAdd;
            this.desc = desc;
        }

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 항목을 추가하는 Command를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static DoCollectionCommand<T> Add
        (
            ICollection<T> collection,
            T item,
            string desc
        ) => new DoCollectionCommand<T>(collection, item, true, desc);

        // ------------------------------------------------------------
        /// <summary>
        /// 항목을 제거하는 Command를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static DoCollectionCommand<T> Remove
        (
            ICollection<T> collection,
            T item,
            string desc
        ) => new DoCollectionCommand<T>(collection, item, false, desc);

    #endregion

    #region 실행

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 Add 또는 Remove를 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Do()
        {
            if (isAdd)
            {
                collection.Add(item);
            }
            else
            {
                collection.Remove(item);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실행한 컬렉션 동작의 반대 동작을 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Undo()
        {
            if (isAdd)
            {
                collection.Remove(item);
            }
            else
            {
                collection.Add(item);
            }
        }

    #endregion

    }
}