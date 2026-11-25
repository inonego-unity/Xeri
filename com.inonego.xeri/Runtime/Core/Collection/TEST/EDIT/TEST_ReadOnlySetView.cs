/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_ReadOnlySetView.cs
수정일 : 2026-11-15

# 설명
ReadOnlySetView의 원본 집합 변경 반영, 외부 수정 차단과 구체 열거자 계약을 검증한다.

# 테스트 구성
 V: 읽기 전용 Live View
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Core._Collection
{
    // ============================================================
    /// <summary>
    /// 읽기 전용 Set View의 런타임 조회 계약을 확인한다.
    /// </summary>
    // ============================================================
    public class TEST_ReadOnlySetView
    {

    #region V: 읽기 전용 Live View

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 변경을 복사 없이 조회하고 수정 인터페이스를 숨긴다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ReadOnlySetView_원본변경을반영하고_수정권한을노출하지않음()
        {
            var source = new HashSet<int>
            {
                1
            };
            var view = new ReadOnlySetView<int>(source);

            Assert.AreEqual(1, view.Count);
            Assert.IsTrue(view.Contains(1));
            Assert.IsFalse(((object)view) is ICollection<int>);
            Assert.IsFalse(((object)view) is HashSet<int>);

            source.Add(2);
            Assert.AreEqual(2, view.Count);
            Assert.IsTrue(view.Contains(2));

            source.Remove(1);
            Assert.IsFalse(view.Contains(1));

            source.Clear();
            Assert.AreEqual(0, view.Count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구체 열거는 struct Enumerator를 사용하며 모든 요소를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ReadOnlySetView_구체열거는_HashSetEnumerator를사용()
        {
            var source = new HashSet<int>
            {
                2,
                4,
                6
            };
            var view = new ReadOnlySetView<int>(source);
            HashSet<int>.Enumerator enumerator = view.GetEnumerator();

            var values = new HashSet<int>();

            while (enumerator.MoveNext())
            {
                values.Add(enumerator.Current);
            }

            CollectionAssert.AreEquivalent(source, values);
            CollectionAssert.AreEquivalent(source, (IEnumerable<int>)view);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// null 원본은 허용하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ReadOnlySetView_Null원본을거절()
        {
            Assert.Throws<ArgumentNullException>
            (
                () => new ReadOnlySetView<object>(null)
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> default View는 빈 집합처럼 동작하고
        /// <br/> var로 받은 View의 foreach는 힙을 할당하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ReadOnlySetView_default는빈집합이고_foreach는할당하지않음()
        {
            var empty = default(ReadOnlySetView<int>);
            var source = new HashSet<int>
            {
                1,
                2,
                3
            };
            var view = new ReadOnlySetView<int>(source);
            var count = 0;

            Assert.AreEqual(0, empty.Count);
            Assert.IsFalse(empty.Contains(1));

            foreach (var item in empty)
            {
                count++;
            }

            Assert.AreEqual(0, count);

            // 계측이 실제로 할당을 잡는지 먼저 확인한다.
            var leaked = new List<object>();
            Assert.That(() => leaked.Add(new object()), new UnityEngine.TestTools.Constraints.AllocatingGCMemoryConstraint());

            foreach (var item in view)
            {
                count++;
            }

            Assert.AreEqual(3, count);

            Assert.That
            (
                () =>
                {
                    foreach (var item in view)
                    {
                        count++;
                    }
                },
                UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not)
            );
        }

    #endregion

    }
}
