/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Spatial.cs
수정일 : 2026-10-31

# 설명
공간 관측·고정·참조 소실의 공개 계약을 검증한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Spatial;

namespace inonego.Xeri.TEST.Spatial
{
    // ============================================================
    /// <summary>
    /// 독립 Spatial 실행 시각과 결과를 검증한다.
    /// </summary>
    // ============================================================
    public sealed class TEST_Spatial
    {

    #region 테스트 데이터

        private sealed class Reference
        {
            public Vector3 Position;
            public Quaternion Rotation = Quaternion.identity;
            public bool IsValid = true;
            public int Calls;
        }

        private sealed class Provider : ISpatialSourceProvider<Reference>
        {
            public bool TryResolve
            (
                in Reference reference,
                out Vector3 position,
                out Quaternion rotation,
                out bool hasRotation
            )
            {
                position = default;
                rotation = default;
                hasRotation = false;
                if (reference == null) return false;

                reference.Calls++;
                if (!reference.IsValid) return false;

                position = reference.Position;
                rotation = reference.Rotation;
                hasRotation = true;
                return true;
            }
        }

        private static SpatialRequest<Reference> Target(Reference reference) =>
            new
            (
                SpatialSource<Reference>.FromReference(reference),
                SpatialAimMode.TowardSource
            );

    #endregion

    #region 공간 해석

        [Test]
        public void WorldPoint와DirectionOnly가독립적으로유효함()
        {
            var point = new SpatialSession<Reference>
            (
                new SpatialRequest<Reference>
                (
                    SpatialSource<Reference>.Point(new Vector3(3f, 0f, 4f)),
                    SpatialAimMode.TowardSource
                ),
                SpatialTimeMode.Capture
            );
            point.Begin(0f, Vector3.zero);

            Assert.AreEqual(new Vector3(3f, 0f, 4f), point.Current.Position);
            Assert.AreEqual(new Vector3(0.6f, 0f, 0.8f), point.Current.Direction);
            Assert.IsFalse(point.Current.HasRotation);

            var direction = new SpatialSession<Reference>
            (
                new SpatialRequest<Reference>
                (
                    SpatialSource<Reference>.None,
                    SpatialAimMode.Explicit,
                    Vector3.right * 8f
                ),
                SpatialTimeMode.Follow
            );
            direction.Begin(0f, Vector3.zero);

            Assert.IsFalse(direction.Current.HasPosition);
            Assert.IsTrue(direction.Current.HasDirection);
            Assert.AreEqual(Vector3.right, direction.Current.Direction);
        }

        [Test]
        public void Reference를평가마다한번읽고Current는캐시함()
        {
            var reference = new Reference
            {
                Position = new Vector3(3f, 0f, 4f),
                Rotation = new Quaternion(0f, 0.7071068f, 0f, 0.7071068f)
            };
            var session = new SpatialSession<Reference>
            (
                Target(reference), SpatialTimeMode.Follow, new Provider()
            );

            session.Begin(0f, Vector3.zero);
            reference.Position = Vector3.right * 9f;

            Assert.AreEqual(1, reference.Calls);
            Assert.AreEqual(new Vector3(3f, 0f, 4f), session.Current.Position);
            Assert.AreEqual(new Vector3(0.6f, 0f, 0.8f), session.Current.Direction);
            Assert.IsTrue(session.Current.HasRotation);

            session.Evaluate(1f, Vector3.zero);
            Assert.AreEqual(2, reference.Calls);
            Assert.AreEqual(Vector3.right * 9f, session.Current.Position);
            Assert.AreEqual(Vector3.right, session.Current.Direction);
        }

    #endregion

    #region 시간 정책

        [Test]
        public void Capture와재시작은서로다른시점의값을고정함()
        {
            var reference = new Reference
            {
                Position = Vector3.right
            };
            var session = new SpatialSession<Reference>
            (
                Target(reference), SpatialTimeMode.Capture, new Provider()
            );

            session.Begin(0f, Vector3.zero);
            reference.Position = Vector3.forward;
            session.Evaluate(1f, Vector3.zero);

            Assert.AreEqual(Vector3.right, session.Current.Position);
            Assert.AreEqual(1, reference.Calls);

            session.Begin(0f, Vector3.zero);
            Assert.AreEqual(Vector3.forward, session.Current.Position);
            Assert.AreEqual(2, reference.Calls);
        }

        [Test]
        public void LockTime오버슈트는해당Tick값으로고정하고중복평가는무시함()
        {
            var reference = new Reference
            {
                Position = Vector3.right
            };
            var session = new SpatialSession<Reference>
            (
                Target(reference),
                SpatialTimeMode.LockAtTime,
                new Provider(),
                0.5f
            );

            session.Begin(0f, Vector3.zero);
            reference.Position = Vector3.forward * 2f;
            session.Evaluate(0.3f, Vector3.zero);
            session.Evaluate(0.3f, Vector3.right);
            Assert.AreEqual(2, reference.Calls);

            reference.Position = Vector3.right * 4f;
            session.Evaluate(0.6f, Vector3.zero);
            Assert.IsTrue(session.IsLocked);
            Assert.AreEqual(Vector3.right * 4f, session.Current.Position);

            reference.Position = Vector3.back;
            session.Evaluate(1f, Vector3.zero);
            Assert.AreEqual(Vector3.right * 4f, session.Current.Position);
            Assert.AreEqual(3, reference.Calls);
            Assert.Throws<InvalidOperationException>
            (
                () => session.Evaluate(0.9f, Vector3.zero)
            );
        }

        [Test]
        public void LockTime이0이면Begin에서즉시고정함()
        {
            var reference = new Reference
            {
                Position = Vector3.forward
            };
            var session = new SpatialSession<Reference>
            (
                Target(reference),
                SpatialTimeMode.LockAtTime,
                new Provider(),
                0f
            );

            session.Begin(0f, Vector3.zero);
            reference.Position = Vector3.right;
            session.Evaluate(1f, Vector3.zero);

            Assert.IsTrue(session.IsLocked);
            Assert.AreEqual(Vector3.forward, session.Current.Position);
            Assert.AreEqual(1, reference.Calls);
        }

    #endregion

    #region 실패 경계

        [Test]
        public void Reference소실은파생필드만무효화하고명시적방향은유지함()
        {
            var reference = new Reference
            {
                Position = Vector3.right
            };
            var request = new SpatialRequest<Reference>
            (
                SpatialSource<Reference>.FromReference(reference),
                SpatialAimMode.Explicit,
                Vector3.forward
            );
            var session = new SpatialSession<Reference>
            (
                request, SpatialTimeMode.Follow, new Provider()
            );

            session.Begin(0f, Vector3.zero);
            Assert.IsTrue(session.Current.HasPosition);

            reference.IsValid = false;
            session.Evaluate(1f, Vector3.zero);

            Assert.IsFalse(session.Current.HasPosition);
            Assert.IsFalse(session.Current.HasRotation);
            Assert.IsTrue(session.Current.HasDirection);
            Assert.AreEqual(Vector3.forward, session.Current.Direction);
        }

        [Test]
        public void TEST_원점과목표가같으면방향은무효이고위치는유효함()
        {
            var request = new SpatialRequest<Reference>
            (
                SpatialSource<Reference>.Point(Vector3.right),
                SpatialAimMode.TowardSource
            );
            var session = new SpatialSession<Reference>
            (
                request, SpatialTimeMode.Capture
            );
            session.Begin(0f, Vector3.right);

            Assert.IsTrue(session.Current.HasPosition);
            Assert.IsFalse(session.Current.HasDirection);
        }

    #endregion

    }
}
