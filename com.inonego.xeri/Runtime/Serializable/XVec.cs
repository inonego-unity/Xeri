using System;
using System.Xml;
using System.Xml.Serialization;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    [Serializable]
    public struct XVec2B
    {
        [SerializeField, XmlAttribute("X")] public bool X;
        [SerializeField, XmlAttribute("Y")] public bool Y;

        public XVec2B(bool x, bool y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    [Serializable]
    public struct XVec3B
    {
        [SerializeField, XmlAttribute("X")] public bool X;
        [SerializeField, XmlAttribute("Y")] public bool Y;
        [SerializeField, XmlAttribute("Z")] public bool Z;

        public XVec3B(bool x, bool y, bool z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
    }

    [Serializable]
    public struct XVec4B
    {
        [SerializeField, XmlAttribute("X")] public bool X;
        [SerializeField, XmlAttribute("Y")] public bool Y;
        [SerializeField, XmlAttribute("Z")] public bool Z;
        [SerializeField, XmlAttribute("W")] public bool W;

        public XVec4B(bool x, bool y, bool z, bool w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }
    }

    [Serializable]
    public struct XVec2I
    {
        [SerializeField, XmlAttribute("X")] public int X;
        [SerializeField, XmlAttribute("Y")] public int Y;

        public XVec2I(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public static implicit operator Vector2Int(XVec2I vector) => new(vector.X, vector.Y);
        public static implicit operator XVec2I(Vector2Int vector2Int) => new(vector2Int.x, vector2Int.y);

        public static implicit operator (int X, int Y)(XVec2I vector) => (vector.X, vector.Y);
        public static implicit operator XVec2I((int X, int Y) lTuple) => new(lTuple.X, lTuple.Y);
    }

    [Serializable]
    public struct XVec3I
    {
        [SerializeField, XmlAttribute("X")] public int X;
        [SerializeField, XmlAttribute("Y")] public int Y;
        [SerializeField, XmlAttribute("Z")] public int Z;

        public XVec3I(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public static implicit operator Vector3Int(XVec3I vector) => new(vector.X, vector.Y, vector.Z);
        public static implicit operator XVec3I(Vector3Int vector3Int) => new(vector3Int.x, vector3Int.y, vector3Int.z);

        public static implicit operator (int X, int Y, int Z)(XVec3I vector) => (vector.X, vector.Y, vector.Z);
        public static implicit operator XVec3I((int X, int Y, int Z) lTuple) => new(lTuple.X, lTuple.Y, lTuple.Z);
    }

    [Serializable]
    public struct XVec2F
    {
        [SerializeField, XmlAttribute("X")] public float X;
        [SerializeField, XmlAttribute("Y")] public float Y;

        public XVec2F(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        public static implicit operator Vector2(XVec2F vector) => new(vector.X, vector.Y);
        public static implicit operator XVec2F(Vector2 vector) => new(vector.x, vector.y);

        public static implicit operator (float X, float Y)(XVec2F vector) => (vector.X, vector.Y);
        public static implicit operator XVec2F((float X, float Y) lTuple) => new(lTuple.X, lTuple.Y);
    }

    [Serializable]
    public struct XVec3F
    {
        [SerializeField, XmlAttribute("X")] public float X;
        [SerializeField, XmlAttribute("Y")] public float Y;
        [SerializeField, XmlAttribute("Z")] public float Z;

        public XVec3F(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public static implicit operator Vector3(XVec3F vector) => new(vector.X, vector.Y, vector.Z);
        public static implicit operator XVec3F(Vector3 vector) => new(vector.x, vector.y, vector.z);

        public static implicit operator (float X, float Y, float Z)(XVec3F vector) => (vector.X, vector.Y, vector.Z);
        public static implicit operator XVec3F((float X, float Y, float Z) lTuple) => new(lTuple.X, lTuple.Y, lTuple.Z);
    }

    [Serializable]
    public struct XVec4F
    {
        [SerializeField, XmlAttribute("X")] public float X;
        [SerializeField, XmlAttribute("Y")] public float Y;
        [SerializeField, XmlAttribute("Z")] public float Z;
        [SerializeField, XmlAttribute("W")] public float W;

        public XVec4F(float x, float y, float z, float w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }

        public static implicit operator Vector4(XVec4F vector) => new(vector.X, vector.Y, vector.Z, vector.W);
        public static implicit operator XVec4F(Vector4 vector) => new(vector.x, vector.y, vector.z, vector.w);

        public static implicit operator (float X, float Y, float Z, float W)(XVec4F vector) => (vector.X, vector.Y, vector.Z, vector.W);
        public static implicit operator XVec4F((float X, float Y, float Z, float W) lTuple) => new(lTuple.X, lTuple.Y, lTuple.Z, lTuple.W);
    }
}