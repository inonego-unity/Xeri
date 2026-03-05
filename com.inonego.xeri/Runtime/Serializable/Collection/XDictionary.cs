using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ========================================================================
    /// <summary>
    /// Interface for a serializable key-value pair.
    /// </summary>
    // ========================================================================
    public interface IXKeyValuePair<TKey, TValue>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// The key.
        /// </summary>
        // ------------------------------------------------------------
        TKey Key { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// The value.
        /// </summary>
        // ------------------------------------------------------------
        TValue Value { get; set; }
    }

    // ========================================================================
    /// <summary>
    /// Base class for a serializable dictionary.
    /// </summary>
    // ========================================================================
    [Serializable]
    public abstract class XDictionaryBase<TKey, TValue, TPair> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    where TPair : IXKeyValuePair<TKey, TValue>, new()
    {

    #region Fields

        [SerializeField]
        private List<TPair> serialized = new();
        protected virtual IList<TPair> Serialized => serialized;

    #endregion

    #region Methods

        // ------------------------------------------------------------
        /// <summary>
        /// Called before serialization.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void OnBeforeSerialize()
        {
            Serialized.Clear();

            foreach (var (key, value) in this)
            {
                var pair = new TPair
                {
                    Key = key,
                    Value = value,
                };

                Serialized.Add(pair);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Called after deserialization.
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

    [Serializable]
    public class XKeyValuePair_RR<TKey, TValue> : IXKeyValuePair<TKey, TValue>
    {
        [SerializeReference]
        public TKey Key;

        [SerializeReference]
        public TValue Value;

        TKey IXKeyValuePair<TKey, TValue>.Key
        {
            get => Key;
            set => Key = value;
        }

        TValue IXKeyValuePair<TKey, TValue>.Value
        {
            get => Value;
            set => Value = value;
        }
    }

    [Serializable]
    public class XKeyValuePair_RV<TKey, TValue> : IXKeyValuePair<TKey, TValue>
    {
        [SerializeReference]
        public TKey Key;

        [SerializeField]
        public TValue Value;

        TKey IXKeyValuePair<TKey, TValue>.Key
        {
            get => Key;
            set => Key = value;
        }

        TValue IXKeyValuePair<TKey, TValue>.Value
        {
            get => Value;
            set => Value = value;
        }
    }

    [Serializable]
    public class XKeyValuePair_VR<TKey, TValue> : IXKeyValuePair<TKey, TValue>
    {
        [SerializeField]
        public TKey Key;

        [SerializeReference]
        public TValue Value;

        TKey IXKeyValuePair<TKey, TValue>.Key
        {
            get => Key;
            set => Key = value;
        }

        TValue IXKeyValuePair<TKey, TValue>.Value
        {
            get => Value;
            set => Value = value;
        }
    }

    [Serializable]
    public class XKeyValuePair_VV<TKey, TValue> : IXKeyValuePair<TKey, TValue>
    {
        [SerializeField]
        public TKey Key;

        [SerializeField]
        public TValue Value;

        TKey IXKeyValuePair<TKey, TValue>.Key
        {
            get => Key;
            set => Key = value;
        }

        TValue IXKeyValuePair<TKey, TValue>.Value
        {
            get => Value;
            set => Value = value;
        }
    }

    // =========================================================================================
    /// <summary>
    /// Dictionary that serializes keys as reference type and values as reference type.
    /// </summary>
    /// <typeparam name="TKey">Type for the key. Serialized as reference type.</typeparam>
    /// <typeparam name="TValue">Type for the value. Serialized as reference type.</typeparam>
    // =========================================================================================
    [Serializable]
    public class XDictionary_RR<TKey, TValue> : XDictionaryBase<TKey, TValue, XKeyValuePair_RR<TKey, TValue>> {}

    // =========================================================================================
    /// <summary>
    /// Dictionary that serializes keys as reference type and values as value type.
    /// </summary>
    /// <typeparam name="TKey">Type for the key. Serialized as reference type.</typeparam>
    /// <typeparam name="TValue">Type for the value. Serialized as value type.</typeparam>
    // =========================================================================================
    [Serializable]
    public class XDictionary_RV<TKey, TValue> : XDictionaryBase<TKey, TValue, XKeyValuePair_RV<TKey, TValue>> {}

    // =========================================================================================
    /// <summary>
    /// Dictionary that serializes keys as value type and values as reference type.
    /// </summary>
    /// <typeparam name="TKey">Type for the key. Serialized as value type.</typeparam>
    /// <typeparam name="TValue">Type for the value. Serialized as reference type.</typeparam>
    // =========================================================================================
    [Serializable]
    public class XDictionary_VR<TKey, TValue> : XDictionaryBase<TKey, TValue, XKeyValuePair_VR<TKey, TValue>> {}

    // =========================================================================================
    /// <summary>
    /// Dictionary that serializes keys as value type and values as value type.
    /// </summary>
    /// <typeparam name="TKey">Type for the key. Serialized as value type.</typeparam>
    /// <typeparam name="TValue">Type for the value. Serialized as value type.</typeparam>
    // =========================================================================================
    [Serializable]
    public class XDictionary_VV<TKey, TValue> : XDictionaryBase<TKey, TValue, XKeyValuePair_VV<TKey, TValue>> {}
}