/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_ReadOnlyListView.cs
수정일 : 2026-11-25

# 설명
ReadOnlyListView의 원본 목록 변경 반영, 외부 수정 차단, 구체 열거자와 할당 계약을 검증한다.

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
    /// 읽기 전용 List View의 런타임 조회 계약을 확인한다.
    /// </summary>
    // ============================================================
    public class TEST_ReadOnlyListView
    {

    #region V: 읽기 전용 Live View

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 변경을 복사 없이 조회하고 수정 인터페이스를 숨긴다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ReadOnlyListView_원본변경을반영하고_수정권한을노출하지않음()
        {
            var source = new List<int>
            {
                1
            };
            var view = new ReadOnlyListView<int>(source);

            Assert.AreEqual(1, view.Count);
            Assert.AreEqual(1, view[0]);
            Assert.IsFalse(((object)view) is IList<int>);
            Assert.IsFalse(((object)view) is List<int>);

            source.Add(2);
            Assert.AreEqual(2, view.Count);
            Assert.AreEqual(2, view[1]);

            source.RemoveAt(0);
            Assert.AreEqual(1, view.Count);
            Assert.AreEqual(2, view[0]);

            source.Clear();
            Assert.AreEqual(0, view.Count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 범위 밖 위치는 ArgumentOutOfRangeException으로 거절한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ReadOnlyListView_범위밖위치를거절()
        {
            var source = new List<int>
            {
                1,
                2
            };
            var view = new ReadOnlyListView<int>(source);

            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = view[2]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = view[-1]; });
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구체 열거는 struct Enumerator로 모든 요소를 순서대로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ReadOnlyListView_구체열거는_struct열거자로순서대로반환()
        {
            var source = new List<int>
            {
                2,
                4,
                6
            };
            var view = new ReadOnlyListView<int>(source);
            ReadOnlyListView<int>.Enumerator enumerator = view.GetEnumerator();

            var values = new List<int>();

            while (enumerator.MoveNext())
            {
                values.Add(enumerator.Current);
            }

            CollectionAssert.AreEqual(source, values);
            CollectionAssert.AreEqual(source, (IEnumerable<int>)view);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 원소 타입을 넓힌 목록도 감쌀 수 있으며
        /// <br/> var로 받은 View의 foreach는 힙을 할당하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ReadOnlyListView_넓힌원소타입을감싸고_foreach는할당하지않음()
        {
            var source = new List<string>
            {
                "a",
                "b",
                "c"
            };
            var view = new ReadOnlyListView<object>(source);
            var count = 0;

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

        // ------------------------------------------------------------
        /// <summary>
        /// default View는 빈 목록처럼 동작하고 null 원본은 허용하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ReadOnlyListView_default는빈목록이고_Null원본을거절()
        {
            var empty = default(ReadOnlyListView<int>);
            var count = 0;

            foreach (var item in empty)
            {
                count++;
            }

            Assert.AreEqual(0, empty.Count);
            Assert.AreEqual(0, count);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = empty[0]; });
            Assert.Throws<ArgumentNullException>
            (
                () => new ReadOnlyListView<object>(null)
            );
        }

    #endregion

    }
}
