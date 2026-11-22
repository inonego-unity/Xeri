/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Modifier.cs
수정일 : 2026-11-22

# 설명
IModifier<T> 계약(Replaces, 복제)과 벡터 Modifier·벡터 연산 규칙을 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.

# 테스트 구성
 R: Replaces 보고
 C: Clone·CloneFrom 복제와 변경 알림
 V: 벡터 연산 규칙과 벡터 Modifier
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.TEST.Serializable._Value
{
    // ============================================================
    /// <summary>
    /// Modifier 계약과 벡터 Modifier 테스트.
    /// </summary>
    // ============================================================
    public class TEST_Modifier
    {

    #region R: Replaces

        // ------------------------------------------------------------
        /// <summary>
        /// 연산형 Modifier는 SET일 때만 입력을 덮어쓴다고 보고한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Modifier_Replaces는_SET만_true()
        {
            foreach (NumericFOperation operation in Enum.GetValues(typeof(NumericFOperation)))
            {
                var expected = operation == NumericFOperation.SET;

                Assert.AreEqual(expected, new NumericFModifier(operation, 1f).Replaces, $"float {operation}");
                Assert.AreEqual(expected, new Vector3FModifier(operation, Vector3.one).Replaces, $"Vector3 {operation}");
            }

            foreach (NumericIOperation operation in Enum.GetValues(typeof(NumericIOperation)))
            {
                var expected = operation == NumericIOperation.SET;

                Assert.AreEqual(expected, new NumericIModifier(operation, 1).Replaces, $"int {operation}");
                Assert.AreEqual(expected, new Vector2IModifier(operation, Vector2Int.one).Replaces, $"Vector2Int {operation}");
            }

            Assert.IsTrue(new BooleanModifier(BooleanOperation.SET, true).Replaces);
            Assert.IsFalse(new BooleanModifier(BooleanOperation.XOR, true).Replaces);
            Assert.IsTrue(new StringModifier(StringOperation.SET, "a").Replaces);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// LambdaModifier는 결과가 입력과 무관한지 알 수 없어 false다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Modifier_Replaces_Lambda는_false()
        {
            Assert.IsFalse(new LambdaModifier<int>(_ => 7).Replaces);
            Assert.IsFalse(new LambdaModifier<int>().Replaces);
        }

    #endregion

    #region C: 복제

        // ------------------------------------------------------------
        /// <summary>
        /// Clone은 같은 구체 타입의 독립된 인스턴스를 같은 상태로 만든다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Modifier_Clone은_같은타입_같은상태_독립인스턴스()
        {
            IModifier<float> source = new NumericFModifier(NumericFOperation.MUL, 0.5f);
            var clone = source.Clone();

            Assert.AreNotSame(source, clone);
            Assert.AreEqual(typeof(NumericFModifier), clone.GetType());
            Assert.AreEqual(NumericFOperation.MUL, ((NumericFModifier)clone).Operation);
            Assert.AreEqual(0.5f, ((NumericFModifier)clone).Value);

            // 복제본을 바꿔도 원본은 그대로다.
            ((NumericFModifier)clone).Value = 2f;

            Assert.AreEqual(0.5f, ((NumericFModifier)source).Value);

            AssertCloneMatches<int>(new NumericIModifier(NumericIOperation.ADD, 3), 10);
            AssertCloneMatches<bool>(new BooleanModifier(BooleanOperation.XOR, true), false);
            AssertCloneMatches<string>(new StringModifier(StringOperation.SET, "x"), "y");
            AssertCloneMatches<Vector2>(new Vector2FModifier(NumericFOperation.ADD, new Vector2(1f, 2f)), Vector2.one);
            AssertCloneMatches<Vector3>(new Vector3FModifier(NumericFOperation.MUL, new Vector3(1f, 2f, 3f)), Vector3.one);
            AssertCloneMatches<Vector4>(new Vector4FModifier(NumericFOperation.SUB, Vector4.one), Vector4.zero);
            AssertCloneMatches<Vector2Int>(new Vector2IModifier(NumericIOperation.MAX, new Vector2Int(2, -2)), Vector2Int.zero);
            AssertCloneMatches<Vector3Int>(new Vector3IModifier(NumericIOperation.MIN, new Vector3Int(2, -2, 0)), Vector3Int.one);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// CloneFrom은 상태가 바뀔 때만 OnChange를 한 번 발행한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Modifier_CloneFrom은_상태가바뀔때만_OnChange한번()
        {
            var target = new NumericFModifier(NumericFOperation.ADD, 1f);
            var changes = 0;

            target.OnChange += () => changes++;

            target.CloneFrom(new NumericFModifier(NumericFOperation.ADD, 1f));

            Assert.AreEqual(0, changes);

            target.CloneFrom(new NumericFModifier(NumericFOperation.MUL, 3f));

            Assert.AreEqual(1, changes);
            Assert.AreEqual(NumericFOperation.MUL, target.Operation);
            Assert.AreEqual(3f, target.Value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// CloneFrom은 null과 다른 구체 타입을 거절하고 상태를 바꾸지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Modifier_CloneFrom은_null과_다른타입_거절()
        {
            var target = new NumericFModifier(NumericFOperation.ADD, 1f);

            Assert.Throws<ArgumentNullException>(() => target.CloneFrom(null));
            Assert.Throws<ArgumentException>(() => target.CloneFrom(new LambdaModifier<float>(x => x)));
            Assert.Throws<ArgumentException>(() => new LambdaModifier<float>().CloneFrom(target));

            Assert.AreEqual(NumericFOperation.ADD, target.Operation);
            Assert.AreEqual(1f, target.Value);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// LambdaModifier 복제는 delegate를 공유하고, 바뀌면 OnChange를 발행한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_Modifier_Lambda복제는_delegate공유_변경시_OnChange()
        {
            Func<int, int> twice = x => x * 2;
            Func<int, int> thrice = x => x * 3;

            IModifier<int> source = new LambdaModifier<int>(twice);
            var clone = (LambdaModifier<int>)source.Clone();

            Assert.AreSame(twice, clone.Lambda);

            var changes = 0;

            clone.OnChange += () => changes++;
            clone.CloneFrom(new LambdaModifier<int>(twice));

            Assert.AreEqual(0, changes);

            clone.CloneFrom(new LambdaModifier<int>(thrice));

            Assert.AreEqual(1, changes);
            Assert.AreEqual(30, clone.Modify(10));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// MValue에 등록된 Modifier를 CloneFrom으로 바꾸면 Modified가 갱신된다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_Modifier_등록된Modifier_CloneFrom_MValue갱신()
        {
            var modifier = new NumericFModifier(NumericFOperation.ADD, 1f);
            var value = new MValue<float>(10f);

            value.AddModifier("a", modifier);

            Assert.AreEqual(11f, value.Modified);

            modifier.CloneFrom(new NumericFModifier(NumericFOperation.MUL, 2f));

            Assert.AreEqual(20f, value.Modified);
        }

    #endregion

    #region V: 벡터

        // ----------------------------------------------------------------------
        /// <summary>
        /// 벡터 Apply는 모든 연산에서 성분마다 스칼라 Apply와 같은 결과를 낸다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_Modifier_벡터Apply는_성분별_스칼라규칙과_같음()
        {
            var current = new Vector4(-7.5f, 0f, 3f, 10f);
            var operand = new Vector4(2f, 0f, float.NaN, -4f);

            foreach (NumericFOperation operation in Enum.GetValues(typeof(NumericFOperation)))
            {
                var expected = new Vector4
                (
                    operation.Apply(current.x, operand.x),
                    operation.Apply(current.y, operand.y),
                    operation.Apply(current.z, operand.z),
                    operation.Apply(current.w, operand.w)
                );

                AssertComponents(expected, operation.Apply(current, operand), 4, $"Vector4 {operation}");
                AssertComponents(expected, operation.Apply((Vector3)current, (Vector3)operand), 3, $"Vector3 {operation}");
                AssertComponents(expected, operation.Apply((Vector2)current, (Vector2)operand), 2, $"Vector2 {operation}");
            }

            var currentI = new Vector3Int(-7, 0, 10);
            var operandI = new Vector3Int(2, 0, -4);

            foreach (NumericIOperation operation in Enum.GetValues(typeof(NumericIOperation)))
            {
                var expected = new Vector3Int
                (
                    operation.Apply(currentI.x, operandI.x),
                    operation.Apply(currentI.y, operandI.y),
                    operation.Apply(currentI.z, operandI.z)
                );

                Assert.AreEqual(expected, operation.Apply(currentI, operandI), $"Vector3Int {operation}");
                Assert.AreEqual((Vector2Int)expected, operation.Apply((Vector2Int)currentI, (Vector2Int)operandI), $"Vector2Int {operation}");
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 벡터 Modifier는 MValue에서 Order 순으로 접힌다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_Modifier_벡터Modifier_MValue연동()
        {
            var value = new MValue<Vector3>(new Vector3(1f, 2f, 3f));
            var scale = new Vector3FModifier(NumericFOperation.MUL, new Vector3(2f, 2f, 2f));

            value.AddModifier("add", new Vector3FModifier(NumericFOperation.ADD, Vector3.one), order: 0);
            value.AddModifier("scale", scale, order: 1);

            Assert.AreEqual(new Vector3(4f, 6f, 8f), value.Modified);

            // Value 변경은 OnChange로 MValue에 전파된다.
            scale.Value = Vector3.zero;

            Assert.AreEqual(Vector3.zero, value.Modified);

            var cells = new MValue<Vector2Int>(new Vector2Int(5, 5));

            cells.AddModifier("cap", new Vector2IModifier(NumericIOperation.MIN, new Vector2Int(3, 9)));

            Assert.AreEqual(new Vector2Int(3, 5), cells.Modified);
        }

    #endregion

    #region 헬퍼

        private static void AssertCloneMatches<T>(IModifier<T> source, T input)
        {
            var clone = source.Clone();

            Assert.AreNotSame(source, clone);
            Assert.AreEqual(source.GetType(), clone.GetType());
            Assert.AreEqual(source.Modify(input), clone.Modify(input), source.GetType().Name);
            Assert.AreEqual(source.Replaces, clone.Replaces, source.GetType().Name);
        }

        private static void AssertComponents(Vector4 expected, Vector4 actual, int count, string message)
        {
            for (var index = 0; index < count; index++)
            {
                Assert.AreEqual(expected[index], actual[index], message);
            }
        }

    #endregion

    }
}
