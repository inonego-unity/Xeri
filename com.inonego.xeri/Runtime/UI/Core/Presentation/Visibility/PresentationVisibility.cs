/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationVisibility.cs
수정일 : 2026-09-17
# 설명
Presentation의 자체 Visibility와 scoped Modifier를 AND 합성해 단일 backend Target에 적용한다.
서로 독립적인 여러 기능이 같은 Presentation을 숨겨도 각 Lease가 모두 해제될 때까지 숨김 상태를 보존한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.UI
{
    // ======================================================================
    /// <summary>
    /// <br/> Presentation Base Visibility와 scoped Modifier를 합성해
    /// <br/> 실제 표시 Target에 적용한다.
    /// </summary>
    // ======================================================================
    [Serializable]
    public sealed class PresentationVisibility
    {

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// 하나의 독립적인 scoped Visibility 기여값.
        /// </summary>
        // ============================================================
        private sealed class Modifier
        {
            public bool Visible = true;
        }

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Modifier 적용 전 Presentation 자체 Visibility.
        /// </summary>
        // ------------------------------------------------------------
        public bool Base { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Base와 모든 Modifier를 AND 합성한 최종 Visibility.
        /// </summary>
        // ------------------------------------------------------------
        public bool Modified => ResolveModified();

        // ------------------------------------------------------------
        /// <summary>
        /// 최종 Visibility를 적용할 backend Target이 현재 유효한지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid => target != null && target.IsValid;

        private readonly IPresentationVisibilityTarget target = null;
        private readonly List<Modifier> modifiers = new List<Modifier>();

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 Visibility backend와 현재 표시 상태를 Base로 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationVisibility(IPresentationVisibilityTarget target) : base()
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));

            if (!target.IsValid)
            {
                throw new InvalidOperationException("Presentation Visibility Target이 유효하지 않습니다.");
            }

            Base = target.IsVisible;
            ApplyModified();
        }

    #endregion

    #region Base 상태

        // ----------------------------------------------------------------------
        /// <summary>
        /// Presentation 자체 Visibility를 변경하고 합성 결과를 즉시 적용한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Set(bool visible)
        {
            Base = visible;
            ApplyModified();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 합성 결과를 backend에 다시 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Refresh()
        {
            ApplyModified();
        }

    #endregion

    #region Modifier 연결

        // ------------------------------------------------------------
        /// <summary>
        /// scoped Visibility Modifier를 추가하고 해제 Lease를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Lease AcquireModifier(bool visible)
        {
            var modifier = new Modifier
            {
                Visible = visible,
            };
            modifiers.Add(modifier);

            try
            {
                ApplyModified();
            }
            catch
            {
                modifiers.Remove(modifier);
                throw;
            }

            return new Lease(() => RemoveModifier(modifier));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Modifier만 제거하고 남은 합성 Visibility를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RemoveModifier(Modifier modifier)
        {
            if (modifier == null || !modifiers.Remove(modifier)) return;

            ApplyModified();
        }

    #endregion

    #region 합성

        // ------------------------------------------------------------
        /// <summary>
        /// Base와 모든 scoped Modifier를 AND 합성한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool ResolveModified()
        {
            if (!Base) return false;

            for (var index = 0; index < modifiers.Count; index++)
            {
                if (!modifiers[index].Visible) return false;
            }

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 합성 Visibility를 실제 Presentation backend에 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyModified()
        {
            if (!target.IsValid)
            {
                throw new InvalidOperationException("Presentation Visibility Target이 유효하지 않습니다.");
            }

            target.SetVisible(Modified);
        }

    #endregion

    }
}