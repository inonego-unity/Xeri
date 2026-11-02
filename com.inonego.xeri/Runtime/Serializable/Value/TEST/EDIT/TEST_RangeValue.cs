/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_RangeValue.cs
수정일 : 2026-10-30

# 설명
RangeValue<T>의 핵심 기능 테스트.

# 테스트 구성
 E: 범위 설정·Clamp·값 표현
 S: 직렬화 (JSON 라운드트립·Base Clamp·변경 확정)
 V: 이벤트 (Base·Range 변경 및 Range 구독 수명)
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Serializable._Value
{

    using inonego;
    using inonego.Xeri;
    using inonego.Xeri.Primitive;
    using inonego.Xeri.Serializable;

    // ============================================================
    /// <summary>
    /// RangeValue 시스템의 핵심 기능 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_RangeValue
    {

    #region E-1: 범위 설정 및 Clamp

        [Test]
        public void TEST_RangeValue_범위_설정_및_값_제한()
        {
            // ------------------------------------------------------------
            // 테스트 준비
            // ------------------------------------------------------------
            var rangeValue = new RangeValue<int>();

            // ------------------------------------------------------------
            // Range.Base로 범위 설정 - 현재값 최소값으로 조정
            // ------------------------------------------------------------
            rangeValue.Range.Base = (10, 50);

            Assert.AreEqual(10, rangeValue.Min);
            Assert.AreEqual(50, rangeValue.Max);
            Assert.AreEqual(10, rangeValue.Base, "범위 설정 시 현재값이 최소값으로 조정되어야 합니다");

            // ------------------------------------------------------------
            // 범위 내 값 설정
            // ------------------------------------------------------------
            rangeValue.Base = 30;

            Assert.AreEqual(30, rangeValue.Base);

            // ------------------------------------------------------------
            // 범위 초과 값 설정 - 최대값으로 제한
            // ------------------------------------------------------------
            rangeValue.Base = 100;

            Assert.AreEqual(50, rangeValue.Base, "범위를 초과하는 값은 최대값으로 제한되어야 합니다");

            // ------------------------------------------------------------
            // 범위 미만 값 설정 - 최소값으로 제한
            // ------------------------------------------------------------
            rangeValue.Base = 5;

            Assert.AreEqual(10, rangeValue.Base, "범위 미만 값은 최소값으로 제한되어야 합니다");

            // ------------------------------------------------------------
            // Min 개별 변경 - 현재값 유지
            // ------------------------------------------------------------
            rangeValue.Base = 30;
            rangeValue.Range.Base = (20, 50);

            Assert.AreEqual(20, rangeValue.Min);
            Assert.AreEqual(50, rangeValue.Max);
            Assert.AreEqual(30, rangeValue.Base, "Min 변경 시 현재값은 유지되어야 합니다");

            // ------------------------------------------------------------
            // Max 개별 변경 - 현재값 유지
            // ------------------------------------------------------------
            rangeValue.Range.Base = (20, 40);

            Assert.AreEqual(20, rangeValue.Min);
            Assert.AreEqual(40, rangeValue.Max);
            Assert.AreEqual(30, rangeValue.Base, "Max 변경 시 현재값은 유지되어야 합니다");

            // ------------------------------------------------------------
            // Min이 현재값보다 클 때 - 현재값 Min으로 조정
            // ------------------------------------------------------------
            rangeValue.Range.Base = (35, 40);

            Assert.AreEqual(35, rangeValue.Base, "Min이 현재값보다 클 때 현재값이 Min으로 조정되어야 합니다");

            // ------------------------------------------------------------
            // Max가 현재값보다 작을 때 - 현재값 Max로 조정
            // ------------------------------------------------------------
            rangeValue.Range.Base = (0, 25);

            Assert.AreEqual(25, rangeValue.Base, "Max가 현재값보다 작을 때 현재값이 Max로 조정되어야 합니다");
        }

    #endregion

    #region E-2: 비교 및 문자열 표현

        [Test]
        public void TEST_RangeValue_비교_및_ToString()
        {
            // ------------------------------------------------------------
            // 테스트 준비
            // ------------------------------------------------------------
            var rangeValue = new RangeValue<int>(30, (10, 50));

            // ------------------------------------------------------------
            // CompareTo 비교 테스트
            // ------------------------------------------------------------
            Assert.AreEqual(0, rangeValue.CompareTo(30));
            Assert.AreEqual(1, rangeValue.CompareTo(20));
            Assert.AreEqual(-1, rangeValue.CompareTo(40));

            // ------------------------------------------------------------
            // ToString 문자열 표현 테스트
            // ------------------------------------------------------------
            Assert.AreEqual("30 (10 - 50)", rangeValue.ToString());
        }

    #endregion

    #region V-1: Base·Range 변경 이벤트

        [Test]
        public void TEST_RangeValue_Base_및_Range_이벤트()
        {
            // ------------------------------------------------------------
            // 테스트 준비
            // ------------------------------------------------------------
            var rangeValue = new RangeValue<int>();
            bool valueChangeEventFired = false;
            Value<int> valueChangeSender = null;
            ValueChangeEventArgs<int> valueChangeEventArgs = default;

            bool rangeChangeFired = false;
            Value<Range<int>> rangeChangeSender = null;
            ValueChangeEventArgs<Range<int>> rangeChangeArgs = default;

            void Reset()
            {
                valueChangeEventFired = false;
                rangeChangeFired = false;
                valueChangeSender = null;
                rangeChangeSender = null;
                valueChangeEventArgs = default;
                rangeChangeArgs = default;
            }

            rangeValue.OnBaseChange += (sender, e) =>
            {
                valueChangeEventFired = true;
                valueChangeSender = sender as Value<int>;
                valueChangeEventArgs = e;
            };

            rangeValue.Range.OnBaseChange += (sender, e) =>
            {
                rangeChangeFired = true;
                rangeChangeSender = sender as Value<Range<int>>;
                rangeChangeArgs = e;
            };

            // ------------------------------------------------------------
            // Range.Base로 범위 설정 - 이벤트 발생 확인
            // ------------------------------------------------------------
            rangeValue.Range.Base = (10, 50);

            Assert.IsTrue(rangeChangeFired);

            Reset();

            // ------------------------------------------------------------
            // Range 범위 변경 이벤트 확인
            // ------------------------------------------------------------
            rangeValue.Range.Base = (15, 50);

            Assert.IsTrue(rangeChangeFired);
            Assert.AreEqual(15, rangeValue.Min);
            Assert.AreEqual(50, rangeValue.Max);

            Reset();

            // ------------------------------------------------------------
            // Base 값 변경 이벤트 확인
            // ------------------------------------------------------------
            rangeValue.Base = 30;

            Assert.IsTrue(valueChangeEventFired);
            Assert.AreEqual(rangeValue, valueChangeSender);
            Assert.AreEqual(15, valueChangeEventArgs.Previous);
            Assert.AreEqual(30, valueChangeEventArgs.Current);

            Reset();

            // ------------------------------------------------------------
            // Range 범위 변경 이벤트 확인
            // ------------------------------------------------------------
            rangeValue.Range.Base = (15, 40);

            Assert.IsTrue(rangeChangeFired);
            Assert.AreEqual(15, rangeValue.Min);
            Assert.AreEqual(40, rangeValue.Max);
            Assert.IsFalse(valueChangeEventFired);
        }


        // ----------------------------------------------------------------------
        /// <summary>
        /// 무통지 Range 변경도 Base를 즉시 Clamp하되 이벤트는 발생하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_RangeValue_Range무통지변경_Base즉시보정()
        {
            var value = new RangeValue<int>(5, new Range<int>(0, 10));
            var baseCount = 0;
            var rangeCount = 0;
            value.OnBaseChange += (_, _) => baseCount++;
            value.Range.OnBaseChange += (_, _) => rangeCount++;

            value.Range.Set(new Range<int>(7, 9), invokeEvent: false);

            Assert.AreEqual(7, value.Base);
            Assert.AreEqual(0, baseCount);
            Assert.AreEqual(0, rangeCount);
        }
    #endregion

    #region V-2: Range 구독 수명

        // ------------------------------------------------------------
        /// <summary>
        /// Range 인스턴스 교체 후 이전 인스턴스의 내부 구독을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_RangeValue_Range교체_이전구독해제()
        {
            var value = new RangeValue<int>(5, new Range<int>(0, 10));
            var original = value.Range;
            var replacement = new Value<Range<int>>(new Range<int>(0, 10));
            var field = typeof(RangeValue<int>).GetField
            (
                "range", System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic
            );
            Assert.IsNotNull(field);
            field.SetValue(value, replacement);
            value.OnAfterDeserialize();

            var baseChanges = 0;
            value.OnBaseChange += (_, _) => baseChanges++;
            original.Set(new Range<int>(7, 9));

            Assert.AreEqual(5, value.Base);
            Assert.AreEqual(0, baseChanges);

            replacement.Set(new Range<int>(7, 9));

            Assert.AreEqual(7, value.Base);
            Assert.AreEqual(1, baseChanges);
        }

    #endregion

    #region S-1: JSON 라운드트립

        [Test]
        public void TEST_RangeValue_JSON_직렬화_라운드트립()
        {
            // ------------------------------------------------------------
            // 테스트 준비
            // ------------------------------------------------------------
            var originalRangeValue = new RangeValue<int>(50, (10, 100));

            // ------------------------------------------------------------
            // JSON 직렬화/역직렬화 - 상태 복원 확인
            // ------------------------------------------------------------
            string json = JsonUtility.ToJson(originalRangeValue);
            var deserializedRangeValue = JsonUtility.FromJson<RangeValue<int>>(json);

            Assert.AreEqual(originalRangeValue.Base, deserializedRangeValue.Base, "현재 값이 올바르게 복원되어야 합니다");
            Assert.AreEqual(originalRangeValue.Min, deserializedRangeValue.Min, "최소값이 올바르게 복원되어야 합니다");
            Assert.AreEqual(originalRangeValue.Max, deserializedRangeValue.Max, "최대값이 올바르게 복원되어야 합니다");

            // ------------------------------------------------------------
            // 역직렬화 콜백이 Range의 내부 변경 구독을 복구하는지 확인한다.
            // ------------------------------------------------------------
            // 범위 밖 Base에 새 Range를 적용하면 현재 Min으로 보정되어야 한다.
            deserializedRangeValue.Range.Base = (60, 120);

            Assert.AreEqual
            (
                60, deserializedRangeValue.Base,
                "역직렬화된 Range의 내부 변경 구독이 복구되고 Base가 Min으로 보정되어야 합니다."
            );
        }

    #endregion

    #region S-2: 직렬화 변경 확정

        // ------------------------------------------------------------
        /// <summary>
        /// Range 복원 후 Base를 먼저 보정하고 변경을 한 번씩 통지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_RangeValue_직렬화복원_BaseRange_일괄통지()
        {
            var value = new RangeValue<int>(5, new Range<int>(0, 10));
            var previous = (Base: value.Base, Range: value.Range.Base);
            var baseField = typeof(Value<int>).GetField
            (
                "base", System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic
            );
            var rangeField = typeof(Value<Range<int>>).GetField
            (
                "base", System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic
            );
            Assert.IsNotNull(baseField);
            Assert.IsNotNull(rangeField);

            baseField.SetValue(value, 5);
            rangeField.SetValue(value.Range, new Range<int>(7, 9));
            value.OnAfterDeserialize();

            var events = new System.Collections.Generic.List<string>();
            value.OnBaseChange += (_, e) => events.Add($"B:{e.Previous}->{e.Current}");
            value.Range.OnBaseChange += (_, e) => events.Add($"R:{e.Previous}->{e.Current}");

            value.FinalizeSerializedChange(previous.Base, previous.Range);
            value.FinalizeSerializedChange(value.Base, value.Range.Base);

            CollectionAssert.AreEqual
            (
                new[]
                {
                    "B:5->7", "R:(0 - 10)->(7 - 9)"
                },
                events
            );
            Assert.AreEqual(7, value.Base);
        }

    #endregion

    }

}
