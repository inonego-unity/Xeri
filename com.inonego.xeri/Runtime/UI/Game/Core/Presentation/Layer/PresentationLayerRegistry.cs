/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationLayerRegistry.cs
수정일 : 2026-07-30

# 설명
stable string ID로 Presentation Layer를 등록하고 조회하며 활성 소비자 수를 추적한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

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
            /// 등록 검증 시 확정한 Layer Root.
            /// </summary>
            // ------------------------------------------------------------
            public Transform Root { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 활성 소비자 수.
            /// </summary>
            // ------------------------------------------------------------
            public int ConsumerCount { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 이 등록 소유권을 나타내는 Handle.
            /// </summary>
            // ------------------------------------------------------------
            public PresentationLayerHandle Handle { get; set; }

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
                IPresentationLayerDriver driver,
                Transform root
            ) : base()
            {
                Asset = asset ?? throw new ArgumentNullException(nameof(asset));
                Driver = driver ?? throw new ArgumentNullException(nameof(driver));
                Root = root != null ? root : throw new ArgumentNullException(nameof(root));
                ConsumerCount = 0;
            }

        #endregion

        #region 메서드

            // ------------------------------------------------------------
            /// <summary>
            /// Layer 소비자 수명을 시작한다.
            /// </summary>
            // ------------------------------------------------------------
            public Lease AcquireUsage()
            {
                ConsumerCount++;
                return new Lease(ReleaseUsage);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Layer 소비자 수명을 종료한다.
            /// </summary>
            // ------------------------------------------------------------
            private void ReleaseUsage()
            {
                ConsumerCount--;
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

            var root = driver.Root;

            if (root == null)
            {
                throw new InvalidOperationException
                (
                    $"Presentation Layer '{asset.ID}' backend의 Root가 없습니다."
                );
            }

            ValidateOrder(asset, root);

            var entry = new Entry(asset, driver, root);

            try
            {
                // backend 활성화가 끝난 등록만 Registry에 공개한다.
                driver.SetActive(true);
                entries.Add(asset.ID, entry);
                ReorderSharedLayers();

                var handle = new PresentationLayerHandle(this, entry);
                entry.Handle = handle;
                return handle;
            }
            catch (Exception exception)
            {
                var errors = new List<Exception> { exception };

                // Registry에 공개한 등록만 제거하고 남은 Layer 순서를 복원한다.
                if (entries.TryGetValue(asset.ID, out var current) &&
                    ReferenceEquals(current, entry))
                {
                    entries.Remove(asset.ID);
                    entry.Handle?.MarkDisposed();
                    entry.Handle = null;

                    try
                    {
                        ReorderSharedLayers();
                    }
                    catch (Exception cleanupException)
                    {
                        errors.Add(cleanupException);
                    }
                }

                // 활성화가 일부 적용된 뒤 실패했을 수 있으므로 이번 backend를 비활성화한다.
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
            out Lease usage
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

            // Preflight가 끝났으므로 등록 소유권을 먼저 종료하고 외부 적용을 한 번씩 시도한다.
            ReleaseRegistration(entry);

            var errors = new List<Exception>();

            try
            {
                entry.Driver.SetActive(false);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }

            try
            {
                ReorderSharedLayers();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }

            if (errors.Count > 0)
            {
                throw new AggregateException
                (
                    $"Presentation Layer '{entry.Asset.ID}' 해제가 실패했습니다.",
                    errors
                );
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Layer 등록과 Handle의 소유 연결을 종료한다.
        /// <br/> Registry 전체 종료에서는 원본 목록을 순회한 뒤 한 번에 비우도록 등록 제거를 생략한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ReleaseRegistration
        (
            Entry entry,
            bool removeFromEntries = true
        )
        {
            if (removeFromEntries)
            {
                entries.Remove(entry.Asset.ID);
            }

            entry.Handle?.MarkDisposed();
            entry.Handle = null;
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
            Transform root
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
                    current.Root.parent != root.parent)
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
        private void ReorderSharedLayers()
        {
            if (isDisposed) return;

            var shared = new List<Entry>();

            foreach (var pair in entries)
            {
                if (pair.Value.Asset.Mode == PresentationLayerMode.Shared)
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
                shared[i].Root.SetSiblingIndex(i);
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

            isDisposed = true;
            var errors = new List<Exception>();

            // 외부 비활성화 콜백 전에 모든 등록 Handle을 함께 Terminal로 전환한다.
            foreach (var entry in entries.Values)
            {
                ReleaseRegistration(entry, removeFromEntries: false);
            }

            try
            {
                foreach (var entry in entries.Values)
                {
                    try
                    {
                        entry.Driver.SetActive(false);
                    }
                    catch (Exception exception)
                    {
                        errors.Add(exception);
                    }
                }
            }
            finally
            {
                entries.Clear();
            }

            if (errors.Count > 0)
            {
                throw new AggregateException("Presentation Layer Registry 해제가 실패했습니다.", errors);
            }
        }

    #endregion

    }
}
