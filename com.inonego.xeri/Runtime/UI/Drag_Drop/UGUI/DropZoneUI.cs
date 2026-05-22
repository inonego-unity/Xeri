/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DropZoneUI.cs
수정일 : 2026-05-22

# 설명
UGUI 오브젝트를 Core DropZone으로 등록하는 드롭 가능 UI 컴포넌트.
========================================================================= BLOCK_HEADER_END */

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.UI.DragDrop
{
    // ============================================================
    /// <summary>
    /// UGUI 드롭 가능 UI 컴포넌트.
    /// </summary>
    // ============================================================
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class DropZoneUI : MonoBehaviour
    {

    #region 필드

        [Header("설정")]
        [SerializeField]
        private bool canDrop = true;

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 허용 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanDrop
        {
            get => canDrop;
            set
            {
                canDrop = value;
                if (dropZone != null)
                {
                    dropZone.CanDrop = value;
                }
            }
        }

        [SerializeField]
        private List<DropRuleAsset> dropRuleAssets = new();

        // ------------------------------------------------------------
        /// <summary>
        /// Core 드롭 영역 상태.
        /// </summary>
        // ------------------------------------------------------------
        public DropZone DropZone => dropZone;

        private DropZone dropZone = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 진입한 드래그 대상.
        /// </summary>
        // ------------------------------------------------------------
        public Draggable Draggable => dropZone?.Draggable;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 드래그 대상이 진입해 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDropping => dropZone != null && dropZone.IsDropping;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 드롭 조율자.
        /// </summary>
        // ------------------------------------------------------------
        public DragDropCoordinator Coordinator
        {
            get => coordinator ?? DragDropCoordinator.Default;
            set => coordinator = value;
        }

        private DragDropCoordinator coordinator = null;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 대상이 드롭 영역에 진입할 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event DropEventHandler OnDropEnter
        {
            add
            {
                EnsureRuntimeObjects();
                dropZone.OnDropEnter += value;
            }
            remove
            {
                EnsureRuntimeObjects();
                dropZone.OnDropEnter -= value;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 대상이 드롭 영역에서 이탈할 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event DropEventHandler OnDropExit
        {
            add
            {
                EnsureRuntimeObjects();
                dropZone.OnDropExit += value;
            }
            remove
            {
                EnsureRuntimeObjects();
                dropZone.OnDropExit -= value;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 대상이 드롭 영역에 드롭될 때 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event DropEventHandler OnDropDone
        {
            add
            {
                EnsureRuntimeObjects();
                dropZone.OnDropDone += value;
            }
            remove
            {
                EnsureRuntimeObjects();
                dropZone.OnDropDone -= value;
            }
        }

    #endregion

    #region 유니티 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Core DropZone을 준비한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Awake()
        {
            EnsureRuntimeObjects();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 활성화 시 Coordinator에 DropZone을 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnEnable()
        {
            EnsureRuntimeObjects();
            Coordinator.Register(dropZone);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비활성화 시 Coordinator에서 DropZone을 등록 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            Coordinator.Unregister(dropZone);
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Core DropZone과 규칙 목록을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CreateRuntimeObjects()
        {
            dropZone = new DropZone(this)
            {
                CanDrop = canDrop,
            };

            foreach (var dropRuleAsset in dropRuleAssets)
            {
                dropZone.AddDropRule(dropRuleAsset);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Core DropZone이 생성되어 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private void EnsureRuntimeObjects()
        {
            if (dropZone != null) return;

            CreateRuntimeObjects();
        }

    #endregion

    }
}
