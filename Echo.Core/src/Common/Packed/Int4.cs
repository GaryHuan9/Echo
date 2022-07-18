using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Echo.Core.Common.Mathematics;

namespace Echo.Core.Common.Packed;

[StructLayout(LayoutKind.Sequential)]
public readonly partial struct Int4 : IEquatable<Int4>, ISpanFormattable
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Int4(int x, int y, int z, int w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public int X { get; }
	public int Y { get; }
	public int Z { get; }
	public int W { get; }

#region Properties

#region Scalar Returns

	public float Magnitude
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (float)MagnitudeDouble;
	}

	public double MagnitudeDouble
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Math.Sqrt(SquaredMagnitudeLong);
	}

	public int SquaredMagnitude
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X * X + Y * Y + Z * Z + W * W;
	}

	public long SquaredMagnitudeLong
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (long)X * X + (long)Y * Y + (long)Z * Z + (long)W * W;
	}

	public int Product
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X * Y * Z * W;
	}

	public long ProductLong
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (long)X * Y * Z * W;
	}

	public int ProductAbsoluted
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Math.Abs(X * Y * Z * W);
	}

	public long ProductAbsolutedLong
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Math.Abs((long)X * Y * Z * W);
	}

	public int Sum
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X + Y + Z + W;
	}

	public long SumLong
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (long)X + Y + Z + W;
	}

	public float Average
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (float)AverageDouble;
	}

	public double AverageDouble
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ((double)X + Y + Z + W) / 4d;
	}

	public int MinComponent
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (X < Y)
			{
				if (X < Z) return X < W ? X : W;
				return Z < W ? Z : W;
			}

			if (Y < Z) return Y < W ? Y : W;
			return Z < W ? Z : W;
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

	public int MaxComponent
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (X > Y)
			{
				if (X > Z) return X > W ? X : W;
				return Z > W ? Z : W;
			}

			if (Y > Z) return Y > W ? Y : W;
			return Z > W ? Z : W;
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

	public int this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			unsafe
			{
				if ((uint)index > 3) throw new ArgumentOutOfRangeException(nameof(index));
				fixed (Int4* pointer = &this) return ((int*)pointer)[index];
			}
		}
	}

#endregion

#region Int4 Returns

	public Int4 Absoluted
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new Int4(Math.Abs(X), Math.Abs(Y), Math.Abs(Z), Math.Abs(W));
	}

	public Int4 Signed
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new Int4(X.Sign(), Y.Sign(), Z.Sign(), W.Sign());
	}

	public Float4 Normalized
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			long squared = SquaredMagnitudeLong;
			if (squared == 0) return Float4.Zero;

			return 1f / (float)Math.Sqrt(squared) * this;
		}
	}

	public Int4 Sorted
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

	public Int4 SortedReversed
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

#endregion

#endregion

#region Methods

#region Instance

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int Dot(in Int4 other) => X * other.X + Y * other.Y + Z * other.Z + W * other.W;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public long DotLong(in Int4 other) => (long)X * other.X + (long)Y * other.Y + (long)Z * other.Z + (long)W * other.W;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float Angle(in Int4 other)
	{
		long squared = SquaredMagnitudeLong * other.SquaredMagnitudeLong;
		if (squared == 0) return 0f;

		return Scalars.ToDegrees((float)Math.Acos(DotLong(other) / Math.Sqrt(squared)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float Distance(in Int4 other) => (other - this).Magnitude;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double DistanceDouble(in Int4 other) => (other - this).MagnitudeDouble;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int SquaredDistance(in Int4 other) => (other - this).SquaredMagnitude;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public long SquaredDistanceLong(in Int4 other) => (other - this).SquaredMagnitudeLong;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int4 Min(in Int4 other) => new Int4(Math.Min(X, other.X), Math.Min(Y, other.Y), Math.Min(Z, other.Z), Math.Min(W, other.W));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int4 Max(in Int4 other) => new Int4(Math.Max(X, other.X), Math.Max(Y, other.Y), Math.Max(Z, other.Z), Math.Max(W, other.W));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int4 Clamp(in Int4 min, in Int4 max) => new Int4(X.Clamp(min.X, max.X), Y.Clamp(min.Y, max.Y), Z.Clamp(min.Z, max.Z), W.Clamp(min.W, max.W));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int4 Clamp(int min = 0, int max = 1) => new Int4(X.Clamp(min, max), Y.Clamp(min, max), Z.Clamp(min, max), W.Clamp(min, max));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Clamp(in Float4 min, in Float4 max) => new Float4(X.Clamp(min.X, max.X), Y.Clamp(min.Y, max.Y), Z.Clamp(min.Z, max.Z), W.Clamp(min.W, max.W));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Clamp(float min, float max = 1f) => new Float4(X.Clamp(min, max), Y.Clamp(min, max), Z.Clamp(min, max), W.Clamp(min, max));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float4 ClampMagnitude(float max)
	{
		double squared = SquaredMagnitudeLong;
		if (squared <= (double)max * max) return this;

		float scale = max / (float)Math.Sqrt(squared);
		return new Float4(X * scale, Y * scale, Z * scale, W * scale);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Power(in Float4 value) => new Float4((float)Math.Pow(X, value.X), (float)Math.Pow(Y, value.Y), (float)Math.Pow(Z, value.Z), (float)Math.Pow(W, value.W));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Power(float value) => new Float4((float)Math.Pow(X, value), (float)Math.Pow(Y, value), (float)Math.Pow(Z, value), (float)Math.Pow(W, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int4 Lerp(in Int4 other, in Int4 value) => new Int4(Scalars.Lerp(X, other.X, value.X), Scalars.Lerp(Y, other.Y, value.Y), Scalars.Lerp(Z, other.Z, value.Z), Scalars.Lerp(W, other.W, value.W));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int4 Lerp(in Int4 other, int value) => new Int4(Scalars.Lerp(X, other.X, value), Scalars.Lerp(Y, other.Y, value), Scalars.Lerp(Z, other.Z, value), Scalars.Lerp(W, other.W, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Lerp(in Int4 other, in Float4 value) => new Float4(Scalars.Lerp(X, other.X, value.X), Scalars.Lerp(Y, other.Y, value.Y), Scalars.Lerp(Z, other.Z, value.Z), Scalars.Lerp(Z, other.Z, value.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Lerp(in Int4 other, float value) => new Float4(Scalars.Lerp(X, other.X, value), Scalars.Lerp(Y, other.Y, value), Scalars.Lerp(Z, other.Z, value), Scalars.Lerp(Z, other.Z, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int4 InverseLerp(in Int4 other, in Int4 value) => new Int4(Scalars.InverseLerp(X, other.X, value.X), Scalars.InverseLerp(Y, other.Y, value.Y), Scalars.InverseLerp(Z, other.Z, value.Z), Scalars.InverseLerp(W, other.W, value.W));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int4 InverseLerp(in Int4 other, int value) => new Int4(Scalars.InverseLerp(X, other.X, value), Scalars.InverseLerp(Y, other.Y, value), Scalars.InverseLerp(Z, other.Z, value), Scalars.InverseLerp(W, other.W, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 InverseLerp(in Int4 other, in Float4 value) => new Float4(Scalars.InverseLerp(X, other.X, value.X), Scalars.InverseLerp(Y, other.Y, value.Y), Scalars.InverseLerp(Z, other.Z, value.Z), Scalars.InverseLerp(W, other.W, value.W));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 InverseLerp(in Int4 other, float value) => new Float4(Scalars.InverseLerp(X, other.X, value), Scalars.InverseLerp(Y, other.Y, value), Scalars.InverseLerp(Z, other.Z, value), Scalars.InverseLerp(W, other.W, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int4 Repeat(in Int4 length) => new Int4(X.Repeat(length.X), Y.Repeat(length.Y), Z.Repeat(length.Z), W.Repeat(length.W));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int4 Repeat(int length) => new Int4(X.Repeat(length), Y.Repeat(length), Z.Repeat(length), W.Repeat(length));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Repeat(in Float4 length) => new Float4(((float)X).Repeat(length.X), ((float)Y).Repeat(length.Y), ((float)Z).Repeat(length.Z), ((float)Z).Repeat(length.W));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Repeat(float length) => new Float4(((float)X).Repeat(length), ((float)Y).Repeat(length), ((float)Z).Repeat(length), ((float)W).Repeat(length));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int4 FlooredDivide(in Int4 divisor) => new Int4(X.FlooredDivide(divisor.X), Y.FlooredDivide(divisor.Y), Z.FlooredDivide(divisor.Z), W.FlooredDivide(divisor.W));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int4 FlooredDivide(int divisor) => new Int4(X.FlooredDivide(divisor), Y.FlooredDivide(divisor), Z.FlooredDivide(divisor), W.FlooredDivide(divisor));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int4 CeiledDivide(in Int4 divisor) => new Int4(X.CeiledDivide(divisor.X), Y.CeiledDivide(divisor.Y), Z.CeiledDivide(divisor.Z), W.CeiledDivide(divisor.W));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int4 CeiledDivide(int divisor) => new Int4(X.CeiledDivide(divisor), Y.CeiledDivide(divisor), Z.CeiledDivide(divisor), W.CeiledDivide(divisor));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Damp(in Float4 target, ref Float4 velocity, in Float4 smoothTime, float deltaTime) => Float4.Damp(this, target, ref velocity, smoothTime, deltaTime);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Damp(in Float4 target, ref Float4 velocity, float smoothTime, float deltaTime) => Float4.Damp(this, target, ref velocity, smoothTime, deltaTime);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int4 Reflect(in Int4 normal) => -2 * Dot(normal) * normal + this;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float4 Project(in Float4 normal) => normal * (normal.Dot(this) / normal.SquaredMagnitude);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is Int4 other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(in Int4 other) => (X == other.X) & (Y == other.Y) & (Z == other.Z) & (W == other.W);

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)] bool IEquatable<Int4>.Equals(Int4 other) => Equals(other);

#endregion

#region Static

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Dot(in Int4 value, in Int4 other) => value.Dot(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static double DotLong(in Int4 value, in Int4 other) => value.DotLong(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Angle(in Int4 first, in Int4 second) => first.Angle(second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Distance(in Int4 value, in Int4 other) => value.Distance(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static double DistanceDouble(in Int4 value, in Int4 other) => value.DistanceDouble(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float SquaredDistance(in Int4 value, in Int4 other) => value.SquaredDistance(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static double SquaredDistanceLong(in Int4 value, in Int4 other) => value.SquaredDistanceLong(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 Min(in Int4 value, in Int4 other) => value.Min(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 Max(in Int4 value, in Int4 other) => value.Max(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 Clamp(in Int4 value, in Int4 min, in Int4 max) => value.Clamp(min, max);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 Clamp(in Int4 value, int min = 0, int max = 1) => value.Clamp(min, max);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Clamp(in Int4 value, in Float4 min, in Float4 max) => value.Clamp(min, max);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Clamp(in Int4 value, float min, float max = 1f) => value.Clamp(min, max);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 ClampMagnitude(in Int4 value, float max) => value.ClampMagnitude(max);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Power(in Float4 value, in Float4 power) => value.Power(power);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Power(in Float4 value, float power) => value.Power(power);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 Lerp(in Int4 first, in Int4 second, in Int4 value) => first.Lerp(second, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 Lerp(in Int4 first, in Int4 second, int value) => first.Lerp(second, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Lerp(in Int4 first, in Int4 second, in Float4 value) => first.Lerp(second, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Lerp(in Int4 first, in Int4 second, float value) => first.Lerp(second, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 InverseLerp(in Int4 first, in Int4 second, in Int4 value) => first.InverseLerp(second, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 InverseLerp(in Int4 first, in Int4 second, int value) => first.InverseLerp(second, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 InverseLerp(in Int4 first, in Int4 second, in Float4 value) => first.InverseLerp(second, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 InverseLerp(in Int4 first, in Int4 second, float value) => first.InverseLerp(second, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 Repeat(in Int4 value, in Int4 length) => value.Repeat(length);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 Repeat(in Int4 value, int length) => value.Repeat(length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Repeat(in Int4 value, in Float4 length) => value.Repeat(length);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Repeat(in Int4 value, float length) => value.Repeat(length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 FlooredDivide(in Int4 value, in Int4 divisor) => value.FlooredDivide(divisor);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 FlooredDivide(in Int4 value, int divisor) => value.FlooredDivide(divisor);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 CeiledDivide(in Int4 value, in Int4 divisor) => value.CeiledDivide(divisor);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 CeiledDivide(in Int4 value, int divisor) => value.CeiledDivide(divisor);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Damp(in Int4 current, in Float4 target, ref Float4 velocity, in Float4 smoothTime, float deltaTime) => Float4.Damp(current, target, ref velocity, smoothTime, deltaTime);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Damp(in Int4 current, in Float4 target, ref Float4 velocity, float smoothTime, float deltaTime) => Float4.Damp(current, target, ref velocity, smoothTime, deltaTime);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 Reflect(in Int4 value, in Int4 normal) => value.Reflect(normal);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 Project(in Int4 value, in Float4 normal) => value.Project(normal);

#endregion

#region Create

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int4 Create(int index, int value)
	{
		unsafe
		{
			if ((uint)index > 3) throw new ArgumentOutOfRangeException(nameof(index));

			Int4 result = default;
			((int*)&result)[index] = value;

			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int4 Create(int index, int value, int other)
	{
		unsafe
		{
			if ((uint)index > 3) throw new ArgumentOutOfRangeException(nameof(index));

			Int4 result = new Int4(other, other, other, other);
			((int*)&result)[index] = value;

			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Int4 Replace(int index, int value)
	{
		unsafe
		{
			if ((uint)index > 3) throw new ArgumentOutOfRangeException(nameof(index));

			Int4 result = this; //Make a copy of this struct
			((int*)&result)[index] = value;

			return result;
		}
	}

#endregion

#region Operators

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 operator +(in Int4 first, in Int4 second) => new Int4(first.X + second.X, first.Y + second.Y, first.Z + second.Z, first.W + second.W);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 operator -(in Int4 first, in Int4 second) => new Int4(first.X - second.X, first.Y - second.Y, first.Z - second.Z, first.W - second.W);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 operator *(in Int4 first, in Int4 second) => new Int4(first.X * second.X, first.Y * second.Y, first.Z * second.Z, first.W * second.W);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 operator /(in Int4 first, in Int4 second) => new Int4(first.X / second.X, first.Y / second.Y, first.Z / second.Z, first.W / second.W);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 operator *(in Int4 first, int second) => new Int4(first.X * second, first.Y * second, first.Z * second, first.W * second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 operator /(in Int4 first, int second) => new Int4(first.X / second, first.Y / second, first.Z / second, first.W / second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator *(in Int4 first, float second) => new Float4(first.X * second, first.Y * second, first.Z * second, first.W * second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator /(in Int4 first, float second) => new Float4(first.X / second, first.Y / second, first.Z / second, first.W / second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 operator *(int first, in Int4 second) => new Int4(first * second.X, first * second.Y, first * second.Z, first * second.W);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 operator /(int first, in Int4 second) => new Int4(first / second.X, first / second.Y, first / second.Z, first / second.W);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator *(float first, in Int4 second) => new Float4(first * second.X, first * second.Y, first * second.Z, first * second.W);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator /(float first, in Int4 second) => new Float4(first / second.X, first / second.Y, first / second.Z, first / second.W);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 operator +(in Int4 value) => value;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 operator -(in Int4 value) => new Int4(-value.X, -value.Y, -value.Z, -value.W);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 operator %(in Int4 first, in Int4 second) => new Int4(first.X % second.X, first.Y % second.Y, first.Z % second.Z, first.W % second.W);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 operator %(in Int4 first, int second) => new Int4(first.X % second, first.Y % second, first.Z % second, first.W % second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int4 operator %(int first, in Int4 second) => new Int4(first % second.X, first % second.Y, first % second.Z, first % second.W);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator %(in Int4 first, float second) => new Float4(first.X % second, first.Y % second, first.Z % second, first.W % second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float4 operator %(float first, in Int4 second) => new Float4(first % second.X, first % second.Y, first % second.Z, first % second.W);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(in Int4 first, in Int4 second) => first.Equals(second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(in Int4 first, in Int4 second) => !first.Equals(second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <(in Int4 first, in Int4 second) => first.X < second.X && first.Y < second.Y && first.Z < second.Z && first.W < second.W;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >(in Int4 first, in Int4 second) => first.X > second.X && first.Y > second.Y && first.Z > second.Z && first.W > second.W;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <=(in Int4 first, in Int4 second) => first.X <= second.X && first.Y <= second.Y && first.Z <= second.Z && first.W <= second.W;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >=(in Int4 first, in Int4 second) => first.X >= second.X && first.Y >= second.Y && first.Z >= second.Z && first.W >= second.W;

#endregion

#endregion

#region Enumerations

	/// <summary>
	/// Returns an enumerable that can be put into a foreach loop.
	/// Yields the two components of this vector in a series.
	/// </summary>
	public SeriesEnumerable Series() => new SeriesEnumerable(this);

	public readonly struct SeriesEnumerable : IEnumerable<int>
	{
		public SeriesEnumerable(in Int4 value) => enumerator = new Enumerator(value);

		readonly Enumerator enumerator;

		public Enumerator GetEnumerator() => enumerator;

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();

		public struct Enumerator : IEnumerator<int>
		{
			public Enumerator(in Int4 source)
			{
				this.source = source;
				index = -1;
			}

			readonly Int4 source;
			int index;

			object IEnumerator.Current => Current;
			public int Current => source[index];

			public bool MoveNext() => index++ < 3;
			public void Reset() => index = -1;

			public void Dispose() { }
		}
	}

#endregion

}