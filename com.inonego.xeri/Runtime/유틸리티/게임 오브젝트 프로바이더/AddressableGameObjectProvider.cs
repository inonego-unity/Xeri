/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : AddressableGameObjectProvider.cs
수정일 : 2026-05-01

# 설명
Addressables를 사용하여 게임 오브젝트를 생성하는 프로바이더.
Release 시 Addressables.ReleaseInstance로 핸들을 해제한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// Addressables를 사용하여 게임 오브젝트를 생성하는 프로바이더입니다.
    /// </summary>
    // ============================================================
    [Serializable]
    public class AddressableGameObjectProvider : IGameObjectProvider
    {

    #region 필드

        // -------------------------------------------------------------------
        /// <summary>
        /// Addressables Asset Reference를 이용하여 게임 오브젝트를 생성합니다.
        /// </summary>
        // -------------------------------------------------------------------
        public AssetReferenceGameObject AssetReference
        {
            get => assetReference;
            set => assetReference = value;
        }

        [SerializeField]
        private AssetReferenceGameObject assetReference = null;

        // ------------------------------------------------------------
        /// <summary>
        /// <br/>게임 오브젝트를 생성할 위치입니다.
        /// <br/>null인 경우, 루트에 생성됩니다.
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

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 게임 오브젝트를 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public GameObject Acquire(bool worldPositionStays = true)
        {
            if (assetReference == null)
            {
                throw new NullReferenceException("AssetReference가 설정되지 않았습니다.");
            }

            AsyncOperationHandle<GameObject> handle = assetReference.InstantiateAsync(parent, worldPositionStays);

            return handle.WaitForCompletion();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 게임 오브젝트를 비동기로 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public async Awaitable<GameObject> AcquireAsync(bool worldPositionStays = true)
        {
            if (assetReference == null)
            {
                throw new NullReferenceException("AssetReference가 설정되지 않았습니다.");
            }

            AsyncOperationHandle<GameObject> handle = assetReference.InstantiateAsync(parent, worldPositionStays);

            return await handle.Task;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 게임 오브젝트를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void Release(GameObject go, bool worldPositionStays = true)
        {
            if (go != null)
            {
                Addressables.ReleaseInstance(go);
            }
        }

    #endregion

    }
}
