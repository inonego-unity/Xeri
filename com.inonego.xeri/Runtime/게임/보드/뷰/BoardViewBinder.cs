/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : BoardViewBinder.cs
수정일 : 2026-05-28

# 설명
Board 모델 이벤트를 tile view 생성/회수 흐름으로 연결한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// Board 모델과 BoardViewBase tile view map을 동기화한다.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class BoardViewBinder<TBoard, TVector, TIndex, TSpace, TPlaceable, TTileView> : INeedToConnect<TBoard>
    where TBoard : class, IBoard<TVector, TIndex, TPlaceable>
    where TVector : struct
    where TIndex : struct
    where TSpace : class, IBoardSpace<TIndex, TPlaceable>
    where TPlaceable : class
    where TTileView : MonoBehaviour
    {

    #region 필드

        private readonly BoardViewBase<TBoard, TVector, TIndex, TSpace, TPlaceable, TTileView> view = null;
        private readonly BoardTileFactory<TTileView> factory = null;
        private readonly BoardTileViewMap<TVector, TTileView> tileMap = null;

        private TBoard connectedBoard = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 연결된 Board 모델.
        /// </summary>
        // ------------------------------------------------------------
        public TBoard ConnectedBoard => connectedBoard;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Board view, tile factory, tile map을 주입받는다.
        /// </summary>
        // ------------------------------------------------------------
        public BoardViewBinder
        (
            BoardViewBase<TBoard, TVector, TIndex, TSpace, TPlaceable, TTileView> view,
            BoardTileFactory<TTileView> factory,
            BoardTileViewMap<TVector, TTileView> tileMap
        ) : base()
        {
            this.view    = view    ?? throw new ArgumentNullException(nameof(view));
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.tileMap = tileMap ?? throw new ArgumentNullException(nameof(tileMap));
        }

    #endregion

    #region 연결

        // ------------------------------------------------------------
        /// <summary>
        /// Board 모델에 연결하고 현재 space를 tile view로 동기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Connect(TBoard board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (connectedBoard != null)
            {
                Disconnect();
            }

            connectedBoard = board;

            ReloadTileMap();

            board.OnAddSpace    += OnAddSpace;
            board.OnRemoveSpace += OnRemoveSpace;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Board 모델 연결을 해제하고 모든 tile view를 회수한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Disconnect()
        {
            if (connectedBoard != null)
            {
                connectedBoard.OnAddSpace    -= OnAddSpace;
                connectedBoard.OnRemoveSpace -= OnRemoveSpace;
            }

            RemoveTileAll();

            connectedBoard = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Board space 기준으로 tile view map을 다시 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ReloadTileMap()
        {
            if (connectedBoard == null)
            {
                throw new InvalidOperationException("Board 모델이 연결되어 있지 않습니다.");
            }

            RemoveTileAll();

            foreach (var (vector, _) in connectedBoard)
            {
                PlaceTile(vector);
            }
        }

    #endregion

    #region Tile 동기화

        // ------------------------------------------------------------
        /// <summary>
        /// vector에 tile view를 배치한다.
        /// </summary>
        // ------------------------------------------------------------
        private void PlaceTile(TVector vector)
        {
            if (tileMap.Contains(vector))
            {
                return;
            }

            if (connectedBoard == null)
            {
                return;
            }

            if (connectedBoard[vector] is not TSpace space)
            {
                return;
            }

            if (!view.CanPlaceTileFromBinder(vector, space))
            {
                return;
            }

            view.InvokePrePlaceTile(vector, space);

            var tile = factory.CreateTile(view.ToLocalPos(vector));

            try
            {
                tileMap.Register(vector, tile);
                view.InvokePlaceTile(vector, space, tile);
            }
            catch
            {
                factory.ReleaseTile(tile);

                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// vector에 대응하는 tile view를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RemoveTile(TVector vector)
        {
            if (!tileMap.TryGet(vector, out var tile))
            {
                return;
            }

            view.InvokePreRemoveTile(vector, tile);

            tileMap.Unregister(vector);
            factory.ReleaseTile(tile);

            view.InvokeRemoveTile(vector);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 tile view를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RemoveTileAll()
        {
            var vectors = new List<TVector>(tileMap.Views.Keys);

            foreach (var vector in vectors)
            {
                RemoveTile(vector);
            }
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// Board space 추가 이벤트를 tile view 배치로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnAddSpace(TVector vector)
        {
            PlaceTile(vector);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Board space 제거 이벤트를 tile view 제거로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnRemoveSpace(TVector vector)
        {
            RemoveTile(vector);
        }

    #endregion

    }
}
