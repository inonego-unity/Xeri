/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MonoEntityBehaviour.cs
수정일 : 2026-05-07

# 설명
엔티티에 종속되는 부속 MonoBehaviour 베이스.
부모 계층에서 IReadOnlyMonoEntity 를 찾아 자동 연결되며, IReadOnlyEntity 를 노출한다.
입력·충돌·AI·표현 등 다양한 역할로 사용한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// 엔티티에 종속되는 부속 MonoBehaviour 베이스 클래스.
    /// </summary>
    // ============================================================
    public abstract class MonoEntityBehaviour : MonoBehaviour
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 연결된 모노 엔티티(읽기 전용 노출).
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyMonoEntity MonoEntity => monoEntity;

        [Header("엔티티", order = -999)]
        [SerializeReference, ReadOnly]
        private IReadOnlyMonoEntity monoEntity = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 연결된 논리 엔티티(읽기 전용).
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyEntity Entity
        {
            get
            {
                if (monoEntity == null)
                {
                    throw new InvalidOperationException("모노 엔티티가 null입니다.");
                }

                return monoEntity.Entity;
            }
        }

    #endregion

    #region 생명주기

        private void Awake()
        {
            GetComponents();
        }

    #endregion

    #region 내부

        // ------------------------------------------------------------
        /// <summary>
        /// 부모 계층에서 IReadOnlyMonoEntity 를 찾아 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void GetComponents()
        {
            if (monoEntity == null)
            {
                monoEntity = GetComponentInParent<IReadOnlyMonoEntity>();
            }

            if (monoEntity == null)
            {
                throw new NullReferenceException("MonoEntityBehaviour 의 부모 오브젝트에 MonoEntity 컴포넌트가 없습니다.");
            }
        }

    #endregion

    }
}
