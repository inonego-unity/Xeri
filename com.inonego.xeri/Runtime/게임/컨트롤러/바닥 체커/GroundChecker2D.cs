/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GroundChecker2D.cs
수정일 : 2026-05-01

# 설명
Rigidbody2D/Collider2D를 사용하는 2D 바닥 체커.
BoxCollider2D, CircleCollider2D, CapsuleCollider2D를 지원한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego.Xeri;

namespace inonego.Xeri.Game.Controller
{
    // ============================================================
    /// <summary>
    /// Rigidbody2D/Collider2D를 사용하는 2D 바닥 체커입니다.
    /// </summary>
    // ============================================================
    [Serializable]
    [RequireComponent(typeof(Rigidbody2D))]
    public class GroundChecker2D : GroundChecker<Rigidbody2D, Collider2D>, INeedToInit<GameObject>
    {

    #region 필드

        public override Vector3 Gravity => Physics2D.gravity;

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

        protected override Vector3 GetLinearVelocity(Rigidbody2D rigidbody) => rigidbody.linearVelocity;
        protected override bool CheckColliderAvailable(Collider2D collider) => collider.enabled;

        // -------------------------------------------------------------
        /// <summary>
        /// Collider2D를 사용하여 바닥을 감지합니다.
        /// </summary>
        // -------------------------------------------------------------
        protected override GameObject DetectWithCollider(Collider2D collider, float deltaTime)
        {
            if (collider is BoxCollider2D boxCollider)
            {
                return DetectWithBoxCollider(boxCollider, deltaTime);
            }
            else if (collider is CircleCollider2D circleCollider)
            {
                return DetectWithCircleCollider(circleCollider, deltaTime);
            }
            else if (collider is CapsuleCollider2D capsuleCollider)
            {
                return DetectWithCapsuleCollider(capsuleCollider, deltaTime);
            }

            return null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/>BoxCollider2D를 사용하여 바닥을 감지합니다.
        /// <br/>바닥면에서 시작해서 BoxCast를 수행합니다.
        /// </summary>
        // ------------------------------------------------------------
        private GameObject DetectWithBoxCollider(BoxCollider2D boxCollider, float deltaTime)
        {
            var info = GetBoxColliderDetectionInfo(boxCollider, deltaTime);

            var center = info.Center - info.Direction * GroundCheckerConfig.Thickness * 0.5f;
            var size   = new Vector3(info.Size.x, GroundCheckerConfig.Thickness, 0);

            var hit = Physics2D.BoxCast(center, size, info.Angle, info.Direction, info.Depth, Config.Layer);

            if (hit.collider != null)
            {
                return hit.collider.gameObject;
            }

            return null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/>CircleCollider2D를 사용하여 바닥을 감지합니다.
        /// <br/>중심점에서 시작해서 CircleCast를 수행합니다.
        /// </summary>
        // ------------------------------------------------------------
        private GameObject DetectWithCircleCollider(CircleCollider2D circleCollider, float deltaTime)
        {
            var info = GetCircleColliderDetectionInfo(circleCollider, deltaTime);
            var hit  = Physics2D.CircleCast(info.Center, info.Radius, info.Direction, info.Depth, Config.Layer);

            if (hit.collider != null)
            {
                return hit.collider.gameObject;
            }

            return null;
        }

        // ------------------------------------------------------------------------------
        /// <summary>
        /// <br/>CapsuleCollider2D를 사용하여 바닥을 감지합니다.
        /// <br/>Vertical인 경우 아래쪽 반구의 중심점에서 시작해서 CircleCast를 수행합니다.
        /// <br/>Horizontal인 경우 아랫면에서 시작해서 BoxCast를 수행합니다.
        /// </summary>
        // ------------------------------------------------------------------------------
        private GameObject DetectWithCapsuleCollider(CapsuleCollider2D capsuleCollider, float deltaTime)
        {
            var info = GetCapsuleColliderDetectionInfo(capsuleCollider, deltaTime);

            RaycastHit2D hit;

            // ------------------------------------------------------------
            // 수직 캡슐 — CircleCast 사용
            // ------------------------------------------------------------
            if (info.Flag)
            {
                hit = Physics2D.CircleCast(info.Center, info.Radius, info.Direction, info.Depth, Config.Layer);
            }
            // ------------------------------------------------------------
            // 수평 캡슐 — BoxCast 사용
            // ------------------------------------------------------------
            else
            {
                var center = info.Center - info.Direction * GroundCheckerConfig.Thickness * 0.5f;
                var size   = new Vector3(info.Size.x, GroundCheckerConfig.Thickness, 0);

                hit = Physics2D.BoxCast(center, size, info.Angle, info.Direction, info.Depth, Config.Layer);
            }

            if (hit.collider != null)
            {
                return hit.collider.gameObject;
            }

            return null;
        }

    #endregion

    #region 방향 계산

        // ------------------------------------------------------------
        /// <summary>
        /// X축과 수직인 방향을 계산합니다.
        /// </summary>
        // ------------------------------------------------------------
        private Vector3 GetCrossDirection(Vector3 forward, Vector3 basisX, Vector3 basisY)
        {
            // 2D에서 X축에 수직인 방향은 90도 회전으로 구함
            Vector2 worldDirection2D = new Vector2(basisX.y, -basisX.x).normalized;

            // 기저 벡터의 외적으로 핸디드니스 결정
            float handedness = Mathf.Sign(Vector3.Cross(basisX, basisY).z);

            return worldDirection2D * handedness;
        }

    #endregion

    #region Box 범위 계산

        // ------------------------------------------------------------
        /// <summary>
        /// BoxCollider2D의 바닥 감지 계산 정보를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public GroundCheckerDetection GetBoxColliderDetectionInfo(BoxCollider2D boxCollider, float deltaTime)
        {
            return GetBoxColliderDetectionInfo(boxCollider.transform, boxCollider.offset, boxCollider.size, deltaTime);
        }

        private GroundCheckerDetection GetBoxColliderDetectionInfo(Transform boxTransform, Vector2 boxOffset, Vector2 boxSize, float deltaTime)
        {
            var info = GroundCheckerCalculation.Create(boxTransform);

            // ------------------------------------------------------------
            // 중심점 계산
            // ------------------------------------------------------------
            Vector3 localCenter = boxOffset;
            Vector3 worldCenter = boxTransform.TransformPoint(localCenter);

            // 바닥면의 중심점을 계산합니다.
            // 사각형이 찌그러지므로 변수 worldDirection 대신에 info.WorldVector를 사용합니다.
            worldCenter += info.WorldVector * boxSize.y * 0.5f;

            // ------------------------------------------------------------
            // 방향 계산
            // ------------------------------------------------------------
            var worldDirection = GetCrossDirection(boxTransform.forward, info.Basis.X, info.Basis.Y);

            // ------------------------------------------------------------
            // 크기 계산
            // ------------------------------------------------------------
            var xScale = ((Vector2)info.Basis.X).magnitude;
            var width  = boxSize.x * xScale;
            var size   = new Vector3(width, 0f, 0f);

            // ------------------------------------------------------------
            // 깊이 계산
            // ------------------------------------------------------------
            var depth = GetDepth(worldDirection, deltaTime);

            return new GroundCheckerDetection(worldCenter, size, info.XAngle2D, worldDirection, depth);
        }

    #endregion

    #region Circle 범위 계산

        // ------------------------------------------------------------
        /// <summary>
        /// CircleCollider2D의 바닥 감지 계산 정보를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public GroundCheckerDetection GetCircleColliderDetectionInfo(CircleCollider2D circleCollider, float deltaTime)
        {
            return GetCircleColliderDetectionInfo(circleCollider.transform, circleCollider.offset, circleCollider.radius, deltaTime);
        }

        private GroundCheckerDetection GetCircleColliderDetectionInfo(Transform circleTransform, Vector2 circleOffset, float circleRadius, float deltaTime)
        {
            var info = GroundCheckerCalculation.Create(circleTransform);

            // ------------------------------------------------------------
            // 중심점 계산
            // ------------------------------------------------------------
            Vector3 localCenter = circleOffset;
            Vector3 worldCenter = circleTransform.TransformPoint(localCenter);

            // ------------------------------------------------------------
            // 방향 계산
            // ------------------------------------------------------------
            var worldDirection = GetCrossDirection(circleTransform.forward, info.Basis.X, info.Basis.Y);

            // ------------------------------------------------------------
            // 반지름 계산 — 월드 스케일 적용
            // ------------------------------------------------------------
            var xScale      = Mathf.Abs(circleTransform.lossyScale.x);
            var yScale      = Mathf.Abs(circleTransform.lossyScale.y);
            var worldScale  = Mathf.Max(xScale, yScale);
            var worldRadius = circleRadius * worldScale;

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
        /// CapsuleCollider2D의 바닥 감지 계산 정보를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public GroundCheckerDetection GetCapsuleColliderDetectionInfo(CapsuleCollider2D capsuleCollider, float deltaTime)
        {
            var capsuleTransform = capsuleCollider.transform;

            var (capsuleOffset, capsuleSize) = (capsuleCollider.offset, capsuleCollider.size);

            var info = GroundCheckerCalculation.Create(capsuleTransform);

            var xScale = ((Vector2)info.Basis.X).magnitude;
            var yScale = ((Vector2)info.Basis.Y).magnitude;

            // ------------------------------------------------------------
            // 중심점 계산
            // ------------------------------------------------------------
            Vector3 localCenter = capsuleOffset;
            Vector3 worldCenter = capsuleTransform.TransformPoint(localCenter);

            if (capsuleCollider.direction == CapsuleDirection2D.Vertical)
            {
                // ------------------------------------------------------------
                // 수직 캡슐
                // ------------------------------------------------------------
                var radius = Mathf.Max(0f, capsuleSize.x * 0.5f);
                var height = Mathf.Max(0f, capsuleSize.y);

                var (worldRadius, worldHeight) = (xScale * radius, yScale * height);

                var yOffset = Mathf.Max(0f, worldHeight * 0.5f - worldRadius);

                Vector3 worldDirection = ((Vector2)info.WorldVector).normalized;

                // 바닥면의 중심점을 계산합니다.
                worldCenter += worldDirection * yOffset;

                var depth = GetDepth(worldDirection, deltaTime);

                return new GroundCheckerDetection(worldCenter, worldRadius, worldDirection, depth, true);
            }
            else
            {
                // ------------------------------------------------------------
                // 수평 캡슐
                // ------------------------------------------------------------
                var radius = Mathf.Max(0f, capsuleSize.y * 0.5f);
                var width  = Mathf.Max(0f, capsuleSize.x);

                var (worldRadius, worldWidth) = (yScale * radius, xScale * width);

                var worldDirection = GetCrossDirection(capsuleTransform.forward, info.Basis.X, info.Basis.Y);

                // 중심점을 바닥 방향으로 이동
                worldCenter += worldDirection * worldRadius;

                worldWidth = Mathf.Max(0f, worldWidth - worldRadius * 2f);

                var size  = new Vector3(worldWidth, 0f, 0f);
                var depth = GetDepth(worldDirection, deltaTime);

                return new GroundCheckerDetection(worldCenter, size, info.XAngle2D, worldDirection, depth, false);
            }
        }

    #endregion

    }
}
