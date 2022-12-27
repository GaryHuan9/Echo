using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Echo.Core.Common.Mathematics;

namespace Echo.Core.Common.Packed;

[StructLayout(LayoutKind.Sequential)]
public readonly partial struct Float4 : IEquatable<Float4>, ISpanFormattable
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float4(float x, float y, float z, float w) : this(Vector128.Create(x, y, z, w)) { }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float4(in Vector128<float> v) => this.v = v;

	public readonly Vector128<float> v;

	public float X
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => v.GetElement(0);
	}

	public float Y
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => v.GetElement(1);
	}

	public float Z
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => v.GetElement(2);
	}

	public float W
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => v.GetElement(3);
	}

#region Properties

#region Scalar Returns

	public float Magnitude
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Sse.SqrtScalar(Vector128.CreateScalarUnsafe(SquaredMagnitude)).ToScalar();
	}

	public float SquaredMagnitude
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (this * this).Sum;
	}

	public float Product
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			Float4 once = this * YYWW;
			return (once * once.ZZZZ).X;
		}
	}

	public float Sum
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			Float4 once = this + YYWW;
			return (once + once.ZZZZ).X;
		}
	}

	public float Average
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Sum / 4f;
	}

	public float MinComponent
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			Float4 once = Min(YYWW);
			return once.Min(once.ZZZZ).X;
		}
	}

	public int MinIndex
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (X < Y)
			{
				if (X < Z) return X < W ? 0 : 3;
				return Z < W ? 2 : 3;
			}

			if (Y < Z) return Y < W ? 1 : 3;
			return Z < W ? 2 : 3;
		}
	}

	public float MaxComponent
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			Float4 once = Max(YYWW);
			return once.Max(once.ZZZZ).X;
		}
	}

	public int MaxIndex
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (X > Y)
			{
				if (X > Z) return X > W ? 0 : 3;
				return Z > W ? 2 : 3;
			}

			if (Y > Z) return Y > W ? 1 : 3;
			return Z > W ? 2 : 3;
		}
	}

	public float this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => v.GetElement(index);
	}

#endregion

#region Float4 Returns

	public Float4 Absoluted
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			Vector128<uint> mask = Vector128.Create(~0u >> 1);
			return new Float4(Sse.And(v, mask.AsSingle()));
		}
	}

	public Int4 Signed
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(X.Sign(), Y.Sign(), Z.Sign(), W.Sign());
	}

	public Float4 Normalized
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			//Find the squared magnitude
			Float4 square = this * this;
			square += square.YXWZ;
			square += square.ZZXX;

			//Find the approximated inverse square root with one iteration of Newton's method
			Vector128<float> three = Vector128.Create(3f);
			Vector128<float> half = Vector128.Create(0.5f);

			Vector128<float> rootR = Sse.ReciprocalSqrt(square.v);
			Vector128<float> quad = Sse.Multiply(Sse.Multiply(square.v, rootR), rootR);
			rootR = Sse.Multiply(Sse.Multiply(half, rootR), Sse.Subtract(three, quad));

			Vector128<float> normalized = Sse.Multiply(v, rootR);
			Vector128<float> zero = Sse.CompareEqual(square.v, Vector128<float>.Zero);
			return new Float4(Sse41.BlendVariable(normalized, Vector128<float>.Zero, zero));
		}
	}

	public Float4 Sorted
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (X < Y)
			{
				if (Y < Z) //XYZ
				{
					if (Z < W) return XYZW;
					if (Y < W) return XYWZ;
					if (X < W) return XWYZ;

					return WXYZ;
				}

				if (X < Z) //XZY
				{
					if (Y < W) return XZYW;
					if (Z < W) return XZWY;
					if (X < W) return XWZY;

					return WXZY;
				}

				//ZXY

				if (Y < W) return ZXYW;
				if (X < W) return ZXWY;
				if (Z < W) return ZWXY;

				return WZXY;
			}

			if (X < Z) //YXZ
			{
				if (Z < W) return YXZW;
				if (X < W) return YXWZ;
				if (Y < W) return YWXZ;

				return WYXZ;
			}

			if (Y < Z) //YZX
			{
				if (X < W) return YZXW;
				if (Z < W) return YZWX;
				if (Y < W) return YWZX;

				return WYZX;
			}

			//ZYX

			if (X < W) return ZYXW;
			if (Y < W) return ZYWX;
			if (Z < W) return ZWYX;

			return WZYX;
		}
	}

	public Float4 SortedReversed
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (X > Y)
			{
				if (Y > Z) //XYZ
				{
					if (Z > W) return XYZW;
					if (Y > W) return XYWZ;
					if (X > W) return XWYZ;

					return WXYZ;
				}

				if (X > Z) //XZY
				{
					if (Y > W) return XZYW;
					if (Z > W) return XZWY;
					if (X > W) return XWZY;

					return WXZY;
				}

				//ZXY

				if (Y > W) return ZXYW;
				if (X > W) return ZXWY;
				if (Z > W) return ZWXY;

				return WZXY;
			}

			if (X > Z) //YXZ
			{
				if (Z > W) return YXZW;
				if (X > W) return YXWZ;
				if (Y > W) return YWXZ;

				return WYXZ;
			}

			if (Y > Z) //YZX
			{
				if (X > W) return YZXW;
				if (Z > W) return YZWX;
				if (Y > W) return YWZX;

				return WYZX;
			}

			//ZYX

			if (X > W) return ZYXW;
			if (Y > W) return ZYWX;
			if (Z > W) return ZWYX;

			return WZYX;
		}
	}

	public Int4 Floored //https://stackoverflow.com/a/37091752/9196958
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new((int)Math.Floor(X), (int)Math.Floor(Y), (int)Math.Floor(Z), (int)Math.Floor(W));
	}

	public Float4 FlooredFloat
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new((float)Math.Floor(X), (float)Math.Floor(Y), (float)Math.Floor(Z), (float)Math.Floor(W));
	}

	public Int4 Ceiled
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new((int)Math.Ceiling(X), (int)Math.Ceiling(Y), (int)Math.Ceiling(Z), (int)Math.Ceiling(W));
	}

	public Float4 CeiledFloat
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new((float)Math.Ceiling(X), (float)Math.Ceiling(Y), (float)Math.Ceiling(Z), (float)Math.Ceiling(W));
	}

	public Int4 Rounded
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new((int)Math.Round(X), (int)Math.Round(Y), (int)Math.Round(Z), (int)Math.Round(W));
	}

	public Float4 RoundedFloat
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new((float)Math.Round(X), (float)Math.Round(Y), (float)Math.Round(Z), (float)Math.Round(W));
	}

#endregion

#endregion

#region Methods

#region Instance

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float Dot(in Float4 other) => (this * other).Sum;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float Angle(in Float4 other)
	{
		float squared = SquaredMagnitude * other.SquaredMagnitude;
		if (squared.AlmostEquals()) return 0f;

		double magnitude = Math.Sqrt(squared);
		if (magnitude.AlmostEquals()) return 0f;

		return Scalars.ToDegrees((float)Math.Acos(Dot(other) / magnitude));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float Distance(in Float4 other) => (other - this).Magnitude;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SquaredDistance(in Float4 other) => (other - this).SquaredMagnitude;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Min(in Float4 other) => new(Sse.Min(v, other.v));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Max(in Float4 other) => new(Sse.Max(v, other.v));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Clamp(in Float4 min, in Float4 max) => min.Max(max.Min(this));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Clamp(float min = 0f, float max = 1f) => Clamp((Float4)min, (Float4)max);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float4 ClampMagnitude(float max)
	{
		double squared = SquaredMagnitude;
		if (squared <= max * max) return this;

		float scale = max / (float)Math.Sqrt(squared);
		return new Float4(X * scale, Y * scale, Z * scale, W * scale);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Power(in Float4 value) => new((float)Math.Pow(X, value.X), (float)Math.Pow(Y, value.Y), (float)Math.Pow(Z, value.Z), (float)Math.Pow(W, value.W));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Power(float value) => new((float)Math.Pow(X, value), (float)Math.Pow(Y, value), (float)Math.Pow(Z, value), (float)Math.Pow(W, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float4 Lerp(in Float4 other, in Float4 value)
	{
		if (Fma.IsSupported)
		{
			Vector128<float> fma = Fma.MultiplyAddNegated(value.v, v, v);
			return new Float4(Fma.MultiplyAdd(value.v, other.v, fma));
		}

		Float4 length = other - this;
		return length * value + this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Lerp(in Float4 other, float value) => Lerp(other, (Float4)value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float4 InverseLerp(in Float4 other, in Float4 value)
	{
		Float4 length = other - this;
		Float4 result = (value - this) / length;

		Vector128<float> zero = Sse.CompareEqual(length.v, Vector128<float>.Zero);
		return new Float4(Sse41.BlendVariable(result.v, Vector128<float>.Zero, zero));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 InverseLerp(in Float4 other, float value) => InverseLerp(other, (Float4)value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Repeat(in Float4 length) => new(X.Repeat(length.X), Y.Repeat(length.Y), Z.Repeat(length.Z), W.Repeat(length.W));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Repeat(float length) => new(X.Repeat(length), Y.Repeat(length), Z.Repeat(length), W.Repeat(length));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float4 Damp(in Float4 target, ref Float4 velocity, in Float4 smoothTime, float deltaTime)
	{
		float velocityX = velocity.X;
		float velocityY = velocity.Y;
		float velocityZ = velocity.Z;
		float velocityW = velocity.W;

		Float4 result = new Float4
		(
			X.Damp(target.X, ref velocityX, smoothTime.X, deltaTime),
			Y.Damp(target.Y, ref velocityY, smoothTime.Y, deltaTime),
			Z.Damp(target.Z, ref velocityZ, smoothTime.Z, deltaTime),
			W.Damp(target.W, ref velocityW, smoothTime.W, deltaTime)
		);

		velocity = new Float4(velocityX, velocityY, velocityZ, velocityW);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float4 Damp(in Float4 target, ref Float4 velocity, float smoothTime, float deltaTime) => Damp(target, ref velocity, (Float4)smoothTime, deltaTime);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Reflect(in Float4 normal) => 2f * Dot(normal) * normal - this;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Project(in Float4 normal) => normal * (Dot(normal) / normal.SquaredMagnitude);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool EqualsExact(in Float4 other) => v.Equals(other.v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is Float4 other && Equals(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(in Float4 other) => X.AlmostEquals(other.X) && Y.AlmostEquals(other.Y) &&
										   Z.AlmostEquals(other.Z) && W.AlmostEquals(other.W);

	public override int GetHashCode()
	{
		unchecked
		{
			int hashCode = X.GetHashCode();
			hashCode = (hashCode * 397) ^ Y.GetHashCode();
			hashCode = (hashCode * 397) ^ Z.GetHashCode();
			hashCode = (hashCode * 397) ^ W.GetHashCode();
			return hashCode;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] bool IEquatable<Float4>.Equals(Float4 other) => Equals(other);

#endregion

#region Static

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Dot(in Float4 value, in Float4 other) => value.Dot(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Angle(in Float4 first, in Float4 second) => first.Angle(second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Distance(in Float4 value, in Float4 other) => value.Distance(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float SquaredDistance(in Float4 value, in Float4 other) => value.SquaredDistance(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Min(in Float4 value, in Float4 other) => value.Min(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Max(in Float4 value, in Float4 other) => value.Max(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Clamp(in Float4 value, in Float4 min, in Float4 max) => value.Clamp(min, max);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Clamp(in Float4 value, float min = 0f, float max = 1f) => value.Clamp(min, max);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 ClampMagnitude(in Float4 value, float max) => value.ClampMagnitude(max);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Power(in Float4 value, in Float4 power) => value.Power(power);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Power(in Float4 value, float power) => value.Power(power);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Lerp(in Float4 first, in Float4 second, in Float4 value) => first.Lerp(second, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Lerp(in Float4 first, in Float4 second, float value) => first.Lerp(second, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 InverseLerp(in Float4 first, in Float4 second, in Float4 value) => first.InverseLerp(second, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 InverseLerp(in Float4 first, in Float4 second, float value) => first.InverseLerp(second, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Repeat(in Float4 value, in Float4 length) => value.Repeat(length);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Repeat(in Float4 value, float length) => value.Repeat(length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Damp(in Float4 current, in Float4 target, ref Float4 velocity, in Float4 smoothTime, float deltaTime) => current.Damp(target, ref velocity, smoothTime, deltaTime);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Damp(in Float4 current, in Float4 target, ref Float4 velocity, float smoothTime, float deltaTime) => current.Damp(target, ref velocity, smoothTime, deltaTime);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Reflect(in Float4 value, in Float4 normal) => value.Reflect(normal);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Project(in Float4 value, in Float4 normal) => value.Project(normal);

#endregion

#region Create

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Float4 Create(int index, float value)
	{
		unsafe
		{
			if ((uint)index > 3) throw new ArgumentOutOfRangeException(nameof(index));

			Float4 result = default;
			((float*)&result)[index] = value;

			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Float4 Create(int index, float value, float other)
	{
		unsafe
		{
			if ((uint)index > 3) throw new ArgumentOutOfRangeException(nameof(index));

			Float4 result = new Float4(other, other, other, other);
			((float*)&result)[index] = value;

			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float4 Replace(int index, float value)
	{
		unsafe
		{
			if ((uint)index > 3) throw new ArgumentOutOfRangeException(nameof(index));

			Float4 result = this; //Make a copy of this struct
			((float*)&result)[index] = value;

			return result;
		}
	}

#endregion

#region Operators

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator +(in Float4 first, in Float4 second) => new(Sse.Add(first.v, second.v));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator -(in Float4 first, in Float4 second) => new(Sse.Subtract(first.v, second.v));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator *(in Float4 first, in Float4 second) => new(Sse.Multiply(first.v, second.v));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator /(in Float4 first, in Float4 second) => new(Sse.Divide(first.v, second.v));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator *(in Float4 first, float second) => first * (Float4)second;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator /(in Float4 first, float second) => first / (Float4)second;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator *(float first, in Float4 second) => (Float4)first * second;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator /(float first, in Float4 second) => (Float4)first / second;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator +(in Float4 value) => value;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator -(in Float4 value) => new(Sse.Xor(value.v, Vector128.Create(-0f)));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator %(in Float4 first, in Float4 second) => new(first.X % second.X, first.Y % second.Y, first.Z % second.Z, first.W % second.W);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator %(in Float4 first, float second) => new(first.X % second, first.Y % second, first.Z % second, first.W % second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator %(float first, in Float4 second) => new(first % second.X, first % second.Y, first % second.Z, first % second.W);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(in Float4 first, in Float4 second) => first.Equals(second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(in Float4 first, in Float4 second) => !first.Equals(second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <(in Float4 first, in Float4 second) => first.X < second.X && first.Y < second.Y && first.Z < second.Z && first.W < second.W;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >(in Float4 first, in Float4 second) => first.X > second.X && first.Y > second.Y && first.Z > second.Z && first.W > second.W;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <=(in Float4 first, in Float4 second) => first.X <= second.X && first.Y <= second.Y && first.Z <= second.Z && first.W <= second.W;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >=(in Float4 first, in Float4 second) => first.X >= second.X && first.Y >= second.Y && first.Z >= second.Z && first.W >= second.W;

#endregion

#endregion

#region Enumerations

	/// <summary>
	/// Returns an enumerable that can be put into a foreach loop.
	/// Yields the two components of this vector in a series.
	/// </summary>
	public SeriesEnumerable Series() => new(this);

	public readonly struct SeriesEnumerable : IEnumerable<float>
	{
		public SeriesEnumerable(in Float4 value) => enumerator = new Enumerator(value);

		readonly Enumerator enumerator;

		public Enumerator GetEnumerator() => enumerator;

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		IEnumerator<float> IEnumerable<float>.GetEnumerator() => GetEnumerator();

		public struct Enumerator : IEnumerator<float>
		{
			public Enumerator(in Float4 source)
			{
				this.source = source;
				index = -1;
			}

			readonly Float4 source;
			int index;

			object IEnumerator.Current => Current;
			public float Current => source[index];

			public bool MoveNext() => index++ < 3;
			public void Reset() => index = -1;

			public void Dispose() { }
		}
	}

#endregion

}