/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SerializedPropertyHelper.cs
수정일 : 2026-04-29

# 설명
SerializedProperty 경로로부터 실제 객체 인스턴스를 얻는다.
배열/리스트 요소 경로와 UI Toolkit 직계 자식 필드 생성을 지원한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Reflection;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// SerializedProperty 리플렉션 헬퍼.
    /// </summary>
    // ============================================================
    public static class SerializedPropertyHelper
    {
        // ------------------------------------------------------------
        /// <summary>
        /// SerializedProperty 경로로부터 실제 객체를 T로 캐스팅해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static T GetTargetObject<T>(SerializedProperty property) where T : class
        {
            return GetTargetObject(property) as T;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> SerializedProperty 경로로부터 실제 객체를 반환한다.
        /// <br/> 배열 요소 경로(Array.data[n])도 지원한다.
        /// </summary>
        // ------------------------------------------------------------
        public static object GetTargetObject(SerializedProperty property)
        {
            var path = property.propertyPath.Replace(".Array.data[", "[");
            object obj = property.serializedObject.targetObject;

            foreach (var element in path.Split('.'))
            {
                if (element.Contains("["))
                {
                    var bracketIndex = element.IndexOf('[');
                    var fieldName    = element[..bracketIndex];
                    var index        = int.Parse(element[(bracketIndex + 1)..element.IndexOf(']')]);

                    var listField = FindField(obj.GetType(), fieldName);
                    if (listField == null) return null;

                    obj = ((IList)listField.GetValue(obj))[index];
                }
                else
                {
                    var field = FindField(obj.GetType(), element);
                    if (field == null) return null;
                    obj = field.GetValue(obj);
                }
            }

            return obj;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 가능한 직계 자식 필드를 container에 PropertyField로 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void AppendVisibleChildren(SerializedProperty property, VisualElement container)
        {
            if (property == null || container == null)
            {
                return;
            }

            var depth    = property.depth;
            var iterator = property.Copy();

            // 현재 property의 drawer를 재호출하지 않고 실제 데이터 자식부터 기본 PropertyField에 위임한다.
            if (!iterator.NextVisible(enterChildren: true))
            {
                return;
            }

            do
            {
                // 다음 sibling으로 넘어가면 현재 property의 자식 범위를 모두 처리한 상태다.
                if (iterator.depth <= depth)
                {
                    break;
                }

                container.Add(new PropertyField(iterator.Copy()));
            }
            while (iterator.NextVisible(enterChildren: false));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 상속 계층을 순회하며 필드를 검색한다.
        /// </summary>
        // ------------------------------------------------------------
        private static FieldInfo FindField(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

            while (type != null)
            {
                var field = type.GetField(name, flags);
                if (field != null) return field;
                type = type.BaseType;
            }

            return null;
        }
    }
}
