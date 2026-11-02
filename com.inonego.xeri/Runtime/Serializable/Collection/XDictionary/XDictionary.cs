/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XDictionary.cs
수정일 : 2026-10-06

# 설명
Unity 직렬화를 지원하는 Dictionary 구현.
SerializeReference / SerializeField 조합으로 4가지 변형(RR/RV/VR/VV)을 제공한다.
Runtime 변경은 내부 ApplyRuntime 호출 시에만 serialized pair 목록에 반영한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// 직렬화 키-값 쌍 인터페이스.
    /// </summary>
    // ============================================================
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

    // ============================================================
    /// <summary>
    /// 직렬화 가능한 딕셔너리 기본 클래스.
    /// </summary>
    // ============================================================
    [Serializable]
    public abstract class XDictionaryBase<TKey, TValue, TPair> : Dictionary<TKey, TValue>,
        ISerializationCallbackReceiver, ISerializedRuntimeCollection
    where TPair : IXKeyValuePair<TKey, TValue>, new()
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Serialized 값을 반환하거나 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual IList<TPair> Serialized
        {
            get => serialized ??= new();
        }

        [SerializeField]
        private List<TPair> serialized = new();

    #endregion

    #region 직렬화와 runtime 투영

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화 이전에 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 역직렬화 이후에 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            UpdateRuntime();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime Dictionary를 저장용 pair 목록에 명시적으로 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void ApplyRuntime()
        {
            // 유효한 pair 목록을 먼저 만든 후 authoring 목록을 교체한다.
            var pairs = new List<TPair>(Count);
            foreach (var (key, value) in this)
            {
                var pair = new TPair
                {
                    Key = key,
                    Value = value,
                };
                pairs.Add(pair);
            }

            var serialized = Serialized;
            serialized.Clear();
            foreach (var pair in pairs)
            {
                serialized.Add(pair);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Serialized 목록에서 런타임 Dictionary를 다시 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void UpdateRuntime()
        {
            var serialized = Serialized;
            Clear();

            for (var index = 0; index < serialized.Count; index++)
            {
                var pair = serialized[index];
                // 작성 중인 null Key와 duplicate 목록은 수정하지 않는다.
                // Dictionary는 마지막 중복 Key의 Value가 유효하다.
                if (pair == null || pair.Key is null)
                {
                    continue;
                }

                this[pair.Key] = pair.Value;
            }
        }

        void ISerializedRuntimeCollection.ApplyRuntime() => ApplyRuntime();

        void ISerializedRuntimeCollection.UpdateRuntime() => UpdateRuntime();

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

    // ============================================================
    /// <summary>
    /// 키를 참조 형식, 값을 참조 형식으로 직렬화하는 딕셔너리.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XDictionary_RR<TKey, TValue> : XDictionaryBase<TKey, TValue, XKeyValuePair_RR<TKey, TValue>>
    {
        // NONE
    }

    // ============================================================
    /// <summary>
    /// 키를 참조 형식, 값을 값 형식으로 직렬화하는 딕셔너리.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XDictionary_RV<TKey, TValue> : XDictionaryBase<TKey, TValue, XKeyValuePair_RV<TKey, TValue>>
    {
        // NONE
    }

    // ============================================================
    /// <summary>
    /// 키를 값 형식, 값을 참조 형식으로 직렬화하는 딕셔너리.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XDictionary_VR<TKey, TValue> : XDictionaryBase<TKey, TValue, XKeyValuePair_VR<TKey, TValue>>
    {
        // NONE
    }

    // ============================================================
    /// <summary>
    /// 키를 값 형식, 값을 값 형식으로 직렬화하는 딕셔너리.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XDictionary_VV<TKey, TValue> : XDictionaryBase<TKey, TValue, XKeyValuePair_VV<TKey, TValue>>
    {
        // NONE
    }
}
