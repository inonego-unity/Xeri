/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriLoopAnimator.cs
수정일 : 2026-05-08

# 설명
USS `--xeri-next` custom property 와 TransitionEndEvent 기반의 무한 루프 애니메이션 컨트롤러.
UXML 에서 `class="xeri-loop"` 을 가진 VisualElement 들을 찾아 step → step 으로 클래스 교체를 반복한다.

# 동작 원리
1. xeri-loop 클래스가 부여된 element 의 첫 CustomStyleResolvedEvent 시점에 --xeri-next 가 가리키는 클래스를 추가
2. transition 종료(TransitionEndEvent) 시 다음 --xeri-next 를 읽고 현재 클래스 제거 → transition 비활성화로 즉시 base 복귀
3. 다음 프레임에 transition 복원 + 다음 클래스 추가 → 정방향 애니메이션 재시작
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// USS `--xeri-next` 기반 루프 애니메이션 컨트롤러.
    /// </summary>
    // ============================================================
    public class XeriLoopAnimator
    {

    #region 필드

        private static readonly CustomStyleProperty<string> XERI_NEXT = new CustomStyleProperty<string>("--xeri-next");

        private const string LOOP_CLASS_NAME = "xeri-loop";

        private readonly Dictionary<VisualElement, string> currentClasses = new();

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 루트 VisualElement 하위에서 `xeri-loop` 클래스를 가진 element 들을 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Setup(VisualElement root)
        {
            if (root == null) return;

            root.Query<VisualElement>(className: LOOP_CLASS_NAME).ForEach(element =>
            {
                element.RegisterCallback<TransitionEndEvent>      (OnTransitionEnd);
                element.RegisterCallback<CustomStyleResolvedEvent>(OnInitialStyleResolved);

                currentClasses[element] = "";
            });
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 등록된 element 들의 콜백/클래스를 모두 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Cleanup()
        {
            foreach (var (element, currentClass) in currentClasses)
            {
                element.UnregisterCallback<TransitionEndEvent>      (OnTransitionEnd);
                element.UnregisterCallback<CustomStyleResolvedEvent>(OnInitialStyleResolved);

                if (!string.IsNullOrEmpty(currentClass))
                {
                    element.RemoveFromClassList(currentClass);
                }
            }

            currentClasses.Clear();
        }

    #endregion

    #region 이벤트 핸들러

        // ----------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 첫 CustomStyleResolvedEvent 시 --xeri-next 를 읽어 첫 클래스를 추가하고 콜백을 해제한다.
        /// <br/> OnEnable 시점에는 USS 가 아직 resolve 되지 않아 별도 시점이 필요하다.
        /// </summary>
        // ----------------------------------------------------------------------------------
        private void OnInitialStyleResolved(CustomStyleResolvedEvent e)
        {
            if (e.currentTarget is not VisualElement element) return;

            if (!currentClasses.ContainsKey(element)) return;

            if (!string.IsNullOrEmpty(currentClasses[element])) return;

            element.UnregisterCallback<CustomStyleResolvedEvent>(OnInitialStyleResolved);

            string nextClass = ReadXeriNext(element);

            if (!string.IsNullOrEmpty(nextClass))
            {
                element.AddToClassList(nextClass);

                currentClasses[element] = nextClass;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> transition 종료 시 다음 --xeri-next 를 미리 읽고 현재 클래스를 제거한다.
        /// <br/> transition 비활성화로 base 로 즉시 점프 (역방향 애니메이션 방지) 후 다음 프레임에 새 클래스 부여.
        /// </summary>
        // ------------------------------------------------------------
        private void OnTransitionEnd(TransitionEndEvent e)
        {
            if (e.currentTarget is not VisualElement element) return;

            if (!currentClasses.ContainsKey(element)) return;

            string nextClass = ReadXeriNext(element);

            // transition 비활성화 → class 제거 시 즉시 base 로 점프
            element.style.transitionDuration = new List<TimeValue> { new TimeValue(0) };

            string previousClass = currentClasses[element];

            if (!string.IsNullOrEmpty(previousClass))
            {
                element.RemoveFromClassList(previousClass);
            }

            currentClasses[element] = "";

            if (!string.IsNullOrEmpty(nextClass))
            {
                string capturedNextClass = nextClass;

                element.schedule.Execute(() =>
                {
                    element.style.transitionDuration = StyleKeyword.Null;
                    element.AddToClassList(capturedNextClass);

                    currentClasses[element] = capturedNextClass;
                });
            }
        }

    #endregion

    #region 내부 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// element 의 --xeri-next 커스텀 프로퍼티를 안전하게 읽는다.
        /// </summary>
        // ------------------------------------------------------------
        private static string ReadXeriNext(VisualElement element)
        {
            if (element.customStyle.TryGetValue(XERI_NEXT, out string value))
            {
                return value ?? "";
            }

            return "";
        }

    #endregion

    }
}
