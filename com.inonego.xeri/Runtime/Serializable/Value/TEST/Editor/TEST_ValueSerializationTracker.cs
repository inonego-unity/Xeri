/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_ValueSerializationTracker.cs
수정일 : 2026-10-30

# 설명
SerializedObject를 통한 직렬화 변경과 ValueSerializationTracker의 상태 확정을 검증한다.
실제 Drawer의 UI 동작은 이 테스트에서 검증하지 않는다.

# 테스트 구성
 B: Base 관찰 및 Runtime 변경 기준값·owner 교체
 M: Modifier 변경과 직렬화 Base 변경의 독립성·owner 교체
 R: Range와 Runtime Base 변경의 독립성·Clamp·owner 교체
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Primitive;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.TEST.Serializable._Value
{
    // ============================================================
    /// <summary>
    /// Editor 직렬화 상태 관찰 및 변경 확정 테스트.
    /// </summary>
    // ============================================================
    public class TEST_ValueSerializationTracker
    {

    #region 헬퍼

        // ============================================================
        /// <summary>
        /// 기본값을 직렬화하는 테스트용 Unity 소유자.
        /// </summary>
        // ============================================================
        private sealed class ValueOwner : ScriptableObject
        {
            public Value<int> Value = new(42);
        }

        // ============================================================
        /// <summary>
        /// Modifier 값을 직렬화하는 테스트용 Unity 소유자.
        /// </summary>
        // ============================================================
        private sealed class MValueOwner : ScriptableObject
        {
            public MValue<int> Value = new(42);
        }

        // ============================================================
        /// <summary>
        /// 범위 값을 직렬화하는 테스트용 Unity 소유자.
        /// </summary>
        // ============================================================
        private sealed class RangeOwner : ScriptableObject
        {
            public RangeValue<int> Value = new(5, new Range<int>(0, 10));
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 실제 SerializedObject 경로로 int 필드를 변경하고 Unity 직렬화 변경을 적용한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static void ApplySerializedInt
        (
            UnityEngine.Object owner,
            string propertyPath,
            int current
        )
        {
            var serialized = new SerializedObject(owner);
            var property = serialized.FindProperty(propertyPath);
            Assert.IsNotNull(property);
            property.intValue = current;

            Assert.IsTrue(serialized.ApplyModifiedProperties());
        }

    #endregion

    #region B-1: Base 직렬화 변경 확정

        // ------------------------------------------------------------
        /// <summary>
        /// 동일 소유자·경로의 중복 관찰은 이벤트를 중복 발행하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueSerializationTracker_Base중복관찰_단일통지()
        {
            var owner = ScriptableObject.CreateInstance<ValueOwner>();
            try
            {
                var value = owner.Value;
                ValueSerializationTracker.Observe(owner, "Value", value);
                ValueSerializationTracker.Observe(owner, "Value", value);

                var count = 0;
                ValueChangeEventArgs<int> last = default;
                value.OnBaseChange += (_, args) =>
                {
                    count++;
                    last = args;
                };

                ApplySerializedInt(owner, "Value.base", 99);
                ValueSerializationTracker.FinalizeSerializedChange(owner, "Value", value);
                ValueSerializationTracker.FinalizeSerializedChange(owner, "Value", value);

                Assert.AreEqual(1, count);
                Assert.AreEqual(42, last.Previous);
                Assert.AreEqual(99, last.Current);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

    #endregion

    #region B-2: Runtime 무통지 변경 기준값

        // ----------------------------------------------------------------------
        /// <summary>
        /// Runtime에서 이벤트 없이 값을 바꿔도 다음 직렬화 차이는 새 값에서 계산한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueSerializationTracker_무통지Runtime변경_기준값갱신()
        {
            var owner = ScriptableObject.CreateInstance<ValueOwner>();
            try
            {
                var value = owner.Value;
                ValueSerializationTracker.Observe(owner, "Value", value);
                value.Set(100, invokeEvent: false);

                ValueChangeEventArgs<int> change = default;
                var count = 0;
                value.OnBaseChange += (_, args) =>
                {
                    count++;
                    change = args;
                };

                ApplySerializedInt(owner, "Value.base", 105);
                ValueSerializationTracker.FinalizeSerializedChange(owner, "Value", value);

                Assert.AreEqual(1, count);
                Assert.AreEqual(100, change.Previous);
                Assert.AreEqual(105, change.Current);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

    #endregion

    #region B-3: owner 인스턴스 교체

        // ----------------------------------------------------------------------
        /// <summary>
        /// 새 Value를 관찰하면 이전 owner의 Base를 이벤트 기준에 사용하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueSerializationTracker_BaseOwner교체_기준값독립()
        {
            var owner = ScriptableObject.CreateInstance<ValueOwner>();
            try
            {
                var oldValue = owner.Value;
                ValueSerializationTracker.Observe(owner, "Value", oldValue);

                owner.Value = new Value<int>(90);
                var current = owner.Value;
                ValueSerializationTracker.Observe(owner, "Value", current);
                oldValue.Set(200, invokeEvent: false);

                var count = 0;
                ValueChangeEventArgs<int> last = default;
                current.OnBaseChange += (_, args) =>
                {
                    count++;
                    last = args;
                };

                ApplySerializedInt(owner, "Value.base", 91);
                ValueSerializationTracker.FinalizeSerializedChange(owner, "Value", current);

                Assert.AreEqual(1, count);
                Assert.AreEqual(90, last.Previous);
                Assert.AreEqual(91, last.Current);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

    #endregion

    #region M-1: 직렬화 Base·Modified 동시 확정

        // ----------------------------------------------------------------------
        /// <summary>
        /// Unity에서 Base가 직렬화로 변경되면 Modified도 이전 상태에서 통지한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueSerializationTracker_Base직렬화변경_Modified동시통지()
        {
            var owner = ScriptableObject.CreateInstance<MValueOwner>();
            try
            {
                var value = owner.Value;
                value.AddModifier
                (
                    "add",
                    new NumericIModifier(NumericIOperation.ADD, 5),
                    invokeEvent: false
                );
                ValueSerializationTracker.Observe(owner, "Value", value);

                var changes = new List<string>();
                value.OnBaseChange += (_, args) =>
                    changes.Add($"Base:{args.Previous}->{args.Current}");
                value.OnModifiedChange += (_, args) =>
                    changes.Add($"Modified:{args.Previous}->{args.Current}");

                ApplySerializedInt(owner, "Value.base", 100);
                Assert.AreEqual(105, value.Modified);

                ValueSerializationTracker.FinalizeSerializedChange(owner, "Value", value);
                ValueSerializationTracker.FinalizeSerializedChange(owner, "Value", value);

                CollectionAssert.AreEqual
                (
                    new[]
                    {
                        "Base:42->100",
                        "Modified:47->105"
                    },
                    changes
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

    #endregion

    #region M-2: 직렬화 Base와 Modifier 이벤트 독립성

        // ----------------------------------------------------------------------
        /// <summary>
        /// 미확정 Base 직렬화 변경은 Modifier의 Runtime 이벤트가 먼저 발생해도 보존된다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueSerializationTracker_Modifier변경후Base직렬화변경보존()
        {
            var owner = ScriptableObject.CreateInstance<MValueOwner>();
            try
            {
                var value = owner.Value;
                var modifier = new NumericIModifier(NumericIOperation.ADD, 5);
                value.AddModifier("add", modifier, invokeEvent: false);
                ValueSerializationTracker.Observe(owner, "Value", value);

                var changes = new List<string>();
                value.OnBaseChange += (_, args) =>
                    changes.Add($"Base:{args.Previous}->{args.Current}");
                value.OnModifiedChange += (_, args) =>
                    changes.Add($"Modified:{args.Previous}->{args.Current}");

                ApplySerializedInt(owner, "Value.base", 100);
                Assert.AreEqual(105, value.Modified);

                modifier.Value = 7;
                ValueSerializationTracker.FinalizeSerializedChange(owner, "Value", value);

                // SerializedObject 적용 시 Modified는 이미 105로 재계산된다.
                // 이후 Modifier 변경은 105에서 107로, 미확정 Base는 42에서 100으로 통지한다.
                CollectionAssert.AreEqual
                (
                    new[]
                    {
                        "Modified:105->107",
                        "Base:42->100"
                    },
                    changes
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

    #endregion

    #region M-3: Modifier owner 교체

        // ------------------------------------------------------------
        /// <summary>
        /// MValue 교체는 Base와 Modified 관찰 기준을 함께 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueSerializationTracker_MValueOwner교체_기준값독립()
        {
            var owner = ScriptableObject.CreateInstance<MValueOwner>();
            try
            {
                var oldValue = owner.Value;
                ValueSerializationTracker.Observe(owner, "Value", oldValue);

                owner.Value = new MValue<int>(90);
                var current = owner.Value;
                current.AddModifier
                (
                    "add", new NumericIModifier(NumericIOperation.ADD, 5),
                    invokeEvent: false
                );
                ValueSerializationTracker.Observe(owner, "Value", current);
                oldValue.Set(200, invokeEvent: false);

                var changes = new List<string>();
                current.OnBaseChange += (_, args) =>
                    changes.Add($"Base:{args.Previous}->{args.Current}");
                current.OnModifiedChange += (_, args) =>
                    changes.Add($"Modified:{args.Previous}->{args.Current}");

                ApplySerializedInt(owner, "Value.base", 100);
                ValueSerializationTracker.FinalizeSerializedChange(owner, "Value", current);

                CollectionAssert.AreEqual
                (
                    new[]
                    {
                        "Base:90->100",
                        "Modified:95->105"
                    },
                    changes
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

    #endregion

    #region R-1: 직렬화 Range와 Runtime Base 독립성

        // ------------------------------------------------------------
        /// <summary>
        /// 미확정 Range 직렬화 변경은 Runtime Base 변경 후에도 보존된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueSerializationTracker_Base변경후Range직렬화변경보존()
        {
            var owner = ScriptableObject.CreateInstance<RangeOwner>();
            try
            {
                var value = owner.Value;
                ValueSerializationTracker.Observe(owner, "Value", value);

                var changes = new List<ValueChangeEventArgs<Range<int>>>();
                value.Range.OnBaseChange += (_, args) => changes.Add(args);

                ApplySerializedInt(owner, "Value.range.base.max", 20);
                value.Set(7);
                ValueSerializationTracker.FinalizeSerializedChange(owner, "Value", value);

                Assert.AreEqual(1, changes.Count);
                Assert.AreEqual(new Range<int>(0, 10), changes[0].Previous);
                Assert.AreEqual(new Range<int>(0, 20), changes[0].Current);
                Assert.AreEqual(7, value.Base);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

    #endregion

    #region R-2: 직렬화 Range의 Base 제한

        // ----------------------------------------------------------------------
        /// <summary>
        /// 실제 직렬화 Range 변경은 Base를 Clamp한 뒤 각 변경을 한 번씩 통지한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueSerializationTracker_Range직렬화변경_Base보정및통지()
        {
            var owner = ScriptableObject.CreateInstance<RangeOwner>();
            try
            {
                var value = owner.Value;
                ValueSerializationTracker.Observe(owner, "Value", value);

                var order = new List<string>();
                value.OnBaseChange += (_, args) =>
                    order.Add($"Base:{args.Previous}->{args.Current}");
                value.Range.OnBaseChange += (_, args) =>
                    order.Add($"Range:{args.Previous}->{args.Current}");

                ApplySerializedInt(owner, "Value.range.base.min", 7);
                ValueSerializationTracker.FinalizeSerializedChange(owner, "Value", value);
                ValueSerializationTracker.FinalizeSerializedChange(owner, "Value", value);

                Assert.AreEqual(7, value.Base);
                Assert.AreEqual(new Range<int>(7, 10), value.Range.Base);
                CollectionAssert.AreEqual
                (
                    new[]
                    {
                        "Base:5->7",
                        "Range:(0 - 10)->(7 - 10)"
                    },
                    order
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

    #endregion

    #region R-3: Range owner 교체

        // ------------------------------------------------------------
        /// <summary>
        /// RangeValue 교체는 Base와 Range의 변경 이전 상태를 함께 교체한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueSerializationTracker_RangeOwner교체_기준값독립()
        {
            var owner = ScriptableObject.CreateInstance<RangeOwner>();
            try
            {
                var oldValue = owner.Value;
                ValueSerializationTracker.Observe(owner, "Value", oldValue);

                owner.Value = new RangeValue<int>(8, new Range<int>(0, 20));
                var current = owner.Value;
                ValueSerializationTracker.Observe(owner, "Value", current);
                oldValue.Set(9, invokeEvent: false);

                var changes = new List<string>();
                current.OnBaseChange += (_, args) =>
                    changes.Add($"Base:{args.Previous}->{args.Current}");
                current.Range.OnBaseChange += (_, args) =>
                    changes.Add($"Range:{args.Previous}->{args.Current}");

                ApplySerializedInt(owner, "Value.range.base.min", 9);
                ValueSerializationTracker.FinalizeSerializedChange(owner, "Value", current);

                CollectionAssert.AreEqual
                (
                    new[]
                    {
                        "Base:8->9",
                        "Range:(0 - 20)->(9 - 20)"
                    },
                    changes
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

    #endregion

    }
}
