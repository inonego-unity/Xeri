/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : HeadUpFollow.cs
수정일 : 2026-05-08

# 설명
캐릭터 모델에서 'Head' 가 포함된 자식 트랜스폼을 찾아 그 위에 root 를 위치시키는 컴포넌트.
Billboard 와 함께 사용해 머리 위 UI 따라가기 등에 활용한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// 캐릭터 머리 위에 root 를 따라가게 만드는 컴포넌트.
    /// </summary>
    // ============================================================
    [RequireComponent(typeof(Billboard))]
    public class HeadUpFollow : MonoBehaviour
    {

    #region 필드

        [Header("기준 트랜스폼")]

        // ------------------------------------------------------------
        /// <summary>
        /// 이동시킬 루트 게임 오브젝트의 트랜스폼.
        /// </summary>
        // ------------------------------------------------------------
        public Transform Root => root;

        [SerializeField]
        private Transform root;

        // ------------------------------------------------------------
        /// <summary>
        /// 머리 부분을 찾기 위한 기준 트랜스폼.
        /// </summary>
        // ------------------------------------------------------------
        public Transform Reference => reference;

        [HelpBox("머리 부분을 찾기 위한 기준이 되는 트랜스폼입니다.\n하위에서 'Head'가 포함된 이름으로 찾습니다.\n모델링의 게임오브젝트를 드래그해서 넣어주세요.")]
        [SerializeField]
        private Transform reference;

        // ------------------------------------------------------------
        /// <summary>
        /// 머리 트랜스폼. 미리 할당하지 않으면 Awake 에서 자동 검색한다.
        /// </summary>
        // ------------------------------------------------------------
        public Transform HeadTransform
        {
            get => headTransform;
            set => headTransform = value;
        }

        [HelpBox("머리 부분에 대한 트랜스폼입니다.\n미리 할당하지 않는 경우 자동으로 검색합니다.")]
        [SerializeField]
        private Transform headTransform;

        [Header("설정")]

        // ------------------------------------------------------------
        /// <summary>
        /// 머리 위 위치 오프셋.
        /// </summary>
        // ------------------------------------------------------------
        public Vector3 Offset = Vector3.up;

    #endregion

    #region 컴포넌트

        private Billboard billboard;

    #endregion

    #region 초기화

        private void Awake()
        {
            GetComponents();

            if (headTransform == null)
            {
                FindHead();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 GameObject 의 Billboard 컴포넌트를 캐시한다.
        /// </summary>
        // ------------------------------------------------------------
        private void GetComponents()
        {
            if (billboard == null)
            {
                billboard = GetComponent<Billboard>();
            }
        }

    #endregion

    #region Head 탐색

        // ---------------------------------------------------------------------------------
        /// <summary>
        /// reference 하위에서 'Head' 가 포함된 이름의 트랜스폼을 찾는다. 없으면 reference 자체 사용.
        /// </summary>
        // ---------------------------------------------------------------------------------
        public void FindHead()
        {
            headTransform = FindChildContainingName(reference, "Head");

            if (headTransform != null) return;

            headTransform = reference;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 자식 트랜스폼 중 이름에 name 을 포함한 첫 트랜스폼을 재귀 탐색한다.
        /// </summary>
        // ------------------------------------------------------------
        private Transform FindChildContainingName(Transform parent, string name)
        {
            if (parent.name.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindChildContainingName(parent.GetChild(i), name);

                if (result != null) return result;
            }

            return null;
        }

    #endregion

    #region 업데이트

        private void LateUpdate()
        {
            if (root == null || reference == null || headTransform == null) return;

            root.position = headTransform.position + root.TransformDirection(Offset);
        }

    #endregion

    }
}
