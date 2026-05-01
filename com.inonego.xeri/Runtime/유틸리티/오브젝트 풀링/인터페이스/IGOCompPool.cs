/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IGOCompPool.cs
수정일 : 2026-05-01

# 설명
게임 오브젝트를 생성하는 오브젝트 풀링을 위한 인터페이스.
IGameObjectProvider를 구현하므로 풀 자체를 프로바이더로 사용할 수 있다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using inonego.Xeri;

namespace inonego.Xeri.Pool
{

    // ============================================================
    /// <summary>
    /// 게임 오브젝트를 생성하는 오브젝트 풀링을 위한 인터페이스입니다.
    /// </summary>
    // ============================================================
    public interface IGOCompPool : IGameObjectProvider
    {

    #region 필드

        // ---------------------------------------------------------------------
        /// <summary>
        /// 게임 오브젝트를 생성하는데 사용하는 게임 오브젝트 프로바이더입니다.
        /// </summary>
        // ---------------------------------------------------------------------
        public IGameObjectProvider GameObjectProvider { get; }

        // ---------------------------------------------------------------------
        /// <summary>
        /// Release되는 경우 게임 오브젝트가 Pool의 하위에 위치합니다.
        /// </summary>
        // ---------------------------------------------------------------------
        public Transform Pool { get; set; }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 특정 컴포넌트를 풀링하기 위한 인터페이스입니다.
    /// </summary>
    // ============================================================
    public interface IGOCompPool<T> : IGOCompPool, IPool<T> where T : Component
    {

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에서 컴포넌트를 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public new T Acquire(bool worldPositionStays);

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에서 컴포넌트를 비동기로 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public new Awaitable<T> AcquireAsync(bool worldPositionStays);

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에 컴포넌트를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void Release(T item, bool pushToReleased, bool worldPositionStays);

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에 이미 존재하는 아이템을 추가합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void PushToReleased(T item, bool worldPositionStays);

        // ------------------------------------------------------------
        /// <summary>
        /// Acquired된 아이템을 다른 풀의 Acquired로 이동합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void MoveAcquiredOneTo(IPool<T> other, T item, bool worldPositionStays);

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/>Released된 아이템을 다른 풀의 Released로 이동합니다.
        /// <br/>풀에 남아있는 오브젝트가 없으면 새로운 오브젝트를 생성하여 이동합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void MoveReleasedOneTo(IPool<T> other, bool worldPositionStays);

    #endregion

    }
}
