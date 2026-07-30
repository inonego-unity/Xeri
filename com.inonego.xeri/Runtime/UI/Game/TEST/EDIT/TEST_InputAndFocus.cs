/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_InputAndFocus.cs
수정일 : 2026-07-29

# 설명
Input System 다중 해제의 최종 상태 단일 적용과 UGUI Focus 유효성 경계를 검증한다.

# 테스트 구성
 I: 여러 입력 해제 Session의 일괄 완료
 F: 비활성·파괴된 UGUI Selectable 거부
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

using NUnit.Framework;

namespace inonego.Xeri.TEST.UI._Game
{
    using inonego.Xeri.UI.Game;

    // ============================================================
    /// <summary>
    /// 실제 Input System과 UGUI Focus backend의 상태 복원 계약 테스트.
    /// </summary>
    // ============================================================
    public sealed class TEST_InputAndFocus
    {
    #region 필드

        private readonly List<UnityEngine.Object> ownedObjects = new List<UnityEngine.Object>();

    #endregion

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// private 직렬화 필드를 테스트 값으로 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void SetField
        (
            object target,
            string name,
            object value
        )
        {
            var field = target.GetType().GetField
            (
                name,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.IsNotNull(field);
            field.SetValue(target, value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// private instance 메서드를 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void Invoke
        (
            object target,
            string name
        )
        {
            var method = target.GetType().GetMethod
            (
                name,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.IsNotNull(method);
            method.Invoke(target, null);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 입력 Session이 현재 Frame에 해제 가능하도록 장벽 Frame을 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        private static void MakeSessionsReady(InputSystemScreenInputDriver driver)
        {
            var sessionsField = typeof(InputSystemScreenInputDriver).GetField
            (
                "sessions",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.IsNotNull(sessionsField);
            var sessions = sessionsField.GetValue(driver) as IList;
            Assert.IsNotNull(sessions);

            for (var i = 0; i < sessions.Count; i++)
            {
                var property = sessions[i].GetType().GetProperty
                (
                    "ReleaseFrame",
                    BindingFlags.Instance | BindingFlags.Public
                );

                Assert.IsNotNull(property);
                property.SetValue(sessions[i], Time.frameCount);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UGUI Selectable GameObject를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private GameObject CreateSelectable(string name)
        {
            var gameObject = new GameObject
            (
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );
            ownedObjects.Add(gameObject);
            return gameObject;
        }

    #endregion

    #region 픽스처

        // ------------------------------------------------------------
        /// <summary>
        /// 생성한 Unity Object를 역순 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            for (var i = ownedObjects.Count - 1; i >= 0; i--)
            {
                if (ownedObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(ownedObjects[i]);
                }
            }

            ownedObjects.Clear();
        }

    #endregion

    #region I-1: 여러 입력 해제 Session 일괄 완료

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 같은 Frame에 해제 가능한 여러 Session을 모두 제거한 최종 Cursor 상태를 한 번 적용한 뒤,
        /// <br/> 각 완료 callback이 중간 Screen 정책이 아니라 기준 상태를 관찰하는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_InputSystemScreenInputDriver_다중해제완료_Callback이최종Cursor만관찰()
        {
            var baselineCursorVisible = Cursor.visible;
            var host = new GameObject("Input Host");
            ownedObjects.Add(host);
            var inputModule = host.AddComponent<InputSystemUIInputModule>();
            var driver = host.AddComponent<InputSystemScreenInputDriver>();
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            ownedObjects.Add(actions);
            var ui = new InputActionMap("UI");
            ui.AddAction("Cancel", InputActionType.Button);
            var gameplay = new InputActionMap("Player");
            gameplay.AddAction("Cancel", InputActionType.Button);
            actions.AddActionMap(ui);
            actions.AddActionMap(gameplay);
            gameplay.Enable();
            inputModule.actionsAsset = actions;
            var settings = ScriptableObject.CreateInstance<GameUISettingsAsset>();
            ownedObjects.Add(settings);
            SetField(settings, "uiActionMap", "UI");
            SetField(settings, "gameplayActionMap", "Player");
            SetField(settings, "releaseActionNames", new[] { "Cancel" });

            try
            {
                Cursor.visible = false;
                driver.Initialize(inputModule, settings);
                var lower = driver.Acquire
                (
                    new ScreenOptions
                    (
                        "Lower",
                        "Screen",
                        showsCursor: true
                    )
                );
                var upper = driver.Acquire
                (
                    new ScreenOptions
                    (
                        "Upper",
                        "Screen",
                        showsCursor: false
                    )
                );
                var observedCursorVisible = true;
                SetField
                (
                    upper,
                    "onReleaseCompleted",
                    (Action)(() => observedCursorVisible = Cursor.visible)
                );
                lower.MarkAwaitingRelease(true);
                upper.MarkAwaitingRelease(true);
                MakeSessionsReady(driver);

                Invoke(driver, "Update");

                Assert.IsTrue(lower.IsReleased);
                Assert.IsTrue(upper.IsReleased);
                Assert.IsFalse(observedCursorVisible);
                Assert.IsFalse(Cursor.visible);
            }
            finally
            {
                driver.Dispose();
                Cursor.visible = baselineCursorVisible;
            }
        }

    #endregion

    #region F-1: UGUI Focus 유효성

        // ------------------------------------------------------------
        /// <summary>
        /// disabled, inactive와 파괴된 Selectable을 Focus 대상으로 인정하지 않는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UGUIFocusDriver_비활성Selectable_유효Focus거부()
        {
            var eventSystemObject = new GameObject("EventSystem");
            ownedObjects.Add(eventSystemObject);
            var eventSystem = eventSystemObject.AddComponent<EventSystem>();
            var driverObject = new GameObject("Focus Driver");
            ownedObjects.Add(driverObject);
            var driver = driverObject.AddComponent<UGUIFocusDriver>();
            var target = CreateSelectable("Target");
            var fallback = CreateSelectable("Fallback");
            SetField(driver, "eventSystem", eventSystem);
            SetField(driver, "fallback", fallback);

            Assert.IsTrue(driver.IsValid(target));
            target.GetComponent<Button>().enabled = false;
            Assert.IsFalse(driver.IsValid(target));

            fallback.GetComponent<Button>().enabled = false;
            Assert.IsNull(driver.FindFallback());

            target.GetComponent<Button>().enabled = true;
            target.SetActive(false);
            Assert.IsFalse(driver.IsValid(target));

            target.SetActive(true);
            UnityEngine.Object.DestroyImmediate(target);
            Assert.IsFalse(driver.IsValid(target));
        }

    #endregion

    }
}
