/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XStack.cs
수정일 : 2026-11-24

# 설명
Unity 직렬화를 지원하는 Stack 구현.
SerializeReference(_R) / SerializeField(_V) 두 가지 변형을 제공한다.
Runtime 변경은 내부 ApplyRuntime 호출 시에만 저장 목록에 반영한다.

# 특이사항, 제약사항
Unity 7 Editor의 역직렬화 복원 뒤 Stack 내부 배열과 개수가 어긋나 Clear가 IndexOutOfRangeException을 낼 수 있다.
UpdateRuntime은 어차피 저장 목록에서 다시 채우므로, Editor에서는 Clear 전에 어긋난 내부 상태만 빈 상태로 되돌린다(XHashSet과 같은 방식).
CloneFrom은 runtime 스택만 top 순서 그대로 외부 알림 없이 복사한다. 직렬화 목록은 ApplyRuntime이 맡는다.
값 형식·string·UnityEngine.Object·delegate·Type 요소는 그대로 복사하고, 그 밖의 참조 요소는 CollectionClone 규칙으로 복제한다. 복제를 지원하지 않는 참조 요소는 예외를 던진다.
복제가 실패하면 이 Stack을 바꾸지 않는다.
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
    /// 직렬화 가능한 Stack 기본 클래스.
    /// </summary>
    // ============================================================
    [Serializable]
    public abstract class XStackBase<T> : Stack<T>,
        ISerializationCallbackReceiver, ISerializedRuntimeCollection,
        IDeepCloneableFrom<Stack<T>>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화 데이터를 저장하는 리스트.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract IList<T> Serialized { get; }

    #endregion

    #region 복제

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> source의 runtime 내용을 top 순서 그대로 외부 알림 없이 복사한다.
        /// <br/> 참조 요소는 복제하며 복제가 실패하면 이 Stack을 바꾸지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void CloneFrom(Stack<T> source)
        {
            CollectionCloneHelper.CloneFrom(this, source);
        }

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
            // Stack은 top-first 순서를 그대로 저장한다.
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
        /// Serialized 순서에서 Runtime Stack을 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void UpdateRuntime()
        {
            var serialized = Serialized;

            #if UNITY_EDITOR
            RepairStackState();
            #endif

            Clear();

            // 저장 순서는 top-first이므로 역순 Push한다.
            for (var index = serialized.Count - 1; index >= 0; index--)
            {
                Push(serialized[index]);
            }
        }

        void ISerializedRuntimeCollection.ApplyRuntime() => ApplyRuntime();

        void ISerializedRuntimeCollection.UpdateRuntime() => UpdateRuntime();

        #if UNITY_EDITOR

        private const BindingFlags StackFieldFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo ArrayField = RequiredField("_array");
        private static readonly FieldInfo SizeField  = RequiredField("_size");

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 7 Stack 내부 필드 정보를 가져오고 필드가 없으면 명확히 예외를 발생시킨다.
        /// </summary>
        // ------------------------------------------------------------
        private static FieldInfo RequiredField(string name) =>
            typeof(Stack<T>).GetField(name, StackFieldFlags)
            ?? throw new MissingFieldException(typeof(Stack<T>).FullName, name);

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 7 Editor의 직렬화 복원 과정에서 어긋난 Stack 내부 상태를 비운다.
        /// 정상 상태에서는 필드를 변경하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        private void RepairStackState()
        {
            var array = (Array)ArrayField.GetValue(this);
            var size = (int)SizeField.GetValue(this);
            if (array != null && size >= 0 && size <= array.Length)
            {
                return;
            }

            ArrayField.SetValue(this, Array.Empty<T>());
            SizeField.SetValue(this, 0);
        }

        #endif

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 요소를 참조 형식으로 직렬화하는 Stack.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XStack_R<T> :
        XStackBase<T>,
        IDeepCloneable<XStack_R<T>>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 같은 구체 타입의 빈 컬렉션을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual XStack_R<T> @new() => new();

        // ------------------------------------------------------------
        /// <summary>
        /// source의 runtime 내용을 외부 알림 없이 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloneFrom(XStack_R<T> source) => base.CloneFrom(source);

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
    /// 요소를 값 형식으로 직렬화하는 Stack.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XStack_V<T> :
        XStackBase<T>,
        IDeepCloneable<XStack_V<T>>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 같은 구체 타입의 빈 컬렉션을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual XStack_V<T> @new() => new();

        // ------------------------------------------------------------
        /// <summary>
        /// source의 runtime 내용을 외부 알림 없이 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloneFrom(XStack_V<T> source) => base.CloneFrom(source);

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
