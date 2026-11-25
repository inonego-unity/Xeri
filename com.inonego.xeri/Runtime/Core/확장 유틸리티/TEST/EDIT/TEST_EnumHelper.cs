/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_EnumHelper.cs
수정일 : 2026-11-25

# 설명
EnumHelper.IsDefined의 정의 여부 판정과 박싱 없음을 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.
========================================================================= BLOCK_HEADER_END */

using System;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Utility._EnumHelper
{
    using inonego;
    using inonego.Xeri;

    // ============================================================
    /// <summary>
    /// EnumHelper 정의 여부 테스트.
    /// </summary>
    // ============================================================
    public class TEST_EnumHelper
    {

    #region 테스트 enum

        private enum Contiguous
        {
            A,
            B,
            C
        }
        private enum Sparse
        {
            X = 1,
            Y = 5,
            Z = 100
        }
        private enum Negative
        {
            Minus = -1,
            Zero = 0,
            One = 1
        }
        private enum ByteEnum : byte
        {
            P = 1,
            Q = 2,
            Max = 255
        }
        private enum SByteEnum : sbyte
        {
            N = -2,
            M = 3
        }
        private enum ShortEnum : short
        {
            S = -300,
            T = 300
        }
        private enum UShortEnum : ushort
        {
            U = 1,
            V = 65535
        }
        private enum UIntEnum : uint
        {
            U = 1,
            V = 4000000000
        }
        private enum LongEnum : long
        {
            L1 = 1,
            L2 = 1L << 40,
            L3 = -(1L << 41)
        }
        private enum ULongEnum : ulong
        {
            U1 = 1,
            U2 = ulong.MaxValue
        }

        [Flags]
        private enum Bits
        {
            None = 0,
            A = 1,
            B = 2,
            C = 4,
            AB = 3
        }

        [Flags]
        private enum ByteBits : byte
        {
            A = 1,
            B = 2,
            High = 128
        }

        [Flags]
        private enum LongBits : long
        {
            A = 1,
            Big = 1L << 40
        }

        private static object sink;

    #endregion

    #region 일반 enum

        // ------------------------------------------------------------
        /// <summary>
        /// 이어진 값은 범위 안만 정의된 값으로 본다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_EnumHelper_IsDefined_이어진값은범위만참()
        {
            Assert.That(Contiguous.A.IsDefined(), Is.True);
            Assert.That(Contiguous.C.IsDefined(), Is.True);
            Assert.That(((Contiguous)3).IsDefined(), Is.False);
            Assert.That(((Contiguous)(-1)).IsDefined(), Is.False);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 값 사이에 빈틈이 있으면 이름 있는 값만 참이다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_EnumHelper_IsDefined_빈틈이있으면이름있는값만참()
        {
            Assert.That(Sparse.Y.IsDefined(), Is.True);
            Assert.That(Sparse.Z.IsDefined(), Is.True);
            Assert.That(((Sparse)2).IsDefined(), Is.False);
            Assert.That(((Sparse)0).IsDefined(), Is.False);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 음수를 포함한 enum도 범위를 올바르게 판정한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_EnumHelper_IsDefined_음수포함판정()
        {
            Assert.That(Negative.Minus.IsDefined(), Is.True);
            Assert.That(((Negative)(-2)).IsDefined(), Is.False);
            Assert.That(((Negative)2).IsDefined(), Is.False);
        }

    #endregion

    #region 기반 형식

        // ------------------------------------------------------------
        /// <summary>
        /// byte·sbyte·short·ushort 기반 enum의 경계 값을 판정한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_EnumHelper_IsDefined_작은정수기반형식()
        {
            Assert.That(ByteEnum.Max.IsDefined(), Is.True);
            Assert.That(((ByteEnum)3).IsDefined(), Is.False);
            Assert.That(((ByteEnum)0).IsDefined(), Is.False);
            Assert.That(SByteEnum.N.IsDefined(), Is.True);
            Assert.That(((SByteEnum)(-3)).IsDefined(), Is.False);
            Assert.That(ShortEnum.S.IsDefined(), Is.True);
            Assert.That(((ShortEnum)(-299)).IsDefined(), Is.False);
            Assert.That(UShortEnum.V.IsDefined(), Is.True);
            Assert.That(((UShortEnum)2).IsDefined(), Is.False);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// uint·long·ulong 기반 enum의 큰 값과 음수 값을 판정한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_EnumHelper_IsDefined_큰정수기반형식()
        {
            Assert.That(UIntEnum.V.IsDefined(), Is.True);
            Assert.That(((UIntEnum)3000000000).IsDefined(), Is.False);
            Assert.That(LongEnum.L2.IsDefined(), Is.True);
            Assert.That(LongEnum.L3.IsDefined(), Is.True);
            Assert.That(((LongEnum)7).IsDefined(), Is.False);
            Assert.That(ULongEnum.U2.IsDefined(), Is.True);
            Assert.That(((ULongEnum)5).IsDefined(), Is.False);
        }

    #endregion

    #region Flags

        // ------------------------------------------------------------
        /// <summary>
        /// [Flags] enum은 정의된 비트만 썼으면 이름 없는 조합과 0도 참이다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_EnumHelper_IsDefined_Flags는정의된비트만참()
        {
            Assert.That(Bits.None.IsDefined(), Is.True);
            Assert.That(((Bits)5).IsDefined(), Is.True);
            Assert.That(((Bits)7).IsDefined(), Is.True);
            Assert.That(((Bits)8).IsDefined(), Is.False);
            Assert.That(((Bits)9).IsDefined(), Is.False);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// byte·long 기반 [Flags] enum도 상위 비트까지 같은 규칙으로 판정한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_EnumHelper_IsDefined_Flags기반형식별판정()
        {
            Assert.That(((ByteBits)129).IsDefined(), Is.True);
            Assert.That(((ByteBits)4).IsDefined(), Is.False);
            Assert.That(((LongBits)((1L << 40) | 1)).IsDefined(), Is.True);
            Assert.That(((LongBits)(1L << 41)).IsDefined(), Is.False);
        }

    #endregion

    #region 박싱

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 정의 여부 판정은 형식 정보를 읽은 뒤에는 할당하지 않는다.
        /// <br/> 할당하는 대조군으로 측정이 살아 있음을 함께 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_EnumHelper_IsDefined_예열뒤할당없음()
        {
            // 형식 정보는 처음 한 번만 읽으므로 먼저 예열한다.
            _ = Contiguous.A.IsDefined();
            _ = Sparse.X.IsDefined();
            _ = ByteEnum.P.IsDefined();
            _ = LongEnum.L1.IsDefined();
            _ = ULongEnum.U1.IsDefined();
            _ = Bits.A.IsDefined();

            Assert.That(() => { sink = new byte[64]; }, new UnityEngine.TestTools.Constraints.AllocatingGCMemoryConstraint());
            Assert.That(() => { _ = Contiguous.B.IsDefined(); }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
            Assert.That(() => { _ = Sparse.Y.IsDefined(); }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
            Assert.That(() => { _ = ByteEnum.Q.IsDefined(); }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
            Assert.That(() => { _ = LongEnum.L3.IsDefined(); }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
            Assert.That(() => { _ = ULongEnum.U2.IsDefined(); }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
            Assert.That(() => { _ = Bits.AB.IsDefined(); }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
        }

    #endregion

    }
}
