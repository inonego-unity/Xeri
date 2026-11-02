/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : HelpBoxElement.cs
수정일 : 2026-10-29

# 설명
속성 안내와 일반 메시지를 위한 박스형 UI. 기본 심각도 아이콘은 SeverityIndicator를 재사용한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 일반 메시지와 심각도 또는 커스텀 아이콘을 표시하는 박스.
    /// </summary>
    // ============================================================
    public class HelpBoxElement : VisualElement
    {

    #region 필드와 프로퍼티

        private static readonly StyleSheet helpBoxStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>
        (
            $"{EditorAssetHelper.GetScriptDirectory(typeof(HelpBoxElement))}/HelpBoxElement.uss"
        );

        private readonly SeverityIndicator indicator;
        private readonly Image customIcon;
        private readonly Label messageLabel;
        // ------------------------------------------------------------
        /// <summary>
        /// 박스 안의 메시지를 가져오거나 변경한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Text
        {
            get => messageLabel.text;
            set => messageLabel.text = value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UI Toolkit의 메시지 심각도를 가져오거나 변경한다.
        /// </summary>
        // ------------------------------------------------------------
        public HelpBoxMessageType MessageType
        {
            get => messageType;
            set
            {
                messageType = value;
                customIconName = null;
                Refresh();
            }
        }
        private HelpBoxMessageType messageType;

        // ------------------------------------------------------------
        /// <summary>
        /// Unity Editor의 커스텀 아이콘 이름을 가져오거나 변경한다.
        /// </summary>
        // ------------------------------------------------------------
        public string CustomIconName
        {
            get => customIconName;
            set
            {
                customIconName = value;
                Refresh();
            }
        }
        private string customIconName;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 Info 메시지 박스를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public HelpBoxElement() : this(string.Empty, HelpBoxMessageType.Info)
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 메시지와 Unity 표준 HelpBox 심각도를 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        public HelpBoxElement(string message, HelpBoxMessageType type)
        {
            AddToClassList("helpbox-root");
            if (helpBoxStyle != null)
            {
                styleSheets.Add(helpBoxStyle);
            }

            indicator = new SeverityIndicator();
            indicator.AddToClassList("helpbox-icon");
            Add(indicator);

            customIcon = new Image();
            customIcon.AddToClassList("helpbox-icon");
            customIcon.style.display = DisplayStyle.None;
            Add(customIcon);

            messageLabel = new Label(message);
            messageLabel.AddToClassList("helpbox-message");
            Add(messageLabel);

            messageType = type;
            Refresh();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 커스텀 Unity Editor 아이콘을 사용한다.
        /// </summary>
        // ------------------------------------------------------------
        public HelpBoxElement(string message, string iconName)
            : this(message, HelpBoxMessageType.None)
        {
            CustomIconName = iconName;
        }

    #endregion

    #region 표시

        // ------------------------------------------------------------
        /// <summary>
        /// 메시지 타입을 박스의 배경색과 공통 아이콘에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Refresh()
        {
            EnableInClassList("helpbox-root--none", messageType == HelpBoxMessageType.None);
            EnableInClassList("helpbox-root--info", messageType == HelpBoxMessageType.Info);
            EnableInClassList("helpbox-root--warning", messageType == HelpBoxMessageType.Warning);
            EnableInClassList("helpbox-root--error", messageType == HelpBoxMessageType.Error);

            // 커스텀 아이콘은 기존 API의 예외적 표시 방식이다.
            if (!string.IsNullOrEmpty(customIconName))
            {
                indicator.SetSeverity(null);
                customIcon.image = EditorGUIUtility.IconContent(customIconName).image;
                customIcon.style.display = customIcon.image != null
                    ? DisplayStyle.Flex : DisplayStyle.None;
                return;
            }

            customIcon.image = null;
            customIcon.style.display = DisplayStyle.None;
            DiagnosticSeverity? severity = messageType switch
            {
                HelpBoxMessageType.Error => DiagnosticSeverity.Error,
                HelpBoxMessageType.Warning => DiagnosticSeverity.Warning,
                HelpBoxMessageType.Info => DiagnosticSeverity.Info,
                _ => null
            };
            indicator.SetSeverity(severity);
        }

    #endregion

    }
}
