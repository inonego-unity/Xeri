/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GroundChecker.cs
수정일 : 2026-07-25

# 설명
2D/3D 공통 로직을 담는 제네릭 추상 바닥 체커.
리지드바디와 콜라이더 타입을 파라미터로 받아 코드 재사용성을 극대화한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego.Xeri;

namespace inonego.Xeri.Game.Controller
{
    // ============================================================
    /// <summary>
    /// 2D/3D 공통 로직을 담는 제네릭 추상 바닥 체커입니다.
    /// </summary>
    // ============================================================
    public abstract class GroundChecker<TRigidbody, TCollider> : GroundCheckerBase
    where TRigidbody : Component
    where TCollider  : Component
    {

    #region 필드

        [SerializeField]
        public GroundCheckerConfig Config;

        [SerializeField, ReadOnly]
        private GameObject gameObject = null;
        public override GameObject GameObject => gameObject;

        [SerializeField]
        private TRigidbody rigid = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 오브젝트에 붙어 있는 리지드바디를 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public TRigidbody Rigid => rigid;

        [SerializeField]
        private TCollider[] colliders = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 오브젝트에 붙어 있는 콜라이더 배열을 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public TCollider[] Colliders => colliders;

        [SerializeField, ReadOnly]
        private TRigidbody groundRigid = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 밟고 있는 바닥 오브젝트의 리지드바디를 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public TRigidbody GroundRigid => groundRigid;

        // ------------------------------------------------------------
        /// <summary>
        /// Rigidbody에서 선형 속도를 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract Vector3 GetLinearVelocity(TRigidbody rigidbody);

        // ------------------------------------------------------------
        /// <summary>
        /// Rigidbody의 지정한 월드 지점 속도를 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract Vector3 GetPointVelocity(TRigidbody rigidbody, Vector3 worldPoint);

        // ------------------------------------------------------------
        /// <summary>
        /// 사용할 수 있는 Collider인지 확인합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract bool CheckColliderAvailable(TCollider collider);

        public override Vector3 Velocity
        {
            get => rigid != null ? GetLinearVelocity(rigid) : Vector3.zero;
        }

        public override Vector3 GroundLinearVelocity
        {
            get => groundRigid != null ? GetLinearVelocity(groundRigid) : Vector3.zero;
        }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// <br/>GameObject를 통해 리지드바디와 콜라이더를 초기화합니다.
        /// <br/>INeedToInit 계열 인터페이스를 통해 호출됩니다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void Init(GameObject gameObject)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException("GameObject가 null입니다.");
            }

            this.gameObject = gameObject;

            this.rigid     = gameObject.GetComponent<TRigidbody>();
            this.colliders = gameObject.GetComponents<TCollider>();

            if (rigid == null)
            {
                throw new ArgumentNullException($"{typeof(TRigidbody).Name}가 null입니다. GameObject에 {typeof(TRigidbody).Name} 컴포넌트가 필요합니다.");
            }

            if (colliders == null || colliders.Length == 0)
            {
                throw new ArgumentNullException($"{typeof(TCollider).Name}가 null 또는 빈 배열입니다. GameObject에 {typeof(TCollider).Name} 컴포넌트가 필요합니다.");
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 참조를 해제합니다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void Release()
        {
            ground      = null;
            gameObject  = null;
            rigid       = null;
            colliders   = null;
            groundRigid = null;
        }

    #endregion

    #region 메서드

        // ----------------------------------------------------------------------
        /// <summary>
        /// 지정한 월드 지점에서 현재 바닥 Rigidbody의 속도를 가져옵니다.
        /// </summary>
        // ----------------------------------------------------------------------
        public override Vector3 GetGroundPointVelocity(Vector3 worldPoint)
        {
            return groundRigid != null
                ? GetPointVelocity(groundRigid, worldPoint)
                : Vector3.zero;
        }

        // ------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/>바닥을 처리합니다. 바닥 오브젝트의 리지드바디를 캐시하고,
        /// <br/>오브젝트가 바닥 방향으로 향하고 있지 않으면 바닥으로 인정하지 않습니다.
        /// </summary>
        // ------------------------------------------------------------------------------------------------------------------------
        protected override void ProcessGround(GameObject prev, ref GameObject next)
        {
            groundRigid = null;

            if (next == null)
            {
                return;
            }

            // 바닥 오브젝트의 리지드바디를 가져옵니다.
            // 없는 경우에는 바닥의 속도를 0으로 설정합니다.
            var nextGroundRigid = next.GetComponentInParent<TRigidbody>();
            var groundVelocity  = nextGroundRigid != null ? GetLinearVelocity(nextGroundRigid) : Vector3.zero;

            var (velocity, gravity) = (GetLinearVelocity(rigid), Gravity);

            // 바닥으로 향하고 있는지 확인합니다.
            var isHeadingToGround = IsHeadingToGround(velocity, groundVelocity, gravity);

            if (!isHeadingToGround)
            {
                next        = null;
                groundRigid = null;

                return;
            }

            groundRigid = nextGroundRigid;
        }

        // -------------------------------------------------------------
        /// <summary>
        /// 바닥을 감지합니다.
        /// </summary>
        // -------------------------------------------------------------
        protected override GameObject Detect(float deltaTime)
        {
            if (rigid == null)
            {
                throw new ArgumentNullException($"{typeof(TRigidbody).Name}가 null입니다. Init 메서드를 통해 초기화해주세요.");
            }

            if (colliders == null || colliders.Length == 0)
            {
                throw new ArgumentNullException($"{typeof(TCollider).Name}가 null 또는 빈 배열입니다. Init 메서드를 통해 초기화해주세요.");
            }

            foreach (var collider in Colliders)
            {
                if (!CheckColliderAvailable(collider)) continue;

                var detected = DetectWithCollider(collider, deltaTime);

                // 바닥이 감지되면 즉시 반환
                if (detected != null)
                {
                    return detected;
                }
            }

            return null;
        }

        // -------------------------------------------------------------
        /// <summary>
        /// 콜라이더를 사용하여 바닥을 감지합니다.
        /// </summary>
        // -------------------------------------------------------------
        protected abstract GameObject DetectWithCollider(TCollider collider, float deltaTime);

        // -------------------------------------------------------------
        /// <summary>
        /// 체크할 깊이를 구합니다.
        /// </summary>
        // -------------------------------------------------------------
        protected float GetDepth(Vector3 vector, float deltaTime)
        {
            var depthByGround = Vector3.Dot(GroundLinearVelocity - Velocity, vector.normalized) * deltaTime;
            return Config.Depth + Mathf.Max(0f, depthByGround);
        }

    #endregion

    }
}
