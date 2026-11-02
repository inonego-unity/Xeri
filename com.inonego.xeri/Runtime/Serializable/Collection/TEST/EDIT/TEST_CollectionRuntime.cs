/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_CollectionRuntime.cs
수정일 : 2026-10-31

# 설명
Editor 의존성 없이 Serializable Collection의 Runtime Apply/Update를 검증한다.
직렬화 원본 보존과 HashSet·Dictionary·Stack·Queue의 데이터 동기화를 확인한다.

# 테스트 구성
 A: 명시적인 Runtime 적용과 동기화
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime;
using System.Runtime.Serialization;

using UnityEngine;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.TEST.Serializable._Collection
{
    // ======================================================================
    /// <summary>
    /// Serializable Collection의 Runtime 전용 저장·복원 계약을 검증한다.
    /// </summary>
    // ======================================================================
    public class TEST_CollectionRuntime
    {

    #region A-1: Runtime 적용과 동기화

        [Serializable]
        private sealed class HashSetSerializationFixture
        {
            public XHashSet_V<int> Values = new();
        }

        [Serializable]
        private sealed class DictionarySerializationFixture
        {
            public XDictionary_VV<string, int> Values = new();
        }

        [Serializable]
        private sealed class QueueSerializationFixture
        {
            public XQueue_V<int> Values = new();
        }

        [Serializable]
        private sealed class StackSerializationFixture
        {
            public XStack_V<int> Values = new();
        }

        [Test]
        public void TEST_CollectionRuntime_HashSet_Json역직렬화후_컬렉션동작()
        {
            var source = new HashSetSerializationFixture();
            source.Values.Add(7);
            source.Values.Add(11);
            source.Values.ApplyRuntime();

            var json = JsonUtility.ToJson(source);
            var restored = JsonUtility.FromJson<HashSetSerializationFixture>(json);
            Assert.IsNotNull(restored);
            Assert.IsNotNull(restored.Values);
            Assert.IsTrue(restored.Values.Contains(7));
            Assert.IsTrue(restored.Values.Contains(11));
            Assert.AreEqual(2, restored.Values.Count);

            restored.Values.Add(13);
            Assert.IsTrue(restored.Values.Contains(13));
        }

        [Test]
        public void TEST_CollectionRuntime_Dictionary_Json역직렬화후_컬렉션동작()
        {
            var source = new DictionarySerializationFixture();
            source.Values.Add("a", 7);
            source.Values.Add("b", 11);
            source.Values.ApplyRuntime();

            var json = JsonUtility.ToJson(source);
            var restored = JsonUtility.FromJson<DictionarySerializationFixture>(json);
            Assert.IsNotNull(restored);
            Assert.IsNotNull(restored.Values);
            Assert.AreEqual(7, restored.Values["a"]);
            Assert.AreEqual(11, restored.Values["b"]);
            Assert.AreEqual(2, restored.Values.Count);

            restored.Values.Add("c", 13);
            Assert.AreEqual(13, restored.Values["c"]);
        }

        [Test]
        public void TEST_CollectionRuntime_Queue_Json역직렬화후_컬렉션동작()
        {
            var source = new QueueSerializationFixture();
            source.Values.Enqueue(7);
            source.Values.Enqueue(11);
            source.Values.ApplyRuntime();

            var json = JsonUtility.ToJson(source);
            var restored = JsonUtility.FromJson<QueueSerializationFixture>(json);
            Assert.IsNotNull(restored);
            Assert.IsNotNull(restored.Values);
            Assert.AreEqual(2, restored.Values.Count);
            Assert.AreEqual(7, restored.Values.Dequeue());
            Assert.AreEqual(11, restored.Values.Dequeue());

            restored.Values.Enqueue(13);
            Assert.AreEqual(13, restored.Values.Dequeue());
        }

        [Test]
        public void TEST_CollectionRuntime_Stack_Json역직렬화후_컬렉션동작()
        {
            var source = new StackSerializationFixture();
            source.Values.Push(7);
            source.Values.Push(11);
            source.Values.ApplyRuntime();

            var json = JsonUtility.ToJson(source);
            var restored = JsonUtility.FromJson<StackSerializationFixture>(json);
            Assert.IsNotNull(restored);
            Assert.IsNotNull(restored.Values);
            Assert.AreEqual(2, restored.Values.Count);
            Assert.AreEqual(11, restored.Values.Pop());
            Assert.AreEqual(7, restored.Values.Pop());

            restored.Values.Push(13);
            Assert.AreEqual(13, restored.Values.Pop());
        }

        [Test]
        public void TEST_CollectionRuntime_HashSet_생성자없는복원()
        {
            var set = RestoreWithoutConstructor<XHashSet_V<int>>
            (
                new List<int>
                {
                    7,
                    11,
                }
            );
            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set.Contains(7));
            Assert.IsTrue(set.Add(13));
        }




        // ------------------------------------------------------------
        /// <summary>
        /// Unity가 BCL 생성자를 건너뛰는 경우의 복원 계약을 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        private static T RestoreWithoutConstructor<T>(object values)
        where T : class, ISerializationCallbackReceiver
        {
            var instance = (T)FormatterServices.GetUninitializedObject(typeof(T));
            var type = typeof(T);
            var field = type.GetField
            (
                "serialized", BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (field == null)
            {
                field = type.BaseType?.GetField
                (
                    "serialized", BindingFlags.Instance | BindingFlags.NonPublic
                );
            }

            Assert.IsNotNull(field);
            field.SetValue(instance, values);
            instance.OnAfterDeserialize();
            return instance;
        }

        [Test]
        public void TEST_CollectionRuntime_HashSet_중복원본과_명시적동기화()
        {
            var set = new XHashSet_V<int>();
            var field = typeof(XHashSet_V<int>).GetField
            (
                "serialized", BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(field);

            var stored = (List<int>)field.GetValue(set);
            stored.Add(1);
            stored.Add(1);
            ((ISerializationCallbackReceiver)set).OnAfterDeserialize();

            Assert.AreEqual(1, set.Count);
            set.Add(2);
            ((ISerializationCallbackReceiver)set).OnBeforeSerialize();

            // 직렬화 콜백만으로 중복 원본을 지우거나 Runtime 값을 적용하지 않는다.
            Assert.AreEqual(2, stored.Count);
            Assert.AreEqual(1, stored[0]);
            Assert.AreEqual(1, stored[1]);

            set.ApplyRuntime();
            Assert.AreEqual(2, stored.Count);
            CollectionAssert.Contains(stored, 1);
            CollectionAssert.Contains(stored, 2);

            set.Remove(2);
            set.UpdateRuntime();
            Assert.IsTrue(set.Contains(2));
        }

        [Test]
        public void TEST_CollectionRuntime_Dictionary_작성중행보존과_명시적적용()
        {
            var dictionary = new XDictionary_VV<string, int>();
            var field = typeof(XDictionary_VV<string, int>).BaseType.GetField
            (
                "serialized", BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(field);

            var stored = (List<XKeyValuePair_VV<string, int>>)field.GetValue(dictionary);
            var first = new XKeyValuePair_VV<string, int>();
            first.Key = "a";
            first.Value = 1;
            stored.Add(first);

            var duplicate = new XKeyValuePair_VV<string, int>();
            duplicate.Key = "a";
            duplicate.Value = 9;
            stored.Add(duplicate);
            ((ISerializationCallbackReceiver)dictionary).OnAfterDeserialize();

            Assert.AreEqual(9, dictionary["a"]);
            dictionary.Add("b", 3);
            ((ISerializationCallbackReceiver)dictionary).OnBeforeSerialize();
            Assert.AreEqual(2, stored.Count);
            Assert.AreEqual("a", stored[1].Key);

            dictionary.ApplyRuntime();
            Assert.AreEqual(2, stored.Count);
            dictionary.Remove("b");
            dictionary.UpdateRuntime();
            Assert.AreEqual(3, dictionary["b"]);
        }

        [Test]
        public void TEST_CollectionRuntime_Queue_선입선출_Apply와Update()
        {
            var queue = new XQueue_V<int>();
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.ApplyRuntime();
            queue.Dequeue();
            queue.Enqueue(3);
            ((ISerializationCallbackReceiver)queue).OnBeforeSerialize();

            queue.UpdateRuntime();
            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual(1, queue.Peek());
            Assert.AreEqual(2, queue.ToArray()[1]);

            queue.Enqueue(3);
            queue.ApplyRuntime();
            queue.Dequeue();
            queue.UpdateRuntime();
            Assert.AreEqual(3, queue.Count);
            Assert.AreEqual(1, queue.Peek());
            Assert.AreEqual(2, queue.ToArray()[1]);
            Assert.AreEqual(3, queue.ToArray()[2]);
        }

        [Test]
        public void TEST_CollectionRuntime_Stack_최상단순서_Apply와Update()
        {
            var stack = new XStack_V<int>();
            stack.Push(1);
            stack.Push(2);
            stack.ApplyRuntime();
            stack.Pop();
            stack.Push(3);
            ((ISerializationCallbackReceiver)stack).OnBeforeSerialize();

            stack.UpdateRuntime();
            Assert.AreEqual(2, stack.Count);
            Assert.AreEqual(2, stack.Peek());
            Assert.AreEqual(1, stack.ToArray()[1]);

            stack.Push(3);
            stack.ApplyRuntime();
            stack.Pop();
            stack.UpdateRuntime();
            Assert.AreEqual(3, stack.Count);
            Assert.AreEqual(3, stack.Peek());
            Assert.AreEqual(2, stack.ToArray()[1]);
            Assert.AreEqual(1, stack.ToArray()[2]);
        }

        [Test]
        public void TEST_CollectionRuntime_참조형변형_Apply와Update()
        {
            var first = new NumericIModifier(NumericIOperation.ADD, 1);
            var second = new NumericIModifier(NumericIOperation.ADD, 2);

            var set = new XHashSet_R<NumericIModifier>();
            set.Add(first);
            set.ApplyRuntime();
            set.Add(second);
            set.UpdateRuntime();
            Assert.IsTrue(set.Contains(first));
            Assert.IsFalse(set.Contains(second));

            var queue = new XQueue_R<NumericIModifier>();
            queue.Enqueue(first);
            queue.Enqueue(second);
            queue.ApplyRuntime();
            queue.Dequeue();
            queue.UpdateRuntime();
            Assert.AreSame(first, queue.Peek());

            var stack = new XStack_R<NumericIModifier>();
            stack.Push(first);
            stack.Push(second);
            stack.ApplyRuntime();
            stack.Pop();
            stack.UpdateRuntime();
            Assert.AreSame(second, stack.Peek());

            var dictionary = new XDictionary_VR<string, NumericIModifier>();
            dictionary.Add("a", first);
            dictionary.ApplyRuntime();
            dictionary.Clear();
            dictionary.UpdateRuntime();
            Assert.AreSame(first, dictionary["a"]);
        }

    #endregion

    }
}
