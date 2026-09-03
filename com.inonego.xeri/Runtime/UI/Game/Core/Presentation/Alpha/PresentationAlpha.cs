/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationAlpha.cs
수정일 : 2026-09-03

# 설명
Presentation의 자체 Alpha와 순서화된 Modifier를 합성해 단일 backend Target에 적용한다.
Base 전환과 외부 Modifier가 같은 실제 Alpha를 직접 덮어쓰지 않도록 최종 작성 경계를 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.UI.Game
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

        private readonly IPresentationTransitionTarget target = null;
        private readonly MValue<float> alpha = new(1.0f);

    #endregion

    #region 생성자

        // ----------------------------------------------------------------------
        /// <summary>
        /// 실제 Alpha backend와 초기 Base Alpha를 연결한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public PresentationAlpha
        (
            IPresentationTransitionTarget target,
            float baseAlpha = 1.0f
        ) : base()
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            alpha.Set(Mathf.Clamp01(baseAlpha), invokeEvent: false);
            ApplyModified();
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
            alpha.AddModifier
            (
                key,
                modifier,
                order,
                invokeEvent: false
            );
            ApplyModified();
            return new Lease(() => RemoveModifier(key));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 key의 Alpha Modifier를 제거하고 최종값을 즉시 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RemoveModifier(string key)
        {
            if (!alpha.RemoveModifier(key, invokeEvent: false)) return;

            ApplyModified();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 내부 상태가 바뀐 Modifier를 다시 평가하고 현재 최종 Alpha를 backend에 적용한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Refresh()
        {
            alpha.Refresh(invokeEvent: false);
            ApplyModified();
        }

    #endregion

    #region IPresentationTransitionTarget

        // ----------------------------------------------------------------------
        /// <summary>
        /// Transition 진행값을 Base Alpha로 적용하고 Modifier 합성 결과를 backend에 반영한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Apply(float value)
        {
            alpha.Set(Mathf.Clamp01(value), invokeEvent: false);
            ApplyModified();
        }

    #endregion

    #region Backend 적용

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Modifier 합성 결과를 실제 Presentation backend에 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyModified()
        {
            if (!target.IsValid)
            {
                throw new InvalidOperationException("Presentation Alpha Target이 유효하지 않습니다.");
            }

            target.Apply(Modified);
        }

    #endregion

    }
}
