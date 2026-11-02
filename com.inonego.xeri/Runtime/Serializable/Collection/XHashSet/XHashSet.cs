/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XHashSet.cs
수정일 : 2026-10-23

# 설명
Unity 직렬화를 지원하는 HashSet 구현.
SerializeReference(_R) / SerializeField(_V) 두 가지 변형을 제공한다.
Runtime 변경은 내부 ApplyRuntime 호출 시에만 직렬화 목록에 반영한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.Reflection;
#endif

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// 직렬화 가능한 HashSet 기본 클래스.
    /// </summary>
    // ============================================================
    [Serializable]
    public abstract class XHashSetBase<T> : HashSet<T>,
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

    #region 해시 집합 상태 복구

        #if UNITY_EDITOR

        private const BindingFlags HashSetFieldFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly FieldInfo EntriesField = RequiredField("_entries");
        private static readonly FieldInfo BucketsField = RequiredField("_buckets");
        private static readonly FieldInfo CountField = RequiredField("_count");
        private static readonly FieldInfo FreeListField = RequiredField("_freeList");
        private static readonly FieldInfo FreeCountField = RequiredField("_freeCount");

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 7 HashSet 내부 필드 정보를 가져오고 필드가 없으면 명확히 예외를 발생시킨다.
        /// </summary>
        // ------------------------------------------------------------
        private static FieldInfo RequiredField(string name) =>
            typeof(HashSet<T>).GetField(name, HashSetFieldFlags)
            ?? throw new MissingFieldException(typeof(HashSet<T>).FullName, name);

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 7 Editor의 직렬화 복원 과정에서 불일치한 HashSet 내부 상태를 복구한다.
        /// 정상 상태에서는 필드를 변경하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        private void RepairHashSetState()
        {
            var entries = EntriesField.GetValue(this);
            var buckets = BucketsField.GetValue(this);
            var count = (int)CountField.GetValue(this);

            var requiresRepair =
                (entries == null && (buckets != null || count > 0)) ||
                (count > 0 && buckets == null);

            if (!requiresRepair)
            {
                return;
            }

            // Comparer와 기존 Clear/Add 복원 로직은 유지한다.
            CountField.SetValue(this, 0);
            BucketsField.SetValue(this, null);
            FreeListField.SetValue(this, -1);
            FreeCountField.SetValue(this, 0);
        }

        #endif

    #endregion

    #region 직렬화와 실행 상태 반영

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 저장 콜백은 runtime 값을 자동으로 덮어쓰지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 역직렬화된 authoring 행을 runtime HashSet으로 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            UpdateRuntime();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 필요할 때만 runtime 집합을 serialized backing에 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void ApplyRuntime()
        {
            // 먼저 완전한 복사본을 구성하고 성공한 경우에만 원본을 교체한다.
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
        /// Editor Apply 또는 Unity 역직렬화 뒤 runtime 집합을 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void UpdateRuntime()
        {
            var serialized = Serialized;

            #if UNITY_EDITOR
            RepairHashSetState();
            #endif

            Clear();

            for (var index = 0; index < serialized.Count; index++)
            {
                Add(serialized[index]);
            }
        }

        void ISerializedRuntimeCollection.ApplyRuntime() => ApplyRuntime();

        void ISerializedRuntimeCollection.UpdateRuntime() => UpdateRuntime();

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 요소를 참조 형식으로 직렬화하는 HashSet.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XHashSet_R<T> : XHashSetBase<T>
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
    /// 요소를 값 형식으로 직렬화하는 HashSet.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XHashSet_V<T> : XHashSetBase<T>
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
