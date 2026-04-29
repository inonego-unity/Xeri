/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Vec.cs
수정일 : 2026-04-27

# 설명
Bool, Integer, Float 벡터 타입들의 직렬화 구조.
Unity Engine, XML, JSON 호환성 지원.

# 특이사항
- XVec4F의 Quaternion 암시적 변환은 회전 표현 필요로 인한 예외사항
========================================================================= BLOCK_HEADER_END */

using System;
using System.Xml;
using System.Xml.Serialization;

using UnityEngine;

using Newtonsoft;
using Newtonsoft.Json;

namespace inonego.Xeri.Serializable
{
    #region Bool 벡터 (B)

    // ============================================================
    /// <summary>
    /// 2차원 Bool 벡터 직렬화 구조
    /// </summary>
    // ============================================================
    [Serializable]
    public struct XVec2B
    {
        #region 필드

            [SerializeField, XmlAttribute("X"), JsonProperty("X")] public bool X;
            [SerializeField, XmlAttribute("Y"), JsonProperty("Y")] public bool Y;

        #endregion

        #region 생성자

            public XVec2B(bool x, bool y)
            {
                this.X = x;
                this.Y = y;
            }

        #endregion
    }

    // ============================================================
    /// <summary>
    /// 3차원 Bool 벡터 직렬화 구조
    /// </summary>
    // ============================================================
    [Serializable]
    public struct XVec3B
    {
        #region 필드

            [SerializeField, XmlAttribute("X"), JsonProperty("X")] public bool X;
            [SerializeField, XmlAttribute("Y"), JsonProperty("Y")] public bool Y;
            [SerializeField, XmlAttribute("Z"), JsonProperty("Z")] public bool Z;

        #endregion

        #region 생성자

            public XVec3B(bool x, bool y, bool z)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
            }

        #endregion
    }

    // ============================================================
    /// <summary>
    /// 4차원 Bool 벡터 직렬화 구조
    /// </summary>
    // ============================================================
    [Serializable]
    public struct XVec4B
    {
        #region 필드

            [SerializeField, XmlAttribute("X"), JsonProperty("X")] public bool X;
            [SerializeField, XmlAttribute("Y"), JsonProperty("Y")] public bool Y;
            [SerializeField, XmlAttribute("Z"), JsonProperty("Z")] public bool Z;
            [SerializeField, XmlAttribute("W"), JsonProperty("W")] public bool W;

        #endregion

        #region 생성자

            public XVec4B(bool x, bool y, bool z, bool w)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
                this.W = w;
            }

        #endregion
    }

    #endregion

    #region Integer 벡터 (I)

    // ============================================================
    /// <summary>
    /// 2차원 정수 벡터 직렬화 구조
    /// </summary>
    // ============================================================
    [Serializable]
    public struct XVec2I
    {
        #region 필드

            [SerializeField, XmlAttribute("X"), JsonProperty("X")] public int X;
            [SerializeField, XmlAttribute("Y"), JsonProperty("Y")] public int Y;

        #endregion

        #region 생성자

            public XVec2I(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

        #endregion

        #region 메서드

            public static implicit operator Vector2Int(XVec2I vector) => new(vector.X, vector.Y);
            public static implicit operator XVec2I(Vector2Int vector2Int) => new(vector2Int.x, vector2Int.y);

            public static implicit operator (int X, int Y)(XVec2I vector) => (vector.X, vector.Y);
            public static implicit operator XVec2I((int X, int Y) lTuple) => new(lTuple.X, lTuple.Y);

        #endregion
    }

    // ============================================================
    /// <summary>
    /// 3차원 정수 벡터 직렬화 구조
    /// </summary>
    // ============================================================
    [Serializable]
    public struct XVec3I
    {
        #region 필드

            [SerializeField, XmlAttribute("X"), JsonProperty("X")] public int X;
            [SerializeField, XmlAttribute("Y"), JsonProperty("Y")] public int Y;
            [SerializeField, XmlAttribute("Z"), JsonProperty("Z")] public int Z;

        #endregion

        #region 생성자

            public XVec3I(int x, int y, int z)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
            }

        #endregion

        #region 메서드

            public static implicit operator Vector3Int(XVec3I vector) => new(vector.X, vector.Y, vector.Z);
            public static implicit operator XVec3I(Vector3Int vector3Int) => new(vector3Int.x, vector3Int.y, vector3Int.z);

            public static implicit operator (int X, int Y, int Z)(XVec3I vector) => (vector.X, vector.Y, vector.Z);
            public static implicit operator XVec3I((int X, int Y, int Z) lTuple) => new(lTuple.X, lTuple.Y, lTuple.Z);

        #endregion
    }

    #endregion

    #region Float 벡터 (F)

    // ============================================================
    /// <summary>
    /// 2차원 실수 벡터 직렬화 구조
    /// </summary>
    // ============================================================
    [Serializable]
    public struct XVec2F
    {
        #region 필드

            [SerializeField, XmlAttribute("X"), JsonProperty("X")] public float X;
            [SerializeField, XmlAttribute("Y"), JsonProperty("Y")] public float Y;

        #endregion

        #region 생성자

            public XVec2F(float x, float y)
            {
                this.X = x;
                this.Y = y;
            }

        #endregion

        #region 메서드

            public static implicit operator Vector2(XVec2F vector) => new(vector.X, vector.Y);
            public static implicit operator XVec2F(Vector2 vector) => new(vector.x, vector.y);

            public static implicit operator (float X, float Y)(XVec2F vector) => (vector.X, vector.Y);
            public static implicit operator XVec2F((float X, float Y) lTuple) => new(lTuple.X, lTuple.Y);

        #endregion
    }

    // ============================================================
    /// <summary>
    /// 3차원 실수 벡터 직렬화 구조
    /// </summary>
    // ============================================================
    [Serializable]
    public struct XVec3F
    {
        #region 필드

            [SerializeField, XmlAttribute("X"), JsonProperty("X")] public float X;
            [SerializeField, XmlAttribute("Y"), JsonProperty("Y")] public float Y;
            [SerializeField, XmlAttribute("Z"), JsonProperty("Z")] public float Z;

        #endregion

        #region 생성자

            public XVec3F(float x, float y, float z)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
            }

        #endregion

        #region 메서드

            public static implicit operator Vector3(XVec3F vector) => new(vector.X, vector.Y, vector.Z);
            public static implicit operator XVec3F(Vector3 vector) => new(vector.x, vector.y, vector.z);

            public static implicit operator (float X, float Y, float Z)(XVec3F vector) => (vector.X, vector.Y, vector.Z);
            public static implicit operator XVec3F((float X, float Y, float Z) lTuple) => new(lTuple.X, lTuple.Y, lTuple.Z);

        #endregion
    }

    // ============================================================
    /// <summary>
    /// 4차원 실수 벡터 직렬화 구조 (회전 표현 포함)
    /// </summary>
    // ============================================================
    [Serializable]
    public struct XVec4F
    {
        #region 필드

            [SerializeField, XmlAttribute("X"), JsonProperty("X")] public float X;
            [SerializeField, XmlAttribute("Y"), JsonProperty("Y")] public float Y;
            [SerializeField, XmlAttribute("Z"), JsonProperty("Z")] public float Z;
            [SerializeField, XmlAttribute("W"), JsonProperty("W")] public float W;

        #endregion

        #region 생성자

            public XVec4F(float x, float y, float z, float w)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
                this.W = w;
            }

        #endregion

        #region 메서드

            public static implicit operator Vector4(XVec4F vector) => new(vector.X, vector.Y, vector.Z, vector.W);
            public static implicit operator XVec4F(Vector4 vector) => new(vector.x, vector.y, vector.z, vector.w);

            public static implicit operator (float X, float Y, float Z, float W)(XVec4F vector) => (vector.X, vector.Y, vector.Z, vector.W);
            public static implicit operator XVec4F((float X, float Y, float Z, float W) lTuple) => new(lTuple.X, lTuple.Y, lTuple.Z, lTuple.W);

            public static implicit operator Quaternion(XVec4F vector) => new(vector.X, vector.Y, vector.Z, vector.W);
            public static implicit operator XVec4F(Quaternion quaternion) => new(quaternion.x, quaternion.y, quaternion.z, quaternion.w);

        #endregion
    }

    #endregion
}