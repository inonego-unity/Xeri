/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriLoopAnimator.cs
수정일 : 2026-08-01

# 설명
UIDocument의 `xeri-loop` VisualElement에서 USS `--xeri-next` 클래스 전환을 반복한다.

# USS 계약
- `--xeri-next`: 현재 Transition 완료 후 적용할 클래스 이름
- `--xeri-loop-trigger`: 선택적 완료 기준 Transition Property 이름
- Trigger가 없으면 첫 TransitionEndEvent가 다음 단계를 시작한다.

# 특이사항
활성 컴포넌트만 문서에서 대상을 탐색해 런타임에 추가된 Screen과 Modal 하위 요소도 처리한다.
========================================================================= BLOCK_HEADER_END */

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI
{
    // ======================================================================================
    /// <summary>
    /// UIDocument 하위의 USS Transition 클래스 루프를 관리한다.
    /// </summary>
    // ======================================================================================
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class XeriLoopAnimator : MonoBehaviour
    {
    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// VisualElement 하나의 현재 루프 단계와 예약 작업을 보관한다.
        /// </summary>
        // ============================================================
        private sealed class LoopState
        {
            public string CurrentClass = "";
            public IVisualElementScheduledItem ScheduledStep = null;
            public StyleList<TimeValue> PreviousTransitionDuration = default;
            public bool IsAdvancing = false;
        }

    #endregion

    #region 필드

        private static readonly CustomStyleProperty<string> XERI_NEXT =
            new CustomStyleProperty<string>("--xeri-next");

        private static readonly CustomStyleProperty<string> XERI_LOOP_TRIGGER =
            new CustomStyleProperty<string>("--xeri-loop-trigger");

        private const string LOOP_CLASS_NAME = "xeri-loop";

        private readonly Dictionary<VisualElement, LoopState> states =
            new Dictionary<VisualElement, LoopState>();

        private UIDocument document = null;
        private VisualElement root = null;

    #endregion

    #region Unity 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// UIDocument Root와 루프 이벤트를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnEnable()
        {
            document = GetComponent<UIDocument>();
            TryAttachRoot();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// OnEnable 시점에 준비되지 않은 UIDocument Root를 다시 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Start()
        {
            if (root == null)
            {
                TryAttachRoot();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UIDocument 교체와 런타임에 추가된 루프 요소를 현재 문서에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Update()
        {
            TryAttachRoot();
            DiscoverElements();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 문서가 비활성화되기 전에 콜백과 예약된 단계를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            ReleaseRoot();
        }

    #endregion

    #region 이벤트 핸들러

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 정적 UXML과 런타임에 추가된 요소의 Custom Style을 확인하고
        /// <br/> `xeri-loop` 요소의 첫 단계를 시작한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void OnElementCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (!(e.currentTarget is VisualElement element)) return;

            if (!element.ClassListContains(LOOP_CLASS_NAME))
            {
                ReleaseElementIfTracked(element);
                return;
            }

            TryStartElement(element);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 현재 단계의 기준 Transition이 완료되면 역방향 전환 없이 base로 복귀한 뒤
        /// <br/> 다음 프레임에 `--xeri-next` 클래스를 적용한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void OnTransitionEnd(TransitionEndEvent e)
        {
            if (!(e.target is VisualElement element)) return;

            if (!states.TryGetValue(element, out var state) || state.IsAdvancing) return;

            var triggerProperty = ReadCustomStyle(element, XERI_LOOP_TRIGGER);

            // 여러 Transition Property가 있으면 USS가 지정한 완료 기준만 단계를 진행한다.
            if
            (
                !string.IsNullOrEmpty(triggerProperty) &&
                !e.AffectsProperty(new StylePropertyName(triggerProperty))
            )
            {
                return;
            }

            var nextClass = ReadCustomStyle(element, XERI_NEXT);

            if (string.IsNullOrEmpty(nextClass)) return;

            // 현재 클래스 제거는 전환 없이 즉시 base 상태로 복귀시킨다.
            state.IsAdvancing = true;
            state.PreviousTransitionDuration = element.style.transitionDuration;
            element.style.transitionDuration = new List<TimeValue> { new TimeValue(0) };

            if (!string.IsNullOrEmpty(state.CurrentClass))
            {
                element.RemoveFromClassList(state.CurrentClass);
                state.CurrentClass = "";
            }

            // USS 재계산이 끝난 다음 프레임에 정방향 Transition을 시작한다.
            state.ScheduledStep = element.schedule.Execute
            (
                () => CompleteAdvance(element, state, nextClass)
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Visual Tree에서 분리된 요소의 루프 상태를 즉시 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnElementDetached(DetachFromPanelEvent e)
        {
            if (e.currentTarget is VisualElement element)
            {
                ReleaseElementIfTracked(element);
            }
        }

    #endregion

    #region 내부 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 UIDocument Root가 준비되었으면 루프 수명을 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void TryAttachRoot()
        {
            var nextRoot = document != null ? document.rootVisualElement : null;

            if (nextRoot == null || nextRoot == root) return;

            ReleaseRoot();
            root = nextRoot;
            root.RegisterCallback<TransitionEndEvent>(OnTransitionEnd);
            DiscoverElements();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 문서에서 아직 추적하지 않는 `xeri-loop` 요소를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void DiscoverElements()
        {
            if (root == null) return;

            if (root.ClassListContains(LOOP_CLASS_NAME))
            {
                EnsureElement(root);
            }

            root.Query<VisualElement>(className: LOOP_CLASS_NAME).ForEach(EnsureElement);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 요소를 루프 대상으로 등록하고 첫 클래스를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void EnsureElement(VisualElement element)
        {
            if (!states.TryGetValue(element, out var state))
            {
                state = new LoopState();
                states.Add(element, state);
                element.RegisterCallback<CustomStyleResolvedEvent>
                (
                    OnElementCustomStyleResolved
                );
                element.RegisterCallback<DetachFromPanelEvent>(OnElementDetached);
            }

            TryStartElement(element);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Custom Style이 준비된 루프 요소에 첫 클래스를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void TryStartElement(VisualElement element)
        {
            if (!states.TryGetValue(element, out var state)) return;

            if (state.IsAdvancing || !string.IsNullOrEmpty(state.CurrentClass)) return;

            var nextClass = ReadCustomStyle(element, XERI_NEXT);

            if (string.IsNullOrEmpty(nextClass)) return;

            element.AddToClassList(nextClass);
            state.CurrentClass = nextClass;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 예약된 단계가 여전히 유효한 요소에만 다음 클래스를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CompleteAdvance
        (
            VisualElement element,
            LoopState state,
            string nextClass
        )
        {
            if (!states.TryGetValue(element, out var current) || current != state) return;

            state.ScheduledStep = null;
            element.style.transitionDuration = state.PreviousTransitionDuration;
            state.IsAdvancing = false;

            // 요소가 현재 문서에서 분리됐다면 예약 시점의 클래스를 다시 적용하지 않는다.
            if (element.panel == null)
            {
                ReleaseElementIfTracked(element);
                return;
            }

            element.AddToClassList(nextClass);
            state.CurrentClass = nextClass;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UIDocument Root에 연결된 모든 루프 콜백과 요소 상태를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseRoot()
        {
            if (root != null)
            {
                root.UnregisterCallback<TransitionEndEvent>(OnTransitionEnd);
            }

            foreach (var (element, state) in states)
            {
                ReleaseElement(element, state);
            }

            states.Clear();
            root = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 등록된 요소라면 소유 목록에서 제거하고 루프 상태를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseElementIfTracked(VisualElement element)
        {
            if (!states.TryGetValue(element, out var state)) return;

            states.Remove(element);
            ReleaseElement(element, state);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 요소의 예약 작업과 임시 Style, 현재 루프 클래스를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseElement
        (
            VisualElement element,
            LoopState state
        )
        {
            element.UnregisterCallback<CustomStyleResolvedEvent>
            (
                OnElementCustomStyleResolved
            );
            element.UnregisterCallback<DetachFromPanelEvent>(OnElementDetached);
            state.ScheduledStep?.Pause();
            state.ScheduledStep = null;

            if (state.IsAdvancing)
            {
                element.style.transitionDuration = state.PreviousTransitionDuration;
                state.IsAdvancing = false;
            }

            if (!string.IsNullOrEmpty(state.CurrentClass))
            {
                element.RemoveFromClassList(state.CurrentClass);
                state.CurrentClass = "";
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 요소의 string Custom Style 값을 읽고 누락 시 빈 문자열을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static string ReadCustomStyle
        (
            VisualElement element,
            CustomStyleProperty<string> property
        )
        {
            if (element.customStyle.TryGetValue(property, out var value))
            {
                return value ?? "";
            }

            return "";
        }

    #endregion

    }
}
