/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XQueue.cs
수정일 : 2026-10-23

# 설명
Unity 직렬화를 지원하는 Queue 구현.
SerializeReference(_R) / SerializeField(_V) 두 가지 변형을 제공한다.
Runtime 변경은 내부 ApplyRuntime 호출 시에만 저장 목록에 반영한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// 직렬화 가능한 Queue 기본 클래스.
    /// </summary>
    // ============================================================
    [Serializable]
    public abstract class XQueueBase<T> : Queue<T>,
        ISerializationCallbackReceiver, ISerializedRuntimeCollection
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
        /// Runtime 열거 순서를 serialized 목록으로 명시적으로 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void ApplyRuntime()
        {
            // Queue는 front-first 순서를 그대로 저장한다.
            var values = new List<T>(this);
            var serialized = Serialized;
            serialized.Clear();
            foreach (var item in values)
            {
                serialized.Add(item);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Serialized 순서에서 Runtime Queue를 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void UpdateRuntime()
        {
            var serialized = Serialized;
            Clear();

            for (var index = 0; index < serialized.Count; index++)
            {
                Enqueue(serialized[index]);
            }
        }

        void ISerializedRuntimeCollection.ApplyRuntime() => ApplyRuntime();

        void ISerializedRuntimeCollection.UpdateRuntime() => UpdateRuntime();

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 요소를 참조 형식으로 직렬화하는 Queue.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XQueue_R<T> : XQueueBase<T>
    {
        // ------------------------------------------------------------
        /// <inheritdoc />
        // ------------------------------------------------------------
        protected override IList<T> Serialized
        {
            get => serialized ??= new();
        }

        [SerializeReference]
        private List<T> serialized = new();
    }

    // ============================================================
    /// <summary>
    /// 요소를 값 형식으로 직렬화하는 Queue.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XQueue_V<T> : XQueueBase<T>
    {
        // ------------------------------------------------------------
        /// <inheritdoc />
        // ------------------------------------------------------------
        protected override IList<T> Serialized
        {
            get => serialized ??= new();
        }

        [SerializeField]
        private List<T> serialized = new();
    }
}
