/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_InputAndFocus.cs
수정일 : 2026-07-31

# 설명
UGUI Focus 유효성 경계를 검증한다.

# 테스트 구성
 F: 비활성·파괴된 UGUI Selectable 거부
========================================================================= BLOCK_HEADER_END */

using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using NUnit.Framework;

namespace inonego.Xeri.TEST.UI._Game
{
    using inonego.Xeri.UI.Game;

    // ============================================================
    /// <summary>
    /// UGUI Focus backend의 유효 대상 계약 테스트.
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
