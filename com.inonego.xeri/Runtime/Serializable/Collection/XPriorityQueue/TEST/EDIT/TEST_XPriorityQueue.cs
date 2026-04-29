/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_XPriorityQueue.cs
수정일 : 2026-04-29

# 설명
XPriorityQueue<TElement, TPriority>의 핵심 기능 테스트.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

using NUnit.Framework;

using inonego.Xeri.Serializable;

// ============================================================================
/// <summary>
/// XPriorityQueue 컬렉션의 핵심 기능 테스트 클래스.
/// </summary>
// ============================================================================
public class TEST_XPriorityQueue
{

#region XPriorityQueue 기본 기능 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// XPriorityQueue의 기본 생성 및 초기 상태를 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void XPriorityQueue_01_기본_생성_테스트()
    {
        // Arrange & Act
        var pq = new XPriorityQueue<TestElement, int>();

        // Assert
        Assert.AreEqual(0, pq.Count);
    }

    // -------------------------------------------------------------------------
    /// <summary>
    /// <br/> XPriorityQueue의 모든 기능을 통합 테스트한다.
    /// <br/> Enqueue, Dequeue, Peek, Contains, Clear, 동일 우선순위 처리.
    /// </summary>
    // -------------------------------------------------------------------------
    [Test]
    public void XPriorityQueue_02_통합_기능_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var pq = new XPriorityQueue<TestElement, int>();

        // ------------------------------------------------------------
        // Enqueue - 우선순위가 다른 요소들 추가
        // ------------------------------------------------------------
        pq.Enqueue(new TestElement("Low"), 10);
        pq.Enqueue(new TestElement("High"), 100);
        pq.Enqueue(new TestElement("Medium"), 50);

        Assert.AreEqual(3, pq.Count);

        // ------------------------------------------------------------
        // Peek - 제거하지 않고 확인
        // ------------------------------------------------------------
        var (peekElement, peekPriority) = pq.Peek();
        Assert.AreEqual("High", peekElement.Name, "가장 높은 우선순위가 Peek되어야 합니다");
        Assert.AreEqual(100, peekPriority);
        Assert.AreEqual(3, pq.Count, "Peek은 요소를 제거하지 않아야 합니다");

        // ------------------------------------------------------------
        // Contains - 존재 확인
        // ------------------------------------------------------------
        Assert.IsTrue(pq.Contains(peekElement), "Enqueue한 요소는 Contains로 찾을 수 있어야 합니다");
        var nonExistent = new TestElement("NonExistent");
        Assert.IsFalse(pq.Contains(nonExistent), "존재하지 않는 요소는 false를 반환해야 합니다");

        // ------------------------------------------------------------
        // Dequeue - 높은 우선순위(큰 값)부터 나와야 함
        // ------------------------------------------------------------
        var (element1, priority1) = pq.Dequeue();
        Assert.AreEqual("High", element1.Name);
        Assert.AreEqual(100, priority1);
        Assert.AreEqual(2, pq.Count);

        var (element2, priority2) = pq.Dequeue();
        Assert.AreEqual("Medium", element2.Name);
        Assert.AreEqual(50, priority2);
        Assert.AreEqual(1, pq.Count);

        var (element3, priority3) = pq.Dequeue();
        Assert.AreEqual("Low", element3.Name);
        Assert.AreEqual(10, priority3);
        Assert.AreEqual(0, pq.Count);

        // ------------------------------------------------------------
        // 동일 우선순위 처리 테스트
        // ------------------------------------------------------------
        pq.Enqueue(new TestElement("First-50"), 50);
        pq.Enqueue(new TestElement("Second-50"), 50);
        pq.Enqueue(new TestElement("VeryHigh"), 200);
        pq.Enqueue(new TestElement("VeryLow"), 5);

        Assert.AreEqual(4, pq.Count);

        var (veryHigh, veryHighPriority) = pq.Dequeue();
        Assert.AreEqual("VeryHigh", veryHigh.Name);
        Assert.AreEqual(200, veryHighPriority);

        var (same1, samePriority1) = pq.Dequeue();
        Assert.AreEqual(50, samePriority1);

        var (same2, samePriority2) = pq.Dequeue();
        Assert.AreEqual(50, samePriority2);

        var (veryLow, veryLowPriority) = pq.Dequeue();
        Assert.AreEqual("VeryLow", veryLow.Name);
        Assert.AreEqual(5, veryLowPriority);

        // ------------------------------------------------------------
        // Clear - 채운 후 전체 제거
        // ------------------------------------------------------------
        pq.Enqueue(new TestElement("A"), 1);
        pq.Enqueue(new TestElement("B"), 2);
        pq.Enqueue(new TestElement("C"), 3);
        Assert.AreEqual(3, pq.Count);

        pq.Clear();
        Assert.AreEqual(0, pq.Count, "Clear 후 Count는 0이어야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// XPriorityQueue를 대량 데이터로 스트레스 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void XPriorityQueue_03_대량_데이터_스트레스_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비 - 100개 요소
        // ------------------------------------------------------------
        var pq = new XPriorityQueue<TestElement, int>();
        const int lTestCount = 100;

        // ------------------------------------------------------------
        // 역순으로 Enqueue (높은 우선순위부터 낮은 순으로)
        // ------------------------------------------------------------
        for (int i = lTestCount; i > 0; i--)
        {
            pq.Enqueue(new TestElement($"Element-{i}"), i);
        }

        Assert.AreEqual(lTestCount, pq.Count);

        // ------------------------------------------------------------
        // Dequeue - 내림차순으로 나와야 함 (100 → 99 → ... → 1)
        // ------------------------------------------------------------
        int expectedPriority = lTestCount;
        while (pq.Count > 0)
        {
            var (_, priority) = pq.Dequeue();
            Assert.AreEqual(expectedPriority, priority, $"Priority {expectedPriority}가 예상되지만 {priority}가 나왔습니다");
            expectedPriority--;
        }

        Assert.AreEqual(0, pq.Count);
        Assert.AreEqual(0, expectedPriority, "모든 요소가 올바른 순서로 Dequeue되었어야 합니다");

        // ------------------------------------------------------------
        // 랜덤 순서로 Enqueue 후 정렬 확인
        // ------------------------------------------------------------
        var random = new System.Random(42); // 고정 시드로 재현 가능
        var priorities = new List<int>();

        for (int i = 0; i < lTestCount; i++)
        {
            int p = random.Next(1, 1000);
            priorities.Add(p);
            pq.Enqueue(new TestElement($"Random-{i}"), p);
        }

        priorities.Sort((a, b) => b.CompareTo(a));

        for (int i = 0; i < lTestCount; i++)
        {
            var (_, priority) = pq.Dequeue();
            Assert.AreEqual(priorities[i], priority, $"인덱스 {i}에서 우선순위가 일치해야 합니다");
        }

        Assert.AreEqual(0, pq.Count);

        // ------------------------------------------------------------
        // Enqueue/Dequeue 혼합 테스트
        // ------------------------------------------------------------
        for (int i = 0; i < 50; i++)
        {
            pq.Enqueue(new TestElement($"Mix-{i}"), i * 2);
        }

        for (int i = 0; i < 25; i++)
        {
            pq.Dequeue();
        }

        Assert.AreEqual(25, pq.Count);

        for (int i = 50; i < 100; i++)
        {
            pq.Enqueue(new TestElement($"Mix-{i}"), i * 2);
        }

        Assert.AreEqual(75, pq.Count);

        int prevPriority = int.MaxValue;
        while (pq.Count > 0)
        {
            var (_, priority) = pq.Dequeue();
            Assert.LessOrEqual(priority, prevPriority, "우선순위는 항상 감소하거나 같아야 합니다");
            prevPriority = priority;
        }
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 빈 XPriorityQueue에서 Dequeue/Peek 시 예외 발생을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void XPriorityQueue_04_빈_큐_예외_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var pq = new XPriorityQueue<TestElement, int>();

        Assert.Throws<InvalidOperationException>(() => pq.Dequeue());
        Assert.Throws<InvalidOperationException>(() => pq.Peek());
    }

#endregion

#region XPriorityQueue 직렬화 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// XPriorityQueue의 JSON 직렬화/역직렬화를 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void XPriorityQueue_05_JSON_직렬화_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var original = new XPriorityQueue<TestElement, int>();
        original.Enqueue(new TestElement("Low"), 10);
        original.Enqueue(new TestElement("High"), 100);
        original.Enqueue(new TestElement("Medium"), 50);

        // ------------------------------------------------------------
        // JSON 직렬화/역직렬화 - 상태 복원 확인
        // ------------------------------------------------------------
        string json = JsonUtility.ToJson(original);
        var deserialized = JsonUtility.FromJson<XPriorityQueue<TestElement, int>>(json);

        Assert.AreEqual(original.Count, deserialized.Count);

        // ------------------------------------------------------------
        // 역직렬화 후 Dequeue 동작 확인 - 우선순위 순서 유지
        // ------------------------------------------------------------
        var (element1, priority1) = deserialized.Dequeue();
        Assert.AreEqual("High", element1.Name);
        Assert.AreEqual(100, priority1);

        var (element2, priority2) = deserialized.Dequeue();
        Assert.AreEqual("Medium", element2.Name);
        Assert.AreEqual(50, priority2);

        var (element3, priority3) = deserialized.Dequeue();
        Assert.AreEqual("Low", element3.Name);
        Assert.AreEqual(10, priority3);
    }

#endregion

#region Helper Classes

    // ------------------------------------------------------------
    /// <summary>
    /// 테스트용 Element 클래스.
    /// </summary>
    // ------------------------------------------------------------
    [Serializable]
    private class TestElement : IEquatable<TestElement>
    {
        [SerializeField]
        public string Name;

        public TestElement() {}

        public TestElement(string name)
        {
            Name = name;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Name 기준 동등성 비교.
        /// </summary>
        // ------------------------------------------------------------
        public bool Equals(TestElement other)
        {
            if (other == null) return false;
            return Name == other.Name;
        }

        public override bool Equals(object obj) => obj is TestElement other && Equals(other);

        public override int GetHashCode() => Name?.GetHashCode() ?? 0;
    }

#endregion

}
