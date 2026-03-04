using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    [Serializable]
    public struct MinMax<T> : IEquatable<MinMax<T>>
    where T : struct, IComparable<T>
    {
        private static readonly EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        [SerializeField]
        private T min;
        public T Min
        {
            get => min;
            set
            {
                if (value.CompareTo(max) > 0)
                {
                    throw new InvalidOperationException($"잘못된 범위입니다. ({value} - {max})");
                }

                min = value;
            }
        }

        [SerializeField]
        private T max;
        public T Max
        {
            get => max;
            set
            {
                if (value.CompareTo(min) < 0)
                {
                    throw new InvalidOperationException($"잘못된 범위입니다. ({min} - {value})");
                }

                max = value;
            }
        }

        public MinMax(T min, T max)
        {
            if (min.CompareTo(max) > 0)
            {
                throw new InvalidOperationException($"잘못된 범위입니다. ({min} - {max})");
            }

            this.min = min;
            this.max = max;
        }

        public T Clamp(T value)
        {
            if (value.CompareTo(Min) < 0) return Min;
            if (value.CompareTo(Max) > 0) return Max;

            return value;
        }

        public bool Equals(MinMax<T> other)
        {
            var minEquals = comparer.Equals(min, other.min);
            var maxEquals = comparer.Equals(max, other.max);

            return minEquals && maxEquals;
        }

        public override bool Equals(object obj)
        {
            if (obj is MinMax<T> other)
                return Equals(other);
            return false;
        }

        public static implicit operator MinMax<T>((T Min, T Max) lTuple) => new(lTuple.Min, lTuple.Max);
        public static implicit operator (T Min, T Max)(MinMax<T> minMax) => (minMax.Min, minMax.Max);

        public override string ToString() => $"({min} - {max})";

        public override int GetHashCode() => HashCode.Combine(min, max);
    }
}