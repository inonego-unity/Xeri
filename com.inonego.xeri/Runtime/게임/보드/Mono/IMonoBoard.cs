/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IMonoBoard.cs
수정일 : 2026-05-01

# 설명
월드상에 게임 오브젝트로 존재할 수 있는 보드를 표현하기 위한 인터페이스.
INeedToConnect<TBoard>를 상속하여 보드 연결/해제를 강제한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Game
{
    /// --------------------------------------------------------------------------------
    /// <summary>
    /// 월드상에 게임 오브젝트로 존재할 수 있는 보드를 표현하기 위한 인터페이스입니다.
    /// </summary>
    // --------------------------------------------------------------------------------
    public interface IMonoBoard<TBoard, TVector, TIndex, TBoardSpace, TPlaceable, TMonoTile> : INeedToConnect<TBoard>
    where TBoard : class, IBoard<TVector, TIndex, TPlaceable>
    where TVector : struct where TIndex : struct
    where TBoardSpace : class, IBoardSpace<TIndex, TPlaceable>
    where TPlaceable : class
    where TMonoTile : MonoBehaviour
    {
        public TBoard Board { get; }

        public IReadOnlyDictionary<TVector, TMonoTile> TileMap { get; }

        public Vector3 ToLocalPos(TVector vector);
        public Vector3 ToLocalPos(TVector vector, TIndex index);
        public Vector3 ToLocalPos(IBoardPoint<TVector, TIndex> point);

        public Vector3 ToWorldPos(TVector vector);
        public Vector3 ToWorldPos(TVector vector, TIndex index);
        public Vector3 ToWorldPos(IBoardPoint<TVector, TIndex> point);
    }
}
