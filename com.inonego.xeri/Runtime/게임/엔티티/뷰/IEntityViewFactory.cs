/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IEntityViewFactory.cs
수정일 : 2026-08-14

# 설명
Entity와 생성 Context를 받아 View를 획득·준비하고 terminal 반환하는 공용 수명 계약.
활성 Entity Key 매핑은 Factory가 아닌 EntityViewController가 소유한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.Game
{
    // ================================================================================
    /// <summary>
    /// <br/> Entity와 생성 Context에 대응하는 View를 생성하고 반환하는 계약.
    /// <br/> Release 호출은 성공 여부와 무관하게 호출자의 반환 책임을 종료한다.
    /// </summary>
    // ================================================================================
    public interface IEntityViewFactory<TEntityView, TEntity, TContext>
    where TEntityView : EntityViewBase<TEntity>
    where TEntity : class, IEntity
    {
        // ------------------------------------------------------------
        /// <summary>
        /// Entity와 생성 Context에 대응하는 View를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TEntityView Create(TEntity entity, in TContext context);

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> View를 지정 사유로 terminal 반환한다.
        /// <br/> 예외가 발생해도 같은 View의 반환을 다시 호출하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Release(TEntityView view, DespawnReason reason);
    }

    // ============================================================
    /// <summary>
    /// 생성 Context가 필요 없는 Entity View Factory용 빈 값.
    /// </summary>
    // ============================================================
    public readonly struct EntityViewNoContext
    {
        // NONE
    }
}
