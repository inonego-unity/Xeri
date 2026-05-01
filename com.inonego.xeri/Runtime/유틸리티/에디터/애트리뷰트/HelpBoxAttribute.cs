/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : HelpBoxAttribute.cs
수정일 : 2026-05-02

# 설명
인스펙터 필드 위에 HelpBox(아이콘 + 메시지)를 표시하는 PropertyAttribute.
메시지 타입은 Unity 표준 UnityEngine.UIElements.HelpBoxMessageType을 사용한다.
커스텀 아이콘 이름을 지정하면 MessageType 대신 EditorGUIUtility.IconContent에서 해당 아이콘을 가져와 표시한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 인스펙터 필드 위에 HelpBox를 표시하는 PropertyAttribute입니다.
    /// </summary>
    // ============================================================
    public class HelpBoxAttribute : PropertyAttribute
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// HelpBox에 표시할 메시지입니다.
        /// </summary>
        // ------------------------------------------------------------
        public string Message { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// HelpBox 메시지 타입입니다.
        /// </summary>
        // ------------------------------------------------------------
        public HelpBoxMessageType MessageType { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/>커스텀 아이콘 이름입니다.
        /// <br/>지정되면 MessageType 대신 해당 아이콘이 사용됩니다.
        /// </summary>
        // ------------------------------------------------------------
        public string CustomIconName { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 메시지 타입 기반 HelpBox 어트리뷰트를 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        public HelpBoxAttribute(string message, HelpBoxMessageType messageType = HelpBoxMessageType.Info)
        {
            Message        = message;
            MessageType    = messageType;
            CustomIconName = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 커스텀 아이콘 이름 기반 HelpBox 어트리뷰트를 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        public HelpBoxAttribute(string message, string iconName)
        {
            Message        = message;
            MessageType    = HelpBoxMessageType.None;
            CustomIconName = iconName;
        }

    #endregion

    }
}
