/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PrefabGameObjectProvider.cs
수정일 : 2026-07-28

# 설명
프리팹을 이용하여 게임 오브젝트를 생성하는 기본 프로바이더.
오브젝트 풀링 없이 Instantiate/Destroy로 단순하게 처리한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 프리팹을 이용하여 게임 오브젝트를 생성하는 기본적인 프로바이더입니다.
    /// </summary>
    // ============================================================
    [Serializable]
    public class PrefabGameObjectProvider : IGameObjectProvider
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 인스턴스를 생성할 프리팹입니다.
        /// </summary>
        // ------------------------------------------------------------
        public GameObject Prefab
        {
            get => prefab;
            set => prefab = value;
        }

        [SerializeField]
        private GameObject prefab = null;

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 게임 오브젝트를 생성할 위치입니다.
        /// <br/> null인 경우, 루트에 생성됩니다.
        /// </summary>
        // ------------------------------------------------------------
        public Transform Parent
        {
            get => parent;
            set => parent = value;
        }

        [SerializeField]
        private Transform parent = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 Prefab Provider를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PrefabGameObjectProvider() : base() {}

        // ------------------------------------------------------------
        /// <summary>
        /// Prefab과 기본 부모 Transform을 지정해 Provider를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PrefabGameObjectProvider(GameObject prefab, Transform parent) : this()
        {
            (this.prefab, this.parent) = (prefab, parent);
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 게임 오브젝트를 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public GameObject Acquire(bool worldPositionStays = true)
        {
            if (prefab == null)
            {
                throw new NullReferenceException("프리팹이 설정되지 않았습니다.");
            }

            return GameObject.Instantiate
            (
                prefab,
                parent,
                worldPositionStays
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 게임 오브젝트를 비동기로 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public async Awaitable<GameObject> AcquireAsync(bool worldPositionStays = true)
        {
            if (prefab == null)
            {
                throw new NullReferenceException("프리팹이 설정되지 않았습니다.");
            }

            var parameters = new InstantiateParameters
            {
                parent     = parent,
                worldSpace = worldPositionStays,
            };

            var instances = await GameObject.InstantiateAsync(prefab, parameters);
            await Awaitable.MainThreadAsync();

            if
            (
                instances == null
                || instances.Length == 0
                || instances[0] == null
            )
            {
                throw new InvalidOperationException("Prefab 인스턴스를 생성하지 못했습니다.");
            }

            return instances[0];
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 게임 오브젝트를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void Release(GameObject go, bool worldPositionStays = true)
        {
            if (go == null)
            {
                throw new ArgumentNullException(nameof(go));
            }

            // Destroy 요청이 정상 접수된 시점에 Provider 소유권이 소비된다.
            GameObject.Destroy(go);
        }

    #endregion

    }
}
