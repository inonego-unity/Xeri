/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GameUIProfileHandle.cs
수정일 : 2026-07-29

# 설명
Profile이 획득한 Layer 등록과 Provider GameObject의 대칭 수명을 생성 역순으로 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Game UI Profile 획득 수명 Handle.
    /// </summary>
    // ============================================================
    public sealed class GameUIProfileHandle : IDisposable
    {
    #region 내부 데이터

        internal sealed class Entry
        {
            public IGameObjectProvider Provider = null;
            public GameObject Instance = null;
            public PresentationLayerHandle LayerHandle = null;
            public bool LayerReleased = true;
            public bool InstanceReleased = false;
        }

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Profile Handle의 모든 소유 리소스가 반환됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDisposed { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Handle이 획득한 Profile Asset.
        /// </summary>
        // ------------------------------------------------------------
        public GameUIProfileAsset Profile { get; }

        private readonly List<Entry> entries = new List<Entry>();
        private Action<GameUIProfileHandle> onDisposed = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 준비 중 Profile Handle을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        internal GameUIProfileHandle
        (
            GameUIProfileAsset profile,
            Action<GameUIProfileHandle> onDisposed
        ) : base()
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            this.onDisposed = onDisposed;
            IsDisposed = false;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Profile 획득 중 생성한 Provider 인스턴스 소유권을 즉시 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        internal Entry Add
        (
            IGameObjectProvider provider,
            GameObject instance
        )
        {
            var entry = new Entry
            {
                Provider = provider ?? throw new ArgumentNullException(nameof(provider)),
                Instance = instance ?? throw new ArgumentNullException(nameof(instance)),
            };

            entries.Add(entry);
            return entry;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Entry에 완료된 Layer 등록 Handle을 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void SetLayerHandle
        (
            Entry entry,
            PresentationLayerHandle layerHandle
        )
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            entry.LayerHandle = layerHandle ?? throw new ArgumentNullException(nameof(layerHandle));
            entry.LayerReleased = false;
        }

    #endregion

    #region IDisposable

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 활성 Layer 소비자가 없는지 먼저 검증한 뒤 등록과 Provider 인스턴스를 역순 반환한다.
        /// <br/> Provider 반환 실패 Entry는 소유권을 유지해 다음 Dispose에서 재시도한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Dispose()
        {
            if (IsDisposed) return;

            // 일부 반환 전에 모든 활성 소비자를 검증해 잘못된 호출 순서가 상태를 훼손하지 않게 한다.
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var layerHandle = entries[i].LayerHandle;

                if (!entries[i].LayerReleased &&
                    layerHandle != null &&
                    layerHandle.HasConsumers)
                {
                    throw new InvalidOperationException
                    (
                        $"Game UI Profile Layer '{layerHandle.ID}'에 활성 소비자가 남아 있습니다."
                    );
                }
            }

            var errors = new List<Exception>();

            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];

                if (!entry.LayerReleased && entry.LayerHandle != null)
                {
                    try
                    {
                        entry.LayerHandle.Dispose();
                        entry.LayerReleased = true;
                    }
                    catch (Exception exception)
                    {
                        errors.Add(exception);
                    }
                }

                if (!entry.InstanceReleased && entry.LayerReleased)
                {
                    try
                    {
                        entry.Provider.Release(entry.Instance, false);
                        entry.InstanceReleased = true;
                    }
                    catch (Exception exception)
                    {
                        errors.Add(exception);
                    }
                }
            }

            var allReleased = true;

            for (var i = 0; i < entries.Count; i++)
            {
                if (!entries[i].LayerReleased || !entries[i].InstanceReleased)
                {
                    allReleased = false;
                    break;
                }
            }

            if (allReleased)
            {
                IsDisposed = true;
                var callback = onDisposed;
                onDisposed = null;
                callback?.Invoke(this);
            }

            if (errors.Count > 0)
            {
                throw new AggregateException("Game UI Profile 해제 중 하나 이상의 반환이 실패했습니다.", errors);
            }
        }

    #endregion

    }
}
