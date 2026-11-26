/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_ValueRegistry.cs
수정일 : 2026-11-26

# 설명
ValueRegistry가 타입별 컬렉션을 첫 값이 들어올 때 만드는 동작을 검증한다.
아직 만들지 않은 컬렉션이 있어도 조회·변경·복제·비교·직렬화가 빈 컬렉션과 같게 동작해야 한다.

# 테스트 구성
 L: 지연 생성 (빈 조회·제거·비우기·첫 값 등록)
 C: 복제 (빈 원본·값 있는 원본·만들지 않은 대상)
 R: 이름 변경
 Q: 비교 (만들지 않은 컬렉션과 비운 컬렉션)
 S: 직렬화 (JSON 라운드트립)
 A: 할당 (비교의 열거자 박싱)

# 특이사항, 제약사항
A는 Unity Test Framework의 AllocatingGCMemory 제약을 쓰므로 Unity 밖에서는 실행할 수 없다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.TEST.Serializable._Value
{
    // ============================================================
    /// <summary>
    /// ValueRegistry 지연 생성 테스트.
    /// </summary>
    // ============================================================
    public class TEST_ValueRegistry
    {

    #region L: 지연 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 Registry의 조회 프로퍼티는 null이 아닌 빈 컬렉션이다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueRegistry_빈Registry_조회프로퍼티는빈컬렉션()
        {
            var registry = new ValueRegistry();

            Assert.IsNotNull(registry.ValSs);
            Assert.IsNotNull(registry.ValIs);
            Assert.IsNotNull(registry.ValFs);
            Assert.IsNotNull(registry.ValFlags);
            Assert.AreEqual(0, registry.ValSs.Count);
            Assert.AreEqual(0, registry.ValIs.Count);
            Assert.AreEqual(0, registry.ValFs.Count);
            Assert.AreEqual(0, registry.ValFlags.Count);
            Assert.IsFalse(registry.HasAny);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 Registry의 조회·제거·비우기는 실패나 변경 통지 없이 끝난다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueRegistry_빈Registry_조회제거비우기()
        {
            var registry = new ValueRegistry();
            var count = 0;
            registry.OnChange += () => count++;

            Assert.IsFalse(registry.Has("a"));
            Assert.IsFalse(registry.HasFlag("a"));
            Assert.AreEqual("x", registry.GetS("a", "x"));
            Assert.AreEqual(7, registry.GetI("a", 7));
            Assert.AreEqual(1.5f, registry.GetF("a", 1.5f));
            Assert.IsFalse(registry.Remove("a"));

            registry.Clear();

            Assert.AreEqual(0, count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 타입마다 첫 값을 등록하면 그 타입의 조회 프로퍼티에 나타난다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueRegistry_첫값등록_타입별로나타난다()
        {
            var registry = new ValueRegistry();
            var count = 0;
            registry.OnChange += () => count++;

            registry.SetS("s", "text");
            registry.AddI("i", 3);
            registry.SetF("f", 2.5f);
            registry.SetFlag("flag");

            Assert.AreEqual(4, count);
            Assert.AreEqual("text", registry.ValSs["s"]);
            Assert.AreEqual(3, registry.ValIs["i"]);
            Assert.AreEqual(2.5f, registry.ValFs["f"]);
            CollectionAssert.Contains(registry.ValFlags, "flag");
            Assert.IsTrue(registry.HasAny);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 타입 컬렉션을 만들지 않았어도 타입 충돌은 그대로 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueRegistry_타입충돌_그대로거부()
        {
            var registry = new ValueRegistry();
            registry.SetI("key", 1);

            Assert.Throws<InvalidOperationException>(() => registry.SetS("key", "text"));
            Assert.Throws<InvalidOperationException>(() => registry.SetFlag("key"));
            Assert.AreEqual(0, registry.ValSs.Count);
            Assert.AreEqual(0, registry.ValFlags.Count);
        }

    #endregion

    #region C: 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 값 있는 원본을 만들지 않은 대상에 복제하면 같은 값이 된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueRegistry_복제_빈대상에값복사()
        {
            var source = new ValueRegistry();
            source.SetI("i", 4);
            source.SetFlag("flag");

            var target = new ValueRegistry();
            var count = 0;
            target.OnChange += () => count++;

            target.CloneFrom(source);

            Assert.AreEqual(4, target.GetI("i"));
            Assert.IsTrue(target.HasFlag("flag"));
            Assert.AreEqual(0, target.ValSs.Count);
            Assert.IsTrue(target.IsSameAs(source));
            Assert.AreEqual(0, count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 원본을 복제하면 대상의 값이 모두 비워진다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueRegistry_복제_빈원본이면대상을비움()
        {
            var target = new ValueRegistry();
            target.SetS("s", "text");
            target.SetI("i", 1);
            target.SetFlag("flag");

            target.CloneFrom(new ValueRegistry());

            Assert.IsFalse(target.HasAny);
            Assert.IsFalse(target.Has("s"));
            Assert.IsFalse(target.HasFlag("flag"));
        }

    #endregion

    #region R: 이름 변경

        // ------------------------------------------------------------
        /// <summary>
        /// 일부 타입만 만든 Registry에서도 이름 변경과 제거가 동작한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueRegistry_이름변경_일부타입만있어도동작()
        {
            var registry = new ValueRegistry();
            registry.SetI("old", 5);
            registry.SetFlag("gone");

            var map = new Dictionary<string, string>
            {
                ["old"] = "new",
                ["gone"] = null,
                ["missing"] = "other",
            };

            registry.Rename(map);

            Assert.IsFalse(registry.Has("old"));
            Assert.AreEqual(5, registry.GetI("new"));
            Assert.IsFalse(registry.HasFlag("gone"));
            Assert.IsFalse(registry.Has("other"));
            Assert.AreEqual(0, registry.ValSs.Count);
        }

    #endregion

    #region Q: 비교

        // ------------------------------------------------------------
        /// <summary>
        /// 만들지 않은 컬렉션과 값을 지워 비운 컬렉션은 같은 구성으로 본다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueRegistry_비교_만들지않은컬렉션과비운컬렉션은같다()
        {
            var untouched = new ValueRegistry();
            var emptied = new ValueRegistry();
            emptied.SetS("s", "text");
            emptied.SetFlag("flag");
            emptied.Remove("s");
            emptied.Remove("flag");

            Assert.IsTrue(untouched.IsSameAs(emptied));
            Assert.IsTrue(emptied.IsSameAs(untouched));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 한쪽에만 값이 있으면 다르고, 제외한 키만 다르면 같다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueRegistry_비교_한쪽에만값이있으면다르다()
        {
            var untouched = new ValueRegistry();
            var filled = new ValueRegistry();
            filled.SetI("i", 1);

            Assert.IsFalse(untouched.IsSameAs(filled));
            Assert.IsFalse(filled.IsSameAs(untouched));
            Assert.IsTrue(untouched.IsSameAs(filled, "i"));
            Assert.IsTrue(filled.IsSameAs(untouched, "i"));
        }

    #endregion

    #region S: 직렬화

        // ------------------------------------------------------------
        /// <summary>
        /// 일부 타입만 만든 Registry도 JSON 왕복 뒤 같은 값을 가진다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueRegistry_직렬화_일부타입만있어도왕복()
        {
            var registry = new ValueRegistry();
            registry.SetI("i", 9);

            var restored = JsonUtility.FromJson<ValueRegistry>(JsonUtility.ToJson(registry));

            Assert.AreEqual(9, restored.GetI("i"));
            Assert.AreEqual(0, restored.ValSs.Count);
            Assert.AreEqual(0, restored.ValFlags.Count);
            Assert.IsTrue(restored.IsSameAs(registry));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 Registry도 JSON 왕복 뒤 빈 Registry다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueRegistry_직렬화_빈Registry왕복()
        {
            var restored = JsonUtility.FromJson<ValueRegistry>(JsonUtility.ToJson(new ValueRegistry()));

            Assert.IsFalse(restored.HasAny);
            Assert.IsTrue(restored.IsSameAs(new ValueRegistry()));
        }

    #endregion

    #region A: 할당

        // ------------------------------------------------------------
        /// <summary>
        /// 값이 있는 두 Registry의 비교는 GC 할당을 만들지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueRegistry_비교_할당없음()
        {
            var left = new ValueRegistry();
            var right = new ValueRegistry();
            left.SetS("s", "text");
            left.SetI("i", 1);
            left.SetFlag("flag");
            right.CloneFrom(left);

            // 할당 제약은 반환값 없는 TestDelegate만 받으므로 결과를 버리는 블록으로 감싼다.
            TestDelegate compare = () =>
            {
                left.IsSameAs(right);
                left.IsSameAs(right, "i");
            };

            // 첫 호출의 JIT·런타임 준비 할당을 빼기 위해 측정 전에 같은 동작을 한 번 실행한다.
            compare();

            Assert.That(compare, Is.Not.AllocatingGCMemory());
        }

    #endregion

    }
}
