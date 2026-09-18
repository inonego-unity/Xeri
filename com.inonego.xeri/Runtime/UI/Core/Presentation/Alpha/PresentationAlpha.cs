/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationAlpha.cs
수정일 : 2026-09-18
# 설명
Presentation의 자체 Alpha와 순서화된 Modifier를 합성해 단일 backend Target에 적용한다.
MValue의 Modified 변경을 구독해 Base와 Modifier 변화를 backend에 즉시 반영한다.
Base 전환과 외부 Modifier가 같은 실제 Alpha를 직접 덮어쓰지 않도록 최종 작성 경계를 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.UI
{
    // ================================================================================
    /// <summary>
    /// Presentation Base Alpha와 외부 Modifier를 합성해 실제 표시 Target에 적용한다.
    /// </summary>
    // ================================================================================
    [Serializable]
    public sealed class PresentationAlpha : IPresentationTransitionTarget
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Modifier 적용 전 Presentation 자체 Alpha.
        /// </summary>
        // ------------------------------------------------------------
        public float Base => alpha.Base;

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 Modifier를 적용한 최종 Presentation Alpha.
        /// </summary>
        // ------------------------------------------------------------
        public float Modified => Mathf.Clamp01(alpha.Modified);

        // ------------------------------------------------------------
        /// <summary>
        /// 최종 Alpha를 적용할 backend Target이 현재 유효한지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid => target != null && target.IsValid;

        private readonly IPresentationAlphaTarget target = null;
        private readonly MValue<float> alpha = new MValue<float>(1.0f);

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 Alpha backend의 현재값을 초기 Base Alpha로 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationAlpha(IPresentationAlphaTarget target) : base()
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));

            if (!target.IsValid)
            {
                throw new InvalidOperationException("Presentation Alpha Target이 유효하지 않습니다.");
            }

            alpha.OnModifiedChange += HandleModifiedChange;
            alpha.Set(Mathf.Clamp01(target.Alpha), invokeEvent: false);
            ApplyCurrent();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 Alpha backend와 명시적 초기 Base Alpha를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationAlpha
        (
            IPresentationAlphaTarget target,
            float baseAlpha
        ) : base()
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));

            if (!target.IsValid)
            {
                throw new InvalidOperationException("Presentation Alpha Target이 유효하지 않습니다.");
            }

            alpha.OnModifiedChange += HandleModifiedChange;
            alpha.Set(Mathf.Clamp01(baseAlpha), invokeEvent: false);
            ApplyCurrent();
        }

    #endregion

    #region Modifier 연결

        // ----------------------------------------------------------------------
        /// <summary>
        /// 지정 key와 순서로 외부 Alpha Modifier를 등록하고 최종값을 즉시 반영한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public Lease AcquireModifier
        (
            string key,
            IModifier<float> modifier,
            int order = 0
        )
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Presentation Alpha Modifier Key가 비어 있습니다.", nameof(key));
            }

            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier));
            }

            alpha.AddModifier(key, modifier, order);

            return new Lease(() => RemoveModifier(key));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 합성된 Alpha를 backend에 명시적으로 다시 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ApplyCurrent()
        {
            if (!target.IsValid)
            {
                throw new InvalidOperationException("Presentation Alpha Target이 유효하지 않습니다.");
            }

            target.SetAlpha(Modified);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 key의 Alpha Modifier를 제거하고 최종값을 즉시 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RemoveModifier(string key)
        {
            alpha.RemoveModifier(key);
        }

    #endregion

    #region IPresentationTransitionTarget

        // --------------------------------------------------------------------------------
        /// <summary>
        /// Transition 진행값을 Base Alpha로 적용하고 Modifier 합성 결과를 backend에 반영한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void Apply(float value)
        {
            alpha.Set(Mathf.Clamp01(value));
        }

    #endregion

    #region Backend 적용

        // ----------------------------------------------------------------------
        /// <summary>
        /// Modified 변경 시 유효한 Presentation backend에 최신 Alpha를 적용한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void HandleModifiedChange
        (
            object _,
            ValueChangeEventArgs<float> e
        )
        {
            if (!target.IsValid) return;

            target.SetAlpha(Mathf.Clamp01(e.Current));
        }

    #endregion

    }
}
