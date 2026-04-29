/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GizmoHelper.cs
수정일 : 2026-04-29

# 설명
Handles API 기반 기즈모 통합 헬퍼.
Line, WireCube, WireSphere, WireHemisphere, WireCapsule, CameraFrustum 드로우 메서드를 제공한다.
에디터 전용 API는 #if UNITY_EDITOR로 격리하므로 Runtime 코드에서도 참조 가능하다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 기즈모 드로우 정적 헬퍼.
    /// </summary>
    // ============================================================
    public static class GizmoHelper
    {

    #region 선

        // ------------------------------------------------------------
        /// <summary>
        /// 선을 그린다.
        /// </summary>
        // ------------------------------------------------------------
        public static void DrawLine(Vector3 p0, Vector3 p1, Color color)
        {

        #if UNITY_EDITOR
            var original = Handles.color;
            Handles.color = color;

            Handles.DrawLine(p0, p1);

            Handles.color = original;
        #endif

        }

        // ------------------------------------------------------------
        /// <summary>
        /// AA 처리된 굵기 지정 폴리라인을 그린다.
        /// </summary>
        // ------------------------------------------------------------
        public static void DrawAAPolyLine(float thickness, Color color, params Vector3[] points)
        {

        #if UNITY_EDITOR
            var original = Handles.color;
            Handles.color = color;

            Handles.DrawAAPolyLine(thickness, points);

            Handles.color = original;
        #endif

        }

    #endregion

    #region 큐브

        // ------------------------------------------------------------
        /// <summary>
        /// 와이어 큐브를 그린다.
        /// </summary>
        // ------------------------------------------------------------
        public static void DrawWireCube(Vector3 position, Quaternion rotation, Vector3 size, Color color)
        {
            DrawWireCube(Matrix4x4.TRS(position, rotation, size), color);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 와이어 큐브를 그린다.
        /// </summary>
        // ------------------------------------------------------------
        public static void DrawWireCube(Matrix4x4 matrix, Color color)
        {

        #if UNITY_EDITOR
            var (originalMatrix, originalColor) = (Handles.matrix, Handles.color);
            (Handles.matrix, Handles.color)     = (matrix, color);

            Handles.DrawWireCube(Vector3.zero, Vector3.one);

            (Handles.matrix, Handles.color) = (originalMatrix, originalColor);
        #endif

        }

    #endregion

    #region 구

        // ------------------------------------------------------------
        /// <summary>
        /// 와이어 구를 그린다.
        /// </summary>
        // ------------------------------------------------------------
        public static void DrawWireSphere(Vector3 position, Quaternion rotation, Vector3 size, Color color)
        {
            DrawWireSphere(Matrix4x4.TRS(position, rotation, size), color);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 와이어 구를 그린다.
        /// </summary>
        // ------------------------------------------------------------
        public static void DrawWireSphere(Matrix4x4 matrix, Color color)
        {

        #if UNITY_EDITOR
            var (originalMatrix, originalColor) = (Handles.matrix, Handles.color);
            (Handles.matrix, Handles.color)     = (matrix, color);

            Handles.DrawWireDisc(Vector3.zero, new Vector3(1f, 0f, 0f), 1f);
            Handles.DrawWireDisc(Vector3.zero, new Vector3(0f, 1f, 0f), 1f);
            Handles.DrawWireDisc(Vector3.zero, new Vector3(0f, 0f, 1f), 1f);

            (Handles.matrix, Handles.color) = (originalMatrix, originalColor);
        #endif

        }

    #endregion

    #region 반구

        // ------------------------------------------------------------
        /// <summary>
        /// 와이어 반구를 그린다.
        /// </summary>
        // ------------------------------------------------------------
        public static void DrawWireHemisphere(Vector3 position, Quaternion rotation, float radius, Color color)
        {
            var size = new Vector3(radius, radius, radius);
            DrawWireHemisphere(Matrix4x4.TRS(position, rotation, size), color);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 와이어 반구를 그린다.
        /// </summary>
        // ------------------------------------------------------------
        public static void DrawWireHemisphere(Matrix4x4 matrix, Color color)
        {

        #if UNITY_EDITOR
            var (originalMatrix, originalColor) = (Handles.matrix, Handles.color);
            (Handles.matrix, Handles.color)     = (matrix, color);

            Handles.DrawWireDisc(Vector3.zero, Vector3.up, 1f);
            Handles.DrawWireArc(Vector3.zero, new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 1f), -180f, 1f);
            Handles.DrawWireArc(Vector3.zero, new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 0f), +180f, 1f);

            (Handles.matrix, Handles.color) = (originalMatrix, originalColor);
        #endif

        }

    #endregion

    #region 캡슐

        // ------------------------------------------------------------
        /// <summary>
        /// 와이어 캡슐을 그린다.
        /// </summary>
        // ------------------------------------------------------------
        public static void DrawWireCapsule(Vector3 position, Quaternion rotation, float radius, float height, Color color)
        {
            var lHeight = Mathf.Max(0f, height - radius * 2f);

            var upVector   = position + rotation * new Vector3(0f,  lHeight * 0.5f, 0f);
            var downVector = position - rotation * new Vector3(0f,  lHeight * 0.5f, 0f);

            DrawWireHemisphere(upVector, rotation, radius, color);
            DrawWireHemisphere(downVector, rotation * Quaternion.Euler(180f, 0f, 0f), radius, color);

            if (lHeight > 0f)
            {
                var right   = rotation * Vector3.right   * radius;
                var forward = rotation * Vector3.forward * radius;

                DrawLine(upVector + right,   downVector + right,   color);
                DrawLine(upVector - right,   downVector - right,   color);
                DrawLine(upVector + forward, downVector + forward, color);
                DrawLine(upVector - forward, downVector - forward, color);
            }
        }

    #endregion

    #region 카메라 프러스텀

        // ------------------------------------------------------------
        /// <summary>
        /// 카메라의 nearClipPlane 기준 프러스텀을 그린다.
        /// </summary>
        // ------------------------------------------------------------
        public static void DrawCameraFrustum(Camera camera, Color color)
        {

        #if UNITY_EDITOR
            var tf     = camera.transform;
            var dist   = camera.nearClipPlane;
            var fov    = camera.fieldOfView;
            var aspect = camera.aspect > 0f ? camera.aspect : 16f / 9f;

            var halfH = dist * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            var halfW = halfH * aspect;

            var center = tf.position + tf.forward * dist;
            var tl     = center + tf.up * halfH - tf.right * halfW;
            var tr     = center + tf.up * halfH + tf.right * halfW;
            var br     = center - tf.up * halfH + tf.right * halfW;
            var bl     = center - tf.up * halfH - tf.right * halfW;

            var coneColor = new Color(color.r, color.g, color.b, color.a * 0.2f);

            DrawLine(tf.position, tl, coneColor);
            DrawLine(tf.position, tr, coneColor);
            DrawLine(tf.position, br, coneColor);
            DrawLine(tf.position, bl, coneColor);

            DrawAAPolyLine(2f, color, tl, tr, br, bl, tl);
        #endif

        }

    #endregion

    }
}
