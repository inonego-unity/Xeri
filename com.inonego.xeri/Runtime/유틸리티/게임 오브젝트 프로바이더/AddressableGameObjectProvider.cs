/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : AddressableGameObjectProvider.cs
수정일 : 2026-07-28

# 설명
Addressables를 사용하여 게임 오브젝트를 생성하는 프로바이더.
Release 시 Addressables.ReleaseInstance로 핸들을 해제한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.AddressableAssets;

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

        // ----------------------------------------------------------------------
        /// <summary>
        /// Addressables Asset Reference를 이용하여 게임 오브젝트를 생성합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        public AssetReferenceGameObject AssetReference
        {
            get => assetReference;
            set => assetReference = value;
        }

        [SerializeField]
        private AssetReferenceGameObject assetReference = null;

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

            var handle = assetReference.InstantiateAsync(parent, worldPositionStays);

            try
            {
                var acquired = handle.WaitForCompletion();

                if (acquired != null)
                {
                    return acquired;
                }

                throw new InvalidOperationException
                (
                    "Addressables가 게임 오브젝트 인스턴스를 생성하지 못했습니다.",
                    handle.OperationException
                );
            }
            catch
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                throw;
            }
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

            var handle = assetReference.InstantiateAsync(parent, worldPositionStays);
            GameObject acquired;

            try
            {
                acquired = await handle.Task;
            }
            catch
            {
                await Awaitable.MainThreadAsync();

                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                throw;
            }

            await Awaitable.MainThreadAsync();

            if (acquired != null)
            {
                return acquired;
            }

            // 실패한 operation의 참조를 해제한 뒤 원래 실패 원인을 호출자에게 전달한다.
            var operationException = handle.OperationException;

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            throw new InvalidOperationException
            (
                "Addressables가 게임 오브젝트 인스턴스를 생성하지 못했습니다.",
                operationException
            );
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

            // false는 Addressables가 인스턴스 소유권을 소비하지 않았다는 의미이므로 성공으로 숨기지 않는다.
            if (!Addressables.ReleaseInstance(go))
            {
                throw new InvalidOperationException
                (
                    $"Addressables가 게임 오브젝트 '{go.name}'의 소유권을 확인하지 못해 반환하지 않았습니다."
                );
            }
        }

    #endregion

    }
}
