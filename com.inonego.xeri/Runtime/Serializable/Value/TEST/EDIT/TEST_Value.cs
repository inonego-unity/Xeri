/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Value.cs
수정일 : 2026-05-08

# 설명
Value<T>의 핵심 기능 테스트.

# 테스트 구성
 E: 기본 기능 (생성/Base 변경/암시적 변환/비교/ToString)
 S: 직렬화 (JSON 라운드트립)
 X: 확장 (상속/ProcessBase, InvokeOnBaseChange 강제 발화)
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Serializable._Value
{

    using inonego.Xeri.Serializable;

    // ============================================================
    /// <summary>
    /// Value 시스템의 핵심 기능 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_Value
    {

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// ProcessBase를 오버라이드하는 커스텀 Value 클래스.
        /// </summary>
        // ------------------------------------------------------------
        private class CustomValue : Value<int>
        {
            protected override void ProcessBase(in int prev, ref int next)
            {
                next = Math.Max(0, next * 2);
            }
        }

    #endregion

    #region E-1: 기본 생성

        [Test]
        public void TEST_Value_기본_생성_초기값()
        {
            // Arrange & Act
            var value = new Value<int>();

            // Assert
            Assert.AreEqual(0, value.Base);
        }

    #endregion

    #region E-2: Base 변경 및 이벤트

        [Test]
        public void TEST_Value_Base_변경_이벤트_통합()
        {
            // ------------------------------------------------------------
            // 테스트 준비
            // ------------------------------------------------------------
            var value = new Value<int>();
            bool valueChangeEventFired = false;
            Value<int> valueChangeSender = null;
            ValueChangeEventArgs<int> valueChangeEventArgs = default;

            void Reset()
            {
                valueChangeEventFired = false;
                valueChangeSender = null;
                valueChangeEventArgs = default;
            }

            value.OnBaseChange += (sender, e) =>
            {
                valueChangeEventFired = true;
                valueChangeSender = sender as Value<int>;
                valueChangeEventArgs = e;
            };

            // ------------------------------------------------------------
            // 일반 값 변경 - 이벤트 발생 확인
            // ------------------------------------------------------------
            value.Base = 10;
            Assert.AreEqual(10, value.Base);
            Assert.IsTrue(valueChangeEventFired, "일반 값 변경 시 이벤트가 발생해야 합니다");

            Assert.AreEqual(value, valueChangeSender);
            Assert.AreEqual(0, valueChangeEventArgs.Previous);
            Assert.AreEqual(10, valueChangeEventArgs.Current);

            Reset();

            // ------------------------------------------------------------
            // 동일한 값 설정 - 이벤트 미발생 확인
            // ------------------------------------------------------------
            value.Base = 10;
            Assert.AreEqual(10, value.Base);
            Assert.IsFalse(valueChangeEventFired, "동일한 값 설정 시 이벤트가 발생하지 않아야 합니다");

            Reset();

            // ------------------------------------------------------------
            // 다른 값 설정 - 이벤트 발생 확인
            // ------------------------------------------------------------
            value.Base = 20;
            Assert.AreEqual(20, value.Base);
            Assert.IsTrue(valueChangeEventFired);

            Assert.AreEqual(10, valueChangeEventArgs.Previous);
            Assert.AreEqual(20, valueChangeEventArgs.Current);

            Reset();

            // ------------------------------------------------------------
            // Set 메서드 invokeEvent: false - 이벤트 미발생 확인
            // ------------------------------------------------------------
            value.Set(30, invokeEvent: false);
            Assert.AreEqual(30, value.Base);
            Assert.IsFalse(valueChangeEventFired, "Set에서 invokeEvent가 False일 때 이벤트가 발생하지 않아야 합니다");

            Reset();

            // ------------------------------------------------------------
            // Set 메서드 invokeEvent: true - 이벤트 발생 확인
            // ------------------------------------------------------------
            value.Set(40, invokeEvent: true);
            Assert.AreEqual(40, value.Base);
            Assert.IsTrue(valueChangeEventFired, "Set에서 invokeEvent가 True일 때 이벤트가 발생해야 합니다");

            Reset();
        }

    #endregion

    #region E-3: 암시적 변환 / 비교 / ToString

        [Test]
        public void TEST_Value_암시적_변환_및_비교()
        {
            // ------------------------------------------------------------
            // 테스트 준비
            // ------------------------------------------------------------
            var value = new Value<int>(42);

            // ------------------------------------------------------------
            // 암시적 변환 - Value<T>를 T로 변환
            // ------------------------------------------------------------
            int intValue = value;

            Assert.AreEqual(42, intValue);

            // ------------------------------------------------------------
            // Equals - 직접 값과 비교
            // ------------------------------------------------------------
            Assert.IsTrue(value.Equals(42));
            Assert.IsFalse(value.Equals(10));
            Assert.IsTrue(value.Equals(new Value<int>(42)));
            Assert.IsFalse(value.Equals(new Value<int>(10)));

            // ------------------------------------------------------------
            // ToString - 문자열 표현
            // ------------------------------------------------------------
            Assert.AreEqual("42", value.ToString());
        }

    #endregion

    #region S-1: JSON 직렬화

        [Test]
        public void TEST_Value_JSON_직렬화_라운드트립()
        {
            // ------------------------------------------------------------
            // 테스트 준비
            // ------------------------------------------------------------
            var originalValue = new CustomValue();
            originalValue.Base = 5; // ProcessBase에서 2배로 처리되어 10이 됨

            // ------------------------------------------------------------
            // JSON 직렬화/역직렬화 - 상태 복원 확인
            // ------------------------------------------------------------
            string json = JsonUtility.ToJson(originalValue);
            var deserializedValue = JsonUtility.FromJson<CustomValue>(json);

            Assert.AreEqual(originalValue.Base, deserializedValue.Base, "현재 값이 올바르게 복원되어야 합니다");

            // ------------------------------------------------------------
            // 역직렬화 후 ProcessBase 동작 확인
            // ------------------------------------------------------------
            deserializedValue.Base = 3; // 3 * 2 = 6

            Assert.AreEqual(6, deserializedValue.Base, "ProcessBase 오버라이드가 올바르게 동작해야 합니다");
        }

    #endregion

    #region X-1: 상속 ProcessBase 오버라이드

        [Test]
        public void TEST_Value_ProcessBase_오버라이드_동작()
        {
            // ------------------------------------------------------------
            // 테스트 준비
            // ------------------------------------------------------------
            var customValue = new CustomValue();

            // ------------------------------------------------------------
            // ProcessBase 오버라이드 - 값 변환 처리
            // ------------------------------------------------------------
            customValue.Base = 5;

            Assert.AreEqual(10, customValue.Base, "ProcessBase에서 값이 2배로 처리되어야 합니다");

            // ------------------------------------------------------------
            // ProcessBase 오버라이드 - 음수 처리
            // ------------------------------------------------------------
            customValue.Base = -3;

            Assert.AreEqual(0, customValue.Base, "ProcessBase에서 음수는 0으로 처리되어야 합니다");
        }

    #endregion

    #region X-2: InvokeOnBaseChange 강제 발화

        [Test]
        public void TEST_Value_InvokeOnBaseChange_강제_발화()
        {
            var value = new Value<int>(5);
            ValueChangeEventArgs<int> fired = default;
            value.OnBaseChange += (_, e) => fired = e;

            value.InvokeOnBaseChange(previousValue: 8);

            Assert.AreEqual(8, fired.Previous);
            Assert.AreEqual(5, fired.Current);
        }

    #endregion

    }

}
