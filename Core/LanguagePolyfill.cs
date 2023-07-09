// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
#if !NET7_0_OR_GREATER
    /// <summary>
    /// Indicates that compiler support for a particular feature is required for the location where this attribute is applied.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName)
        {
            this.FeatureName = featureName;
        }

        /// <summary>
        /// The name of the compiler feature.
        /// </summary>
        public string FeatureName { get; }

        /// <summary>
        /// If true, the compiler can choose to allow access to the location where this attribute is applied if it does not understand <see cref="FeatureName"/>.
        /// </summary>
        public bool IsOptional { get; init; }

        /// <summary>
        /// The <see cref="FeatureName"/> used for the ref structs C# feature.
        /// </summary>
        public const string RefStructs = nameof(RefStructs);

        /// <summary>
        /// The <see cref="FeatureName"/> used for the required members C# feature.
        /// </summary>
        public const string RequiredMembers = nameof(RequiredMembers);
    }

    /// <summary>Specifies that a type has required members or that a member is required.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute
    {
    }
#endif

#if !NET5_0_OR_GREATER
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
#endif

#if NETSTANDARD2_0
    internal static class RuntimeHelpers
    {
        public static T[] GetSubArray<T>(T[] array, Range range)
        {
            if (array == null)
            {
                throw new ArgumentNullException();
            }

            (var offset, var length) = range.GetOffsetAndLength(array.Length);

            if (default(T)! != null || typeof(T[]) == array.GetType())
            {
                if (length == 0)
                {
                    return Array.Empty<T>();
                }

                var dest = new T[length];
                Array.Copy(array, offset, dest, 0, length);
                return dest;
            }
            else
            {
                var dest = (T[]) Array.CreateInstance(array.GetType().GetElementType()!, length);
                Array.Copy(array, offset, dest, 0, length);
                return dest;
            }
        }
    }
#endif
}

namespace System
{
#if NETSTANDARD2_0
    public readonly struct Index : IEquatable<Index>
    {
        private readonly int _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Index(int value, bool fromEnd = false)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "value must be non-negative");
            }

            this._value = fromEnd ? ~value : value;
        }

        private Index(int value)
        {
            this._value = value;
        }

        public static Index Start => new Index(0);

        public static Index End => new Index(~0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Index FromStart(int value)
        {
            return value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "value must be non-negative") : new Index(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Index FromEnd(int value)
        {
            return value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "value must be non-negative") : new Index(~value);
        }

        public int Value => this._value < 0 ? ~this._value : this._value;

        public bool IsFromEnd => this._value < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOffset(int length)
        {
            var offset = this._value;
            if (this.IsFromEnd)
            {
                offset += length + 1;
            }
            return offset;
        }

        public override bool Equals(object? value)
        {
            return value is Index index && this._value == index._value;
        }

        public bool Equals(Index other)
        {
            return this._value == other._value;
        }

        public override int GetHashCode()
        {
            return this._value;
        }

        public static implicit operator Index(int value) => FromStart(value);

        public override string ToString()
        {
            return this.IsFromEnd ? "^" + ((uint) this.Value).ToString() : ((uint) this.Value).ToString();
        }
    }

    public readonly struct Range : IEquatable<Range>
    {
        public Index Start { get; }

        public Index End { get; }

        public Range(Index start, Index end)
        {
            this.Start = start;
            this.End = end;
        }

        public override bool Equals(object? value)
        {
            return value is Range r &&
            r.Start.Equals(this.Start) &&
            r.End.Equals(this.End);
        }

        public bool Equals(Range other)
        {
            return other.Start.Equals(this.Start) && other.End.Equals(this.End);
        }

        public override int GetHashCode()
        {
            return (this.Start.GetHashCode() * 31) + this.End.GetHashCode();
        }

        public override string ToString()
        {
            return this.Start + ".." + this.End;
        }

        public static Range StartAt(Index start)
        {
            return new Range(start, Index.End);
        }

        public static Range EndAt(Index end)
        {
            return new Range(Index.Start, end);
        }

        public static Range All => new Range(Index.Start, Index.End);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int Offset, int Length) GetOffsetAndLength(int length)
        {
            int start;
            var startIndex = this.Start;
            start = startIndex.IsFromEnd ? length - startIndex.Value : startIndex.Value;

            int end;
            var endIndex = this.End;
            end = endIndex.IsFromEnd ? length - endIndex.Value : endIndex.Value;

            return (uint) end > (uint) length || (uint) start > (uint) end
                ? throw new ArgumentOutOfRangeException(nameof(length))
                : ((int Offset, int Length)) (start, end - start);
        }
    }

    public static class PolyfillExtensions
    {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
    }
#endif
}