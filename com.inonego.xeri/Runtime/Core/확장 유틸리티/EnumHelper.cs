/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EnumHelper.cs
수정일 : 2026-11-25

# 설명
enum 값이 정의된 값인지 박싱 없이 확인하는 확장 메서드 모음.
IsDefined를 제공한다.

# 특이사항, 제약사항
Enum.IsDefined(Type, object)는 값을 object로 받아 호출마다 박싱하므로 매 프레임·매 명령 경로에서 쓰지 않는다.
enum 형식마다 처음 한 번 정의된 값을 읽어 캐시하고, 이후 호출은 할당하지 않는다.
byte·sbyte·short·ushort·int·uint·long·ulong 기반 enum을 모두 지원한다.
[Flags] enum은 정의된 비트만 사용했으면 정의된 값으로 본다. 이름 없는 조합과 0도 통과하며,
이 점이 이름 있는 값만 인정하는 Enum.IsDefined와 다르다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// enum 확장 메서드 정적 헬퍼.
    /// </summary>
    // ============================================================
    public static class EnumHelper
    {

    #region 정의 여부

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 값이 enum에 정의된 값인지 박싱 없이 확인한다.
        /// <br/> 일반 enum은 이름이 있는 값만 참이다.
        /// <br/> [Flags] enum은 정의된 비트만 사용했으면 참이며 0과 이름 없는 조합도 포함한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDefined<T>(this T value) where T : unmanaged, Enum
        {
            return EnumCache<T>.Contains(value);
        }

    #endregion

    #region 형식별 캐시

        // ======================================================================
        /// <summary>
        /// <br/> enum 형식 하나에 정의된 값을 처음 한 번 읽어 판정 방식에 맞게 캐시한다.
        /// <br/> 형식마다 정적 필드가 따로 생기므로 형식 사이에 영향을 주지 않는다.
        /// </summary>
        // ======================================================================
        private static class EnumCache<T> where T : unmanaged, Enum
        {

        #region 필드

            // 기반 형식이 부호 있는 정수인지 여부. 값을 long으로 넓힐 때 부호를 유지한다.
            private static readonly bool isSigned;

            // [Flags] enum인지 여부. 참이면 정의된 비트 합(mask)으로 판정한다.
            private static readonly bool isFlags;
            private static readonly ulong mask;

            // 일반 enum의 정의된 값이 min부터 max까지 빈틈없이 이어지는지 여부. 참이면 범위로 판정한다.
            private static readonly bool isContiguous;
            private static readonly long min;
            private static readonly long max;

            // 일반 enum의 정의된 값 집합. 값이 이어지지 않을 때 조회한다.
            private static readonly HashSet<long> values = new();

        #endregion

        #region 생성자

            static EnumCache()
            {
                var underlying = Enum.GetUnderlyingType(typeof(T));
                isSigned = underlying == typeof(sbyte) || underlying == typeof(short)
                    || underlying == typeof(int) || underlying == typeof(long);
                isFlags = typeof(T).IsDefined(typeof(FlagsAttribute), false);

                long lowest = long.MaxValue;
                long highest = long.MinValue;
                bool exceedsLong = false;
                foreach (var defined in (T[])Enum.GetValues(typeof(T)))
                {
                    long bits = ToBits(defined);
                    values.Add(bits);
                    mask |= (ulong)bits;

                    // ulong의 상위 절반은 long으로 넓히면 음수가 되어 범위 비교를 쓸 수 없다.
                    if (!isSigned && bits < 0)
                    {
                        exceedsLong = true;
                    }

                    lowest = Math.Min(lowest, bits);
                    highest = Math.Max(highest, bits);
                }

                min = lowest;
                max = highest;
                isContiguous = !isFlags && !exceedsLong && (highest - lowest + 1) == values.Count;
            }

        #endregion

        #region 판정

            internal static bool Contains(T value)
            {
                long bits = ToBits(value);
                if (isFlags)
                {
                    return ((ulong)bits & ~mask) == 0;
                }

                return isContiguous ? (bits >= min && bits <= max) : values.Contains(bits);
            }

        #endregion

        #region 변환

            // 값을 박싱 없이 읽어 부호를 유지한 long으로 넓힌다. ulong은 비트 그대로 담는다.
            private static long ToBits(T value)
            {
                var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
                switch (bytes.Length)
                {
                    case 1:
                        return isSigned ? (sbyte)bytes[0] : bytes[0];
                    case 2:
                        return isSigned ? MemoryMarshal.Read<short>(bytes) : MemoryMarshal.Read<ushort>(bytes);
                    case 4:
                        return isSigned ? MemoryMarshal.Read<int>(bytes) : MemoryMarshal.Read<uint>(bytes);
                    default:
                        return isSigned
                            ? MemoryMarshal.Read<long>(bytes)
                            : unchecked((long)MemoryMarshal.Read<ulong>(bytes));
                }
            }

        #endregion

        }

    #endregion

    }
}
