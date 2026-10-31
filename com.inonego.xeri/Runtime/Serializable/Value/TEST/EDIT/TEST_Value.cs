/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Value.cs
수정일 : 2026-10-30

# 설명
Value<T>의 핵심 기능 테스트.

# 테스트 구성
 E: Base 변경·이벤트·값 표현
 S: 직렬화 (JSON 라운드트립/직렬화 변경 확정)
 X: 상속 확장 (초기 정규화/ProcessBase 재정의)
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
    /// Value 시스템의 핵심 기능 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_Value
    {

    #region 헬퍼

        // ============================================================
        /// <summary>
        /// ProcessBase를 오버라이드하는 커스텀 Value 클래스.
        /// </summary>
        // ============================================================
        private class CustomValue : Value<int>
        {
            protected override void ProcessBase(in int prev, ref int next)
            {
                next = Math.Max(0, next * 2);
            }
        }

        // ============================================================
        /// <summary>
        /// 생성자 인자에 의존하는 정규화를 초기화 완료 후 수행한다.
        /// </summary>
        // ============================================================
        private sealed class ScaledValue : Value<int>
        {
            private readonly int factor;

            // ------------------------------------------------------------
            /// <summary>
            /// 파생 필드 초기화 완료 후 초기값을 정규화한다.
            /// </summary>
            // ------------------------------------------------------------
            public ScaledValue(int initial, int factor) : base(initial)
            {
                this.factor = factor;
                InitializeBase(initial);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 파생 인자를 사용해 값을 정규화한다.
            /// </summary>
            // ------------------------------------------------------------
            protected override void ProcessBase(in int previous, ref int current)
            {
                current *= factor;
            }
        }

    #endregion

    #region E-1: Base 설정 및 변경 통지

        // ------------------------------------------------------------
        /// <summary>
        /// Base의 실제 변경은 sender와 이전·현재 값을 한 번 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Value_Base변경_이전현재값전달()
        {
            var value = new Value<int>(10);
            var count = 0;
            object sender = null;
            ValueChangeEventArgs<int> change = default;

            value.OnBaseChange += (source, args) =>
            {
                count++;
                sender = source;
                change = args;
            };

            value.Base = 20;

            Assert.AreEqual(1, count);
            Assert.AreSame(value, sender);
            Assert.AreEqual(10, change.Previous);
            Assert.AreEqual(20, change.Current);
            Assert.AreEqual(20, value.Base);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 Base와 동일한 값은 외부 변경 이벤트를 발생시키지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Value_동일Base설정_변경통지없음()
        {
            var value = new Value<int>(10);
            var count = 0;
            value.OnBaseChange += (_, _) => count++;

            value.Set(10);

            Assert.AreEqual(0, count);
            Assert.AreEqual(10, value.Base);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 무통지 변경 후 다음 이벤트는 이전 통지값이 아닌 현재 Base를 기준으로 발생한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_Value_무통지Base변경_다음변경기준갱신()
        {
            var value = new Value<int>(10);
            var count = 0;
            ValueChangeEventArgs<int> change = default;
            value.OnBaseChange += (_, args) =>
            {
                count++;
                change = args;
            };

            value.Set(20, invokeEvent: false);

            Assert.AreEqual(20, value.Base);
            Assert.AreEqual(0, count);

            value.Set(30);

            Assert.AreEqual(1, count);
            Assert.AreEqual(20, change.Previous);
            Assert.AreEqual(30, change.Current);
        }
    #endregion

    #region E-2: 암시적 변환·비교·문자열 표현

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

        // ------------------------------------------------------------
        /// <summary>
        /// 서로 다른 Value 계열 런타임 타입은 값이 같아도 대칭적으로 구별한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Value_서로다른파생타입_동등성대칭()
        {
            var value = new Value<int>(10);
            var modified = new MValue<int>(10);
            var ranged = new RangeValue<int>(10, new Range<int>(0, 20));

            Assert.IsFalse(value.Equals(modified));
            Assert.IsFalse(modified.Equals(value));

            Assert.IsFalse(value.Equals(ranged));
            Assert.IsFalse(ranged.Equals(value));

            Assert.IsFalse(modified.Equals(ranged));
            Assert.IsFalse(ranged.Equals(modified));

            Assert.IsTrue(value.Equals(new Value<int>(10)));
            Assert.IsTrue(modified.Equals(new MValue<int>(10)));
            Assert.IsTrue(ranged.Equals(new RangeValue<int>(10, new Range<int>(0, 20))));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 참조형 기본값이 null이어도 해시와 문자열 표현을 안전하게 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Value_참조형Null_해시문자열안전()
        {
            var value = new Value<string>(null);

            Assert.AreEqual(0, value.GetHashCode());
            Assert.AreEqual(string.Empty, value.ToString());

            value.Set("ready");

            Assert.AreEqual("ready".GetHashCode(), value.GetHashCode());
            Assert.AreEqual("ready", value.ToString());
        }

    #endregion

    #region S-1: JSON 라운드트립

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

    #region S-2: 직렬화 변경 확정

        // ------------------------------------------------------------
        /// <summary>
        /// 직접 필드 복원은 이전 스냅샷과 한 번만 비교하여 통지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Value_직렬화복원_스냅샷기반_중복통지없음()
        {
            var value = new Value<int>(10);
            var previous = value.Base;
            var field = typeof(Value<int>).GetField
            (
                "base", System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic
            );
            Assert.IsNotNull(field);

            var eventCount = 0;
            ValueChangeEventArgs<int> change = default;
            value.OnBaseChange += (_, args) =>
            {
                eventCount++;
                change = args;
            };

            field.SetValue(value, 20);
            value.FinalizeSerializedChange(previous);
            value.FinalizeSerializedChange(value.Base);

            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(10, change.Previous);
            Assert.AreEqual(20, change.Current);
        }

    #endregion

    #region X-1: 파생 타입 초기화

        // ------------------------------------------------------------
        /// <summary>
        /// 파생 생성자 필드가 준비된 뒤 초기 정규화를 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Value_파생초기화_가상메서드조기호출없음()
        {
            var value = new ScaledValue(5, 3);

            Assert.AreEqual(15, value.Base);
            value.Set(2);
            Assert.AreEqual(6, value.Base);
        }

    #endregion

    #region X-2: ProcessBase 재정의

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

    }

}
