/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ManagedReferenceValueSerializer.cs
수정일 : 2026-09-29

# 설명
Managed-reference 값을 실제 runtime type 기준으로 직렬화·역직렬화하는 Editor 전용 helper.
ManagedReferenceClipboard와 Collection item snapshot이 동일한 deep-copy 경로를 재사용한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// Managed-reference 값 직렬화 helper.
    /// </summary>
    // ============================================================
    internal static class ManagedReferenceValueSerializer
    {

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 runtime type으로 값을 직렬화한다.
        /// </summary>
        // ------------------------------------------------------------
        public static string Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            var serialize = typeof(ISerializer)
                .GetMethod(nameof(ISerializer.Serialize))
                .MakeGenericMethod(value.GetType());

            var arguments = new object[]
            {
                value,
            };

            return (string)serialize.Invoke
            (
                UnityJsonSerializer.Default,
                arguments
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 runtime type으로 payload를 역직렬화한다.
        /// </summary>
        // ------------------------------------------------------------
        public static object Deserialize(Type type, string payload)
        {
            if (type == null)
            {
                return null;
            }

            var deserialize = typeof(ISerializer)
                .GetMethod(nameof(ISerializer.Deserialize))
                .MakeGenericMethod(type);

            var arguments = new object[]
            {
                payload,
            };

            return deserialize.Invoke
            (
                UnityJsonSerializer.Default,
                arguments
            );
        }

    #endregion

    }
}
