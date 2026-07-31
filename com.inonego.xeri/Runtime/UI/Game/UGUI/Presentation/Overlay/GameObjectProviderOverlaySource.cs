/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GameObjectProviderOverlaySource.cs
수정일 : 2026-07-31

# 설명
IGameObjectProvider를 Overlay View Source로 연결하고 Parent와 View 반환 수명을 관리한다.
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
    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// Source가 소유하는 Overlay View와 GameObject.
        /// </summary>
        // ============================================================
        private sealed class OwnedView
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 획득한 Overlay View.
            /// </summary>
            // ------------------------------------------------------------
            public TView View { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// View를 제공한 GameObject.
            /// </summary>
            // ------------------------------------------------------------
            public GameObject Instance { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// Source가 소유하는 Overlay View를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public OwnedView(TView view, GameObject instance) : base()
            {
                View = view ?? throw new ArgumentNullException(nameof(view));
                Instance = instance != null
                    ? instance
                    : throw new ArgumentNullException(nameof(instance));
            }
        }

    #endregion

    #region 필드

        private readonly IGameObjectProvider provider = null;
        private readonly List<OwnedView> ownedViews = new List<OwnedView>();
        private bool isDisposed = false;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// GameObject Provider를 Overlay Source로 감싼다.
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
        public TView Acquire(IPresentationLayerDriver layer)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(GameObjectProviderOverlaySource<TView>));
            }

            if (!(layer is IPresentationLayerDriver<RectTransform> layerCanvas))
            {
                throw new InvalidOperationException("UGUI Overlay Source에는 RectTransform Layer가 필요합니다.");
            }

            if (layerCanvas.Root == null)
            {
                throw new InvalidOperationException("UGUI Overlay Layer Root가 없습니다.");
            }

            var previousParent = provider.Parent;
            GameObject instance = null;

            try
            {
                provider.Parent = layerCanvas.Root;
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
                var componentException = new InvalidOperationException
                (
                    $"획득한 GameObject에 {typeof(TView).Name} 구현이 없습니다."
                );

                try
                {
                    provider.Release(instance, false);
                }
                catch (Exception releaseException)
                {
                    throw new AggregateException
                    (
                        "Overlay View 확인과 GameObject 반환이 모두 실패했습니다.",
                        componentException,
                        releaseException
                    );
                }

                throw componentException;
            }

            if (FindOwnedViewIndex(component) >= 0)
            {
                // 사용 중인 동일 인스턴스를 반환한 Provider의 소유 상태는 Source가 안전하게 보정할 수 없다.
                throw new InvalidOperationException("Provider가 이미 사용 중인 Overlay View를 다시 반환했습니다.");
            }

            ownedViews.Add(new OwnedView(component, instance));
            return component;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 대응하는 GameObject 소유 매핑을 종료하고 Provider 반환을 한 번 시도한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Release(TView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (isDisposed) return;

            var index = FindOwnedViewIndex(view);

            if (index < 0)
            {
                throw new InvalidOperationException("이 Source가 소유하지 않은 Overlay View입니다.");
            }

            var instance = ownedViews[index].Instance;
            ownedViews.RemoveAt(index);
            provider.Release(instance, false);
        }

    #endregion

    #region 내부 처리

        // ------------------------------------------------------------
        /// <summary>
        /// 참조가 같은 소유 View의 위치를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private int FindOwnedViewIndex(TView view)
        {
            for (var i = 0; i < ownedViews.Count; i++)
            {
                if (ReferenceEquals(ownedViews[i].View, view))
                {
                    return i;
                }
            }

            return -1;
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 아직 Source가 소유하는 Overlay 인스턴스를 모두 Provider에 한 번씩 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            isDisposed = true;
            var errors = new List<Exception>();

            // Source가 소유한 각 인스턴스를 목록에서 먼저 제거해 반환 실패도 Terminal로 확정한다.
            for (var i = ownedViews.Count - 1; i >= 0; i--)
            {
                var instance = ownedViews[i].Instance;
                ownedViews.RemoveAt(i);

                try
                {
                    provider.Release(instance, false);
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
        }

    #endregion

    }
}
