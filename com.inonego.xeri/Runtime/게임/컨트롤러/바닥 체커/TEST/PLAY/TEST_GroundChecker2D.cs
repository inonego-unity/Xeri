/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_GroundChecker2D.cs
수정일 : 2026-08-01

# 설명
GroundChecker2D 시스템의 Play Mode 테스트.
GroundCheckSample의 Cast 정보와 시작 중첩 표현을 검증한다.
Box, Circle, VerticalCapsule, HorizontalCapsule 4종 콜라이더로
Kinematic→Dynamic 전환 후 착지(OnLand) → 점프 → 이탈(OnLeave) 흐름을 검증한다.

# 테스트 구성
 S: GroundCheckSample 결과
 E: 기본 기능 (착지/이탈 이벤트 통합 흐름)
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

using NUnit;
using NUnit.Framework;

using inonego.Xeri.Game.Controller;

namespace inonego.Xeri.TEST.Game.Controller._GroundChecker
{

    // ============================================================
    /// <summary>
    /// GroundChecker2D Play Mode 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_GroundChecker2D
    {

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// Space키 입력을 체크합니다.
        /// </summary>
        // ------------------------------------------------------------
        private bool IsSpaceKeyPressed()
        {
        #if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        #else
            return Input.GetKeyDown(KeyCode.Space);
        #endif
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 스프라이트를 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        private Sprite CreateSprite()
        {
            return Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 바닥 오브젝트를 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        private GameObject CreateGroundObject(int layer)
        {
            var groundObject = new GameObject("Ground");
            groundObject.transform.position = new Vector3(0f, -4f, 0f);
            groundObject.transform.localScale = new Vector3(20f, 4f, 20f);
            groundObject.layer = layer;

            var groundRigidbody = groundObject.AddComponent<Rigidbody2D>();
            var groundCollider  = groundObject.AddComponent<BoxCollider2D>();
            var spriteRenderer  = groundObject.AddComponent<SpriteRenderer>();

            spriteRenderer.sprite = CreateSprite();
            spriteRenderer.color  = Color.white;

            groundRigidbody.bodyType = RigidbodyType2D.Kinematic;

            return groundObject;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 카메라 오브젝트를 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        private GameObject CreateCameraObject()
        {
            var cameraObject = new GameObject("Camera");

            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);

            return cameraObject;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 플레이어 위치를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        private Vector3 GetPlayerPosition(int index)
        {
            var startX  = -6f;
            var spacing = 4f;

            return new Vector3(startX + spacing * index, 1f, 0f);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 플레이어 오브젝트를 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        private GameObject CreatePlayerObject(string name, int index)
        {
            var playerObject = new GameObject(name);

            var parent = new GameObject($"{playerObject.name}_Parent");
            parent.transform.position = playerObject.transform.position;
            playerObject.transform.SetParent(parent.transform);

            playerObject.transform.position = GetPlayerPosition(index);

            var playerRigidbody = playerObject.AddComponent<Rigidbody2D>();

            playerRigidbody.bodyType    = RigidbodyType2D.Kinematic;
            playerRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

            return playerObject;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// GroundCheckSample 검증에 사용할 2D 지면과 원형 검사 오브젝트를 생성합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        private (GameObject Ground, BoxCollider2D GroundCollider, Rigidbody2D GroundRigid,
            GameObject Player, GroundChecker2D Checker) CreateSampleFixture(float playerY)
        {
            const int groundLayer = 8;

            var ground = new GameObject("SampleGround");
            ground.layer = groundLayer;
            ground.transform.position = new Vector3(0f, -0.5f, 0f);

            var groundCollider = ground.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2(4f, 1f);

            var groundRigid = ground.AddComponent<Rigidbody2D>();
            groundRigid.bodyType = RigidbodyType2D.Kinematic;

            var player = new GameObject("SamplePlayer");
            player.transform.position = new Vector3(0f, playerY, 0f);

            var playerRigid = player.AddComponent<Rigidbody2D>();
            playerRigid.bodyType = RigidbodyType2D.Kinematic;

            var playerCollider = player.AddComponent<CircleCollider2D>();
            playerCollider.radius = 0.5f;

            var checker = new GroundChecker2D
            {
                Config = new GroundCheckerConfig
                {
                    Layer = 1 << groundLayer,
                    Depth = 0.25f,
                },
            };
            checker.Init(player);

            return (ground, groundCollider, groundRigid, player, checker);
        }

    #endregion

    #region S-1: GroundCheckSample 결과

        // ----------------------------------------------------------------------
        /// <summary>
        /// Cast로 감지한 지면과 표면 정보가 Sample에 기록되고 같은 지면에서도 갱신되는지 검증합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_GroundChecker2D_Cast결과를_Sample에_기록하고_갱신한다()
        {
            var fixture = CreateSampleFixture(0.6f);
            var landCount = 0;
            var leaveCount = 0;

            fixture.Checker.OnLand += (_, _) => landCount++;
            fixture.Checker.OnLeave += (_, _) => leaveCount++;

            try
            {
                Physics2D.SyncTransforms();
                fixture.Checker.Check(Time.fixedDeltaTime);

                var firstSample = fixture.Checker.Sample;

                Assert.That(firstSample.HasGround, Is.True);
                Assert.That(firstSample.Ground, Is.SameAs(fixture.Ground));
                Assert.That(firstSample.GroundCollider, Is.SameAs(fixture.GroundCollider));
                Assert.That(firstSample.GroundRigid, Is.SameAs(fixture.GroundRigid));
                Assert.That(firstSample.Hit.HasValue, Is.True);

                var firstHit = firstSample.Hit.Value;
                Assert.That(firstHit.Distance, Is.EqualTo(0.1f).Within(0.02f));
                Assert.That(firstHit.Point.y, Is.EqualTo(0f).Within(0.02f));
                Assert.That(firstHit.Normal.y, Is.GreaterThan(0.9f));
                Assert.That(landCount, Is.EqualTo(1));
                Assert.That(leaveCount, Is.Zero);

                // 같은 지면을 유지해도 다음 Tick의 거리 정보가 이전 표본에 머물지 않아야 합니다.
                fixture.Player.transform.position = new Vector3(0f, 0.55f, 0f);
                Physics2D.SyncTransforms();
                fixture.Checker.Check(Time.fixedDeltaTime);

                var nextSample = fixture.Checker.Sample;
                Assert.That(nextSample.Hit.HasValue, Is.True);
                Assert.That(nextSample.Hit.Value.Distance, Is.LessThan(firstHit.Distance));
                Assert.That(landCount, Is.EqualTo(1));
                Assert.That(leaveCount, Is.Zero);

                // 감지 범위를 벗어나면 이전 Ground와 Hit을 함께 남기지 않아야 합니다.
                fixture.Player.transform.position = new Vector3(0f, 2f, 0f);
                Physics2D.SyncTransforms();
                fixture.Checker.Check(Time.fixedDeltaTime);

                var clearedSample = fixture.Checker.Sample;
                Assert.That(clearedSample.HasGround, Is.False);
                Assert.That(clearedSample.Hit.HasValue, Is.False);
                Assert.That(landCount, Is.EqualTo(1));
                Assert.That(leaveCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fixture.Player);
                UnityEngine.Object.DestroyImmediate(fixture.Ground);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 시작부터 중첩된 지면은 감지하되 Cast 표면 정보는 제공하지 않는지 검증합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_GroundChecker2D_시작중첩은_Ground만_기록한다()
        {
            var previousQueriesStartInColliders = Physics2D.queriesStartInColliders;
            var fixture = CreateSampleFixture(0.45f);

            try
            {
                Physics2D.queriesStartInColliders = true;
                Physics2D.SyncTransforms();
                fixture.Checker.Check(Time.fixedDeltaTime);

                var sample = fixture.Checker.Sample;

                Assert.That(sample.HasGround, Is.True);
                Assert.That(sample.GroundCollider, Is.SameAs(fixture.GroundCollider));
                Assert.That(sample.GroundRigid, Is.SameAs(fixture.GroundRigid));
                Assert.That(sample.Hit.HasValue, Is.False);
            }
            finally
            {
                Physics2D.queriesStartInColliders = previousQueriesStartInColliders;
                UnityEngine.Object.DestroyImmediate(fixture.Player);
                UnityEngine.Object.DestroyImmediate(fixture.Ground);
            }
        }

    #endregion

    #region E-1: 착지 및 이탈 이벤트 통합

        [Explicit]
        [Category("Manual")]
        [UnityTest]
        public IEnumerator TEST_GroundChecker2D_4종_콜라이더_착지_이탈_이벤트()
        {
            var groundLayer = 1;
            var prevIgnore  = Physics2D.GetIgnoreLayerCollision(0, groundLayer);
            Physics2D.IgnoreLayerCollision(0, groundLayer, false);

            try
            {
                var monoForTEST = new GameObject("MonoForTEST").AddComponent<MonoForTEST>();

                // ------------------------------------------------------------
                // 테스트 준비
                // ------------------------------------------------------------
                var groundObject = CreateGroundObject(groundLayer);
                var cameraObject = CreateCameraObject();

                var players        = new List<GameObject>();
                var groundCheckers = new List<GroundChecker2D>();
                var gizmoDrawers   = new List<GroundChecker2DGizmoDrawer>();

                var boxPlayer              = CreatePlayerObject("BoxPlayer", 0);
                var circlePlayer           = CreatePlayerObject("CirclePlayer", 1);
                var verticalCapsulePlayer  = CreatePlayerObject("VerticalCapsulePlayer", 2);
                var horizontalCapsulePlayer = CreatePlayerObject("HorizontalCapsulePlayer", 3);

                // BoxCollider2D
                boxPlayer.transform.localScale = new Vector3(1f, 1f, 1f);
                var boxCollider = boxPlayer.AddComponent<BoxCollider2D>();
                boxCollider.size = new Vector2(1f, 1f);

                // CircleCollider2D
                circlePlayer.transform.localScale = new Vector3(1f, 1f, 1f);
                var circleCollider = circlePlayer.AddComponent<CircleCollider2D>();
                circleCollider.radius = 0.5f;

                // CapsuleCollider2D(수직)
                verticalCapsulePlayer.transform.localScale = new Vector3(1f, 2f, 1f);
                var verticalCapsuleCollider = verticalCapsulePlayer.AddComponent<CapsuleCollider2D>();
                verticalCapsuleCollider.direction = CapsuleDirection2D.Vertical;
                verticalCapsuleCollider.size = new Vector2(1f, 2f);

                // CapsuleCollider2D(수평)
                horizontalCapsulePlayer.transform.localScale = new Vector3(2f, 1f, 1f);
                var horizontalCapsuleCollider = horizontalCapsulePlayer.AddComponent<CapsuleCollider2D>();
                horizontalCapsuleCollider.direction = CapsuleDirection2D.Horizontal;
                horizontalCapsuleCollider.size = new Vector2(2f, 1f);

                players.Add(boxPlayer);
                players.Add(circlePlayer);
                players.Add(verticalCapsulePlayer);
                players.Add(horizontalCapsulePlayer);

                foreach (var player in players)
                {
                    var groundChecker = new GroundChecker2D();
                    groundChecker.Config = new GroundCheckerConfig { Layer = groundLayer.ToLayerMask(), Depth = 0.1f };
                    groundChecker.Init(player);
                    groundCheckers.Add(groundChecker);

                    var gizmoDrawer = player.AddComponent<GroundChecker2DGizmoDrawer>();
                    gizmoDrawer.Init(groundChecker);
                    gizmoDrawers.Add(gizmoDrawer);
                }

                // ------------------------------------------------------------
                // 이벤트 카운터 초기화 및 구독
                // ------------------------------------------------------------
                var landEventCount  = new int[groundCheckers.Count];
                var leaveEventCount = new int[groundCheckers.Count];

                for (int i = 0; i < groundCheckers.Count; i++)
                {
                    int index = i;
                    groundCheckers[i].OnLand += (groundChecker, gameObject) =>
                    {
                        landEventCount[index]++;
                        Debug.Log($"Land 이벤트 발생: {players[index].name} (카운트: {landEventCount[index]})");
                    };
                    groundCheckers[i].OnLeave += (groundChecker, gameObject) =>
                    {
                        leaveEventCount[index]++;
                        Debug.Log($"OnLeave 이벤트 발생: {players[index].name} (카운트: {leaveEventCount[index]})");
                    };
                }

                // ------------------------------------------------------------
                // Update 루프 시작
                // ------------------------------------------------------------
                IEnumerator MonitorGroundState()
                {
                    while (true)
                    {
                        yield return new WaitForFixedUpdate();

                        foreach (var groundChecker in groundCheckers)
                        {
                            groundChecker.Check(Time.fixedDeltaTime);
                        }

                        if (IsSpaceKeyPressed())
                        {
                            break;
                        }
                    }
                }

                monoForTEST.StartCoroutine(MonitorGroundState());

                // ------------------------------------------------------------
                // 1. 처음 3초 대기
                // ------------------------------------------------------------
                yield return new WaitForSeconds(3f);

                // ------------------------------------------------------------
                // 2. Dynamic으로 변경
                // ------------------------------------------------------------
                foreach (var player in players)
                {
                    var rigidbody = player.GetComponent<Rigidbody2D>();
                    if (rigidbody != null)
                    {
                        rigidbody.bodyType = RigidbodyType2D.Dynamic;
                    }
                }

                // ------------------------------------------------------------
                // 3. Land 이벤트가 모든 오브젝트에서 딱 한번씩만 호출되는지 확인 (5초 유예)
                // ------------------------------------------------------------
                var landEventTriggered = new bool[groundCheckers.Count];

                IEnumerator WaitForLandEvents(List<GroundChecker2D> checkers, bool[] triggered, float timeout)
                {
                    float timer          = 0f;
                    var playerWaitTimes  = new float[checkers.Count];
                    const float waitAfterLand = 3f;

                    while (timer < timeout)
                    {
                        for (int i = 0; i < checkers.Count; i++)
                        {
                            if (!triggered[i] && checkers[i].IsOnGround)
                            {
                                triggered[i]       = true;
                                playerWaitTimes[i] = 0f;
                                Debug.Log($"{players[i].name} 착지! 3초 대기 시작...");
                            }
                        }

                        for (int i = 0; i < checkers.Count; i++)
                        {
                            if (triggered[i])
                            {
                                playerWaitTimes[i] += Time.deltaTime;

                                if (leaveEventCount[i] > 0)
                                {
                                    Debug.LogError($"{players[i].name} Land 대기 중에 OnLeave 이벤트가 {leaveEventCount[i]}번 발생했습니다!");
                                    Assert.Fail($"Land 대기 중에 OnLeave 이벤트가 발생했습니다: {players[i].name}");
                                }

                                if (playerWaitTimes[i] >= waitAfterLand && landEventCount[i] != 1)
                                {
                                    Debug.LogError($"{players[i].name} Land 이벤트가 {landEventCount[i]}번 발생했습니다! (예상: 1번)");
                                    Assert.Fail($"Land 이벤트가 올바르게 호출되지 않았습니다: {players[i].name}");
                                }
                            }
                        }

                        bool allCompleted = true;
                        for (int i = 0; i < checkers.Count; i++)
                        {
                            if (!triggered[i] || playerWaitTimes[i] < waitAfterLand)
                            {
                                allCompleted = false;
                                break;
                            }
                        }

                        if (allCompleted)
                        {
                            Debug.Log("모든 플레이어의 Land 이벤트 체크 완료!");
                            yield break;
                        }

                        timer += Time.deltaTime;
                        yield return null;
                    }

                    Debug.LogError($"Land 이벤트 타임아웃! {timeout}초 초과");
                }

                yield return monoForTEST.StartCoroutine(WaitForLandEvents(groundCheckers, landEventTriggered, 5f));

                for (int i = 0; i < groundCheckers.Count; i++)
                {
                    if (!landEventTriggered[i] || landEventCount[i] != 1)
                    {
                        Debug.LogError($"Land 이벤트 실패: {players[i].name} - 호출됨: {landEventTriggered[i]}, 횟수: {landEventCount[i]}");
                        Assert.Fail("Land 이벤트가 올바르게 호출되지 않았습니다.");
                    }
                }

                // ------------------------------------------------------------
                // 4. 모든 오브젝트가 바닥에 닿고 점프
                // ------------------------------------------------------------
                foreach (var player in players)
                {
                    var rigidbody = player.GetComponent<Rigidbody2D>();
                    if (rigidbody != null)
                    {
                        rigidbody.AddForce(Vector2.up * 15f, ForceMode2D.Impulse);
                    }
                }

                // ------------------------------------------------------------
                // 5. OnLeave 이벤트가 한번씩만 호출되는지 확인 (5초 유예)
                // ------------------------------------------------------------
                var leaveEventTriggered = new bool[groundCheckers.Count];

                IEnumerator WaitForLeaveEvents(List<GroundChecker2D> checkers, bool[] triggered, float timeout)
                {
                    float timer         = 0f;
                    var playerWaitTimes = new float[checkers.Count];
                    const float waitAfterLeave = 3f;

                    while (timer < timeout)
                    {
                        for (int i = 0; i < checkers.Count; i++)
                        {
                            if (!triggered[i] && !checkers[i].IsOnGround)
                            {
                                triggered[i]       = true;
                                playerWaitTimes[i] = 0f;
                                Debug.Log($"{players[i].name} 바닥 이탈! 3초 대기 시작...");
                            }
                        }

                        for (int i = 0; i < checkers.Count; i++)
                        {
                            if (triggered[i])
                            {
                                playerWaitTimes[i] += Time.deltaTime;

                                if (landEventCount[i] > 1)
                                {
                                    Debug.LogError($"{players[i].name} OnLeave 대기 중에 Land 이벤트가 추가로 {landEventCount[i]}번 발생했습니다!");
                                    Assert.Fail($"OnLeave 대기 중에 Land 이벤트가 추가로 발생했습니다: {players[i].name}");
                                }

                                if (playerWaitTimes[i] >= waitAfterLeave && leaveEventCount[i] != 1)
                                {
                                    Debug.LogError($"{players[i].name} OnLeave 이벤트가 {leaveEventCount[i]}번 발생했습니다! (예상: 1번)");
                                    Assert.Fail($"OnLeave 이벤트가 올바르게 호출되지 않았습니다: {players[i].name}");
                                }
                            }
                        }

                        bool allCompleted = true;
                        for (int i = 0; i < checkers.Count; i++)
                        {
                            if (!triggered[i] || playerWaitTimes[i] < waitAfterLeave)
                            {
                                allCompleted = false;
                                break;
                            }
                        }

                        if (allCompleted)
                        {
                            Debug.Log("모든 플레이어의 OnLeave 이벤트 체크 완료!");
                            yield break;
                        }

                        timer += Time.deltaTime;
                        yield return null;
                    }

                    Debug.LogError($"OnLeave 이벤트 타임아웃! {timeout}초 초과");
                }

                yield return monoForTEST.StartCoroutine(WaitForLeaveEvents(groundCheckers, leaveEventTriggered, 5f));

                for (int i = 0; i < groundCheckers.Count; i++)
                {
                    if (!leaveEventTriggered[i] || leaveEventCount[i] != 1)
                    {
                        Debug.LogError($"OnLeave 이벤트 실패: {players[i].name} - 호출됨: {leaveEventTriggered[i]}, 횟수: {leaveEventCount[i]}");
                        Assert.Fail("OnLeave 이벤트가 올바르게 호출되지 않았습니다.");
                    }
                }

                // ------------------------------------------------------------
                // 6. 원래 위치로 돌려놓고 Kinematic으로 설정
                // ------------------------------------------------------------
                for (int i = 0; i < players.Count; i++)
                {
                    var player    = players[i];
                    var rigidbody = player.GetComponent<Rigidbody2D>();

                    player.transform.position = GetPlayerPosition(i);

                    if (rigidbody != null)
                    {
                        rigidbody.linearVelocity  = Vector2.zero;
                        rigidbody.angularVelocity = 0f;
                        rigidbody.bodyType        = RigidbodyType2D.Kinematic;
                    }
                }

                // ------------------------------------------------------------
                // 7. Space바를 눌러서 종료
                // ------------------------------------------------------------
                Debug.Log("테스트 성공! Space바를 눌러서 종료하세요.");
                while (true)
                {
                    if (IsSpaceKeyPressed())
                    {
                        Debug.Log("테스트 완료!");
                        break;
                    }
                    yield return null;
                }
            }
            finally
            {
                Physics2D.IgnoreLayerCollision(0, groundLayer, prevIgnore);
            }
        }

    #endregion

    }

}
