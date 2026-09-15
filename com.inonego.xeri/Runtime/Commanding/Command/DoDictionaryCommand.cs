/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DoDictionaryCommand.cs
수정일 : 2026-09-15

# 설명
IDictionary<TKey, TValue>의 Add/Remove 동작을 실행하고 되돌리는 Command를 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Commanding
{
    // ============================================================
    /// <summary>
    /// IDictionary의 Add 또는 Remove를 실행하는 Command.
    /// </summary>
    // ============================================================
    public class DoDictionaryCommand<TKey, TValue> : IDoCommand
    {

    #region 필드

        private IDictionary<TKey, TValue> dictionary;
        private TKey key;
        private TValue value;
        private bool isAdd;
        private string desc;

        // ------------------------------------------------------------
        /// <summary>
        /// dictionary가 존재해 실행 취소할 수 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanUndo => dictionary != null;

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
        /// Dictionary 변경 Command를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private DoDictionaryCommand
        (
            IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue value,
            bool isAdd,
            string desc
        ) : base()
        {
            this.dictionary = dictionary;
            this.key = key;
            this.value = value;
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
        public static DoDictionaryCommand<TKey, TValue> Add
        (
            IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue value,
            string desc
        ) => new DoDictionaryCommand<TKey, TValue>(dictionary, key, value, true, desc);

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 값을 캡처하고 항목을 제거하는 Command를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static DoDictionaryCommand<TKey, TValue> Remove
        (
            IDictionary<TKey, TValue> dictionary,
            TKey key,
            string desc
        ) => new DoDictionaryCommand<TKey, TValue>(dictionary, key, dictionary[key], false, desc);

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
                dictionary.Add(key, value);
            }
            else
            {
                dictionary.Remove(key);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실행한 Dictionary 동작의 반대 동작을 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Undo()
        {
            if (isAdd)
            {
                dictionary.Remove(key);
            }
            else
            {
                dictionary.Add(key, value);
            }
        }

    #endregion

    }
}