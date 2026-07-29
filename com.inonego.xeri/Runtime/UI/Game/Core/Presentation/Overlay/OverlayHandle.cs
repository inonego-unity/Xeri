/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : OverlayHandle.cs
수정일 : 2026-07-29

# 설명
Overlay View와 Presentation Layer 사용 수명을 정확히 한 번 반환하는 Handle을 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Overlay View 표시 수명 Handle.
    /// </summary>
    // ============================================================
    public sealed class OverlayHandle<TView> : IDisposable
    where TView : class
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 획득한 Overlay View.
        /// </summary>
        // ------------------------------------------------------------
        public TView View { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Overlay View가 반환됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDisposed => source == null;

        private IOverlaySource<TView> source = null;
        private IDisposable layerUsage = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Overlay View와 Layer 사용 수명을 묶는다.
        /// </summary>
        // ------------------------------------------------------------
        private OverlayHandle
        (
            IOverlaySource<TView> source,
            TView view,
            IDisposable layerUsage
        ) : base()
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            View = view ?? throw new ArgumentNullException(nameof(view));
            this.layerUsage = layerUsage ?? throw new ArgumentNullException(nameof(layerUsage));
        }

    #endregion

    #region 메서드

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Layer 사용 수명을 획득한 뒤 Overlay View를 생성하고,
        /// <br/> 실패 시 Layer 사용 수명을 즉시 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static OverlayHandle<TView> Acquire
        (
            PresentationLayerRegistry layerRegistry,
            string layerID,
            IOverlaySource<TView> source
        )
        {
            if (layerRegistry == null)
            {
                throw new ArgumentNullException(nameof(layerRegistry));
            }

            if (string.IsNullOrWhiteSpace(layerID))
            {
                throw new ArgumentException("Overlay Layer ID가 비어 있습니다.", nameof(layerID));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!layerRegistry.TryAcquireUsage(layerID, out var driver, out var usage))
            {
                throw new InvalidOperationException
                (
                    $"Overlay Layer '{layerID}'가 등록되어 있지 않습니다."
                );
            }

            try
            {
                var view = source.Acquire(driver.Root);

                if (view == null)
                {
                    throw new InvalidOperationException("Overlay Source가 null View를 반환했습니다.");
                }

                return new OverlayHandle<TView>(source, view, usage);
            }
            catch
            {
                // View 획득이 완료되지 않았으므로 Layer 사용 수명만 원래 상태로 되돌린다.
                usage.Dispose();
                throw;
            }
        }

    #endregion

    #region IDisposable

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Overlay View를 Source에 반환한 뒤 Layer 사용 수명을 종료한다.
        /// <br/> Source 반환 실패 시 소유권을 유지해 다음 Dispose에서 재시도한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Dispose()
        {
            if (source == null) return;

            // Source 반환 성공 전에는 Layer 사용 수명을 해제하지 않는다.
            source.Release(View);

            var usage = layerUsage;
            source = null;
            layerUsage = null;
            usage.Dispose();
        }

    #endregion

    }
}
