/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XDictionary.cs
수정일 : 2026-11-24

# 설명
Unity 직렬화를 지원하는 Dictionary 구현.
SerializeReference / SerializeField 조합으로 4가지 변형(RR/RV/VR/VV)을 제공한다.
Runtime 변경은 명시적 ApplyRuntime 호출 시에만 serialized pair 목록에 반영한다. 외부 Game 패키지의 Registry도 저장·복원 경계에서 이를 호출할 수 있다.

# 특이사항, 제약사항
CloneFrom은 runtime Dictionary만 외부 알림 없이 복사한다. serialized pair 목록은 ApplyRuntime이 맡는다.
Key는 복제하지 않고 공유한다. Value는 CollectionClone 규칙으로 복제하며 복제를 지원하지 않는 참조 Value는 예외를 던진다.
참조 Value는 같은 Key·같은 타입의 기존 인스턴스를 재사용하되 source가 가진 인스턴스는 재사용하지 않는다.
복제가 실패하면 이 Dictionary를 바꾸지 않는다. 재사용 Value의 CloneFrom이 실패하면 Key 구성은 그대로 남는다.
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
        ISerializationCallbackReceiver, ISerializedRuntimeCollection,
        IDeepCloneableFrom<Dictionary<TKey, TValue>>
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

    #region 복제

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> source의 runtime 내용을 외부 알림 없이 복사한다.
        /// <br/> Key는 공유하고, 값 형식과 string Value는 그대로 복사한다.
        /// <br/> 참조 Value는 복제하며 복제가 실패하면 이 Dictionary를 바꾸지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void CloneFrom(Dictionary<TKey, TValue> source)
        {
            CollectionCloneHelper.CloneFrom(this, source);
        }

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
        public void ApplyRuntime()
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
        public void UpdateRuntime()
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
        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화할 키 값.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeReference] public TKey   Key;

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화할 값.
        /// </summary>
        // ------------------------------------------------------------
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
        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화할 키 값.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeReference] public TKey   Key;

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화할 값.
        /// </summary>
        // ------------------------------------------------------------
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
        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화할 키 값.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]     public TKey   Key;

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화할 값.
        /// </summary>
        // ------------------------------------------------------------
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
        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화할 키 값.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField] public TKey   Key;

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화할 값.
        /// </summary>
        // ------------------------------------------------------------
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
    public class XDictionary_RR<TKey, TValue> :
        XDictionaryBase<TKey, TValue, XKeyValuePair_RR<TKey, TValue>>,
        IDeepCloneable<XDictionary_RR<TKey, TValue>>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 같은 구체 타입의 빈 딕셔너리를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual XDictionary_RR<TKey, TValue> @new() => new();

        // ------------------------------------------------------------
        /// <summary>
        /// source의 runtime 내용을 외부 알림 없이 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloneFrom(XDictionary_RR<TKey, TValue> source) => base.CloneFrom(source);
    }

    // ============================================================
    /// <summary>
    /// 키를 참조 형식, 값을 값 형식으로 직렬화하는 딕셔너리.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XDictionary_RV<TKey, TValue> :
        XDictionaryBase<TKey, TValue, XKeyValuePair_RV<TKey, TValue>>,
        IDeepCloneable<XDictionary_RV<TKey, TValue>>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 같은 구체 타입의 빈 딕셔너리를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual XDictionary_RV<TKey, TValue> @new() => new();

        // ------------------------------------------------------------
        /// <summary>
        /// source의 runtime 내용을 외부 알림 없이 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloneFrom(XDictionary_RV<TKey, TValue> source) => base.CloneFrom(source);
    }

    // ============================================================
    /// <summary>
    /// 키를 값 형식, 값을 참조 형식으로 직렬화하는 딕셔너리.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XDictionary_VR<TKey, TValue> :
        XDictionaryBase<TKey, TValue, XKeyValuePair_VR<TKey, TValue>>,
        IDeepCloneable<XDictionary_VR<TKey, TValue>>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 같은 구체 타입의 빈 딕셔너리를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual XDictionary_VR<TKey, TValue> @new() => new();

        // ------------------------------------------------------------
        /// <summary>
        /// source의 runtime 내용을 외부 알림 없이 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloneFrom(XDictionary_VR<TKey, TValue> source) => base.CloneFrom(source);
    }

    // ============================================================
    /// <summary>
    /// 키를 값 형식, 값을 값 형식으로 직렬화하는 딕셔너리.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XDictionary_VV<TKey, TValue> :
        XDictionaryBase<TKey, TValue, XKeyValuePair_VV<TKey, TValue>>,
        IDeepCloneable<XDictionary_VV<TKey, TValue>>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 같은 구체 타입의 빈 딕셔너리를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual XDictionary_VV<TKey, TValue> @new() => new();

        // ------------------------------------------------------------
        /// <summary>
        /// source의 runtime 내용을 외부 알림 없이 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloneFrom(XDictionary_VV<TKey, TValue> source) => base.CloneFrom(source);
    }
}
