using System;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ==============================================================================
    /// <summary>
    /// <br/>A serializable Nullable struct.
    /// <br/>A property drawer is also implemented to display it in the Inspector.
    /// </summary>
    // ==============================================================================
    [Serializable]
    public struct XNullable<T> where T : struct
    {
        [SerializeField] private bool hasValue;
        [SerializeField] private T value;

        public bool HasValue => hasValue;
        public T Value => hasValue ? value : throw new InvalidOperationException("The Nullable has no value. Check HasValue before accessing Value.");

        public XNullable(T value) 
        {
            this.hasValue = true;
            this.value = value;
        }

        public XNullable(T? nullable) 
        {
            this.hasValue = nullable.HasValue;
            this.value  = nullable.GetValueOrDefault();
        }

        public static implicit operator XNullable<T>(T? value) => new(value);
        public static implicit operator T?(XNullable<T> nullable) => nullable.GetValueOrDefault();

        public static implicit operator XNullable<T>(T value) => new(value);
        public static explicit operator T(XNullable<T> nullable) => nullable.GetValueOrDefault();

        public static bool operator ==(XNullable<T> left, XNullable<T> right)
        {
            if (!left.hasValue && !right.hasValue) return true;
            if (!left.hasValue || !right.hasValue) return false;
            return left.value.Equals(right.value);
        }

        public static bool operator !=(XNullable<T> left, XNullable<T> right) => !(left == right);
        
        public T GetValueOrDefault() => hasValue ? value : default;
        public T GetValueOrDefault(T defaultValue) => hasValue ? value : defaultValue;

        public override bool Equals(object obj)
        {
            if (obj is XNullable<T> other)
                return this == other;
            if (obj is T directValue)
                return hasValue && value.Equals(directValue);
            return false;
        }

        public override int GetHashCode() => hasValue ? value.GetHashCode() : 0;

        public override string ToString() => hasValue ? value.ToString() : "null";
    }

}