/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionRowMoveWindow.cs
수정일 : 2026-10-31

# 설명
Collection Editor에서 선택한 직렬화 행의 이동 대상 전체 인덱스를 입력받는다.
이동 가능 여부와 실제 순서 변경은 호출한 CollectionEditorView가 검증·수행한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 컬렉션 행의 0 기반 대상 인덱스를 입력받는 Editor 창.
    /// </summary>
    // ============================================================
    internal sealed class CollectionRowMoveWindow : EditorWindow
    {

    #region 필드

        private Action<int> onMove;
        private IntegerField destinationField;
        private int lastIndex;
        private int sourceIndex;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 전체 컬렉션의 이동 가능한 인덱스 범위를 안내한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Open(int sourceIndex, int count, Action<int> onMove)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var window = CreateInstance<CollectionRowMoveWindow>();
            window.onMove = onMove ?? throw new ArgumentNullException(nameof(onMove));
            window.lastIndex = count - 1;
            window.sourceIndex = sourceIndex;
            window.titleContent = new GUIContent("인덱스로 이동");
            window.minSize = new Vector2(300f, 120f);
            window.maxSize = new Vector2(420f, 160f);
            window.ShowUtility();
        }

    #endregion

    #region 입력 화면

        // ------------------------------------------------------------
        /// <summary>
        /// 전체 인덱스 입력 필드와 확정·취소 버튼을 배치한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CreateGUI()
        {
            var content = CollectionUtilityWindowLayout.Build(rootVisualElement);
            content.AddToClassList("xeri-collection-utility--move-index");

            var rangeLabel = new Label($"입력 범위: 0 - {lastIndex}");
            rangeLabel.AddToClassList("xeri-collection-utility__range-label");
            content.Add(rangeLabel);

            destinationField = new IntegerField("대상 인덱스")
            {
                value = sourceIndex,
            };
            destinationField.tooltip = "첫 항목은 0";
            destinationField.RegisterCallback<KeyDownEvent>(HandleKeyDown);
            content.Add(destinationField);

            var actions = new VisualElement();
            actions.AddToClassList("xeri-collection-utility__actions");
            actions.Add
            (
                new Button(Close)
                {
                    text = "취소",
                }
            );
            actions.Add
            (
                new Button(Apply)
                {
                    text = "이동",
                }
            );
            content.Add(actions);
        }

        private void HandleKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                Apply();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                Close();
                evt.StopPropagation();
            }
        }

    #endregion

    #region 적용

        // ------------------------------------------------------------
        /// <summary>
        /// 허용 범위의 입력값만 원본 View의 이동 명령으로 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Apply()
        {
            if (destinationField == null || onMove == null)
            {
                Close();
                return;
            }

            var destination = destinationField.value;
            if (destination < 0 || destination > lastIndex)
            {
                EditorUtility.DisplayDialog
                (
                    "인덱스로 이동",
                    $"0부터 {lastIndex} 사이의 인덱스를 입력하세요.",
                    "확인"
                );
                return;
            }

            onMove(destination);
            Close();
        }

    #endregion

    }
}
