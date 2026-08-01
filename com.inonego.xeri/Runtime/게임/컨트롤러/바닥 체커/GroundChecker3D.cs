/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GroundChecker3D.cs
수정일 : 2026-08-01

# 설명
Rigidbody/Collider를 사용하는 3D 바닥 체커.
BoxCollider, SphereCollider, CapsuleCollider를 지원하며,
Overlap은 지면만, Cast는 GroundHit까지 표본에 기록한다.
GC 할당 방지를 위해 재사용 가능한 콜라이더 배열을 관리한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego.Xeri;

namespace inonego.Xeri.Game.Controller
{
    // ============================================================
    /// <summary>
    /// Rigidbody/Collider를 사용하는 3D 바닥 체커입니다.
    /// </summary>
    // ============================================================
    [Serializable]
    [RequireComponent(typeof(Rigidbody))]
    public class GroundChecker3D : GroundChecker<Rigidbody, Collider, GroundCheckSample3D>, INeedToInit<GameObject>
    {

    #region 필드

        public override Vector3 Gravity => Physics.gravity;

        // GC 할당 방지를 위한 재사용 가능한 콜라이더 배열
        private readonly Collider[] overlappingColliders = new Collider[1];

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// GameObject를 통해 초기화합니다.
        /// </summary>
        // ------------------------------------------------------------
        public override void Init(GameObject gameObject)
        {
            base.Init(gameObject);
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Rigidbody의 선형 속도를 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        protected override Vector3 GetLinearVelocity(Rigidbody rigidbody) => rigidbody.linearVelocity;

        // ------------------------------------------------------------
        /// <summary>
        /// Rigidbody의 월드 각속도를 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        protected override Vector3 GetAngularVelocity(Rigidbody rigidbody) => rigidbody.angularVelocity;

        // ------------------------------------------------------------
        /// <summary>
        /// Rigidbody의 지정한 월드 지점 속도를 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        protected override Vector3 GetPointVelocity(Rigidbody rigidbody, Vector3 worldPoint) => rigidbody.GetPointVelocity(worldPoint);

        // ------------------------------------------------------------
        /// <summary>
        /// 지면 감지에 사용할 수 있는 Collider인지 확인합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected override bool CheckColliderAvailable(Collider collider) => collider.enabled && !collider.isTrigger;

        // -------------------------------------------------------------
        /// <summary>
        /// Collider를 사용하여 바닥을 감지합니다.
        /// </summary>
        // -------------------------------------------------------------
        protected override GroundCheckSample3D DetectWithCollider(Collider collider, float deltaTime)
        {
            if (collider is BoxCollider boxCollider)
            {
                return DetectWithBoxCollider(boxCollider, deltaTime);
            }
            else if (collider is SphereCollider sphereCollider)
            {
                return DetectWithSphereCollider(sphereCollider, deltaTime);
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                return DetectWithCapsuleCollider(capsuleCollider, deltaTime);
            }

            return default;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/>BoxCollider를 사용하여 바닥을 감지합니다.
        /// <br/>먼저 OverlapBox로 체크하고, 없으면 BoxCast를 수행합니다.
        /// </summary>
        // ------------------------------------------------------------
        private GroundCheckSample3D DetectWithBoxCollider(BoxCollider boxCollider, float deltaTime)
        {
            var info = GetBoxColliderDetectionInfo(boxCollider, deltaTime);

            var center      = info.Center - info.Direction * GroundCheckerConfig.Thickness * 0.5f;
            var size        = new Vector3(info.Size.x, GroundCheckerConfig.Thickness, info.Size.z);
            var orientation = boxCollider.transform.rotation;

            // ------------------------------------------------------------
            // 먼저 초기 위치에서 OverlapBox 체크
            // ------------------------------------------------------------
            int overlapCount = Physics.OverlapBoxNonAlloc
            (
                center,
                size * 0.5f,
                overlappingColliders,
                orientation,
                Config.Layer,
                QueryTriggerInteraction.Ignore
            );

            if (overlapCount > 0)
            {
                return BuildSample(overlappingColliders[0], null);
            }

            // ------------------------------------------------------------
            // OverlapBox에서 감지되지 않으면 BoxCast 수행
            // ------------------------------------------------------------
            if (Physics.BoxCast(center, size * 0.5f, info.Direction, out RaycastHit hit, orientation, info.Depth, Config.Layer, QueryTriggerInteraction.Ignore))
            {
                return CreateSample(hit);
            }

            return default;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/>SphereCollider를 사용하여 바닥을 감지합니다.
        /// <br/>먼저 OverlapSphere로 체크하고, 없으면 SphereCast를 수행합니다.
        /// </summary>
        // ------------------------------------------------------------
        private GroundCheckSample3D DetectWithSphereCollider(SphereCollider sphereCollider, float deltaTime)
        {
            var info = GetSphereColliderDetectionInfo(sphereCollider, deltaTime);

            // ------------------------------------------------------------
            // 먼저 초기 위치에서 OverlapSphere 체크
            // ------------------------------------------------------------
            int overlapCount = Physics.OverlapSphereNonAlloc
            (
                info.Center,
                info.Radius,
                overlappingColliders,
                Config.Layer,
                QueryTriggerInteraction.Ignore
            );

            if (overlapCount > 0)
            {
                return BuildSample(overlappingColliders[0], null);
            }

            // ------------------------------------------------------------
            // OverlapSphere에서 감지되지 않으면 SphereCast 수행
            // ------------------------------------------------------------
            if (Physics.SphereCast(info.Center, info.Radius, info.Direction, out RaycastHit hit, info.Depth, Config.Layer, QueryTriggerInteraction.Ignore))
            {
                return CreateSample(hit);
            }

            return default;
        }

        // ------------------------------------------------------------------------------
        /// <summary>
        /// <br/>CapsuleCollider를 사용하여 바닥을 감지합니다.
        /// <br/>먼저 OverlapSphere로 체크하고, 없으면 SphereCast를 수행합니다.
        /// </summary>
        // ------------------------------------------------------------------------------
        private GroundCheckSample3D DetectWithCapsuleCollider(CapsuleCollider capsuleCollider, float deltaTime)
        {
            var info = GetCapsuleColliderDetectionInfo(capsuleCollider, deltaTime);

            // ------------------------------------------------------------
            // 수직 캡슐
            // ------------------------------------------------------------
            if (info.Flag)
            {
                int overlapCount = Physics.OverlapSphereNonAlloc
                (
                    info.Center,
                    info.Radius,
                    overlappingColliders,
                    Config.Layer,
                    QueryTriggerInteraction.Ignore
                );

                if (overlapCount > 0)
                {
                    return BuildSample(overlappingColliders[0], null);
                }

                if (Physics.SphereCast(info.Center, info.Radius, info.Direction, out RaycastHit hit, info.Depth, Config.Layer, QueryTriggerInteraction.Ignore))
                {
                    return CreateSample(hit);
                }
            }
            // ------------------------------------------------------------
            // 수평 캡슐 — 미구현
            // ------------------------------------------------------------

            return default;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 3D Cast 결과를 공통 바닥 표본으로 변환합니다.
        /// </summary>
        // ------------------------------------------------------------
        private GroundCheckSample3D CreateSample(RaycastHit hit)
        {
            return BuildSample(hit.collider, new GroundHit(hit.distance, hit.point, hit.normal));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 3D 바닥 표본을 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected override GroundCheckSample3D CreateSample(Collider groundCollider, Rigidbody groundRigid, GroundHit? hit)
        {
            return new GroundCheckSample3D(groundCollider, groundRigid, hit);
        }

    #endregion

    #region Box 범위 계산

        // ------------------------------------------------------------
        /// <summary>
        /// BoxCollider의 바닥 감지 계산 정보를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public GroundCheckerDetection GetBoxColliderDetectionInfo(BoxCollider boxCollider, float deltaTime)
        {
            return GetBoxColliderDetectionInfo(boxCollider.transform, boxCollider.center, boxCollider.size, deltaTime);
        }

        private GroundCheckerDetection GetBoxColliderDetectionInfo(Transform boxTransform, Vector3 boxCenter, Vector3 boxSize, float deltaTime)
        {
            var info = GroundCheckerCalculation.Create(boxTransform);

            // ------------------------------------------------------------
            // 방향 계산
            // ------------------------------------------------------------
            var worldDirection = info.WorldDirection;

            // ------------------------------------------------------------
            // 크기 계산
            // ------------------------------------------------------------
            var scale = Vector3.Scale(boxSize, boxTransform.lossyScale);
            var size  = new Vector3(scale.x, 0f, scale.z);

            // ------------------------------------------------------------
            // 중심점 계산
            // ------------------------------------------------------------
            Vector3 localCenter = boxCenter;
            Vector3 worldCenter = boxTransform.TransformPoint(localCenter);

            // 바닥면의 중심점을 계산합니다.
            worldCenter += worldDirection * scale.y * 0.5f;

            // ------------------------------------------------------------
            // 깊이 계산
            // ------------------------------------------------------------
            var depth = GetDepth(worldDirection, deltaTime);

            return new GroundCheckerDetection(worldCenter, size, worldDirection, depth);
        }

    #endregion

    #region Sphere 범위 계산

        // ------------------------------------------------------------
        /// <summary>
        /// SphereCollider의 바닥 감지 계산 정보를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public GroundCheckerDetection GetSphereColliderDetectionInfo(SphereCollider sphereCollider, float deltaTime)
        {
            return GetSphereColliderDetectionInfo(sphereCollider.transform, sphereCollider.center, sphereCollider.radius, deltaTime);
        }

        private GroundCheckerDetection GetSphereColliderDetectionInfo(Transform sphereTransform, Vector3 sphereCenter, float sphereRadius, float deltaTime)
        {
            var info = GroundCheckerCalculation.Create(sphereTransform);

            // ------------------------------------------------------------
            // 중심점 계산
            // ------------------------------------------------------------
            Vector3 localCenter = sphereCenter;
            Vector3 worldCenter = sphereTransform.TransformPoint(localCenter);

            // ------------------------------------------------------------
            // 방향 계산
            // ------------------------------------------------------------
            var worldDirection = info.WorldDirection;

            // ------------------------------------------------------------
            // 반지름 계산 — 월드 스케일 적용
            // ------------------------------------------------------------
            var worldScale  = sphereTransform.lossyScale;
            var worldRadius = sphereRadius * Mathf.Max(worldScale.x, worldScale.y, worldScale.z);

            // ------------------------------------------------------------
            // 깊이 계산
            // ------------------------------------------------------------
            var depth = GetDepth(worldDirection, deltaTime);

            return new GroundCheckerDetection(worldCenter, worldRadius, worldDirection, depth);
        }

    #endregion

    #region Capsule 범위 계산

        // ------------------------------------------------------------
        /// <summary>
        /// CapsuleCollider의 바닥 감지 계산 정보를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public GroundCheckerDetection GetCapsuleColliderDetectionInfo(CapsuleCollider capsuleCollider, float deltaTime)
        {
            var capsuleTransform = capsuleCollider.transform;
            var (capsuleCenter, capsuleRadius, capsuleHeight) = (capsuleCollider.center, capsuleCollider.radius, capsuleCollider.height);

            if (capsuleCollider.direction == 1) // Y축 (수직)
            {
                var info = GroundCheckerCalculation.Create(capsuleTransform);

                // ------------------------------------------------------------
                // 중심점 계산
                // ------------------------------------------------------------
                Vector3 localCenter = capsuleCenter;
                Vector3 worldCenter = capsuleTransform.TransformPoint(localCenter);

                // ------------------------------------------------------------
                // 방향 계산
                // ------------------------------------------------------------
                var worldDirection = info.WorldDirection;

                // ------------------------------------------------------------
                // 크기 계산
                // ------------------------------------------------------------
                var worldScale  = capsuleTransform.lossyScale;
                var worldRadius = capsuleRadius * Mathf.Max(worldScale.x, worldScale.z);
                var worldHeight = capsuleHeight * worldScale.y;

                var yOffset = Mathf.Max(0f, worldHeight * 0.5f - worldRadius);

                // 바닥면의 중심점을 계산합니다.
                worldCenter += worldDirection * yOffset;

                // ------------------------------------------------------------
                // 깊이 계산
                // ------------------------------------------------------------
                var depth = GetDepth(worldDirection, deltaTime);

                return new GroundCheckerDetection(worldCenter, worldRadius, worldDirection, depth, true);
            }
            else
            {
                // 수평 캡슐 — 빈 구현
                return new GroundCheckerDetection(Vector3.zero, Vector3.zero, 0f, Vector3.down, 0f, false);
            }
        }

    #endregion

    }
}
