/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationLayerRegistry.cs
수정일 : 2026-07-29

# 설명
stable string ID로 Presentation Layer를 등록하고 조회하며 활성 소비자 수를 추적한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Presentation Layer 등록과 조회를 소유하는 Registry.
    /// </summary>
    // ============================================================
    public sealed class PresentationLayerRegistry : IDisposable
    {
    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// 등록된 Layer와 활성 소비자 수를 보관한다.
        /// </summary>
        // ============================================================
        internal sealed class Entry
        {
        #region 필드

            // ------------------------------------------------------------
            /// <summary>
            /// Layer Asset.
            /// </summary>
            // ------------------------------------------------------------
            public PresentationLayerAsset Asset { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// Layer backend.
            /// </summary>
            // ------------------------------------------------------------
            public IPresentationLayerDriver Driver { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 활성 소비자 수.
            /// </summary>
            // ------------------------------------------------------------
            public int ConsumerCount { get; private set; }

        #endregion

        #region 생성자

            // ------------------------------------------------------------
            /// <summary>
            /// Layer 등록 Entry를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public Entry
            (
                PresentationLayerAsset asset,
                IPresentationLayerDriver driver
            ) : base()
            {
                Asset = asset ?? throw new ArgumentNullException(nameof(asset));
                Driver = driver ?? throw new ArgumentNullException(nameof(driver));
                ConsumerCount = 0;
            }

        #endregion

        #region 메서드

            // ------------------------------------------------------------
            /// <summary>
            /// Layer 소비자 수명을 시작한다.
            /// </summary>
            // ------------------------------------------------------------
            public IDisposable AcquireUsage()
            {
                ConsumerCount++;
                return new Usage(this);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Layer 소비자 수명을 종료한다.
            /// </summary>
            // ------------------------------------------------------------
            private void ReleaseUsage()
            {
                if (ConsumerCount <= 0)
                {
                    throw new InvalidOperationException("Presentation Layer 소비자 수가 이미 0입니다.");
                }

                ConsumerCount--;
            }

        #endregion

        #region 내부 데이터

            // ============================================================
            /// <summary>
            /// Entry 소비자 수를 정확히 한 번 반환하는 내부 Handle.
            /// </summary>
            // ============================================================
            private sealed class Usage : IDisposable
            {
                private Entry owner = null;

                // ------------------------------------------------------------
                /// <summary>
                /// Layer 사용 Handle을 생성한다.
                /// </summary>
                // ------------------------------------------------------------
                public Usage(Entry owner) : base()
                {
                    this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
                }

                // ------------------------------------------------------------
                /// <summary>
                /// Layer 소비자 수를 정확히 한 번 반환한다.
                /// </summary>
                // ------------------------------------------------------------
                public void Dispose()
                {
                    if (owner == null) return;

                    var current = owner;
                    owner = null;
                    current.ReleaseUsage();
                }
            }

        #endregion

        }

    #endregion

    #region 필드

        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        private bool isDisposed = false;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Layer Asset과 backend를 등록하고 소유권 Handle을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationLayerHandle Register
        (
            PresentationLayerAsset asset,
            IPresentationLayerDriver driver
        )
        {
            ThrowIfDisposed();

            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            asset.Validate();

            if (entries.ContainsKey(asset.ID))
            {
                throw new InvalidOperationException
                (
                    $"Presentation Layer '{asset.ID}'가 이미 등록되어 있습니다."
                );
            }

            if (!driver.Validate(asset, out var error))
            {
                throw new InvalidOperationException
                (
                    $"Presentation Layer '{asset.ID}' 구성이 유효하지 않습니다. {error}"
                );
            }

            if (driver.Root == null)
            {
                throw new InvalidOperationException
                (
                    $"Presentation Layer '{asset.ID}' backend의 Root가 없습니다."
                );
            }

            ValidateOrder(asset, driver);

            // Registry 공개 전 backend를 활성화해 조회 시 항상 사용 가능한 상태를 보장한다.
            driver.SetActive(true);
            var entry = new Entry(asset, driver);
            entries.Add(asset.ID, entry);

            try
            {
                ReorderSharedLayers();
            }
            catch (Exception exception)
            {
                var errors = new List<Exception> { exception };
                entries.Remove(asset.ID);

                try
                {
                    ReorderSharedLayers();
                }
                catch (Exception cleanupException)
                {
                    errors.Add(cleanupException);
                }

                try
                {
                    driver.SetActive(false);
                }
                catch (Exception cleanupException)
                {
                    errors.Add(cleanupException);
                }

                if (errors.Count == 1)
                {
                    throw;
                }

                throw new AggregateException
                (
                    $"Presentation Layer '{asset.ID}' 등록과 롤백이 실패했습니다.",
                    errors
                );
            }

            return new PresentationLayerHandle(this, entry);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// ID에 해당하는 Layer backend를 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGet
        (
            string id,
            out IPresentationLayerDriver driver
        )
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(id))
            {
                driver = null;
                return false;
            }

            if (!entries.TryGetValue(id, out var entry))
            {
                driver = null;
                return false;
            }

            driver = entry.Driver;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// ID에 해당하는 Layer가 등록되어 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Contains(string id)
        {
            if (isDisposed || string.IsNullOrWhiteSpace(id)) return false;

            return entries.ContainsKey(id);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Layer를 사용하는 내부 소비자 수명을 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        internal bool TryAcquireUsage
        (
            string id,
            out IPresentationLayerDriver driver,
            out IDisposable usage
        )
        {
            ThrowIfDisposed();

            if (!entries.TryGetValue(id, out var entry))
            {
                driver = null;
                usage = null;
                return false;
            }

            driver = entry.Driver;
            usage = entry.AcquireUsage();
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 등록 Handle이 소유한 Entry를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Unregister(Entry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (isDisposed) return;

            if (!entries.TryGetValue(entry.Asset.ID, out var current) ||
                !ReferenceEquals(current, entry))
            {
                return;
            }

            if (entry.ConsumerCount > 0)
            {
                throw new InvalidOperationException
                (
                    $"Presentation Layer '{entry.Asset.ID}'에 활성 소비자가 남아 있습니다."
                );
            }

            entry.Driver.SetActive(false);

            try
            {
                ReorderSharedLayers(entry);
            }
            catch (Exception exception)
            {
                var errors = new List<Exception> { exception };

                // Registry 소유권이 남아 있으므로 backend 활성 상태와 기존 정렬을 되돌린다.
                try
                {
                    entry.Driver.SetActive(true);
                }
                catch (Exception rollbackException)
                {
                    errors.Add(rollbackException);
                }

                try
                {
                    ReorderSharedLayers();
                }
                catch (Exception rollbackException)
                {
                    errors.Add(rollbackException);
                }

                if (errors.Count == 1)
                {
                    throw;
                }

                throw new AggregateException
                (
                    $"Presentation Layer '{entry.Asset.ID}' 해제와 롤백이 실패했습니다.",
                    errors
                );
            }

            // backend 비활성화와 남은 Layer 재정렬이 끝난 뒤 Registry 제거를 커밋한다.
            entries.Remove(entry.Asset.ID);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 같은 Canvas 모드에서 Order가 충돌하지 않는지 검증하고,
        /// <br/> 공유 Layer가 하나의 부모 계층에 속하는지 확인한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ValidateOrder
        (
            PresentationLayerAsset asset,
            IPresentationLayerDriver driver
        )
        {
            foreach (var pair in entries)
            {
                var current = pair.Value;

                if (current.Asset.Mode == asset.Mode &&
                    current.Asset.Order == asset.Order)
                {
                    throw new InvalidOperationException
                    (
                        $"Presentation Layer '{asset.ID}'의 Order({asset.Order})가 " +
                        $"'{current.Asset.ID}'와 충돌합니다."
                    );
                }

                if (asset.Mode == PresentationLayerMode.Shared &&
                    current.Asset.Mode == PresentationLayerMode.Shared &&
                    current.Driver.Root.parent != driver.Root.parent)
                {
                    throw new InvalidOperationException
                    (
                        $"공유 Presentation Layer '{asset.ID}'와 '{current.Asset.ID}'의 부모가 다릅니다."
                    );
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 살아 있는 모든 공유 Layer를 Order 정렬 키 순서로 다시 배치한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReorderSharedLayers(Entry excluded = null)
        {
            var shared = new List<Entry>();

            foreach (var pair in entries)
            {
                if (!ReferenceEquals(pair.Value, excluded) &&
                    pair.Value.Asset.Mode == PresentationLayerMode.Shared)
                {
                    shared.Add(pair.Value);
                }
            }

            shared.Sort
            (
                (left, right) => left.Asset.Order.CompareTo(right.Asset.Order)
            );

            for (var i = 0; i < shared.Count; i++)
            {
                shared[i].Driver.Root.SetSiblingIndex(i);
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
                throw new ObjectDisposedException(nameof(PresentationLayerRegistry));
            }
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 활성 소비자가 없을 때 모든 Layer 등록을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            // Registry 종료는 사용 중인 Layer를 암묵적으로 파괴하지 않는다.
            foreach (var pair in entries)
            {
                if (pair.Value.ConsumerCount > 0)
                {
                    throw new InvalidOperationException
                    (
                        $"Presentation Layer '{pair.Key}'에 활성 소비자가 남아 있습니다."
                    );
                }
            }

            foreach (var pair in entries)
            {
                pair.Value.Driver.SetActive(false);
            }

            entries.Clear();
            isDisposed = true;
        }

    #endregion

    }
}
