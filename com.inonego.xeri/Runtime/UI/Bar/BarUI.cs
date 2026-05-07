/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : BarUI.cs
수정일 : 2026-05-08

# 설명
RangeValue<float> 의 비율을 시각화하는 진행바 UGUI 컴포넌트.
양/음 변화를 별도 색(positive/negative)으로 표현하며 DOTween 이 있으면 부드럽게 전이한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UI;

#if DOTWEEN
using DG.Tweening;
#endif

namespace inonego.Xeri.UI
{
    using Xeri.Serializable;
    using Xeri.Primitive;

    // ============================================================
    /// <summary>
    /// 진행바 UGUI 컴포넌트.
    /// </summary>
    // ============================================================
    [ExecuteInEditMode]
    public class BarUI : MonoBehaviour
    {

    #region 내부 데이터

        // ------------------------------------------------------------
        /// <summary>
        /// 진행 방향.
        /// </summary>
        // ------------------------------------------------------------
        public enum BarDirection
        {
            LeftToRight,
            RightToLeft,
            BottomToTop,
            TopToBottom,
        }

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 표시할 RangeValue.
        /// </summary>
        // ------------------------------------------------------------
        public RangeValue<float> Value => value;

        [SerializeField]
        private RangeValue<float> value;

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 값이 범위 내에서 차지하는 비율 (0.0 ~ 1.0). NaN/Infinity 입력은 0 으로 보정한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public float Ratio
        {
            get
            {
                var range = Value.Max - Value.Min;

                if (range == 0f || float.IsNaN(range) || float.IsInfinity(range))
                {
                    return 0f;
                }

                var ratio = (Value.Base - Value.Min) / range;

                if (float.IsNaN(ratio) || float.IsInfinity(ratio))
                {
                    return 0f;
                }

                return ratio;
            }
        }

        [Header("Animation")]
        [SerializeField]
        private TweenCurve changeCurve;

        [Header("UI")]
        [SerializeField] private Image ForeFillImage;
        [SerializeField] private Image BackFillImage;
        [SerializeField] private Image BackgroundImage;

        [Header("Direction")]
        [SerializeField]
        private BarDirection direction;
        public BarDirection Direction => direction;

        [Header("Color")]
        [SerializeField] private Color defaultColor  = Color.white;
        [SerializeField] private Color positiveColor = Color.green;
        [SerializeField] private Color negativeColor = Color.red;

        private float lCurrentRatio = 0f;

    #if DOTWEEN
        private Tween lCurrentTween;
    #endif

    #endregion

    #region 초기화

        private void OnEnable()
        {
            value.OnBaseChange       += OnBaseChange;
            value.Range.OnBaseChange += OnRangeChange;

            UpdateBarInstantly();
        }

        private void OnDisable()
        {
            value.OnBaseChange       -= OnBaseChange;
            value.Range.OnBaseChange -= OnRangeChange;
        }

    #endregion

    #region 메서드

        // ----------------------------------------------------------------------
        /// <summary>
        /// 진행바를 갱신한다. instant 가 true 면 트윈 없이 즉시 적용한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Refresh(bool instant = false)
        {
            if (instant)
            {
            #if DOTWEEN
                lCurrentTween?.Kill();
            #endif
                UpdateBarInstantly();

                return;
            }

            var ratio = Ratio;

        #if DOTWEEN

            if (Application.isPlaying)
            {
                lCurrentTween.Kill();

                float Getter() => lCurrentRatio;
                void Setter(float value) => lCurrentRatio = value;

                void OnUpdate() => UpdateBar(lCurrentRatio, ratio);

                OnUpdate();

                lCurrentTween = DOTween.To(Getter, Setter, ratio, changeCurve.Duration)
                    .SetDelay(changeCurve.Delay)
                    .SetEase(changeCurve.Ease);

                lCurrentTween.onUpdate = OnUpdate;
            }
            else
            {
                // ------------------------------------------------------------
                // 에디터 모드에서는 DOTween 이 작동하지 않으므로 즉시 갱신한다.
                // ------------------------------------------------------------
                UpdateBarInstantly();
            }

        #else

            UpdateBarInstantly();

        #endif
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// Value.Base 변경 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnBaseChange(object sender, ValueChangeEventArgs<float> args)
        {
            Refresh();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Value.Range 변경 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnRangeChange(object sender, ValueChangeEventArgs<MinMax<float>> args)
        {
            Refresh();
        }

    #endregion

    #region 업데이트

        // ----------------------------------------------------------------------
        /// <summary>
        /// 트윈 중간 단계의 비율로 ForeFill/BackFill 을 갱신한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void UpdateBar(float lCurrentRatio, float lTargetRatio)
        {
            if (lCurrentRatio < lTargetRatio)
            {
                // ------------------------------------------------------------
                // 증가
                // ------------------------------------------------------------
                SetFillRatio(ForeFillImage, 0, lCurrentRatio);
                SetFillRatio(BackFillImage, lCurrentRatio, lTargetRatio);

                if (BackFillImage != null) BackFillImage.color = positiveColor;
            }
            else
            {
                // ------------------------------------------------------------
                // 감소
                // ------------------------------------------------------------
                SetFillRatio(ForeFillImage, 0, lTargetRatio);
                SetFillRatio(BackFillImage, lTargetRatio, lCurrentRatio);

                if (BackFillImage != null) BackFillImage.color = negativeColor;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 즉시 진행바를 현재 Ratio 로 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void UpdateBarInstantly()
        {
            var ratio = Ratio;

            if (ForeFillImage != null) ForeFillImage.color = defaultColor;
            if (BackFillImage != null) BackFillImage.color = defaultColor;

            lCurrentRatio = ratio;

            SetFillRatio(ForeFillImage, 0,     ratio);
            SetFillRatio(BackFillImage, ratio, ratio);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Image 의 RectTransform anchor 를 진행 방향에 맞게 [beginRatio, endRatio] 로 설정한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void SetFillRatio(Image image, float beginRatio, float endRatio)
        {
            if (image == null) return;

            var rectTransform = image.rectTransform;

            switch (direction)
            {
                case BarDirection.TopToBottom:
                    rectTransform.anchorMin = new Vector2(0, 1 - endRatio);
                    rectTransform.anchorMax = new Vector2(1, 1 - beginRatio);
                    rectTransform.pivot     = new Vector2(0.5f, 1);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    break;

                case BarDirection.BottomToTop:
                    rectTransform.anchorMin = new Vector2(0, beginRatio);
                    rectTransform.anchorMax = new Vector2(1, endRatio);
                    rectTransform.pivot     = new Vector2(0.5f, 0);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    break;

                case BarDirection.RightToLeft:
                    rectTransform.anchorMin = new Vector2(1 - endRatio, 0);
                    rectTransform.anchorMax = new Vector2(1 - beginRatio, 1);
                    rectTransform.pivot     = new Vector2(1, 0.5f);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    break;

                case BarDirection.LeftToRight:
                    rectTransform.anchorMin = new Vector2(beginRatio, 0);
                    rectTransform.anchorMax = new Vector2(endRatio, 1);
                    rectTransform.pivot     = new Vector2(0, 0.5f);
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    break;
            }
        }

    #endregion

    }
}
