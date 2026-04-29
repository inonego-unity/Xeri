/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XHashSet.cs
수정일 : 2026-04-29

# 설명
Unity 직렬화를 지원하는 HashSet 구현.
SerializeReference(_R) / SerializeField(_V) 두 가지 변형을 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ========================================================================
    /// <summary>
    /// 직렬화 가능한 HashSet 기본 클래스.
    /// </summary>
    // ========================================================================
    [Serializable]
    public abstract class XHashSetBase<T> : HashSet<T>, ISerializationCallbackReceiver
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화 데이터를 저장하는 리스트.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract IList<T> Serialized { get; }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화 이전에 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void OnBeforeSerialize()
        {
            Serialized.Clear();

            foreach (var item in this)
            {
                Serialized.Add(item);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 역직렬화 이후에 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void OnAfterDeserialize()
        {
            Clear();

            for (int i = 0; i < Serialized.Count; i++)
            {
                Add(Serialized[i]);
            }
        }

    #endregion

    }

    // ========================================================================
    /// <summary>
    /// 요소를 참조 형식으로 직렬화하는 HashSet.
    /// </summary>
    // ========================================================================
    [Serializable]
    public class XHashSet_R<T> : XHashSetBase<T>
    {
        [SerializeReference]
        private List<T> serialized = new();
        protected override IList<T> Serialized => serialized;
    }

    // ========================================================================
    /// <summary>
    /// 요소를 값 형식으로 직렬화하는 HashSet.
    /// </summary>
    // ========================================================================
    [Serializable]
    public class XHashSet_V<T> : XHashSetBase<T>
    {
        [SerializeField]
        private List<T> serialized = new();
        protected override IList<T> Serialized => serialized;
    }
}
