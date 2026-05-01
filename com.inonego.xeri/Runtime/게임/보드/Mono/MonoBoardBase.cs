/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MonoBoardBase.cs
수정일 : 2026-05-01

# 설명
월드상에 게임 오브젝트로 존재할 수 있는 보드를 표현하기 위한 추상 클래스.
Connect() 시 보드의 OnAddSpace/OnRemoveSpace 이벤트를 구독하여 타일을 자동 관리한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Game
{
    /// --------------------------------------------------------------------------------
    /// <summary>
    /// 월드상에 게임 오브젝트로 존재할 수 있는 보드를 표현하기 위한 추상 클래스입니다.
    /// </summary>
    // --------------------------------------------------------------------------------
    [Serializable]
    public abstract class MonoBoardBase<TBoard, TVector, TIndex, TSpace, TPlaceable, TMonoTile> : MonoBehaviour, IMonoBoard<TBoard, TVector, TIndex, TSpace, TPlaceable, TMonoTile>
    where TBoard : class, IBoard<TVector, TIndex, TPlaceable>
    where TVector : struct where TIndex : struct
    where TSpace : class, IBoardSpace<TIndex, TPlaceable>
    where TPlaceable : class
    where TMonoTile : MonoBehaviour
    {

    #region 원본 보드

        [SerializeReference, HideInInspector]
        protected TBoard board;
        public TBoard Board => board;

    #endregion

    #region 필드

        [NonSerialized]
        protected Dictionary<TVector, TMonoTile> lTileMap = new();
        public IReadOnlyDictionary<TVector, TMonoTile> TileMap => lTileMap;

        [SerializeReference]
        protected IGameObjectProvider lTileProvider = new PrefabGameObjectProvider();
        public IGameObjectProvider TileProvider => lTileProvider;

    #endregion

    #region 초기 설정 및 초기화

        public virtual void Connect(TBoard board)
        {
            if (board == null)
            {
                throw new ArgumentNullException("보드가 null입니다.");
            }

            if (this.board != null)
            {
                Disconnect();
            }

            this.board = board;

            InitTileMap();

            board.OnAddSpace += OnAddSpace;
            board.OnRemoveSpace += OnRemoveSpace;
        }

        public virtual void Disconnect()
        {
            ReleaseTileMap();

            if (board != null)
            {
                board.OnAddSpace -= OnAddSpace;
                board.OnRemoveSpace -= OnRemoveSpace;
            }

            board = null;
        }

    #endregion

    #region 이벤트 핸들러

        protected virtual void OnAddSpace(TVector vector)
        {
            PlaceTile(vector);
        }

        protected virtual void OnRemoveSpace(TVector vector)
        {
            RemoveTile(vector);
        }

    #endregion

    #region 좌표 변환 메서드

        /// ------------------------------------------------------------
        /// <summary>
        /// 보드상의 벡터를 로컬 좌표로 변환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract Vector3 ToLocalPos(TVector vector);

        /// ------------------------------------------------------------
        /// <summary>
        /// 보드상의 벡터와 인덱스를 로컬 좌표로 변환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract Vector3 ToLocalPos(TVector vector, TIndex index);

        /// ------------------------------------------------------------
        /// <summary>
        /// 보드상의 포인트를 로컬 좌표로 변환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public Vector3 ToLocalPos(IBoardPoint<TVector, TIndex> point)
        {
            return ToLocalPos(point.Vector, point.Index);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 보드상의 벡터를 월드 좌표로 변환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public Vector3 ToWorldPos(TVector vector)
        {
            return lTileProvider.Parent.TransformPoint(ToLocalPos(vector));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 보드상의 벡터와 인덱스를 월드 좌표로 변환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public Vector3 ToWorldPos(TVector vector, TIndex index)
        {
            return lTileProvider.Parent.TransformPoint(ToLocalPos(vector, index));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 보드상의 포인트를 월드 좌표로 변환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public Vector3 ToWorldPos(IBoardPoint<TVector, TIndex> point)
        {
            return ToWorldPos(point.Vector, point.Index);
        }

    #endregion

    #region 타일 관리 메서드

        /// ------------------------------------------------------------
        /// <summary>
        /// 지정된 벡터에 타일을 배치할 수 있는지 확인합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual bool CanPlaceTile(TVector vector) => true;

        /// ------------------------------------------------------------
        /// <summary>
        /// 지정된 벡터에 타일을 배치합니다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void PlaceTile(TVector vector)
        {
            bool isPlaced = lTileMap.ContainsKey(vector);
            bool canPlace = CanPlaceTile(vector);

            // 타일이 배치되어 있지 않고 배치할 수 있는 경우 타일을 배치합니다.
            if (!isPlaced && canPlace)
            {
                var lTileGO = TileProvider.Acquire(worldPositionStays: false);

                if (!lTileGO.TryGetComponent(out TMonoTile lTile))
                {
                    throw new NullReferenceException($"게임 오브젝트에서 타일 컴포넌트({lTileGO.name})를 찾을 수 없습니다.");
                }

                lTileMap[vector] = lTile;

                lTileGO.transform.localPosition = ToLocalPos(vector);

                lTileGO.SetActive(true);
            }
        }

        /// ------------------------------------------------------------
        /// <summary>
        /// 지정된 벡터의 타일을 제거합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void RemoveTile(TVector vector)
        {
            RemoveTile(vector, removeFromMap: true);
        }

        protected virtual void RemoveTile(TVector vector, bool removeFromMap = true)
        {
            if (lTileMap.TryGetValue(vector, out var lTile))
            {
                TileProvider.Release(lTile.gameObject);

                if (removeFromMap)
                {
                    lTileMap.Remove(vector);
                }

                lTile.transform.localPosition = Vector3.zero;

                lTile.gameObject.SetActive(false);
            }
        }

        /// ------------------------------------------------------------
        /// <summary>
        /// 모든 타일을 제거합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void RemoveTileAll()
        {
            foreach (var vector in lTileMap.Keys)
            {
                RemoveTile(vector, removeFromMap: false);
            }

            lTileMap.Clear();
        }

    #endregion

    #region 새로고침

        /// ------------------------------------------------------------
        /// <summary>
        /// 타일 맵을 새로 구성합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void ReloadTileMap()
        {
            if (board == null)
            {
                throw new InvalidOperationException("보드가 초기화되지 않았습니다. Connect()를 먼저 호출해주세요.");
            }

            ReleaseTileMap();
            InitTileMap();
        }

        protected void InitTileMap()
        {
            if (board == null)
            {
                throw new InvalidOperationException("보드가 null입니다.");
            }

            foreach (var (vector, space) in board)
            {
                PlaceTile(vector);
            }
        }

        protected void ReleaseTileMap()
        {
            RemoveTileAll();
        }

    #endregion

    }
}
