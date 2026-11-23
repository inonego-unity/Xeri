/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_ValueAllocation.cs
수정일 : 2026-11-22

# 설명
MValue의 반복 갱신 경로(Set, Modifier 값 변경, 묶음 갱신, 같은 구성 복제)가 GC 할당을 만들지 않는지 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.

# 특이사항, 제약사항
Unity Test Framework의 AllocatingGCMemory 제약을 쓰므로 Unity 밖에서는 실행할 수 없다.
첫 호출의 JIT·버퍼 할당을 빼기 위해 측정 전에 같은 동작을 한 번 실행한다.
========================================================================= BLOCK_HEADER_END */

using System;

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
    /// MValue 반복 갱신 경로의 GC 할당 테스트.
    /// </summary>
    // ============================================================
    public class TEST_ValueAllocation
    {

    #region 할당

        // ------------------------------------------------------------
        /// <summary>
        /// Base 설정은 할당하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueAllocation_Set_할당없음()
        {
            var value = new MValue<float>(0f);
            var next = 0f;

            value.AddModifier("mul", new NumericFModifier(NumericFOperation.MUL, 2f));
            value.Set(1f);

            Assert.That(() => value.Set(next += 1f), Is.Not.AllocatingGCMemory());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 등록된 Modifier 값 변경은 할당하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueAllocation_Modifier값변경_할당없음()
        {
            var value = new MValue<Vector3>(Vector3.one);
            var push = new Vector3FModifier(NumericFOperation.ADD, Vector3.zero);
            var next = 0f;

            value.AddModifier("push", push);
            push.Value = Vector3.right;

            // 할당 검사는 반환값 없는 TestDelegate만 받으므로 대입을 문 블록으로 감싼다.
            Assert.That(() => { push.Value = new Vector3(next += 1f, 0f, 0f); }, Is.Not.AllocatingGCMemory());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 묶음 범위 안의 여러 변경은 할당하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueAllocation_묶음갱신_할당없음()
        {
            var value = new MValue<Vector3>(Vector3.zero);
            var push = new Vector3FModifier(NumericFOperation.ADD, Vector3.zero);
            var scale = new Vector3FModifier(NumericFOperation.MUL, Vector3.one);
            var next = 0f;

            value.AddModifier("push", push, order: 0);
            value.AddModifier("scale", scale, order: 1);

            void Step()
            {
                next += 1f;

                using (value.BeginBatch())
                {
                    value.Set(new Vector3(next, 0f, 0f));
                    push.Value = new Vector3(0f, next, 0f);
                    scale.Value = new Vector3(next, next, next);
                }
            }

            Step();

            Assert.That(Step, Is.Not.AllocatingGCMemory());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 구성끼리의 저장·복원은 할당하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueAllocation_같은구성복제_할당없음()
        {
            var live = new MValue<Vector3>(Vector3.one);
            var push = new Vector3FModifier(NumericFOperation.ADD, Vector3.one);

            live.AddModifier("push", push, order: 0);
            live.AddModifier("scale", new Vector3FModifier(NumericFOperation.MUL, Vector3.one), order: 1);

            var snapshot = new MValue<Vector3>();
            var next = 0f;

            void RoundTrip()
            {
                snapshot.CloneFrom(live);
                push.Value = new Vector3(next += 1f, 0f, 0f);
                live.CloneFrom(snapshot);
            }

            RoundTrip();

            Assert.That(RoundTrip, Is.Not.AllocatingGCMemory());
        }

    #endregion

    }
}
