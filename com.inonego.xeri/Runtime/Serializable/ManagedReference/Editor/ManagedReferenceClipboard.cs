/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ManagedReferenceClipboard.cs
수정일 : 2026-08-04

# 설명
SerializeReference picker의 독립 값 복사와 동일 serialized root 내 Link 복사를 관리한다.

# 특이사항
값 복사는 기존 ISerializer generic 메서드를 runtime type으로 닫아 사용한다.
Link는 다른 UnityEngine.Object host를 넘지 않으며 Editor 세션의 정적 clipboard에만 유지된다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// managed-reference 값과 동일 root identity의 Editor clipboard를 제공한다.
    /// </summary>
    // ============================================================
    internal static class ManagedReferenceClipboard
    {
    #region 내부 데이터

        private static ValueEntry valueEntry = null;
        private static LinkEntry linkEntry = null;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 managed-reference를 독립 값으로 직렬화해 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryCopyAsValue(SerializedProperty property, out string reason)
        {
            if (!TryGetManagedReference(property, out var value, out reason))
            {
                return false;
            }

            if (value == null)
            {
                valueEntry = new ValueEntry(null, null);
                reason = null;
                return true;
            }

            try
            {
                // 실제 runtime type으로 닫아야 interface 선언 필드도 기존 serializer를 그대로 사용할 수 있다.
                valueEntry = new ValueEntry
                (
                    value.GetType(),
                    SerializeRuntimeType(value)
                );
                reason = null;
                return true;
            }
            catch (Exception exception)
            {
                reason = exception.Message;
                return false;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 복사한 독립 값을 현재 property마다 새 instance로 붙여 넣는다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryPasteAsValue(SerializedProperty property, Type declaredType, out string reason)
        {
            if (!CanPasteAsValue(property, declaredType, out reason))
            {
                return false;
            }

            foreach (var target in property.serializedObject.targetObjects)
            {
                var targetObject   = new SerializedObject(target);
                var targetProperty = targetObject.FindProperty(property.propertyPath);
                if (targetProperty == null)
                {
                    reason = "대상 property를 찾을 수 없습니다.";
                    return false;
                }

                // multi-object 편집도 각 host에 독립 deserialization 결과를 대입해야 shared object가 되지 않는다.
                targetProperty.managedReferenceValue = valueEntry.Type == null
                    ? null
                    : DeserializeRuntimeType(valueEntry.Type, valueEntry.Payload);
                targetObject.ApplyModifiedProperties();
            }

            property.serializedObject.Update();
            reason = null;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Value 붙여 넣기가 가능한지와 불가능한 사유를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool CanPasteAsValue(SerializedProperty property, Type declaredType, out string reason)
        {
            if (!TryGetManagedReference(property, out _, out reason))
            {
                return false;
            }

            if (valueEntry == null)
            {
                reason = "Copy as Value를 먼저 실행하세요.";
                return false;
            }

            if (declaredType == null)
            {
                reason = "선언 타입을 확인할 수 없습니다.";
                return false;
            }

            if (valueEntry.Type != null && !declaredType.IsAssignableFrom(valueEntry.Type))
            {
                reason = $"{ManagedReferenceTypeCatalog.GetDisplayName(valueEntry.Type)}은(는) 이 필드에 대입할 수 없습니다.";
                return false;
            }

            reason = null;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Link 복사가 가능한지와 불가능한 사유를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool CanCopyAsLink(SerializedProperty property, out string reason)
        {
            if (!TryGetManagedReference(property, out var value, out reason))
            {
                return false;
            }

            if (value == null)
            {
                reason = "null reference는 Link로 복사할 수 없습니다.";
                return false;
            }

            if (property.serializedObject.isEditingMultipleObjects)
            {
                reason = "multi-object 편집에서는 Link를 복사할 수 없습니다.";
                return false;
            }

            var root       = property.serializedObject.targetObject;
            var id         = property.managedReferenceId;
            var registered = id == 0 ? null : ManagedReferenceUtility.GetManagedReference(root, id);
            if (id == 0 || !ReferenceEquals(registered, value))
            {
                reason = "현재 serialized root에 등록된 managed reference가 아닙니다.";
                return false;
            }

            reason = null;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 managed-reference identity를 같은 serialized root 전용 Link로 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryCopyAsLink(SerializedProperty property, out string reason)
        {
            if (!CanCopyAsLink(property, out reason))
            {
                return false;
            }

            var value = property.managedReferenceValue;
            var root  = property.serializedObject.targetObject;
            var id    = property.managedReferenceId;

            // ID만 보관하면 이후 root 내부 구성이 바뀌었을 때 다른 객체를 잘못 연결할 수 있어 원본 참조도 함께 보관한다.
            linkEntry = new LinkEntry(root, id, value);
            reason = null;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 복사한 identity를 현재 property에 Link로 붙여 넣는다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryPasteAsLink(SerializedProperty property, Type declaredType, out string reason)
        {
            if (!CanPasteAsLink(property, declaredType, out reason))
            {
                return false;
            }

            // 동일 host에 같은 object identity를 대입해 Unity가 같은 managed-reference ID를 유지하도록 한다.
            property.managedReferenceValue = linkEntry.Reference;
            property.serializedObject.ApplyModifiedProperties();
            reason = null;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Link 붙여 넣기가 가능한지와 불가능한 사유를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool CanPasteAsLink(SerializedProperty property, Type declaredType, out string reason)
        {
            if (!TryGetManagedReference(property, out _, out reason))
            {
                return false;
            }

            if (linkEntry == null)
            {
                reason = "Copy as Link를 먼저 실행하세요.";
                return false;
            }

            if (property.serializedObject.isEditingMultipleObjects)
            {
                reason = "multi-object 편집에서는 Link를 붙여 넣을 수 없습니다.";
                return false;
            }

            var root = property.serializedObject.targetObject;
            if (!ReferenceEquals(linkEntry.Root, root))
            {
                reason = "동일한 serialized root에서만 Link를 붙여 넣을 수 있습니다.";
                return false;
            }

            var registered = linkEntry.Id == 0
                ? null
                : ManagedReferenceUtility.GetManagedReference(root, linkEntry.Id);
            if (linkEntry.Reference == null || !ReferenceEquals(registered, linkEntry.Reference))
            {
                reason = "복사한 Link의 managed reference가 현재 root에 없습니다.";
                return false;
            }

            var linkType = linkEntry.Reference.GetType();
            if (declaredType == null || !declaredType.IsAssignableFrom(linkType))
            {
                reason = $"{ManagedReferenceTypeCatalog.GetDisplayName(linkType)}은(는) 이 필드에 대입할 수 없습니다.";
                return false;
            }

            reason = null;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 ISerializer generic Serialize 메서드를 실제 runtime type으로 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        private static string SerializeRuntimeType(object value)
        {
            var serialize = typeof(ISerializer)
                .GetMethod(nameof(ISerializer.Serialize))
                .MakeGenericMethod(value.GetType());

            return (string)serialize.Invoke(UnityJsonSerializer.Default, new[] { value });
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 ISerializer generic Deserialize 메서드를 실제 runtime type으로 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        private static object DeserializeRuntimeType(Type type, string payload)
        {
            var deserialize = typeof(ISerializer)
                .GetMethod(nameof(ISerializer.Deserialize))
                .MakeGenericMethod(type);

            return deserialize.Invoke(UnityJsonSerializer.Default, new object[] { payload });
        }

        // ------------------------------------------------------------
        /// <summary>
        /// property가 managed-reference인지 확인하고 현재 값을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool TryGetManagedReference(SerializedProperty property, out object value, out string reason)
        {
            value = null;
            reason = null;

            if (property == null || property.propertyType != SerializedPropertyType.ManagedReference)
            {
                reason = "SerializeReference property가 아닙니다.";
                return false;
            }

            value = property.managedReferenceValue;
            return true;
        }

    #endregion

    #region 내부 형식

        // ============================================================
        /// <summary>
        /// 독립 값 복원에 필요한 실제 type과 serializer payload를 보관한다.
        /// </summary>
        // ============================================================
        private sealed class ValueEntry
        {
        #region 필드

            // ------------------------------------------------------------
            /// <summary>
            /// 복사한 값의 실제 runtime type.
            /// </summary>
            // ------------------------------------------------------------
            public Type Type { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 기존 serializer가 생성한 payload.
            /// </summary>
            // ------------------------------------------------------------
            public string Payload { get; }

        #endregion

        #region 생성자

            // ------------------------------------------------------------
            /// <summary>
            /// 값 clipboard 항목을 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public ValueEntry(Type type, string payload) : base()
            {
                Type    = type;
                Payload = payload;
            }

        #endregion
        }

        // ============================================================
        /// <summary>
        /// 동일 serialized root에만 다시 연결할 수 있는 managed-reference identity를 보관한다.
        /// </summary>
        // ============================================================
        private sealed class LinkEntry
        {
        #region 필드

            // ------------------------------------------------------------
            /// <summary>
            /// managed-reference를 소유하는 Unity serialized root.
            /// </summary>
            // ------------------------------------------------------------
            public UnityEngine.Object Root { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// root 내에서 Unity가 부여한 managed-reference ID.
            /// </summary>
            // ------------------------------------------------------------
            public long Id { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// ID 재사용을 방지하기 위해 함께 검증하는 원본 object identity.
            /// </summary>
            // ------------------------------------------------------------
            public object Reference { get; }

        #endregion

        #region 생성자

            // ------------------------------------------------------------
            /// <summary>
            /// Link clipboard 항목을 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public LinkEntry(UnityEngine.Object root, long id, object reference) : base()
            {
                Root      = root;
                Id        = id;
                Reference = reference;
            }

        #endregion
        }

    #endregion
    }
}
