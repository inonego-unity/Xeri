/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionKeyRangeWindow.cs
수정일 : 2026-10-31

# 설명
정수 Key를 사용하는 XDictionary에서 범위별 키 추가·제거·제한을 입력받는다.
생성된 Key 집합의 적용과 Undo는 XDictionaryEditorModel에 위임한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// XDictionary의 정수 Key 범위를 입력받는 보조 편집 창.
    /// </summary>
    // ============================================================
    internal sealed class CollectionKeyRangeWindow : EditorWindow
    {

    #region 상수

        private const int maxGeneratedKeys = 10000;

    #endregion

    #region 필드

        private XDictionaryEditorModel model = null;
        private SerializedPropertyLocator locator = null;
        private CollectionKeySetOperation operation = CollectionKeySetOperation.Add;
        private IntegerField minField = null;
        private IntegerField maxField = null;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 operation의 int range 입력 창을 연다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Open
        (
            XDictionaryEditorModel model,
            CollectionKeySetOperation operation
        )
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var window = CreateInstance<CollectionKeyRangeWindow>();
            window.model = model;
            window.locator = new SerializedPropertyLocator(model.GetCollectionProperty());

            window.operation = operation;
            window.titleContent = new GUIContent("정수 키 범위");
            window.minSize = new Vector2(280f, 120f);
            window.maxSize = new Vector2(420f, 180f);
            window.ShowUtility();
        }

    #endregion

    #region Unity 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// range 입력과 적용 버튼을 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CreateGUI()
        {
            var content = CollectionUtilityWindowLayout.Build(rootVisualElement);

            minField = new IntegerField("최솟값")
            {
                value = 0,
            };
            maxField = new IntegerField("최댓값")
            {
                value = 10,
            };

            var applyButton = new Button(Apply)
            {
                text = operation switch
                {
                    CollectionKeySetOperation.Add => "추가",
                    CollectionKeySetOperation.Remove => "제거",
                    CollectionKeySetOperation.Confine => "지정 범위만 유지",
                    _ => "적용"
                },
            };

            content.Add(minField);
            content.Add(maxField);
            content.Add(applyButton);
        }

    #endregion

    #region 적용

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Min/Max에서 inclusive int Key 집합을 생성해 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Apply()
        {
            if (model == null || minField == null || maxField == null)
            {
                Close();
                return;
            }

            var min = minField.value;
            var max = maxField.value;
            if (max < min)
            {
                EditorUtility.DisplayDialog
                (
                    "정수 키 범위",
                    "최댓값은 최솟값 이상이어야 합니다.",
                    "확인"
                );
                return;
            }

            var count = (long)max - min + 1L;
            if (count > maxGeneratedKeys)
            {
                EditorUtility.DisplayDialog
                (
                    "정수 키 범위",
                    $"한 번에 최대 {maxGeneratedKeys}개의 키를 생성할 수 있습니다.",
                    "확인"
                );
                return;
            }

            var keys = new List<object>((int)count);
            for (var value = (long)min; value <= max; value++)
            {
                keys.Add((int)value);
            }

            var target = model.SerializedObject.targetObject;
            if (target == null || locator == null)
            {
                Close();
                return;
            }

            using (var current = new SerializedObject(target))
            {
                if (!locator.TryResolve(current, out _, out var reason))
                {
                    EditorUtility.DisplayDialog("정수 키 범위", reason, "확인");
                    Close();
                    return;
                }
            }

            CollectionValueEditorWindow.CloseForCollection
            (
                target,
                model.CollectionPropertyPath
            );

            model.ApplyKeySet(keys, operation);
            Close();
        }

    #endregion

    }
}
