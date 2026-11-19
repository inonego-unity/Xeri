/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XQueue.cs
수정일 : 2026-11-19

# 설명
Unity 직렬화를 지원하는 Queue 구현.
SerializeReference(_R) / SerializeField(_V) 두 가지 변형을 제공한다.
Runtime 변경은 내부 ApplyRuntime 호출 시에만 저장 목록에 반영한다.

# 특이사항, 제약사항
Unity 7 Editor의 역직렬화 복원 뒤 Queue 내부 배열과 위치·개수가 어긋나 Clear가 IndexOutOfRangeException을 낼 수 있다.
UpdateRuntime은 어차피 저장 목록에서 다시 채우므로, Editor에서는 Clear 전에 어긋난 내부 상태만 빈 상태로 되돌린다(XHashSet과 같은 방식).
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

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

            #if UNITY_EDITOR
            RepairQueueState();
            #endif

            Clear();

            for (var index = 0; index < serialized.Count; index++)
            {
                Enqueue(serialized[index]);
            }
        }

        void ISerializedRuntimeCollection.ApplyRuntime() => ApplyRuntime();

        void ISerializedRuntimeCollection.UpdateRuntime() => UpdateRuntime();

        #if UNITY_EDITOR

        private const BindingFlags QueueFieldFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo ArrayField = RequiredField("_array");
        private static readonly FieldInfo HeadField  = RequiredField("_head");
        private static readonly FieldInfo TailField  = RequiredField("_tail");
        private static readonly FieldInfo SizeField  = RequiredField("_size");

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 7 Queue 내부 필드 정보를 가져오고 필드가 없으면 명확히 예외를 발생시킨다.
        /// </summary>
        // ------------------------------------------------------------
        private static FieldInfo RequiredField(string name) =>
            typeof(Queue<T>).GetField(name, QueueFieldFlags)
            ?? throw new MissingFieldException(typeof(Queue<T>).FullName, name);

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 7 Editor의 직렬화 복원 과정에서 어긋난 Queue 내부 상태를 비운다.
        /// 정상 상태에서는 필드를 변경하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        private void RepairQueueState()
        {
            var array = (Array)ArrayField.GetValue(this);
            var head = (int)HeadField.GetValue(this);
            var tail = (int)TailField.GetValue(this);
            var size = (int)SizeField.GetValue(this);
            if (array != null && IsValid(array.Length, head, tail, size))
            {
                return;
            }

            ArrayField.SetValue(this, Array.Empty<T>());
            HeadField.SetValue(this, 0);
            TailField.SetValue(this, 0);
            SizeField.SetValue(this, 0);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원형 버퍼의 위치와 개수가 배열 길이 안에 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsValid(int length, int head, int tail, int size)
        {
            if (size < 0 || size > length)
            {
                return false;
            }

            if (length == 0)
            {
                return head == 0 && tail == 0;
            }

            return head >= 0 && head < length && tail >= 0 && tail < length;
        }

        #endif

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
