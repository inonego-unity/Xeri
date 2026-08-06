/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : InteractionController.cs
수정일 : 2026-08-04

# 설명
외부 Scanner가 공급한 InteractionOffer 후보 중 CurrentOffer를 선택하고 Use 입력을 전달한다.

# 제약사항
Physics·Raycast 탐색과 InputAction 구독은 프로젝트 Driver가 담당하며 이 Component는 특정 탐색 방식에 의존하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 현재 상호작용 후보 선택과 Use 전달을 소유하는 Component.
    /// </summary>
    // ============================================================
    public sealed class InteractionController : MonoBehaviour
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Scanner가 현재 범위 안에 있다고 알려준 Offer 목록.
        /// </summary>
        // ------------------------------------------------------------
        private readonly List<InteractionOffer> offers = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Use와 Prompt가 연결된 Offer.
        /// </summary>
        // ------------------------------------------------------------
        public InteractionOffer CurrentOffer => currentOffer;

        private InteractionOffer currentOffer = null;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// CurrentOffer가 바뀌어 외부 Prompt Presenter가 표시를 전환해야 할 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<InteractionOffer> OnCurrentOfferChange = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 새 CurrentOffer의 Prompt를 표시할 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<InteractionPrompt> OnPromptShow = null;

        // ------------------------------------------------------------
        /// <summary>
        /// CurrentOffer가 사라져 기존 Prompt를 감춰야 할 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action OnPromptHide = null;

    #endregion

    #region 유니티 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 비활성화 동안 보존한 Scanner 후보를 다시 구독하고 CurrentOffer를 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnEnable()
        {
            foreach (var offer in offers)
            {
                if (offer != null)
                {
                    offer.OnAvailabilityChange += _OnOfferAvailabilityChange;
                }
            }

            RefreshCurrentOffer();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Controller가 비활성화되면 외부 Offer 구독과 현재 Prompt만 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            foreach (var offer in offers)
            {
                if (offer != null)
                {
                    offer.OnAvailabilityChange -= _OnOfferAvailabilityChange;
                }
            }

            // Scanner가 아직 범위 안에 있는 후보는 다음 활성화 때 다시 선택할 수 있도록 보존한다.
            SetCurrentOffer(null);
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Scanner가 새 후보를 발견했을 때 Controller에 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddOffer(InteractionOffer offer)
        {
            if (offer == null || offers.Contains(offer))
            {
                return;
            }

            offers.Add(offer);

            if (isActiveAndEnabled)
            {
                offer.OnAvailabilityChange += _OnOfferAvailabilityChange;
                RefreshCurrentOffer();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Scanner 범위를 벗어난 후보를 Controller에서 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void RemoveOffer(InteractionOffer offer)
        {
            if (offer == null || !offers.Remove(offer))
            {
                return;
            }

            offer.OnAvailabilityChange -= _OnOfferAvailabilityChange;

            if (isActiveAndEnabled)
            {
                RefreshCurrentOffer();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 선택된 Offer에 Use 입력을 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryUse(GameObject instigator)
        {
            var offer = currentOffer;

            if (offer == null || !offer.TryUse(instigator))
            {
                // 비활성화 직후처럼 선택 결과가 오래되었을 수 있어 다음 입력 전에 즉시 보정한다.
                RefreshCurrentOffer();
                return false;
            }

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 후보 중 사용 가능하며 우선순위가 가장 높은 Offer를 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshCurrentOffer()
        {
            InteractionOffer selected = null;

            foreach (var offer in offers)
            {
                if (offer == null || !offer.IsAvailable)
                {
                    continue;
                }

                if (selected == null || offer.Priority > selected.Priority)
                {
                    selected = offer;
                }
            }

            SetCurrentOffer(selected);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// CurrentOffer 전환과 Prompt 표시 전환을 같은 순서로 전파한다.
        /// </summary>
        // ------------------------------------------------------------
        private void SetCurrentOffer(InteractionOffer value)
        {
            if (currentOffer == value)
            {
                return;
            }

            if (currentOffer != null)
            {
                // 이전 Offer의 표시가 먼저 종료되어 Prompt가 동시에 두 개 보이지 않게 한다.
                OnPromptHide?.Invoke();
            }

            currentOffer = value;
            OnCurrentOfferChange?.Invoke(currentOffer);

            if (currentOffer != null)
            {
                OnPromptShow?.Invoke(new InteractionPrompt(currentOffer));
            }
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// 후보 Offer의 Enabled 상태가 바뀌면 CurrentOffer를 다시 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        private void _OnOfferAvailabilityChange(InteractionOffer offer)
        {
            RefreshCurrentOffer();
        }

    #endregion
    }
}
