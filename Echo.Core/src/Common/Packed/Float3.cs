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
public readonly partial struct Float3 : IEquatable<Float3>, ISpanFormattable
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float3(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public float X { get; }
	public float Y { get; }
	public float Z { get; }

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
		get => Math.Sqrt(SquaredMagnitudeDouble);
	}

	public float SquaredMagnitude
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X * X + Y * Y + Z * Z;
	}

	public double SquaredMagnitudeDouble
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (double)X * X + (double)Y * Y + (double)Z * Z;
	}

	public float Product
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X * Y * Z;
	}

	public double ProductDouble
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (double)X * Y * Z;
	}

	public float ProductAbsoluted
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Math.Abs(X * Y * Z);
	}

	public double ProductAbsolutedDouble
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Math.Abs((double)X * Y * Z);
	}

	public float Sum
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X + Y + Z;
	}

	public double SumDouble
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (double)X + Y + Z;
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

	public float MinComponent
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

	public float MaxComponent
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

	public float this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			unsafe
			{
				if ((uint)index > 2) throw new ArgumentOutOfRangeException(nameof(index));
				fixed (Float3* pointer = &this) return ((float*)pointer)[index];
			}
		}
	}

#endregion

#region Float3 Returns

	public Float3 Absoluted
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new Float3(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
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
			double squared = SquaredMagnitudeDouble;
			if (squared.AlmostEquals()) return Zero;

			return 1f / (float)Math.Sqrt(squared) * this;
		}
	}

	public Float3 Sorted
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

	public Float3 SortedReversed
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

	public Int3 Floored
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new Int3((int)Math.Floor(X), (int)Math.Floor(Y), (int)Math.Floor(Z));
	}

	public Float3 FlooredFloat
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new Float3((float)Math.Floor(X), (float)Math.Floor(Y), (float)Math.Floor(Z));
	}

	public Int3 Ceiled
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new Int3((int)Math.Ceiling(X), (int)Math.Ceiling(Y), (int)Math.Ceiling(Z));
	}

	public Float3 CeiledFloat
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new Float3((float)Math.Ceiling(X), (float)Math.Ceiling(Y), (float)Math.Ceiling(Z));
	}

	public Int3 Rounded
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new Int3((int)Math.Round(X), (int)Math.Round(Y), (int)Math.Round(Z));
	}

	public Float3 RoundedFloat
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new Float3((float)Math.Round(X), (float)Math.Round(Y), (float)Math.Round(Z));
	}

#endregion

#endregion

#region Methods

#region Instance

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float3 Cross(in Float3 other) => new Float3
	(
		(float)((double)Y * other.Z - (double)Z * other.Y),
		(float)((double)Z * other.X - (double)X * other.Z),
		(float)((double)X * other.Y - (double)Y * other.X)
	);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float Dot(in Float3 other) => X * other.X + Y * other.Y + Z * other.Z;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double DotDouble(in Float3 other) => (double)X * other.X + (double)Y * other.Y + (double)Z * other.Z;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float Angle(in Float3 other)
	{
		double squared = SquaredMagnitudeDouble * other.SquaredMagnitudeDouble;
		if (squared.AlmostEquals()) return 0f;

		double magnitude = Math.Sqrt(squared);
		if (magnitude.AlmostEquals()) return 0f;

		return Scalars.ToDegrees((float)Math.Acos(DotDouble(other) / magnitude));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedAngle(in Float3 other, in Float3 normal)
	{
		float angle = Angle(other);
		return Cross(other).Dot(normal) < 0f ? -angle : angle;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float Distance(in Float3 other) => (other - this).Magnitude;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double DistanceDouble(in Float3 other) => (other - this).MagnitudeDouble;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SquaredDistance(in Float3 other) => (other - this).SquaredMagnitude;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double SquaredDistanceDouble(in Float3 other) => (other - this).SquaredMagnitudeDouble;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Min(in Float3 other) => new Float3(Math.Min(X, other.X), Math.Min(Y, other.Y), Math.Min(Z, other.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Max(in Float3 other) => new Float3(Math.Max(X, other.X), Math.Max(Y, other.Y), Math.Max(Z, other.Z));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Clamp(in Float3 min, in Float3 max) => new Float3(X.Clamp(min.X, max.X), Y.Clamp(min.Y, max.Y), Z.Clamp(min.Z, max.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Clamp(float min = 0f, float max = 1f) => new Float3(X.Clamp(min, max), Y.Clamp(min, max), Z.Clamp(min, max));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float3 ClampMagnitude(float max)
	{
		double squared = SquaredMagnitudeDouble;
		if (squared <= (double)max * max) return this;

		float scale = max / (float)Math.Sqrt(squared);
		return new Float3(X * scale, Y * scale, Z * scale);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Power(in Float3 value) => new Float3((float)Math.Pow(X, value.X), (float)Math.Pow(Y, value.Y), (float)Math.Pow(Z, value.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Power(float value) => new Float3((float)Math.Pow(X, value), (float)Math.Pow(Y, value), (float)Math.Pow(Z, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Lerp(in Float3 other, in Float3 value) => new Float3(Scalars.Lerp(X, other.X, value.X), Scalars.Lerp(Y, other.Y, value.Y), Scalars.Lerp(Z, other.Z, value.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Lerp(in Float3 other, float value) => new Float3(Scalars.Lerp(X, other.X, value), Scalars.Lerp(Y, other.Y, value), Scalars.Lerp(Z, other.Z, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 InverseLerp(in Float3 other, in Float3 value) => new Float3(Scalars.InverseLerp(X, other.X, value.X), Scalars.InverseLerp(Y, other.Y, value.Y), Scalars.InverseLerp(Z, other.Z, value.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 InverseLerp(in Float3 other, float value) => new Float3(Scalars.InverseLerp(X, other.X, value), Scalars.InverseLerp(Y, other.Y, value), Scalars.InverseLerp(Z, other.Z, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Repeat(in Float3 length) => new Float3(X.Repeat(length.X), Y.Repeat(length.Y), Z.Repeat(length.Z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Repeat(float length) => new Float3(X.Repeat(length), Y.Repeat(length), Z.Repeat(length));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateXY(float degrees) => CreateXY(XY.Rotate(degrees), Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateXY(float degrees, Float2 pivot) => CreateXY(XY.Rotate(degrees, pivot), Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateXY(float degrees, in Float3 pivot) => CreateXY(XY.Rotate(degrees, pivot.XY), Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateXZ(float degrees) => CreateXZ(XZ.Rotate(degrees), Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateXZ(float degrees, Float2 pivot) => CreateXZ(XZ.Rotate(degrees, pivot), Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateXZ(float degrees, in Float3 pivot) => CreateXZ(XZ.Rotate(degrees, pivot.XZ), Y);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateYZ(float degrees) => CreateYZ(YZ.Rotate(degrees), X);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateYZ(float degrees, Float2 pivot) => CreateYZ(YZ.Rotate(degrees, pivot), X);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 RotateYZ(float degrees, in Float3 pivot) => CreateYZ(YZ.Rotate(degrees, pivot.YZ), X);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float3 Damp(in Float3 target, ref Float3 velocity, in Float3 smoothTime, float deltaTime)
	{
		float velocityX = velocity.X;
		float velocityY = velocity.Y;
		float velocityZ = velocity.Z;

		Float3 result = new Float3
		(
			X.Damp(target.X, ref velocityX, smoothTime.X, deltaTime),
			Y.Damp(target.Y, ref velocityY, smoothTime.Y, deltaTime),
			Z.Damp(target.Z, ref velocityZ, smoothTime.Z, deltaTime)
		);

		velocity = new Float3(velocityX, velocityY, velocityZ);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Damp(in Float3 target, ref Float3 velocity, float smoothTime, float deltaTime) => Damp(target, ref velocity, (Float3)smoothTime, deltaTime);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Reflect(in Float3 normal) => -2f * Dot(normal) * normal + this;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float3 Project(in Float3 normal) => normal * (Dot(normal) / normal.SquaredMagnitude);

	// ReSharper disable CompareOfFloatsByEqualityOperator
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool EqualsExact(in Float3 other) => (X == other.X) & (Y == other.Y) & (Z == other.Z);
	// ReSharper restore CompareOfFloatsByEqualityOperator

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is Float3 other && Equals(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(in Float3 other) => X.AlmostEquals(other.X) && Y.AlmostEquals(other.Y) && Z.AlmostEquals(other.Z);

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)] bool IEquatable<Float3>.Equals(Float3 other) => Equals(other);

#endregion

#region Static

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Cross(in Float3 first, in Float3 second) => first.Cross(second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Dot(in Float3 value, in Float3 other) => value.Dot(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static double DotDouble(in Float3 value, in Float3 other) => value.DotDouble(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Angle(in Float3 first, in Float3 second) => first.Angle(second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float SignedAngle(in Float3 first, in Float3 second, in Float3 normal) => first.SignedAngle(second, normal);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Distance(in Float3 value, in Float3 other) => value.Distance(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static double DistanceDouble(in Float3 value, in Float3 other) => value.DistanceDouble(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float SquaredDistance(in Float3 value, in Float3 other) => value.SquaredDistance(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static double SquaredDistanceDouble(in Float3 value, in Float3 other) => value.SquaredDistanceDouble(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Min(in Float3 value, in Float3 other) => value.Min(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Max(in Float3 value, in Float3 other) => value.Max(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Clamp(in Float3 value, in Float3 min, in Float3 max) => value.Clamp(min, max);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Clamp(in Float3 value, float min = 0f, float max = 1f) => value.Clamp(min, max);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 ClampMagnitude(in Float3 value, float max) => value.ClampMagnitude(max);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Power(in Float3 value, in Float3 power) => value.Power(power);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Power(in Float3 value, float power) => value.Power(power);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Lerp(in Float3 first, in Float3 second, in Float3 value) => first.Lerp(second, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Lerp(in Float3 first, in Float3 second, float value) => first.Lerp(second, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 InverseLerp(in Float3 first, in Float3 second, in Float3 value) => first.InverseLerp(second, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 InverseLerp(in Float3 first, in Float3 second, float value) => first.InverseLerp(second, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Repeat(in Float3 value, in Float3 length) => value.Repeat(length);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Repeat(in Float3 value, float length) => value.Repeat(length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateXY(in Float3 value, float degrees) => value.RotateXY(degrees);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateXY(in Float3 value, float degrees, Float2 pivot) => value.RotateXY(degrees, pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateXY(in Float3 value, float degrees, in Float3 pivot) => value.RotateXY(degrees, pivot);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateXZ(in Float3 value, float degrees) => value.RotateXZ(degrees);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateXZ(in Float3 value, float degrees, Float2 pivot) => value.RotateXZ(degrees, pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateXZ(in Float3 value, float degrees, in Float3 pivot) => value.RotateXZ(degrees, pivot);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateYZ(in Float3 value, float degrees) => value.RotateYZ(degrees);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateYZ(in Float3 value, float degrees, Float2 pivot) => value.RotateYZ(degrees, pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 RotateYZ(in Float3 value, float degrees, in Float3 pivot) => value.RotateYZ(degrees, pivot);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Damp(in Float3 current, in Float3 target, ref Float3 velocity, in Float3 smoothTime, float deltaTime) => current.Damp(target, ref velocity, smoothTime, deltaTime);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Damp(in Float3 current, in Float3 target, ref Float3 velocity, float smoothTime, float deltaTime) => current.Damp(target, ref velocity, smoothTime, deltaTime);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Reflect(in Float3 value, in Float3 normal) => value.Reflect(normal);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 Project(in Float3 value, in Float3 normal) => value.Project(normal);

#endregion

#region Create

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Float3 Create(int index, float value)
	{
		unsafe
		{
			if ((uint)index > 2) throw new ArgumentOutOfRangeException(nameof(index));

			Float3 result = default;
			((float*)&result)[index] = value;

			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Float3 Create(int index, float value, float other)
	{
		unsafe
		{
			if ((uint)index > 2) throw new ArgumentOutOfRangeException(nameof(index));

			Float3 result = new Float3(other, other, other);
			((float*)&result)[index] = value;

			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public static Float3 CreateX(float value, float other = 0f) => new Float3(value, other, other);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public static Float3 CreateY(float value, float other = 0f) => new Float3(other, value, other);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public static Float3 CreateZ(float value, float other = 0f) => new Float3(other, other, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public static Float3 CreateXY(Float2 value, float other = 0f) => new Float3(value.X, value.Y, other);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public static Float3 CreateYZ(Float2 value, float other = 0f) => new Float3(other, value.X, value.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public static Float3 CreateXZ(Float2 value, float other = 0f) => new Float3(value.X, other, value.Y);

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

	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float3 ReplaceX(float value) => new Float3(value, Y, Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float3 ReplaceY(float value) => new Float3(X, value, Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float3 ReplaceZ(float value) => new Float3(X, Y, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float3 ReplaceXY(Float2 value) => new Float3(value.X, value.Y, Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float3 ReplaceYZ(Float2 value) => new Float3(X, value.X, value.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float3 ReplaceXZ(Float2 value) => new Float3(value.X, Y, value.Y);

#endregion

#region Operators

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator +(in Float3 first, in Float3 second) => new Float3(first.X + second.X, first.Y + second.Y, first.Z + second.Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator -(in Float3 first, in Float3 second) => new Float3(first.X - second.X, first.Y - second.Y, first.Z - second.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator *(in Float3 first, in Float3 second) => new Float3(first.X * second.X, first.Y * second.Y, first.Z * second.Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator /(in Float3 first, in Float3 second) => new Float3(first.X / second.X, first.Y / second.Y, first.Z / second.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator *(in Float3 first, float second) => new Float3(first.X * second, first.Y * second, first.Z * second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator /(in Float3 first, float second) => new Float3(first.X / second, first.Y / second, first.Z / second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator *(float first, in Float3 second) => new Float3(first * second.X, first * second.Y, first * second.Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator /(float first, in Float3 second) => new Float3(first / second.X, first / second.Y, first / second.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator +(in Float3 value) => value;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator -(in Float3 value) => new Float3(-value.X, -value.Y, -value.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator %(in Float3 first, in Float3 second) => new Float3(first.X % second.X, first.Y % second.Y, first.Z % second.Z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator %(in Float3 first, float second) => new Float3(first.X % second, first.Y % second, first.Z % second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float3 operator %(float first, in Float3 second) => new Float3(first % second.X, first % second.Y, first % second.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(in Float3 first, in Float3 second) => first.Equals(second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(in Float3 first, in Float3 second) => !first.Equals(second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <(in Float3 first, in Float3 second) => first.X < second.X && first.Y < second.Y && first.Z < second.Z;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >(in Float3 first, in Float3 second) => first.X > second.X && first.Y > second.Y && first.Z > second.Z;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <=(in Float3 first, in Float3 second) => first.X <= second.X && first.Y <= second.Y && first.Z <= second.Z;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >=(in Float3 first, in Float3 second) => first.X >= second.X && first.Y >= second.Y && first.Z >= second.Z;

#endregion

#endregion

#region Enumerations

	/// <summary>
	/// Returns an enumerable that can be put into a foreach loop.
	/// Yields the two components of this vector in a series.
	/// </summary>
	public SeriesEnumerable Series() => new SeriesEnumerable(this);

	public readonly struct SeriesEnumerable : IEnumerable<float>
	{
		public SeriesEnumerable(in Float3 value) => enumerator = new Enumerator(value);

		readonly Enumerator enumerator;

		public Enumerator GetEnumerator() => enumerator;

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		IEnumerator<float> IEnumerable<float>.GetEnumerator() => GetEnumerator();

		public struct Enumerator : IEnumerator<float>
		{
			public Enumerator(in Float3 source)
			{
				this.source = source;
				index = -1;
			}

			readonly Float3 source;
			int index;

			object IEnumerator.Current => Current;
			public float Current => source[index];

			public bool MoveNext() => index++ < 2;
			public void Reset() => index = -1;

			public void Dispose() { }
		}
	}

#endregion

}