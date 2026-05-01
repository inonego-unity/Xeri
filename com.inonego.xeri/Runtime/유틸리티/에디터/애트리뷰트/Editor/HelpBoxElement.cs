/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : HelpBoxElement.cs
수정일 : 2026-05-02

# 설명
HelpBox(아이콘 + 메시지)를 표시하는 커스텀 VisualElement.
HelpBoxAttribute의 PropertyDrawer에서 사용되며, 외부 에디터 코드에서도 직접 new로 사용할 수 있다.
HelpBoxMessageType 또는 customIconName을 기반으로 아이콘과 배경색을 결정한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// HelpBox(아이콘 + 메시지)를 표시하는 커스텀 VisualElement.
    /// </summary>
    // ============================================================
    public class HelpBoxElement : VisualElement
    {

    #region 상수

        private const string ClassRoot          = "helpbox-root";
        private const string ClassIcon          = "helpbox-icon";
        private const string ClassMessage       = "helpbox-message";
        private const string ClassModifierNone    = "helpbox-root--none";
        private const string ClassModifierInfo    = "helpbox-root--info";
        private const string ClassModifierWarning = "helpbox-root--warning";
        private const string ClassModifierError   = "helpbox-root--error";

        private const string IconNameInfo    = "console.infoicon";
        private const string IconNameWarning = "console.warnicon";
        private const string IconNameError   = "console.erroricon";

    #endregion

    #region 필드

        private Image iconImage;
        private Label messageLabel;

        private HelpBoxMessageType messageType;
        private string             customIconName;

        // ------------------------------------------------------------
        /// <summary>
        /// HelpBox에 표시할 메시지입니다.
        /// </summary>
        // ------------------------------------------------------------
        public string Text
        {
            get => messageLabel != null ? messageLabel.text : null;
            set { if (messageLabel != null) messageLabel.text = value; }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// HelpBox 메시지 타입입니다. 변경 시 배경색과 기본 아이콘이 갱신됩니다.
        /// </summary>
        // ------------------------------------------------------------
        public HelpBoxMessageType MessageType
        {
            get => messageType;
            set
            {
                messageType    = value;
                customIconName = null;
                Refresh();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/>커스텀 아이콘 이름입니다. EditorGUIUtility.IconContent로 조회됩니다.
        /// <br/>지정되면 MessageType 대신 해당 아이콘이 사용됩니다.
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

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 메시지와 Info 타입으로 HelpBox를 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        public HelpBoxElement() : this(string.Empty, HelpBoxMessageType.Info) { }

        // ------------------------------------------------------------
        /// <summary>
        /// 메시지 타입 기반 HelpBox를 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        public HelpBoxElement(string message, HelpBoxMessageType type)
        {
            Build();

            Text        = message;
            messageType = type;

            Refresh();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 커스텀 아이콘 이름 기반 HelpBox를 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        public HelpBoxElement(string message, string iconName)
        {
            Build();

            Text           = message;
            messageType    = HelpBoxMessageType.None;
            customIconName = iconName;

            Refresh();
        }

    #endregion

    #region 초기화

        // ------------------------------------------------------------
        /// <summary>
        /// UXML/USS를 로드해 자식 요소를 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Build()
        {
            var dir  = EditorAssetHelper.GetScriptDirectory(GetType());
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{dir}/HelpBoxElement.uxml");
            var uss  = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dir}/HelpBoxElement.uss");

            if (uxml != null) uxml.CloneTree(this);
            if (uss  != null) styleSheets.Add(uss);

            AddToClassList(ClassRoot);

            iconImage    = this.Q<Image>("icon");
            messageLabel = this.Q<Label>("message");
        }

    #endregion

    #region 갱신

        // ------------------------------------------------------------
        /// <summary>
        /// modifier 클래스와 아이콘을 현재 상태에 맞게 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Refresh()
        {
            RefreshModifierClass();
            RefreshIcon();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// messageType에 맞는 modifier 클래스만 남기고 나머지는 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshModifierClass()
        {
            RemoveFromClassList(ClassModifierNone);
            RemoveFromClassList(ClassModifierInfo);
            RemoveFromClassList(ClassModifierWarning);
            RemoveFromClassList(ClassModifierError);

            var modifier = messageType switch
            {
                HelpBoxMessageType.Info    => ClassModifierInfo,
                HelpBoxMessageType.Warning => ClassModifierWarning,
                HelpBoxMessageType.Error   => ClassModifierError,
                _                          => ClassModifierNone,
            };

            AddToClassList(modifier);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 커스텀 아이콘이 있으면 그것, 없으면 messageType 기반 기본 아이콘을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshIcon()
        {
            if (iconImage == null) return;

            var image = ResolveIcon();

            iconImage.image = image;
            iconImage.style.display = image != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 상태에 맞는 아이콘 텍스처를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private Texture ResolveIcon()
        {
            if (!string.IsNullOrEmpty(customIconName))
            {
                return EditorGUIUtility.IconContent(customIconName).image;
            }

            var iconName = messageType switch
            {
                HelpBoxMessageType.Info    => IconNameInfo,
                HelpBoxMessageType.Warning => IconNameWarning,
                HelpBoxMessageType.Error   => IconNameError,
                _                          => null,
            };

            if (iconName == null) return null;

            return EditorGUIUtility.IconContent(iconName).image;
        }

    #endregion

    }
}
