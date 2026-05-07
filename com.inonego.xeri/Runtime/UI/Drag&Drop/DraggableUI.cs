/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DraggableUI.cs
수정일 : 2026-05-08

# 설명
UGUI EventSystem 기반 드래그 가능 UI 컴포넌트.
허용 마우스 버튼·SpecificDropZone 지정·강제 드래그 종료를 지원하며 활성 컬렉션을 정적으로 추적한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;

namespace inonego.Xeri.UI
{
    using Xeri.Serializable;

    // ============================================================
    /// <summary>
    /// 드래그 가능한 마우스 버튼(플래그).
    /// </summary>
    // ============================================================
    [Flags]
    public enum MouseButton : int
    {
        Left   = 1 << 0,
        Right  = 1 << 1,
        Middle = 1 << 2,
    }

    // ============================================================
    /// <summary>
    /// 드래그 이벤트 인자.
    /// </summary>
    // ============================================================
    [Serializable]
    public struct DragEventArgs
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작 위치.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 Origin;

        // ------------------------------------------------------------
        /// <summary>
        /// 마우스 위치와 UI 위치 간의 오프셋.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 Offset;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 UI 위치.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 Point;

        // ------------------------------------------------------------
        /// <summary>
        /// 목표 위치.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 Target;
    }

    // ============================================================
    /// <summary>
    /// UI 요소를 드래그할 수 있게 해주는 컴포넌트.
    /// </summary>
    // ============================================================
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class DraggableUI : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IInitializePotentialDragHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {

    #region 정적 관리

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 드래그 중인 DraggableUI 들.
        /// </summary>
        // ------------------------------------------------------------
        public static IReadOnlyCollection<DraggableUI> ActiveCollection => activeCollection;

        private static HashSet<DraggableUI> activeCollection = new();

        // ------------------------------------------------------------
        /// <summary>
        /// ActiveCollection 변경 이벤트.
        /// </summary>
        // ------------------------------------------------------------
        public static event Action OnActiveCollectionChange = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 도메인 리로드 후 활성 컬렉션 초기화.
        /// </summary>
        // ------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeInitializeOnLoad()
        {
            activeCollection.Clear();
        }

    #endregion

    #region 필드

        [Header("설정")]
        public bool CanMove       = true;
        public bool CanDrop       = true;
        public bool RaycastTarget = false;

        public MouseButton Button = MouseButton.Left;

        [Header("드롭존 정보")]
        public DropZoneUI SpecificDropZone = null;

        [Header("드래그 정보")]
        public bool IsDragging => current != null;

        private PointerEventData current = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작 위치.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2? Origin => origin;

        [SerializeField, ReadOnly]
        private XNullable<Vector2> origin = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 마우스 위치와 UI 위치 간의 오프셋.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2? Offset => offset;

        [SerializeField, ReadOnly]
        private XNullable<Vector2> offset = null;

        private bool originalBlocksRaycasts = true;

        private Vector2? beginOrigin = null;
        private Vector2? beginOffset = null;

    #endregion

    #region 컴포넌트

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 이벤트 핸들러.
        /// </summary>
        // ------------------------------------------------------------
        public delegate void DragEventHandler(DraggableUI sender, DragEventArgs e);

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작 시 호출.
        /// </summary>
        // ------------------------------------------------------------
        public event DragEventHandler OnDragBegin = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 중 호출.
        /// </summary>
        // ------------------------------------------------------------
        public event DragEventHandler OnDrag = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 종료 시 호출.
        /// </summary>
        // ------------------------------------------------------------
        public event DragEventHandler OnDragEnd = null;

    #endregion

    #region 유니티 이벤트

        private void Awake()
        {
            canvasGroup   = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (IsDragging)
            {
                _OnDrag(current);
            }
        }

        private void OnDisable()
        {
            ForceDragEnd();
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// PointerEventData.InputButton 을 MouseButton 으로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        private MouseButton ConvertToMouseButton(PointerEventData.InputButton button)
        {
            return button switch
            {
                PointerEventData.InputButton.Left   => MouseButton.Left,
                PointerEventData.InputButton.Right  => MouseButton.Right,
                PointerEventData.InputButton.Middle => MouseButton.Middle,
                _                                   => 0,
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정된 버튼이 드래그를 허용하는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool IsButtonAllowed(PointerEventData.InputButton button)
        {
            MouseButton mouseButton = ConvertToMouseButton(button);

            return (Button & mouseButton) != 0;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 화면 좌표를 Canvas 로컬 좌표로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        private Vector2 ScreenToLocalPoint(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle
            (
                rectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint
            );

            return localPoint;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 마우스 위치와 오프셋을 기반으로 목표 위치를 계산한다.
        /// </summary>
        // ------------------------------------------------------------
        private Vector2 CalculateTarget(Vector2 localPoint)
        {
            return localPoint + (offset.HasValue ? offset.Value : Vector2.zero);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 이벤트 인자를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private DragEventArgs CreateDragEventArgs(Vector2 localPoint)
        {
            return new DragEventArgs
            {
                Origin = origin.HasValue ? origin.Value : Vector2.zero,
                Offset = offset.HasValue ? offset.Value : Vector2.zero,
                Point  = rectTransform.anchoredPosition,
                Target = CalculateTarget(localPoint),
            };
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        private void _OnDragBegin(PointerEventData eventData)
        {
            if (eventData == null) return;

            if (!activeCollection.Contains(this))
            {
                activeCollection.Add(this);
                OnActiveCollectionChange?.Invoke();
            }

            originalBlocksRaycasts = canvasGroup.blocksRaycasts;

            if (!RaycastTarget)
            {
                canvasGroup.blocksRaycasts = false;
            }

            Vector2 localPoint = ScreenToLocalPoint(eventData);

            // ------------------------------------------------------------
            // 드래그 정보 설정
            // ------------------------------------------------------------
            offset = beginOffset;
            origin = beginOrigin;

            beginOffset = null;
            beginOrigin = null;

            current = eventData;

            eventData.pointerDrag = gameObject;

            // ----------------------------------------------------------------------
            // 드래그 시작 시점에 즉시 위치를 마우스 위치 + 오프셋으로 이동시켜 Threshold 지연을 보정.
            // ----------------------------------------------------------------------
            if (CanMove)
            {
                rectTransform.anchoredPosition = CalculateTarget(localPoint);
            }

            var dragEventArgs = CreateDragEventArgs(localPoint);

            OnDragBegin?.Invoke(this, dragEventArgs);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 중일 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        private void _OnDrag(PointerEventData eventData)
        {
            if (eventData == null) return;

            Vector2 localPoint = ScreenToLocalPoint(eventData);

            if (CanMove)
            {
                rectTransform.anchoredPosition = CalculateTarget(localPoint);
            }

            var dragEventArgs = CreateDragEventArgs(localPoint);

            OnDrag?.Invoke(this, dragEventArgs);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 종료 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        private void _OnDragEnd(PointerEventData eventData)
        {
            if (eventData == null) return;

            if (activeCollection.Contains(this))
            {
                activeCollection.Remove(this);
                OnActiveCollectionChange?.Invoke();
            }

            canvasGroup.blocksRaycasts = originalBlocksRaycasts;

            Vector2 localPoint = ScreenToLocalPoint(eventData);

            var dragEventArgs = CreateDragEventArgs(localPoint);

            // ------------------------------------------------------------
            // 드래그 정보 정리
            // ------------------------------------------------------------
            origin = null;
            offset = null;

            eventData.pointerDrag = null;

            current = null;

            OnDragEnd?.Invoke(this, dragEventArgs);
        }

    #endregion

    #region 강제 드래그 이벤트 발생

        // ------------------------------------------------------------
        /// <summary>
        /// 강제로 드래그를 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ForceDragEnd()
        {
            _OnDragEnd(current);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 마우스 위치와 부모를 기준으로 드래그 오프셋을 재계산한다(부모 변경 시 호출).
        /// </summary>
        // ----------------------------------------------------------------------
        public void RefreshDragOriginAndOffset()
        {
            if (!IsDragging) return;

            Vector2 localPoint = ScreenToLocalPoint(current);

            offset = rectTransform.anchoredPosition - localPoint;
            origin = rectTransform.anchoredPosition;
        }

    #endregion

    #region 인터페이스 구현

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            // ------------------------------------------------------------
            // 허용되지 않은 버튼은 부모로 전달
            // ------------------------------------------------------------
            if (!IsButtonAllowed(eventData.button))
            {
                var parent = transform.parent;
                if (parent != null)
                {
                    ExecuteEvents.ExecuteHierarchy(parent.gameObject, eventData, ExecuteEvents.pointerDownHandler);
                }
                return;
            }

            // ----------------------------------------------------------------------
            // 드래그 이전 마우스 클릭 시점에 오프셋·원점을 미리 계산.
            // ----------------------------------------------------------------------
            Vector2 localPoint = ScreenToLocalPoint(eventData);

            beginOffset = rectTransform.anchoredPosition - localPoint;
            beginOrigin = rectTransform.anchoredPosition;
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            // ------------------------------------------------------------
            // 허용되지 않은 버튼은 부모로 전달
            // ------------------------------------------------------------
            if (!IsButtonAllowed(eventData.button))
            {
                var parent = transform.parent;
                if (parent != null)
                {
                    ExecuteEvents.ExecuteHierarchy(parent.gameObject, eventData, ExecuteEvents.pointerUpHandler);
                }
            }
        }

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData)
        {
            // ------------------------------------------------------------
            // 드래그 시작 전에 버튼 체크
            // ------------------------------------------------------------
            if (!IsButtonAllowed(eventData.button))
            {
                eventData.pointerDrag = null;
            }
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (!IsDragging)
            {
                _OnDragBegin(eventData);
            }
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            // ----------------------------------------------------------------------
            // 실제 처리는 Update 에서. 이 구현이 있어야 Drop 이벤트가 작동한다.
            // ----------------------------------------------------------------------
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (IsDragging)
            {
                _OnDragEnd(eventData);
            }
        }

    #endregion

    }
}
