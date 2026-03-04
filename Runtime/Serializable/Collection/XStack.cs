using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ========================================================================
    /// <summary>
    /// Base class for a serializable Stack.
    /// </summary>
    // ========================================================================
    [Serializable]
    public abstract class XStackBase<T> : Stack<T>, ISerializationCallbackReceiver
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
            
            // Stack is serialized in reverse order
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

            for (int i = Serialized.Count - 1; i >= 0; i--)
            {
                Push(Serialized[i]);
            }
        }

    #endregion

    }

    // ========================================================================
    /// <summary>
    /// Stack that serializes elements as reference type.
    /// </summary>
    // ========================================================================
    [Serializable]
    public class XStack_R<T> : XStackBase<T>
    {
        [SerializeReference]
        private List<T> serialized = new();
        protected override IList<T> Serialized => serialized;
    }

    // ========================================================================
    /// <summary>
    /// Stack that serializes elements as value type.
    /// </summary>
    // ========================================================================
    [Serializable]
    public class XStack_V<T> : XStackBase<T>
    {
        [SerializeField]
        private List<T> serialized = new();
        protected override IList<T> Serialized => serialized;
    }
}