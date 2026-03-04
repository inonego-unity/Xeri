using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ========================================================================
    /// <summary>
    /// Base class for a serializable HashSet.
    /// </summary>
    // ========================================================================
    [Serializable]
    public abstract class XHashSetBase<T> : HashSet<T>, ISerializationCallbackReceiver
    {

    #region Fields

        // ------------------------------------------------------------
        /// <summary>
        /// List that stores serialized data.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract IList<T> Serialized { get; }

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

            foreach (var item in this)
            {
                Serialized.Add(item);
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
                Add(Serialized[i]);
            }
        }

    #endregion

    }

    // ========================================================================
    /// <summary>
    /// HashSet that serializes elements as reference type.
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
    /// HashSet that serializes elements as value type.
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