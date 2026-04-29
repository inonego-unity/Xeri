/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XDictionary.cs
수정일 : 2026-04-29

# 설명
Unity 직렬화를 지원하는 Dictionary 구현.
SerializeReference / SerializeField 조합으로 4가지 변형(RR/RV/VR/VV)을 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ========================================================================
    /// <summary>
    /// 직렬화 키-값 쌍 인터페이스.
    /// </summary>
    // ========================================================================
    public interface IXKeyValuePair<TKey, TValue>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 키 값.
        /// </summary>
        // ------------------------------------------------------------
        TKey Key { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 값.
        /// </summary>
        // ------------------------------------------------------------
        TValue Value { get; set; }
    }

    // ========================================================================
    /// <summary>
    /// 직렬화 가능한 딕셔너리 기본 클래스.
    /// </summary>
    // ========================================================================
    [Serializable]
    public abstract class XDictionaryBase<TKey, TValue, TPair> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    where TPair : IXKeyValuePair<TKey, TValue>, new()
    {

    #region 필드

        [SerializeField]
        private List<TPair> serialized = new();
        protected virtual IList<TPair> Serialized => serialized;

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

            foreach (var (key, value) in this)
            {
                var pair = new TPair
                {
                    Key   = key,
                    Value = value,
                };

                Serialized.Add(pair);
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
                var pair = Serialized[i];

                this[pair.Key] = pair.Value;
            }
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 키·값 모두 SerializeReference로 직렬화하는 쌍.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XKeyValuePair_RR<TKey, TValue> : IXKeyValuePair<TKey, TValue>
    {
        [SerializeReference] public TKey   Key;
        [SerializeReference] public TValue Value;

        TKey   IXKeyValuePair<TKey, TValue>.Key   { get => Key;   set => Key   = value; }
        TValue IXKeyValuePair<TKey, TValue>.Value { get => Value; set => Value = value; }
    }

    // ============================================================
    /// <summary>
    /// 키는 SerializeReference, 값은 SerializeField로 직렬화하는 쌍.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XKeyValuePair_RV<TKey, TValue> : IXKeyValuePair<TKey, TValue>
    {
        [SerializeReference] public TKey   Key;
        [SerializeField]     public TValue Value;

        TKey   IXKeyValuePair<TKey, TValue>.Key   { get => Key;   set => Key   = value; }
        TValue IXKeyValuePair<TKey, TValue>.Value { get => Value; set => Value = value; }
    }

    // ============================================================
    /// <summary>
    /// 키는 SerializeField, 값은 SerializeReference로 직렬화하는 쌍.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XKeyValuePair_VR<TKey, TValue> : IXKeyValuePair<TKey, TValue>
    {
        [SerializeField]     public TKey   Key;
        [SerializeReference] public TValue Value;

        TKey   IXKeyValuePair<TKey, TValue>.Key   { get => Key;   set => Key   = value; }
        TValue IXKeyValuePair<TKey, TValue>.Value { get => Value; set => Value = value; }
    }

    // ============================================================
    /// <summary>
    /// 키·값 모두 SerializeField로 직렬화하는 쌍.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XKeyValuePair_VV<TKey, TValue> : IXKeyValuePair<TKey, TValue>
    {
        [SerializeField] public TKey   Key;
        [SerializeField] public TValue Value;

        TKey   IXKeyValuePair<TKey, TValue>.Key   { get => Key;   set => Key   = value; }
        TValue IXKeyValuePair<TKey, TValue>.Value { get => Value; set => Value = value; }
    }

    // ----------------------------------------------------------------------------------------
    /// <summary>
    /// 키를 참조 형식, 값을 참조 형식으로 직렬화하는 딕셔너리.
    /// </summary>
    /// <typeparam name="TKey">키 타입. SerializeReference로 직렬화된다.</typeparam>
    /// <typeparam name="TValue">값 타입. SerializeReference로 직렬화된다.</typeparam>
    // ----------------------------------------------------------------------------------------
    [Serializable]
    public class XDictionary_RR<TKey, TValue> : XDictionaryBase<TKey, TValue, XKeyValuePair_RR<TKey, TValue>> {}

    // ----------------------------------------------------------------------------------------
    /// <summary>
    /// 키를 참조 형식, 값을 값 형식으로 직렬화하는 딕셔너리.
    /// </summary>
    /// <typeparam name="TKey">키 타입. SerializeReference로 직렬화된다.</typeparam>
    /// <typeparam name="TValue">값 타입. SerializeField로 직렬화된다.</typeparam>
    // ----------------------------------------------------------------------------------------
    [Serializable]
    public class XDictionary_RV<TKey, TValue> : XDictionaryBase<TKey, TValue, XKeyValuePair_RV<TKey, TValue>> {}

    // ----------------------------------------------------------------------------------------
    /// <summary>
    /// 키를 값 형식, 값을 참조 형식으로 직렬화하는 딕셔너리.
    /// </summary>
    /// <typeparam name="TKey">키 타입. SerializeField로 직렬화된다.</typeparam>
    /// <typeparam name="TValue">값 타입. SerializeReference로 직렬화된다.</typeparam>
    // ----------------------------------------------------------------------------------------
    [Serializable]
    public class XDictionary_VR<TKey, TValue> : XDictionaryBase<TKey, TValue, XKeyValuePair_VR<TKey, TValue>> {}

    // ----------------------------------------------------------------------------------------
    /// <summary>
    /// 키를 값 형식, 값을 값 형식으로 직렬화하는 딕셔너리.
    /// </summary>
    /// <typeparam name="TKey">키 타입. SerializeField로 직렬화된다.</typeparam>
    /// <typeparam name="TValue">값 타입. SerializeField로 직렬화된다.</typeparam>
    // ----------------------------------------------------------------------------------------
    [Serializable]
    public class XDictionary_VV<TKey, TValue> : XDictionaryBase<TKey, TValue, XKeyValuePair_VV<TKey, TValue>> {}
}
