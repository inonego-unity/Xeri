/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GameUIFocusDriver.cs
수정일 : 2026-07-31

# 설명
UGUI와 UI Toolkit Focus Driver를 현재 대상 타입에 따라 하나의 Runtime Focus 계약으로 조립한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UGUI와 UI Toolkit Focus 실행을 대상 타입별로 위임하는 Driver.
    /// </summary>
    // ============================================================
    public sealed class GameUIFocusDriver : MonoBehaviour, IFocusDriver
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 활성 Focus Driver에서 선택된 대상.
        /// </summary>
        // ------------------------------------------------------------
        public object Current
        {
            get
            {
                var current = currentDriver?.Current;

                if (currentDriver != null && currentDriver.IsValid(current))
                {
                    return current;
                }

                current = uguiFocusDriver?.Current;

                if (uguiFocusDriver != null && uguiFocusDriver.IsValid(current))
                {
                    currentDriver = uguiFocusDriver;
                    return current;
                }

                current = uitkFocusDriver?.Current;

                if (uitkFocusDriver != null && uitkFocusDriver.IsValid(current))
                {
                    currentDriver = uitkFocusDriver;
                    return current;
                }

                return null;
            }
        }

        [SerializeField]
        private UGUIFocusDriver uguiFocusDriver = null;

        [SerializeField]
        private UITKFocusDriver uitkFocusDriver = null;

        private IFocusDriver currentDriver = null;

    #endregion

    #region 초기화

        // ------------------------------------------------------------
        /// <summary>
        /// 두 native Focus Driver와 공통 EventSystem 연결을 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Initialize(EventSystem eventSystem)
        {
            if (!enabled)
            {
                throw new InvalidOperationException("Game UI Focus Driver가 비활성 상태입니다.");
            }

            if (eventSystem == null)
            {
                throw new ArgumentNullException(nameof(eventSystem));
            }

            if (uguiFocusDriver == null || !uguiFocusDriver.enabled)
            {
                throw new InvalidOperationException("활성 UGUI Focus Driver가 연결되지 않았습니다.");
            }

            if (uguiFocusDriver.EventSystem != eventSystem)
            {
                throw new InvalidOperationException("UGUI Focus Driver가 Host EventSystem에 연결되지 않았습니다.");
            }

            if (uitkFocusDriver == null || !uitkFocusDriver.enabled)
            {
                throw new InvalidOperationException("활성 UITK Focus Driver가 연결되지 않았습니다.");
            }
        }

    #endregion

    #region IFocusDriver

        // ------------------------------------------------------------
        /// <summary>
        /// 대상의 native Focus Driver에서 현재 선택 가능 여부를 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid(object target)
        {
            if (target is GameObject)
            {
                return uguiFocusDriver != null && uguiFocusDriver.IsValid(target);
            }

            if (target is VisualElement)
            {
                return uitkFocusDriver != null && uitkFocusDriver.IsValid(target);
            }

            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이전 기술의 Focus를 비운 뒤 대상의 native Driver에 선택을 위임한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Select(object target)
        {
            if (target is GameObject)
            {
                // 한 Screen Stack에서 두 native Focus가 동시에 남지 않게 이전 UITK 선택을 비운다.
                uitkFocusDriver.Select(null);
                uguiFocusDriver.Select(target);
                currentDriver = uguiFocusDriver;
                return;
            }

            if (target is VisualElement)
            {
                // UGUI EventSystem 선택을 비운 뒤 현재 UITK Panel에 Focus를 적용한다.
                uguiFocusDriver.Select(null);
                uitkFocusDriver.Select(target);
                currentDriver = uitkFocusDriver;
                return;
            }

            uguiFocusDriver.Select(null);
            uitkFocusDriver.Select(null);
            currentDriver = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 기술을 우선하고 두 native Driver의 유효한 대체 Focus를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        public object FindFallback()
        {
            var fallback = currentDriver?.FindFallback();

            if (currentDriver != null && currentDriver.IsValid(fallback))
            {
                return fallback;
            }

            fallback = uguiFocusDriver?.FindFallback();

            if (uguiFocusDriver != null && uguiFocusDriver.IsValid(fallback))
            {
                return fallback;
            }

            fallback = uitkFocusDriver?.FindFallback();
            return uitkFocusDriver != null && uitkFocusDriver.IsValid(fallback)
                ? fallback
                : null;
        }

    #endregion

    }
}
