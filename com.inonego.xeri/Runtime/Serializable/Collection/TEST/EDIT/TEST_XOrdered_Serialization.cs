/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_XOrdered_Serialization.cs
수정일 : 2026-10-31

# 설명
XOrdered의 JSON 직렬화와 관리 참조 공유를 검증한다.

# 테스트 구성
 S: Unity JSON 직렬화 계약
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.TEST.Serializable._XOrdered
{
    // ============================================================
    /// <summary>
    /// XOrdered 컬렉션의 현재 공개 계약을 검증한다.
    /// </summary>
    // ============================================================
    public partial class TEST_XOrdered
    {

    #region S-1: 키 없는 컬렉션 직렬화

        [Test]
        public void TEST_XOrdered_JSON_직렬화_순서보존_재사용()
        {
            // Arrange
            var original = new XOrdered<int, TestElement>();

            original.Add(30, new TestElement("C"));
            original.Add(10, new TestElement("A"));
            original.Add(20, new TestElement("B"));

            // Act
            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<XOrdered<int, TestElement>>(json);

            // Assert
            Assert.AreEqual(3, restored.Count);
            Assert.AreEqual(10, restored[0].Order);
            Assert.AreEqual("A", restored[0].Value.Name);
            Assert.AreEqual(20, restored[1].Order);
            Assert.AreEqual("B", restored[1].Value.Name);
            Assert.AreEqual(30, restored[2].Order);
            Assert.AreEqual("C", restored[2].Value.Name);

            restored.Add(15, new TestElement("Inserted"));

            Assert.AreEqual(4, restored.Count);
            Assert.AreEqual(15, restored[1].Order);
            Assert.AreEqual("Inserted", restored[1].Value.Name);
        }

    #endregion

    #region S-2: 키 있는 컬렉션 직렬화와 조회 복구

        [Test]
        public void TEST_XOrdered_Keyed_JSON_직렬화_Lookup_Lazy재구성()
        {
            // Arrange
            var original = new XOrdered<int, string, TestElement>();

            original.Add(30, "c", new TestElement("C"));
            original.Add(10, "a", new TestElement("A"));
            original.Add(20, "b", new TestElement("B"));

            // Act
            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<XOrdered<int, string, TestElement>>(json);

            var foundValue = restored.TryGetValue("b", out var value);
            var foundEntry = restored.TryGetEntry("b", out var entry);

            // Assert
            Assert.AreEqual(3, restored.Count);
            Assert.IsTrue(restored.ContainsKey("a"));
            Assert.IsTrue(restored.ContainsKey("b"));
            Assert.IsTrue(restored.ContainsKey("c"));
            Assert.IsTrue(foundValue);
            Assert.AreEqual("B", value.Name);
            Assert.IsTrue(foundEntry);
            Assert.AreSame(restored[1], entry);

            restored.Add(15, "inserted", new TestElement("Inserted"));

            Assert.AreEqual("inserted", restored[1].Key);
            Assert.IsTrue(restored.ContainsKey("inserted"));
        }

    #endregion

    #region S-3: 관리 참조 공유 상태 보존

        [Test]
        public void TEST_XOrdered_Keyed_JSON_SerializeReference_공유참조_보존()
        {
            // Arrange
            var shared = new TestElement("Shared");
            var original = new XOrdered<int, string, TestElement>();

            original.Add(10, "a", shared);
            original.Add(20, "b", shared);

            // Act
            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<XOrdered<int, string, TestElement>>(json);

            // Assert
            Assert.AreEqual(2, restored.Count);
            Assert.AreSame(restored[0].Value, restored[1].Value);
            Assert.AreEqual("Shared", restored[0].Value.Name);
            Assert.IsTrue(restored.TryGetValue("a", out var first));
            Assert.IsTrue(restored.TryGetValue("b", out var second));
            Assert.AreSame(first, second);
        }

    #endregion

    }
}
