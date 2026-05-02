/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ModifierEntry.cs
수정일 : 2026-05-03

# 설명
IReadOnlyMValue<T>.Modifiers 가 노출하는 항목 타입.
Order(적용 우선순위)와 Modifier(IModifier<T>) 쌍을 담는다.
XOrderedBase<int, IModifier<T>>.Pair 를 인터페이스 경계 밖으로 노출하지 않도록 격리한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// 수정자 목록의 항목 타입.
    /// </summary>
    // ============================================================
    public readonly struct ModifierEntry<T>
    {
        public readonly int          Order;
        public readonly IModifier<T> Modifier;

        public ModifierEntry(int order, IModifier<T> modifier)
        {
            Order    = order;
            Modifier = modifier;
        }
    }
}
