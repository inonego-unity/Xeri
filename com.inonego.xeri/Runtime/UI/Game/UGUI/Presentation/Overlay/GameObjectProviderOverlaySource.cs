/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GameObjectProviderOverlaySource.cs
수정일 : 2026-07-29

# 설명
기존 IGameObjectProvider를 Overlay View Source로 연결하고 Parent와 반환 소유권을 보존한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// IGameObjectProvider 기반 Overlay View Source.
    /// </summary>
    // ============================================================
    public sealed class GameObjectProviderOverlaySource<TView> : IOverlaySource<TView>, IDisposable
    where TView : class
    {
    #region 필드

        private readonly IGameObjectProvider provider = null;
        private readonly Dictionary<TView, GameObject> instances = new Dictionary<TView, GameObject>();
        private readonly List<GameObject> pendingRelease = new List<GameObject>();
        private bool isDisposed = false;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 GameObject Provider를 Overlay Source로 감싼다.
        /// </summary>
        // ------------------------------------------------------------
        public GameObjectProviderOverlaySource(IGameObjectProvider provider) : base()
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

    #endregion

    #region IOverlaySource

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Provider Parent를 요청 Layer로 잠시 바꿔 GameObject를 획득하고,
        /// <br/> 요청 View 계약을 구현한 Component를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public TView Acquire(Transform parent)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(GameObjectProviderOverlaySource<TView>));
            }

            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var previousParent = provider.Parent;
            GameObject instance = null;

            try
            {
                provider.Parent = parent;
                instance = provider.Acquire(false);
            }
            finally
            {
                provider.Parent = previousParent;
            }

            if (instance == null)
            {
                throw new InvalidOperationException("GameObject Provider가 null 인스턴스를 반환했습니다.");
            }

            var component = instance.GetComponent(typeof(TView)) as TView;

            if (component == null)
            {
                try
                {
                    provider.Release(instance, false);
                }
                catch (Exception releaseException)
                {
                    pendingRelease.Add(instance);
                    throw new AggregateException
                    (
                        $"획득한 GameObject에 {typeof(TView).Name} 구현이 없습니다.",
                        releaseException
                    );
                }

                throw new InvalidOperationException
                (
                    $"획득한 GameObject에 {typeof(TView).Name} 구현이 없습니다."
                );
            }

            if (instances.ContainsKey(component))
            {
                // 사용 중인 동일 인스턴스를 반환한 Provider의 소유 상태는 Source가 안전하게 보정할 수 없다.
                throw new InvalidOperationException("Provider가 이미 사용 중인 Overlay View를 다시 반환했습니다.");
            }

            instances.Add(component, instance);
            return component;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> View에 대응하는 GameObject를 원래 Provider에 반환한다.
        /// <br/> Provider 반환 실패 시 매핑과 소유권을 유지해 재시도할 수 있다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Release(TView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (!instances.TryGetValue(view, out var instance))
            {
                throw new InvalidOperationException("이 Source가 소유하지 않은 Overlay View입니다.");
            }

            provider.Release(instance, false);
            instances.Remove(view);
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 아직 반환되지 않은 Overlay와 실패 정리 인스턴스를 모두 Provider에 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            var errors = new List<Exception>();
            var views = new List<TView>(instances.Keys);

            for (var i = views.Count - 1; i >= 0; i--)
            {
                var view = views[i];

                try
                {
                    provider.Release(instances[view], false);
                    instances.Remove(view);
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            for (var i = pendingRelease.Count - 1; i >= 0; i--)
            {
                try
                {
                    provider.Release(pendingRelease[i], false);
                    pendingRelease.RemoveAt(i);
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            if (errors.Count > 0)
            {
                throw new AggregateException("Overlay Source 해제가 실패했습니다.", errors);
            }

            isDisposed = true;
        }

    #endregion

    }
}
