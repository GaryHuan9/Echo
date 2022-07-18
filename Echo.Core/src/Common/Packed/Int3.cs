using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;

namespace Echo.Core.Common.Packed;

[StructLayout(LayoutKind.Sequential)]
public readonly partial struct Int3 : IEquatable<Int3>, ISpanFormattable
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Int3(int x, int y, int z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public int X { get; }
	public int Y { get; }
	public int Z { get; }

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
		get => X * X + Y * Y + Z * Z;
	}

	public long SquaredMagnitudeLong
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (long)X * X + (long)Y * Y + (long)Z * Z;
	}

	public int Product
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X * Y * Z;
	}

	public long ProductLong
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (long)X * Y * Z;
	}

	public int ProductAbsoluted
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Math.Abs(X * Y * Z);
	}

	public long ProductAbsolutedLong
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Math.Abs((long)X * Y * Z);
	}

	public int Sum
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X + Y + Z;
	}

	public long SumLong
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (long)X + Y + Z;
	}

	public float Average
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (float)AverageDouble;
	}

	public double AverageDouble
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ((double)X + Y + Z) / 3d;
	}

	public int MinComponent
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (X < Y) return X < Z ? X : Z;
			return Y < Z ? Y : Z;
		}
	}

	public int MinIndex
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (X < Y) return X < Z ? 0 : 2;
			return Y < Z ? 1 : 2;
		}
	}

	public int MaxComponent
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (X > Y) return X > Z ? X : Z;
			return Y > Z ? Y : Z;
		}
	}

	public int MaxIndex
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (X > Y) return X > Z ? 0 : 2;
			return Y > Z ? 1 : 2;
		}
	}

	public int this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			unsafe
			{
				if ((uint)index > 2) throw new ArgumentOutOfRangeException(nameof(index));
				fixed (Int3* pointer = &this) return ((int*)pointer)[index];
			}
		}
	}

#endregion

#region Int3 Returns

	public Int3 Absoluted
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new Int3(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
	}

	public Int3 Signed
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new Int3(X.Sign(), Y.Sign(), Z.Sign());
	}

	public Float3 Normalized
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			long squared = SquaredMagnitudeLong;
			if (squared == 0) return Float3.Zero;

			return 1f / (float)Math.Sqrt(squared) * this;
		}
	}

	public Int3 Sorted
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (X < Y)
			{
				if (Y < Z) return XYZ;
				if (X < Z) return XZY;

				return ZXY;
			}

			if (X < Z) return YXZ;
			if (Y < Z) return YZX;

			return ZYX;
		}
	}

	public Int3 SortedReversed
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (X > Y)
			{
				if (Y > Z) return XYZ;
				if (X > Z) return XZY;

				return ZXY;
			}

			if (X > Z) return YXZ;
			if (Y > Z) return YZX;

			return ZYX;
		}
	}

#endregion

#endregion

#region Methods

#region Instance

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Int3 Cross(in Int3 other) => new Int3
	(
		(int)((long)Y * other.Z - (long)Z * other.Y),
		(int)((long)Z * other.X - (long)X * other.Z),
		(int)((long)X * other.Y - (long)Y * other.X)
	);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int Dot(in Int3 other) => X * other.X + Y * other.Y + Z * other.Z;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public long DotLong(in Int3 other) => (long)X * other.X + (long)Y * other.Y + (long)Z * other.Z;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float Angle(in Int3 other)
	{
		long squared = SquaredMagnitudeLong * other.SquaredMagnitudeLong;
		if (squared == 0) return 0f;

		return Scalars.ToDegrees((float)Math.Acos(DotLong(other) / Math.Sqrt(squared)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedAngle(in Int3 other, in Int3 normal)
	{
		float angle = Angle(other);
		return Cross(other).Dot(normal) < 0f ? -angle : angle;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float Distance(in Int3 other) => (other - this).Magnitude;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double DistanceDouble(in Int3 other) => (other - this).MagnitudeDouble;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int SquaredDistance(in Int3 other) => (other - this).SquaredMagnitude;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public long SquaredDistanceLong(in Int3 other) => (other - this).SquaredMagnitudeLong;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 Min(in Int3 other) => new Int3(Math.Min(X, other.X), Math.Min(Y, other.Y), Math.Min(Z, other.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 Max(in Int3 other) => new Int3(Math.Max(X, other.X), Math.Max(Y, other.Y), Math.Max(Z, other.Z));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 Clamp(in Int3 min, in Int3 max) => new Int3(X.Clamp(min.X, max.X), Y.Clamp(min.Y, max.Y), Z.Clamp(min.Z, max.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 Clamp(int min = 0, int max = 1) => new Int3(X.Clamp(min, max), Y.Clamp(min, max), Z.Clamp(min, max));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Clamp(in Float3 min, in Float3 max) => new Float3(X.Clamp(min.X, max.X), Y.Clamp(min.Y, max.Y), Z.Clamp(min.Z, max.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Clamp(float min, float max = 1f) => new Float3(X.Clamp(min, max), Y.Clamp(min, max), Z.Clamp(min, max));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float3 ClampMagnitude(float max)
	{
		double squared = SquaredMagnitudeLong;
		if (squared <= (double)max * max) return this;

		float scale = max / (float)Math.Sqrt(squared);
		return new Float3(X * scale, Y * scale, Z * scale);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Power(in Float3 value) => new Float3((float)Math.Pow(X, value.X), (float)Math.Pow(Y, value.Y), (float)Math.Pow(Z, value.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Power(float value) => new Float3((float)Math.Pow(X, value), (float)Math.Pow(Y, value), (float)Math.Pow(Z, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 Lerp(in Int3 other, in Int3 value) => new Int3(Scalars.Lerp(X, other.X, value.X), Scalars.Lerp(Y, other.Y, value.Y), Scalars.Lerp(Z, other.Z, value.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 Lerp(in Int3 other, int value) => new Int3(Scalars.Lerp(X, other.X, value), Scalars.Lerp(Y, other.Y, value), Scalars.Lerp(Z, other.Z, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Lerp(in Int3 other, in Float3 value) => new Float3(Scalars.Lerp(X, other.X, value.X), Scalars.Lerp(Y, other.Y, value.Y), Scalars.Lerp(Z, other.Z, value.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Lerp(in Int3 other, float value) => new Float3(Scalars.Lerp(X, other.X, value), Scalars.Lerp(Y, other.Y, value), Scalars.Lerp(Z, other.Z, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 InverseLerp(in Int3 other, in Int3 value) => new Int3(Scalars.InverseLerp(X, other.X, value.X), Scalars.InverseLerp(Y, other.Y, value.Y), Scalars.InverseLerp(Z, other.Z, value.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 InverseLerp(in Int3 other, int value) => new Int3(Scalars.InverseLerp(X, other.X, value), Scalars.InverseLerp(Y, other.Y, value), Scalars.InverseLerp(Z, other.Z, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 InverseLerp(in Int3 other, in Float3 value) => new Float3(Scalars.InverseLerp(X, other.X, value.X), Scalars.InverseLerp(Y, other.Y, value.Y), Scalars.InverseLerp(Z, other.Z, value.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 InverseLerp(in Int3 other, float value) => new Float3(Scalars.InverseLerp(X, other.X, value), Scalars.InverseLerp(Y, other.Y, value), Scalars.InverseLerp(Z, other.Z, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 Repeat(in Int3 length) => new Int3(X.Repeat(length.X), Y.Repeat(length.Y), Z.Repeat(length.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 Repeat(int length) => new Int3(X.Repeat(length), Y.Repeat(length), Z.Repeat(length));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Repeat(in Float3 length) => new Float3(((float)X).Repeat(length.X), ((float)Y).Repeat(length.Y), ((float)Z).Repeat(length.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Repeat(float length) => new Float3(((float)X).Repeat(length), ((float)Y).Repeat(length), ((float)Z).Repeat(length));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 FlooredDivide(in Int3 divisor) => new Int3(X.FlooredDivide(divisor.X), Y.FlooredDivide(divisor.Y), Z.FlooredDivide(divisor.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 FlooredDivide(int divisor) => new Int3(X.FlooredDivide(divisor), Y.FlooredDivide(divisor), Z.FlooredDivide(divisor));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 CeiledDivide(in Int3 divisor) => new Int3(X.CeiledDivide(divisor.X), Y.CeiledDivide(divisor.Y), Z.CeiledDivide(divisor.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 CeiledDivide(int divisor) => new Int3(X.CeiledDivide(divisor), Y.CeiledDivide(divisor), Z.CeiledDivide(divisor));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateXY(float degrees) => Float3.CreateXY(XY.Rotate(degrees), Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateXY(float degrees, Float2 pivot) => Float3.CreateXY(XY.Rotate(degrees, pivot), Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateXY(float degrees, in Float3 pivot) => Float3.CreateXY(XY.Rotate(degrees, pivot.XY), Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateXZ(float degrees) => Float3.CreateXZ(XZ.Rotate(degrees), Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateXZ(float degrees, Float2 pivot) => Float3.CreateXZ(XZ.Rotate(degrees, pivot), Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateXZ(float degrees, in Float3 pivot) => Float3.CreateXZ(XZ.Rotate(degrees, pivot.XZ), Y);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateYZ(float degrees) => Float3.CreateYZ(YZ.Rotate(degrees), X);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateYZ(float degrees, Float2 pivot) => Float3.CreateYZ(YZ.Rotate(degrees, pivot), X);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateYZ(float degrees, in Float3 pivot) => Float3.CreateYZ(YZ.Rotate(degrees, pivot.YZ), X);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Damp(in Float3 target, ref Float3 velocity, in Float3 smoothTime, float deltaTime) => Float3.Damp(this, target, ref velocity, smoothTime, deltaTime);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Damp(in Float3 target, ref Float3 velocity, float smoothTime, float deltaTime) => Float3.Damp(this, target, ref velocity, smoothTime, deltaTime);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 Reflect(in Int3 normal) => -2 * Dot(normal) * normal + this;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Project(in Float3 normal) => normal * (normal.Dot(this) / normal.SquaredMagnitude);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is Int3 other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(in Int3 other) => (X == other.X) & (Y == other.Y) & (Z == other.Z);

	public override int GetHashCode()
	{
		unchecked
		{
			int hashCode = X.GetHashCode();
			hashCode = (hashCode * 397) ^ Y.GetHashCode();
			hashCode = (hashCode * 397) ^ Z.GetHashCode();
			return hashCode;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] bool IEquatable<Int3>.Equals(Int3 other) => Equals(other);

#endregion

#region Static

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 Cross(in Int3 first, in Int3 second) => first.Cross(second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int Dot(in Int3 value, in Int3 other) => value.Dot(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static long DotLong(in Int3 value, in Int3 other) => value.DotLong(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Angle(in Int3 first, in Int3 second) => first.Angle(second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float SignedAngle(in Int3 first, in Int3 second, in Int3 normal) => first.SignedAngle(second, normal);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Distance(in Int3 value, in Int3 other) => value.Distance(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static double DistanceDouble(in Int3 value, in Int3 other) => value.DistanceDouble(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int SquaredDistance(in Int3 value, in Int3 other) => value.SquaredDistance(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static long SquaredDistanceLong(in Int3 value, in Int3 other) => value.SquaredDistanceLong(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 Min(in Int3 value, in Int3 other) => value.Min(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 Max(in Int3 value, in Int3 other) => value.Max(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 Clamp(in Int3 value, in Int3 min, in Int3 max) => value.Clamp(min, max);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 Clamp(in Int3 value, int min = 0, int max = 1) => value.Clamp(min, max);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Clamp(in Int3 value, in Float3 min, in Float3 max) => value.Clamp(min, max);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Clamp(in Int3 value, float min, float max = 1f) => value.Clamp(min, max);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 ClampMagnitude(in Int3 value, float max) => value.ClampMagnitude(max);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Power(in Float3 value, in Float3 power) => value.Power(power);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Power(in Float3 value, float power) => value.Power(power);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 Lerp(in Int3 first, in Int3 second, in Int3 value) => first.Lerp(second, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 Lerp(in Int3 first, in Int3 second, int value) => first.Lerp(second, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Lerp(in Int3 first, in Int3 second, in Float3 value) => first.Lerp(second, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Lerp(in Int3 first, in Int3 second, float value) => first.Lerp(second, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 InverseLerp(in Int3 first, in Int3 second, in Int3 value) => first.InverseLerp(second, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 InverseLerp(in Int3 first, in Int3 second, int value) => first.InverseLerp(second, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 InverseLerp(in Int3 first, in Int3 second, in Float3 value) => first.InverseLerp(second, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 InverseLerp(in Int3 first, in Int3 second, float value) => first.InverseLerp(second, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 Repeat(in Int3 value, in Int3 length) => value.Repeat(length);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 Repeat(in Int3 value, int length) => value.Repeat(length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Repeat(in Int3 value, in Float3 length) => value.Repeat(length);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Repeat(in Int3 value, float length) => value.Repeat(length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 FlooredDivide(in Int3 value, in Int3 divisor) => value.FlooredDivide(divisor);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 FlooredDivide(in Int3 value, int divisor) => value.FlooredDivide(divisor);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 CeiledDivide(in Int3 value, in Int3 divisor) => value.CeiledDivide(divisor);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 CeiledDivide(in Int3 value, int divisor) => value.CeiledDivide(divisor);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateXY(in Int3 value, float degrees) => value.RotateXY(degrees);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateXY(in Int3 value, float degrees, Float2 pivot) => value.RotateXY(degrees, pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateXY(in Int3 value, float degrees, in Float3 pivot) => value.RotateXY(degrees, pivot);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateXZ(in Int3 value, float degrees) => value.RotateXZ(degrees);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateXZ(in Int3 value, float degrees, Float2 pivot) => value.RotateXZ(degrees, pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateXZ(in Int3 value, float degrees, in Float3 pivot) => value.RotateXZ(degrees, pivot);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateYZ(in Int3 value, float degrees) => value.RotateYZ(degrees);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateYZ(in Int3 value, float degrees, Float2 pivot) => value.RotateYZ(degrees, pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateYZ(in Int3 value, float degrees, in Float3 pivot) => value.RotateYZ(degrees, pivot);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Damp(in Int3 current, in Float3 target, ref Float3 velocity, in Float3 smoothTime, float deltaTime) => Float3.Damp(current, target, ref velocity, smoothTime, deltaTime);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Damp(in Int3 current, in Float3 target, ref Float3 velocity, float smoothTime, float deltaTime) => Float3.Damp(current, target, ref velocity, smoothTime, deltaTime);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 Reflect(in Int3 value, in Int3 normal) => value.Reflect(normal);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Project(in Int3 value, in Float3 normal) => value.Project(normal);

#endregion

#region Create

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int3 Create(int index, int value)
	{
		unsafe
		{
			if ((uint)index > 2) throw new ArgumentOutOfRangeException(nameof(index));

			Int3 result = default;
			((int*)&result)[index] = value;

			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int3 Create(int index, int value, int other)
	{
		unsafe
		{
			if ((uint)index > 2) throw new ArgumentOutOfRangeException(nameof(index));

			Int3 result = new Int3(other, other, other);
			((int*)&result)[index] = value;

			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public static Int3 CreateX(int value, int other = 0) => new Int3(value, other, other);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public static Int3 CreateY(int value, int other = 0) => new Int3(other, value, other);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public static Int3 CreateZ(int value, int other = 0) => new Int3(other, other, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public static Int3 CreateXY(Int2 value, int other = 0) => new Int3(value.X, value.Y, other);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public static Int3 CreateYZ(Int2 value, int other = 0) => new Int3(other, value.X, value.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public static Int3 CreateXZ(Int2 value, int other = 0) => new Int3(value.X, other, value.Y);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Int3 Replace(int index, int value)
	{
		unsafe
		{
			if ((uint)index > 2) throw new ArgumentOutOfRangeException(nameof(index));

			Int3 result = this; //Make a copy of this struct
			((int*)&result)[index] = value;

			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float3 Replace(int index, float value)
	{
		unsafe
		{
			if ((uint)index > 2) throw new ArgumentOutOfRangeException(nameof(index));

			Float3 result = this; //Make a copy of this struct
			((float*)&result)[index] = value;

			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Int3 ReplaceX(int value) => new Int3(value, Y, Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Int3 ReplaceY(int value) => new Int3(X, value, Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Int3 ReplaceZ(int value) => new Int3(X, Y, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float3 ReplaceX(float value) => new Float3(value, Y, Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float3 ReplaceY(float value) => new Float3(X, value, Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float3 ReplaceZ(float value) => new Float3(X, Y, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Int3 ReplaceXY(Int2 value) => new Int3(value.X, value.Y, Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Int3 ReplaceYZ(Int2 value) => new Int3(X, value.X, value.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Int3 ReplaceXZ(Int2 value) => new Int3(value.X, Y, value.Y);

	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float3 ReplaceXY(Float2 value) => new Float3(value.X, value.Y, Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float3 ReplaceYZ(Float2 value) => new Float3(X, value.X, value.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float3 ReplaceXZ(Float2 value) => new Float3(value.X, Y, value.Y);

#endregion

#region Operators

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 operator +(in Int3 first, in Int3 second) => new Int3(first.X + second.X, first.Y + second.Y, first.Z + second.Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 operator -(in Int3 first, in Int3 second) => new Int3(first.X - second.X, first.Y - second.Y, first.Z - second.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 operator *(in Int3 first, in Int3 second) => new Int3(first.X * second.X, first.Y * second.Y, first.Z * second.Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 operator /(in Int3 first, in Int3 second) => new Int3(first.X / second.X, first.Y / second.Y, first.Z / second.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 operator *(in Int3 first, int second) => new Int3(first.X * second, first.Y * second, first.Z * second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 operator /(in Int3 first, int second) => new Int3(first.X / second, first.Y / second, first.Z / second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator *(in Int3 first, float second) => new Float3(first.X * second, first.Y * second, first.Z * second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator /(in Int3 first, float second) => new Float3(first.X / second, first.Y / second, first.Z / second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 operator *(int first, in Int3 second) => new Int3(first * second.X, first * second.Y, first * second.Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 operator /(int first, in Int3 second) => new Int3(first / second.X, first / second.Y, first / second.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator *(float first, in Int3 second) => new Float3(first * second.X, first * second.Y, first * second.Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator /(float first, in Int3 second) => new Float3(first / second.X, first / second.Y, first / second.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 operator +(in Int3 value) => value;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 operator -(in Int3 value) => new Int3(-value.X, -value.Y, -value.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 operator %(in Int3 first, in Int3 second) => new Int3(first.X % second.X, first.Y % second.Y, first.Z % second.Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 operator %(in Int3 first, int second) => new Int3(first.X % second, first.Y % second, first.Z % second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 operator %(int first, in Int3 second) => new Int3(first % second.X, first % second.Y, first % second.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator %(in Int3 first, float second) => new Float3(first.X % second, first.Y % second, first.Z % second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator %(float first, in Int3 second) => new Float3(first % second.X, first % second.Y, first % second.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(in Int3 first, in Int3 second) => first.Equals(second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(in Int3 first, in Int3 second) => !first.Equals(second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <(in Int3 first, in Int3 second) => first.X < second.X && first.Y < second.Y && first.Z < second.Z;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >(in Int3 first, in Int3 second) => first.X > second.X && first.Y > second.Y && first.Z > second.Z;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <=(in Int3 first, in Int3 second) => first.X <= second.X && first.Y <= second.Y && first.Z <= second.Z;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >=(in Int3 first, in Int3 second) => first.X >= second.X && first.Y >= second.Y && first.Z >= second.Z;

#endregion

#endregion

#region Enumerations

	/// <summary>
	/// Returns an enumerable that can be put into a foreach loop.
	/// Yields the three components of this vector in a series.
	/// </summary>
	public SeriesEnumerable Series() => new SeriesEnumerable(this);

	/// <summary>
	/// Returns an enumerable that can be put into a foreach loop; from (0,0,0) to (vector.x-1,vector.y-1,vector.z-1)
	/// If <paramref name="zeroAsOne"/> is true then the loop will treat zeros in the vector as ones.
	/// </summary>
	public LoopEnumerable Loop(bool zeroAsOne = false) => new LoopEnumerable(this, zeroAsOne);

	public readonly struct SeriesEnumerable : IEnumerable<int>
	{
		public SeriesEnumerable(in Int3 value) => enumerator = new Enumerator(value);

		readonly Enumerator enumerator;

		public Enumerator GetEnumerator() => enumerator;

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();

		public struct Enumerator : IEnumerator<int>
		{
			public Enumerator(in Int3 source)
			{
				this.source = source;
				index = -1;
			}

			readonly Int3 source;
			int index;

			object IEnumerator.Current => Current;
			public int Current => source[index];

			public bool MoveNext() => index++ < 2;
			public void Reset() => index = -1;

			public void Dispose() { }
		}
	}

	public readonly struct LoopEnumerable : IEnumerable<Int3>
	{
		public LoopEnumerable(in Int3 vector, bool zeroAsOne) => enumerator = new Enumerator(vector, zeroAsOne);

		readonly Enumerator enumerator;

		public Enumerator GetEnumerator() => enumerator;

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		IEnumerator<Int3> IEnumerable<Int3>.GetEnumerator() => GetEnumerator();

		public struct Enumerator : IEnumerator<Int3>
		{
			internal Enumerator(Int3 size, bool zeroAsOne)
			{
				direction = size.Signed;
				size = size.Absoluted;

				if (zeroAsOne) size = One.Max(size);
				this.size = size;

				product = size.Product;
				current = -1;
			}

			readonly Int3 direction;
			readonly Int3 size;

			readonly int product;
			int current;

			object IEnumerator.Current => Current;

			public Int3 Current => new Int3
			(
				current / (size.Y * size.Z) * direction.X,
				current / size.Z % size.Y * direction.Y,
				current % size.Z * direction.Z
			);

			public bool MoveNext() => ++current < product;

			public void Reset() => current = -1;
			public void Dispose() { }
		}
	}

#endregion

}