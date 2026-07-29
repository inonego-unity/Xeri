/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ScreenRegistry.cs
수정일 : 2026-07-29

# 설명
Screen Options와 Source를 stable string ID로 등록하고 새 Open 조회를 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Screen 등록과 조회를 소유하는 Registry.
    /// </summary>
    // ============================================================
    public sealed class ScreenRegistry : IDisposable
    {
    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// Screen 등록 시점의 Options와 Source 참조를 묶는다.
        /// </summary>
        // ============================================================
        internal sealed class Entry
        {
        #region 필드

            // ------------------------------------------------------------
            /// <summary>
            /// Screen 등록 정책.
            /// </summary>
            // ------------------------------------------------------------
            public ScreenOptions Options { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// Screen View와 Presenter를 공급하는 Source.
            /// </summary>
            // ------------------------------------------------------------
            public IScreenSource Source { get; }

        #endregion

        #region 생성자

            // ------------------------------------------------------------
            /// <summary>
            /// Screen 등록 Entry를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public Entry
            (
                ScreenOptions options,
                IScreenSource source
            ) : base()
            {
                Options = options ?? throw new ArgumentNullException(nameof(options));
                Source = source ?? throw new ArgumentNullException(nameof(source));
            }

        #endregion

        }

    #endregion

    #region 필드

        private readonly PresentationLayerRegistry layerRegistry = null;
        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        private bool isDisposed = false;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Layer 등록 상태를 검증하는 Screen Registry를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public ScreenRegistry(PresentationLayerRegistry layerRegistry) : base()
        {
            this.layerRegistry = layerRegistry ?? throw new ArgumentNullException(nameof(layerRegistry));
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Screen Options와 Source를 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public ScreenRegistrationHandle Register
        (
            ScreenOptions options,
            IScreenSource source
        )
        {
            ThrowIfDisposed();

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!layerRegistry.Contains(options.LayerID))
            {
                throw new InvalidOperationException
                (
                    $"Screen '{options.ID}'의 Layer '{options.LayerID}'가 등록되어 있지 않습니다."
                );
            }

            if (entries.ContainsKey(options.ID))
            {
                throw new InvalidOperationException
                (
                    $"Screen '{options.ID}'가 이미 등록되어 있습니다."
                );
            }

            var entry = new Entry(options, source);
            entries.Add(options.ID, entry);
            return new ScreenRegistrationHandle(this, entry);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Screen ID가 새 Open 조회에 등록되어 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Contains(string id)
        {
            if (isDisposed || string.IsNullOrWhiteSpace(id)) return false;

            return entries.ContainsKey(id);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Controller가 사용할 Screen 등록 Entry를 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        internal bool TryGet
        (
            string id,
            out Entry entry
        )
        {
            if (isDisposed || string.IsNullOrWhiteSpace(id))
            {
                entry = null;
                return false;
            }

            return entries.TryGetValue(id, out entry);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 등록 Handle이 소유한 Entry를 새 조회에서 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Unregister(Entry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (isDisposed) return;

            if (entries.TryGetValue(entry.Options.ID, out var current) &&
                ReferenceEquals(current, entry))
            {
                entries.Remove(entry.Options.ID);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 해제된 Registry 사용을 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(ScreenRegistry));
            }
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 새 Screen 조회를 모두 제거하되 Source 자체는 해제하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            entries.Clear();
            isDisposed = true;
        }

    #endregion

    }
}
