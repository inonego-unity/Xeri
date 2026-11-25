/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Revision.cs
수정일 : 2026-11-25

# 설명
Revision 값의 발급·비교 규칙과 RevisionMark의 현재성 판정을 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.

# 테스트 구성
 R: Revision 발급·원시 값·비교
 S: RevisionMark 캡처와 현재성
 A: 할당

# 특이사항, 제약사항
할당 검사는 Unity Test Framework의 AllocatingGCMemory 제약을 쓰므로 Unity 밖에서는 실행할 수 없다.
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

namespace inonego.Xeri.TEST.Core._Revision
{
    // ============================================================
    /// <summary>
    /// Revision과 RevisionMark 테스트.
    /// </summary>
    // ============================================================
    public class TEST_Revision
    {

    #region 헬퍼

        // ============================================================
        /// <summary>
        /// 변경할 때마다 번호를 올리는 테스트 소유자.
        /// </summary>
        // ============================================================
        private sealed class TestOwner : IRevisioned
        {
            internal Revision Value;

            Revision IRevisioned.Revision => Value;

            internal void Change()
            {
                Value = Value.Inc();
            }
        }

    #endregion

    #region R-1: 발급과 원시 값

        // ------------------------------------------------------------
        /// <summary>
        /// 기본값과 None은 같고 미발급을 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Revision_기본값은_None이고_미발급()
        {
            Revision value = default;

            Assert.IsTrue(value.IsNone);
            Assert.AreEqual(Revision.None, value);
            Assert.AreEqual(0UL, value.ToUInt64());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// None의 다음은 1이고 그다음부터 1씩 오른다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Revision_Inc는_None에서_1부터_1씩_오름()
        {
            var first = Revision.None.Inc();
            var second = first.Inc();

            Assert.IsFalse(first.IsNone);
            Assert.AreEqual(1UL, first.ToUInt64());
            Assert.AreEqual(2UL, second.ToUInt64());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Inc는 원본을 바꾸지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Revision_Inc는_원본을_바꾸지_않음()
        {
            var original = Revision.FromUInt64(5);

            var next = original.Inc();

            Assert.AreEqual(5UL, original.ToUInt64());
            Assert.AreEqual(6UL, next.ToUInt64());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 최댓값에서 Inc는 넘침 예외를 던진다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Revision_Inc는_최댓값에서_OverflowException()
        {
            var last = Revision.FromUInt64(ulong.MaxValue);

            Assert.Throws<OverflowException>(() => last.Inc());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원시 값 변환은 왕복해도 값이 보존된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Revision_원시값_왕복_보존()
        {
            var value = Revision.FromUInt64(123456789012UL);

            Assert.AreEqual(123456789012UL, value.ToUInt64());
            Assert.AreEqual(value, Revision.FromUInt64(value.ToUInt64()));
            Assert.IsTrue(Revision.FromUInt64(0).IsNone);
        }

    #endregion

    #region R-2: 비교

        // ------------------------------------------------------------
        /// <summary>
        /// 같음 비교와 해시 코드는 번호가 같으면 같다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Revision_같음비교와_해시는_번호가_같으면_같음()
        {
            var a = Revision.FromUInt64(7);
            var b = Revision.FromUInt64(7);
            var c = Revision.FromUInt64(8);

            Assert.IsTrue(a == b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a.Equals((object)b));
            Assert.IsFalse(a.Equals((object)"7"));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 순서 비교는 번호의 크기를 따르고 None이 가장 작다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Revision_순서비교는_번호_크기를_따르고_None이_가장_작음()
        {
            var low = Revision.FromUInt64(2);
            var high = Revision.FromUInt64(9);

            Assert.IsTrue(low < high);
            Assert.IsTrue(high > low);
            Assert.IsTrue(low <= low);
            Assert.IsTrue(high >= high);
            Assert.IsFalse(high < low);
            Assert.IsTrue(Revision.None < low);
            Assert.Less(low.CompareTo(high), 0);
            Assert.Greater(high.CompareTo(low), 0);
            Assert.AreEqual(0, low.CompareTo(low));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬하면 번호 순서가 된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Revision_정렬하면_번호_순서()
        {
            var list = new List<Revision>
            {
                Revision.FromUInt64(5), Revision.None, Revision.FromUInt64(2),
            };

            list.Sort();

            Assert.AreEqual(0UL, list[0].ToUInt64());
            Assert.AreEqual(2UL, list[1].ToUInt64());
            Assert.AreEqual(5UL, list[2].ToUInt64());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 문자열은 번호를 보여 준다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Revision_ToString은_번호를_보여줌()
        {
            Assert.AreEqual("42", Revision.FromUInt64(42).ToString());
            Assert.AreEqual("0", Revision.None.ToString());
        }

    #endregion

    #region S-1: RevisionMark

        // ------------------------------------------------------------
        /// <summary>
        /// 캡처 직후에는 현재이고 소유자가 바뀌면 현재가 아니다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_RevisionMark_캡처직후는_현재이고_소유자가_바뀌면_현재가_아님()
        {
            var owner = new TestOwner();
            owner.Change();

            var mark = RevisionMark.Capture(owner);

            Assert.IsTrue(mark.IsCurrent);

            owner.Change();

            Assert.IsFalse(mark.IsCurrent);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 바뀐 적 없는 소유자의 None 캡처는 바뀌기 전까지 현재이고 바뀌면 현재가 아니다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_RevisionMark_None캡처는_바뀌기전까지_현재()
        {
            var owner = new TestOwner();

            var mark = RevisionMark.Capture(owner);

            Assert.IsTrue(owner.Value.IsNone);
            Assert.IsTrue(mark.IsCurrent);

            owner.Change();

            Assert.IsFalse(mark.IsCurrent);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 번호가 같아도 다른 소유자에서 캡처한 Mark는 그 소유자와 대응하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_RevisionMark_다른소유자와는_번호가_같아도_대응하지_않음()
        {
            var a = new TestOwner();
            var b = new TestOwner();
            a.Change();
            b.Change();

            var mark = RevisionMark.Capture(a);

            Assert.AreEqual(a.Value, b.Value);
            Assert.IsTrue(mark.IsCapturedFrom(a));
            Assert.IsFalse(mark.IsCapturedFrom(b));

            // 다른 소유자가 바뀌어도 캡처한 소유자의 현재성에는 영향이 없다.
            b.Change();

            Assert.IsTrue(mark.IsCurrent);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 캡처하지 않은 default 값은 현재가 아니고 어떤 소유자와도 대응하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_RevisionMark_default는_현재가_아니고_대응하는_소유자가_없음()
        {
            var owner = new TestOwner();
            RevisionMark mark = default;

            Assert.IsFalse(mark.IsCurrent);
            Assert.IsFalse(mark.IsCapturedFrom(owner));
            Assert.IsFalse(mark.IsCapturedFrom(null));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// null 소유자는 캡처할 수 없다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_RevisionMark_null소유자는_캡처할수_없음()
        {
            Assert.Throws<ArgumentNullException>(() => RevisionMark.Capture(null));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 소유자가 바뀐 뒤 다시 캡처하면 다시 현재다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_RevisionMark_다시_캡처하면_다시_현재()
        {
            var owner = new TestOwner();
            var old = RevisionMark.Capture(owner);

            owner.Change();
            var fresh = RevisionMark.Capture(owner);

            Assert.IsFalse(old.IsCurrent);
            Assert.IsTrue(fresh.IsCurrent);
        }

    #endregion

    #region A: 할당

        // ------------------------------------------------------------
        /// <summary>
        /// 번호 발급과 Mark 캡처·판정은 할당하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Revision_발급과_Mark_판정은_할당없음()
        {
            var owner = new TestOwner();
            var current = Revision.None;

            void Run()
            {
                owner.Change();
                current = current.Inc();

                var mark = RevisionMark.Capture(owner);
                var consistent = mark.IsCurrent
                    && mark.IsCapturedFrom(owner)
                    && current == owner.Value
                    && current >= Revision.None;

                if (!consistent)
                {
                    throw new InvalidOperationException();
                }
            }

            // 첫 호출의 지연 초기화를 측정에서 뺀다.
            Run();

            Assert.That(Run, Is.Not.AllocatingGCMemory());
        }

    #endregion

    }
}
