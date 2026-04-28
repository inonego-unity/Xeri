/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : HP_IF.cs
수정일 : 2026-04-28

# 설명
HP<,> 추상 클래스의 int/float 구체 구현체.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Game
{
    using Primitive;

    // ============================================================
    /// <summary>
    /// int 기반 체력 구현 클래스.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class HP_I : HP<XNumericI, int>, IReadOnlyHP_I, IDeepCloneable<HP_I>
    {
        public HP_I @new() => new HP_I();

        public void CloneFrom(HP_I source) => CloneFrom((HP<XNumericI, int>)source);
    }

    // ============================================================
    /// <summary>
    /// float 기반 체력 구현 클래스.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class HP_F : HP<XNumericF, float>, IReadOnlyHP_F, IDeepCloneable<HP_F>
    {
        public HP_F @new() => new HP_F();

        public void CloneFrom(HP_F source) => CloneFrom((HP<XNumericF, float>)source);
    }

}
