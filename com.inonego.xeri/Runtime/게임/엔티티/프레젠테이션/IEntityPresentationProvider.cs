/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IEntityPresentationProvider.cs
수정일 : 2026-08-14

# 설명
Entity View 수명에 Presentation 종류 하나의 공급·분리·최종 반환을 연결하는 공용 계약.
구체 Presentation의 독립 Despawn 정책과 source Provider routing은 구현이 소유한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.Game
{
    // ================================================================================
    /// <summary>
    /// Entity View에 Presentation 종류 하나를 공급하고 정리하는 수명 계약.
    /// </summary>
    // ================================================================================
    public interface IEntityPresentationProvider<TEntityView, TEntity>
    where TEntityView : EntityViewBase<TEntity>
    where TEntity : class, IEntity
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 새 View가 준비된 뒤 Presentation을 공급하고 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public void OnViewSpawned(TEntity entity, TEntityView view);

        // ------------------------------------------------------------
        /// <summary>
        /// View 반환 전에 Presentation 관계와 독립 수명을 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        public void OnViewReleasing
        (
            ulong entityKey,
            TEntityView view,
            DespawnReason reason
        );
    }
}
