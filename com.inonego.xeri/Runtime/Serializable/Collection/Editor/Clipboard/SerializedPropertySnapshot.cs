/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SerializedPropertySnapshot.cs
수정일 : 2026-10-17

# 설명
SerializedProperty 하나를 Editor session 메모리에서 독립적으로 복원할 수 있는 snapshot으로 캡처한다.
Array/Generic은 재귀 구조로, ManagedReference는 기존 Xeri serializer 경로로 deep-copy한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// SerializedProperty Editor-session snapshot.
    /// </summary>
    // ============================================================
    internal sealed class SerializedPropertySnapshot
    {

    #region 내부 데이터

        private sealed class ChildSnapshot
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 항목의 이름을 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public string Name { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 저장된 프로퍼티 스냅샷을 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public SerializedPropertySnapshot Snapshot { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// ChildSnapshot 인스턴스를 초기화한다.
            /// </summary>
            // ------------------------------------------------------------
            public ChildSnapshot
            (
                string name,
                SerializedPropertySnapshot snapshot
            )
            {
                Name = name;
                Snapshot = snapshot;
            }
        }

    #endregion

    #region 필드

        private readonly SerializedPropertyType propertyType;
        private readonly string serializedType;
        private readonly bool isArray;
        private readonly object boxedValue;
        private readonly Type managedReferenceType;
        private readonly string managedReferencePayload;
        private readonly IReadOnlyList<SerializedPropertySnapshot> arrayItems;
        private readonly IReadOnlyList<ChildSnapshot> children;

    #endregion

    #region 생성자

        private SerializedPropertySnapshot
        (
            SerializedPropertyType propertyType,
            string serializedType,
            bool isArray,
            object boxedValue,
            Type managedReferenceType,
            string managedReferencePayload,
            IReadOnlyList<SerializedPropertySnapshot> arrayItems,
            IReadOnlyList<ChildSnapshot> children
        )
        {
            this.propertyType = propertyType;
            this.serializedType = serializedType ?? string.Empty;
            this.isArray = isArray;
            this.boxedValue = boxedValue;
            this.managedReferenceType = managedReferenceType;
            this.managedReferencePayload = managedReferencePayload;
            this.arrayItems = arrayItems ?? Array.Empty<SerializedPropertySnapshot>();
            this.children = children ?? Array.Empty<ChildSnapshot>();
        }

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// property를 snapshot으로 캡처한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryCreate
        (
            SerializedProperty property,
            out SerializedPropertySnapshot snapshot,
            out string reason
        )
        {
            snapshot = null;
            reason = null;

            if (property == null)
            {
                reason = "복사할 SerializedProperty가 없습니다.";
                return false;
            }

            try
            {
                snapshot = Create(property);
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
        /// property 형식에 맞는 snapshot을 재귀 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static SerializedPropertySnapshot Create(SerializedProperty property)
        {
            if (property.isArray
                && property.propertyType != SerializedPropertyType.String)
            {
                return CreateArray(property);
            }

            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                return CreateManagedReference(property);
            }

            if (property.propertyType == SerializedPropertyType.Generic)
            {
                return CreateGeneric(property);
            }

            // Unity 객체는 값을 복제하지 않고 동일한 객체 참조를 보관한다.
            var value = property.propertyType == SerializedPropertyType.ObjectReference
                ? property.objectReferenceValue
                : property.boxedValue;

            return new SerializedPropertySnapshot
            (
                property.propertyType,
                property.type,
                isArray: false,
                value,
                managedReferenceType: null,
                managedReferencePayload: null,
                arrayItems: null,
                children: null
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// array/list property와 모든 element snapshot을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static SerializedPropertySnapshot CreateArray(SerializedProperty property)
        {
            var items = new SerializedPropertySnapshot[property.arraySize];
            for (var i = 0; i < property.arraySize; i++)
            {
                items[i] = Create(property.GetArrayElementAtIndex(i));
            }

            return new SerializedPropertySnapshot
            (
                property.propertyType,
                property.type,
                isArray: true,
                boxedValue: null,
                managedReferenceType: null,
                managedReferencePayload: null,
                arrayItems: items,
                children: null
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// managed-reference runtime type과 payload를 캡처한다.
        /// </summary>
        // ------------------------------------------------------------
        private static SerializedPropertySnapshot CreateManagedReference
        (
            SerializedProperty property
        )
        {
            var value = property.managedReferenceValue;
            var type = value?.GetType();
            var payload = value == null
                ? null
                : ManagedReferenceValueSerializer.Serialize(value);

            return new SerializedPropertySnapshot
            (
                property.propertyType,
                property.type,
                isArray: false,
                boxedValue: null,
                managedReferenceType: type,
                managedReferencePayload: payload,
                arrayItems: null,
                children: null
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// generic property의 direct child snapshot을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static SerializedPropertySnapshot CreateGeneric(SerializedProperty property)
        {
            var childSnapshots = new List<ChildSnapshot>();
            var depth = property.depth;
            var iterator = property.Copy();

            // UI에 숨긴 필드도 항목 데이터이므로 snapshot에는 모두 포함한다.
            if (iterator.Next(enterChildren: true))
            {
                do
                {
                    if (iterator.depth <= depth)
                    {
                        break;
                    }

                    childSnapshots.Add
                    (
                        new ChildSnapshot
                        (
                            iterator.name,
                            Create(iterator)
                        )
                    );
                }
                while (iterator.Next(enterChildren: false));
            }

            return new SerializedPropertySnapshot
            (
                property.propertyType,
                property.type,
                isArray: false,
                boxedValue: null,
                managedReferenceType: null,
                managedReferencePayload: null,
                arrayItems: null,
                children: childSnapshots
            );
        }

    #endregion

    #region 적용

        // ------------------------------------------------------------
        /// <summary>
        /// snapshot을 destination property에 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryApply(SerializedProperty destination, out string reason)
        {
            reason = null;

            if (!CanApply(destination, out reason))
            {
                return false;
            }

            try
            {
                Apply(destination);
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
        /// destination이 snapshot payload를 받을 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanApply(SerializedProperty destination, out string reason)
        {
            if (destination == null)
            {
                reason = "붙여 넣을 SerializedProperty가 없습니다.";
                return false;
            }

            // 문자열도 isArray를 반환할 수 있으므로 목록 판정에서 제외한다.
            var destinationIsArray = destination.isArray
                && destination.propertyType != SerializedPropertyType.String;

            if (destinationIsArray != isArray
                || destination.propertyType != propertyType
                || !string.Equals
                (
                    destination.type,
                    serializedType,
                    StringComparison.Ordinal
                ))
            {
                reason = "복사한 항목과 대상 항목의 serialized type이 다릅니다.";
                return false;
            }

            // 배열도 가능한 범위에서 원소 타입 계약을 적용 전에 검증한다.
            // 빈 대상 배열은 원소 property를 조회할 수 없어 Apply 실패 시 미확정 변경을 폐기한다.
            if (isArray)
            {
                if (destination.arraySize > 0)
                {
                    var element = destination.GetArrayElementAtIndex(0);
                    foreach (var item in arrayItems)
                    {
                        if (!item.CanApply(element, out reason))
                        {
                            return false;
                        }
                    }
                }

                reason = null;
                return true;
            }

            if (propertyType == SerializedPropertyType.ManagedReference
                && managedReferenceType != null)
            {
                var declaredType = ManagedReferenceTypeCatalog.GetDeclaredReferenceType
                (
                    destination
                );
                if (declaredType == null
                    || !declaredType.IsAssignableFrom(managedReferenceType))
                {
                    reason = "복사한 managed-reference 타입을 대상에 할당할 수 없습니다.";
                    return false;
                }
            }

            // Generic의 하위 계약도 적용 전에 확인해 부분 붙여넣기를 막는다.
            if (!isArray && propertyType == SerializedPropertyType.Generic)
            {
                foreach (var child in children)
                {
                    if (!child.Snapshot.CanApply
                    (
                        destination.FindPropertyRelative(child.Name),
                        out reason
                    ))
                    {
                        return false;
                    }
                }
            }

            reason = null;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// snapshot 종류에 따라 destination 값을 대입한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Apply(SerializedProperty destination)
        {
            if (isArray)
            {
                destination.arraySize = arrayItems.Count;

                for (var i = 0; i < arrayItems.Count; i++)
                {
                    arrayItems[i].Apply(destination.GetArrayElementAtIndex(i));
                }

                return;
            }

            if (propertyType == SerializedPropertyType.ManagedReference)
            {
                destination.managedReferenceValue = managedReferenceType == null
                    ? null
                    : ManagedReferenceValueSerializer.Deserialize
                    (
                        managedReferenceType,
                        managedReferencePayload
                    );
                return;
            }

            if (propertyType == SerializedPropertyType.Generic)
            {
                foreach (var child in children)
                {
                    var destinationChild = destination.FindPropertyRelative(child.Name);
                    if (destinationChild == null)
                    {
                        throw new InvalidOperationException
                        (
                            $"Snapshot 필드를 찾을 수 없습니다: {child.Name}"
                        );
                    }

                    child.Snapshot.Apply(destinationChild);
                }

                return;
            }

            if (propertyType == SerializedPropertyType.ObjectReference)
            {
                destination.objectReferenceValue = boxedValue as UnityEngine.Object;
                return;
            }

            destination.boxedValue = boxedValue;
        }

    #endregion

    }
}
