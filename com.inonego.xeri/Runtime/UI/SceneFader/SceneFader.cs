/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SceneFader.cs
수정일 : 2026-05-09

# 설명
화면 전환 페이드 효과를 제공하는 단일 인스턴스 컴포넌트.
RuntimeInitializeOnLoadMethod 로 첫 씬 로드 후 자동 캔버스/Image 를 생성하고 DontDestroyOnLoad 로 보존된다.
DOTween 이 활성화되면 TweenCurve 기반 페이드 인/아웃을 제공한다.

# 특이사항
MonoSingleton 슬롯에 TryRegisterOrDestroy 로 등록 — 사용자가 실수로 SceneFader 를 두 번 추가하면 두 번째 GameObject 가 즉시 파괴된다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UI;

#if DOTWEEN
using DG.Tweening;
#endif

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 페이드 방향.
    /// </summary>
    // ============================================================
    public enum FadeType
    {
        In, Out,
    }

    // ============================================================
    /// <summary>
    /// 화면 전환 페이드 단일 인스턴스 컴포넌트.
    /// </summary>
    // ============================================================
    public class SceneFader : MonoSingleton<SceneFader>
    {

    #region 필드

        private Image image;

#if DOTWEEN
        private Tween fadeTween;
#endif

    #endregion

    #region 부트스트랩

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 첫 씬 로드 후 자동으로 SceneFader 를 띄우는 진입점.
        /// <br/> ScreenSpaceOverlay 캔버스 위에 화면 전체를 덮는 Image 를 생성한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // 캔버스 — Overlay + 매우 높은 sortingOrder 로 모든 UI 위에 그려지도록.
            var canvasGo = new GameObject("SceneFaderCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();

            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            DontDestroyOnLoad(canvasGo);

            // SceneFader 본체 — RectTransform 으로 화면 전체 커버.
            var faderGo = new GameObject("SceneFader");
            faderGo.transform.SetParent(canvasGo.transform);

            var image = faderGo.AddComponent<Image>();
            image.color         = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;

            faderGo.AddComponent<SceneFader>();

            // RectTransform 풀 스트레치
            var rect = faderGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot     = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

    #endregion

    #region 유니티 이벤트

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> MonoSingleton 슬롯에 자기 자신을 등록한다.
        /// <br/> 이미 다른 SceneFader 가 점유 중이면 자기 GameObject 를 파괴한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void Awake()
        {
            if (!TryRegisterOrDestroy(this)) return;

            image = GetComponent<Image>();
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 페이드 색을 설정한다 (알파는 0 으로 초기화).
        /// </summary>
        // ------------------------------------------------------------
        public void Color(Color color)
        {
            image.color = new Color(color.r, color.g, color.b, 0f);
        }

    #if DOTWEEN

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> TweenCurve 기반으로 페이드 인/아웃을 실행한다.
        /// <br/> 진행 중인 트윈은 즉시 Kill 후 새 트윈으로 교체된다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Fade(FadeType type, TweenCurve curve, Action onComplete = null)
        {
            fadeTween?.Kill();

            float alpha = type == FadeType.In ? 1f : 0f;

            fadeTween = image
                .DOFade(alpha, curve.Duration)
                .SetDelay(curve.Delay)
                .SetEase(curve.Ease);

            fadeTween.onComplete = () =>
            {
                onComplete?.Invoke();
                fadeTween = null;
            };
        }

    #endif

    #endregion

    }
}
