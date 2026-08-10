/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_UnityParticleSystemCuePlayer.cs
수정일 : 2026-08-10

# 설명
UnityParticleSystemCuePlayer의 Binding 배치, Transform 추적과 Prefab 렌더링 설정 보존을 검증한다.

# 테스트 구성
 B: Runtime Binding 재생
 F: Binding 누락·무효 입력 거부
========================================================================= BLOCK_HEADER_END */

using System.Collections;

using UnityEngine;
using UnityEngine.TestTools;

using NUnit.Framework;

namespace inonego.Xeri.TEST._Playback
{
    using inonego.Xeri.Playback;

    // ============================================================
    /// <summary>
    /// Unity ParticleSystem Cue Player 공개 계약 테스트.
    /// </summary>
    // ============================================================
    public sealed class TEST_UnityParticleSystemCuePlayer
    {

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// Pool 복제에 사용할 비활성 ParticleSystem 원본을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static ParticleSystem CreateParticlePrefab()
        {
            var gameObject = new GameObject("TEST_ParticlePrefab");
            gameObject.SetActive(false);

            var particle = gameObject.AddComponent<ParticleSystem>();
            var main = particle.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 10.0f;

            return particle;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Player Host 하위에서 현재 활성화된 ParticleSystem 인스턴스를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static ParticleSystem FindActiveParticle(GameObject root)
        {
            var particles = root.GetComponentsInChildren<ParticleSystem>(includeInactive: true);

            for (var index = 0; index < particles.Length; index++)
            {
                if (particles[index].gameObject.activeSelf)
                {
                    return particles[index];
                }
            }

            return null;
        }

    #endregion

    #region B-1: Runtime Binding 재생

        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> WorldPoseBinding은 지정 Pose에서 재생하고 TransformBinding은 대상 Transform 이동을 추적한다.
        /// <br/> Pool 복제 과정에서 ParticleSystemRenderer의 authored Material 참조를 Player가 변경하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_UnityParticleSystemCuePlayer_Binding배치와_Material설정을_보존()
        {
            var root = new GameObject("TEST_UnityParticleSystemCuePlayer");
            var emitter = new GameObject("TEST_Emitter");
            var prefab = CreateParticlePrefab();
            var materialSource = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cue = ScriptableObject.CreateInstance<UnityParticleSystemCue>();

            try
            {
                cue.Prefab = prefab;
                cue.UsesUnscaledTime = true;

                var authoredMaterial = materialSource.GetComponent<Renderer>().sharedMaterial;
                Assert.IsNotNull(authoredMaterial);
                prefab.GetComponent<ParticleSystemRenderer>().sharedMaterial = authoredMaterial;
                var player = root.AddComponent<UnityParticleSystemCuePlayer>();
                var service = new CuePlaybackService(new ICuePlayer[] { player });
                var pose = new WorldPoseBinding
                (
                    new Vector3(1.0f, 2.0f, 3.0f),
                    Quaternion.Euler(10.0f, 20.0f, 30.0f)
                );
                var posePlayback = service.Play(cue, in pose);
                var poseParticle = FindActiveParticle(root);

                Assert.IsNotNull(poseParticle);
                Assert.AreEqual(CuePlaybackState.Playing, posePlayback.State);
                Assert.AreEqual(pose.Position, poseParticle.transform.position);
                Assert.Less(Quaternion.Angle(pose.Rotation, poseParticle.transform.rotation), 0.01f);
                Assert.AreSame
                (
                    authoredMaterial,
                    poseParticle.GetComponent<ParticleSystemRenderer>().sharedMaterial
                );

                posePlayback.Dispose();
                Assert.AreEqual(CuePlaybackState.Released, posePlayback.State);

                emitter.transform.SetPositionAndRotation
                (
                    new Vector3(4.0f, 5.0f, 6.0f),
                    Quaternion.Euler(0.0f, 45.0f, 0.0f)
                );
                var emitterBinding = new TransformBinding(emitter.transform);
                var emitterPlayback = service.Play(cue, in emitterBinding);
                var emitterParticle = FindActiveParticle(root);

                emitter.transform.SetPositionAndRotation
                (
                    new Vector3(7.0f, 8.0f, 9.0f),
                    Quaternion.Euler(0.0f, 90.0f, 0.0f)
                );
                yield return null;

                Assert.AreEqual(emitter.transform.position, emitterParticle.transform.position);
                Assert.Less
                (
                    Quaternion.Angle(emitter.transform.rotation, emitterParticle.transform.rotation),
                    0.01f
                );

                emitterPlayback.Dispose();
                Assert.AreEqual(CuePlaybackState.Released, emitterPlayback.State);
                Assert.IsFalse(emitterParticle.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(cue);
                Object.DestroyImmediate(prefab.gameObject);
                Object.DestroyImmediate(materialSource);
                Object.DestroyImmediate(emitter);
                Object.DestroyImmediate(root);
            }
        }

    #endregion

    #region F-1: Binding 없는 fallback 거부

        // ------------------------------------------------------------
        /// <summary>
        /// 위치 Binding이 필요한 Particle Cue는 Play(cue) 호출을 임의 Host 위치로 fallback하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UnityParticleSystemCuePlayer_Binding없는_재생을_거부()
        {
            var root = new GameObject("TEST_UnityParticleSystemCuePlayer");
            var prefab = CreateParticlePrefab();
            var cue = ScriptableObject.CreateInstance<UnityParticleSystemCue>();

            try
            {
                cue.Prefab = prefab;
                var player = root.AddComponent<UnityParticleSystemCuePlayer>();
                var service = new CuePlaybackService(new ICuePlayer[] { player });

                Assert.Throws<System.InvalidOperationException>
                (
                    () => service.Play(cue)
                );
            }
            finally
            {
                Object.DestroyImmediate(cue);
                Object.DestroyImmediate(prefab.gameObject);
                Object.DestroyImmediate(root);
            }
        }

    #endregion

    #region F-2: 유효하지 않은 Transform Binding 거부

        // ------------------------------------------------------------
        /// <summary>
        /// 대상이 없는 TransformBinding은 지원 가능한 재생 조합으로 선택하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UnityParticleSystemCuePlayer_대상없는_TransformBinding을_거부()
        {
            var root = new GameObject("TEST_UnityParticleSystemCuePlayer");
            var prefab = CreateParticlePrefab();
            var cue = ScriptableObject.CreateInstance<UnityParticleSystemCue>();

            try
            {
                cue.Prefab = prefab;
                var player = root.AddComponent<UnityParticleSystemCuePlayer>();
                var service = new CuePlaybackService(new ICuePlayer[] { player });
                var binding = default(TransformBinding);

                Assert.Throws<System.InvalidOperationException>
                (
                    () => service.Play(cue, in binding)
                );
            }
            finally
            {
                Object.DestroyImmediate(cue);
                Object.DestroyImmediate(prefab.gameObject);
                Object.DestroyImmediate(root);
            }
        }

    #endregion

    #region F-3: 유효하지 않은 World Pose Binding 거부

        // ------------------------------------------------------------
        /// <summary>
        /// default WorldPoseBinding의 zero quaternion은 유효한 재생 Pose로 선택하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UnityParticleSystemCuePlayer_defaultWorldPoseBinding을_거부()
        {
            var root = new GameObject("TEST_UnityParticleSystemCuePlayer");
            var prefab = CreateParticlePrefab();
            var cue = ScriptableObject.CreateInstance<UnityParticleSystemCue>();

            try
            {
                cue.Prefab = prefab;
                var player = root.AddComponent<UnityParticleSystemCuePlayer>();
                var service = new CuePlaybackService(new ICuePlayer[] { player });
                var binding = default(WorldPoseBinding);

                Assert.Throws<System.InvalidOperationException>
                (
                    () => service.Play(cue, in binding)
                );
            }
            finally
            {
                Object.DestroyImmediate(cue);
                Object.DestroyImmediate(prefab.gameObject);
                Object.DestroyImmediate(root);
            }
        }

    #endregion

    #region F-4: Overflow World Pose Binding 거부

        // ------------------------------------------------------------
        /// <summary>
        /// Quaternion 제곱합이 overflow하는 WorldPoseBinding은 유효한 Pose로 선택하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UnityParticleSystemCuePlayer_overflowWorldPoseBinding을_거부()
        {
            var root = new GameObject("TEST_UnityParticleSystemCuePlayer");
            var prefab = CreateParticlePrefab();
            var cue = ScriptableObject.CreateInstance<UnityParticleSystemCue>();

            try
            {
                cue.Prefab = prefab;
                var player = root.AddComponent<UnityParticleSystemCuePlayer>();
                var service = new CuePlaybackService(new ICuePlayer[] { player });
                var rotation = new Quaternion(float.MaxValue, float.MaxValue, 0.0f, 0.0f);
                var binding = new WorldPoseBinding(Vector3.zero, rotation);

                Assert.Throws<System.InvalidOperationException>
                (
                    () => service.Play(cue, in binding)
                );
            }
            finally
            {
                Object.DestroyImmediate(cue);
                Object.DestroyImmediate(prefab.gameObject);
                Object.DestroyImmediate(root);
            }
        }

    #endregion

    }
}
