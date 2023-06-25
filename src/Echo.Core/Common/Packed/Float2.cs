using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Echo.Core.Common.Mathematics;

namespace Echo.Core.Common.Packed;

[StructLayout(LayoutKind.Sequential)]
public readonly partial struct Float2 : IEquatable<Float2>, ISpanFormattable
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float2(float x, float y)
	{
		X = x;
		Y = y;
	}

	public float X { get; }
	public float Y { get; }

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
		get => X * X + Y * Y;
	}

	public double SquaredMagnitudeDouble
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (double)X * X + (double)Y * Y;
	}

	public float Product
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X * Y;
	}

	public double ProductDouble
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (double)X * Y;
	}

	public float ProductAbsoluted
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Math.Abs(X * Y);
	}

	public double ProductAbsolutedDouble
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Math.Abs((double)X * Y);
	}

	public float Sum
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X + Y;
	}

	public double SumDouble
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (double)X + Y;
	}

	public float Average
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (float)AverageDouble;
	}

	public double AverageDouble
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ((double)X + Y) / 3d;
	}

	public float MinComponent
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X < Y ? X : Y;
	}

	public int MinIndex
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X < Y ? 0 : 1;
	}

	public float MaxComponent
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X > Y ? X : Y;
	}

	public int MaxIndex
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X > Y ? 0 : 1;
	}

	public float this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			unsafe
			{
				if ((uint)index > 1) throw new ArgumentOutOfRangeException(nameof(index));
				fixed (Float2* pointer = &this) return ((float*)pointer)[index];
			}
		}
	}

#endregion

#region Float2 Returns

	public Float2 Absoluted
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(Math.Abs(X), Math.Abs(Y));
	}

	public Int2 Signed
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(X.Sign(), Y.Sign());
	}

	public Float2 Normalized
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			double squared = SquaredMagnitudeDouble;
			if (squared.AlmostEquals()) return Zero;

			return 1f / (float)Math.Sqrt(squared) * this;
		}
	}

	public Float2 Perpendicular
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(-Y, X);
	}

	public Float2 Sorted
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X < Y ? XY : YX;
	}

	public Float2 SortedReversed
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => X > Y ? XY : YX;
	}

	public Int2 Floored
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new((int)Math.Floor(X), (int)Math.Floor(Y));
	}

	public Float2 FlooredFloat
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new((float)Math.Floor(X), (float)Math.Floor(Y));
	}

	public Int2 Ceiled
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new((int)Math.Ceiling(X), (int)Math.Ceiling(Y));
	}

	public Float2 CeiledFloat
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new((float)Math.Ceiling(X), (float)Math.Ceiling(Y));
	}

	public Int2 Rounded
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new((int)Math.Round(X), (int)Math.Round(Y));
	}

	public Float2 RoundedFloat
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new((float)Math.Round(X), (float)Math.Round(Y));
	}

#endregion

#endregion

#region Methods

#region Instance

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float Dot(Float2 other) => X * other.X + Y * other.Y;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double DotDouble(Float2 other) => (double)X * other.X + (double)Y * other.Y;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float Angle(Float2 other) => Math.Abs(SignedAngle(other));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SignedAngle(Float2 other) => Scalars.ToDegrees((float)Math.Atan2((double)X * other.Y - (double)Y * other.X, DotDouble(other)));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float Distance(Float2 other) => (other - this).Magnitude;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double DistanceDouble(Float2 other) => (other - this).MagnitudeDouble;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SquaredDistance(Float2 other) => (other - this).SquaredMagnitude;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double SquaredDistanceDouble(Float2 other) => (other - this).SquaredMagnitudeDouble;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float2 Min(Float2 other) => new(Math.Min(X, other.X), Math.Min(Y, other.Y));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float2 Max(Float2 other) => new(Math.Max(X, other.X), Math.Max(Y, other.Y));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float2 Clamp(Float2 min, Float2 max) => new(X.Clamp(min.X, max.X), Y.Clamp(min.Y, max.Y));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float2 Clamp(float min = 0f, float max = 1f) => new(X.Clamp(min, max), Y.Clamp(min, max));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float2 ClampMagnitude(float max)
	{
		double squared = SquaredMagnitudeDouble;
		if (squared <= (double)max * max) return this;

		float scale = max / (float)Math.Sqrt(squared);
		return new Float2(X * scale, Y * scale);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float2 Power(Float2 value) => new((float)Math.Pow(X, value.X), (float)Math.Pow(Y, value.Y));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float2 Power(float value) => new((float)Math.Pow(X, value), (float)Math.Pow(Y, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float2 Lerp(Float2 other, Float2 value) => new(Scalars.Lerp(X, other.X, value.X), Scalars.Lerp(Y, other.Y, value.Y));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float2 Lerp(Float2 other, float value) => new(Scalars.Lerp(X, other.X, value), Scalars.Lerp(Y, other.Y, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float2 InverseLerp(Float2 other, Float2 value) => new(Scalars.InverseLerp(X, other.X, value.X), Scalars.InverseLerp(Y, other.Y, value.Y));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float2 InverseLerp(Float2 other, float value) => new(Scalars.InverseLerp(X, other.X, value), Scalars.InverseLerp(Y, other.Y, value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float2 Repeat(Float2 length) => new(X.Repeat(length.X), Y.Repeat(length.Y));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float2 Repeat(float length) => new(X.Repeat(length), Y.Repeat(length));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float2 Rotate(float degrees)
	{
		float radians = Scalars.ToRadians(degrees);

		float sin = (float)Math.Sin(radians);
		float cos = (float)Math.Cos(radians);

		return new Float2(cos * X - sin * Y, sin * X + cos * Y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float2 Rotate(float degrees, Float2 pivot)
	{
		float radians = Scalars.ToRadians(degrees);

		float sin = (float)Math.Sin(radians);
		float cos = (float)Math.Cos(radians);

		float offsetX = X - pivot.X;
		float offsetY = Y - pivot.Y;

		return new Float2(cos * offsetX - sin * offsetY + pivot.X, sin * offsetX + cos * offsetY + pivot.Y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float2 Damp(Float2 target, ref Float2 velocity, Float2 smoothTime, float deltaTime)
	{
		float velocityX = velocity.X;
		float velocityY = velocity.Y;

		Float2 result = new Float2
		(
			X.Damp(target.X, ref velocityX, smoothTime.X, deltaTime),
			Y.Damp(target.Y, ref velocityY, smoothTime.Y, deltaTime)
		);

		velocity = new Float2(velocityX, velocityY);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float2 Damp(Float2 target, ref Float2 velocity, float smoothTime, float deltaTime) => Damp(target, ref velocity, (Float2)smoothTime, deltaTime);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float2 Reflect(Float2 normal) => 2f * Dot(normal) * normal - this;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Float2 Project(Float2 normal) => normal * (Dot(normal) / normal.SquaredMagnitude);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is Float2 other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(Float2 other) => X.AlmostEquals(other.X) && Y.AlmostEquals(other.Y);

	public override int GetHashCode() => unchecked((X.GetHashCode() * 397) ^ Y.GetHashCode());

#endregion

#region Static

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Dot(Float2 value, Float2 other) => value.Dot(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static double DotDouble(Float2 value, Float2 other) => value.DotDouble(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Angle(Float2 first, Float2 second) => first.Angle(second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float SignedAngle(Float2 first, Float2 second) => first.SignedAngle(second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Distance(Float2 value, Float2 other) => value.Distance(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static double DistanceDouble(Float2 value, Float2 other) => value.DistanceDouble(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float SquaredDistance(Float2 value, Float2 other) => value.SquaredDistance(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static double SquaredDistanceDouble(Float2 value, Float2 other) => value.SquaredDistanceDouble(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Min(Float2 value, Float2 other) => value.Min(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Max(Float2 value, Float2 other) => value.Max(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Clamp(Float2 value, Float2 min, Float2 max) => value.Clamp(min, max);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Clamp(Float2 value, float min = 0f, float max = 1f) => value.Clamp(min, max);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 ClampMagnitude(Float2 value, float max) => value.ClampMagnitude(max);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Power(Float2 value, Float2 power) => value.Power(power);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Power(Float2 value, float power) => value.Power(power);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Lerp(Float2 first, Float2 second, Float2 value) => first.Lerp(second, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Lerp(Float2 first, Float2 second, float value) => first.Lerp(second, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 InverseLerp(Float2 first, Float2 second, Float2 value) => first.InverseLerp(second, value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 InverseLerp(Float2 first, Float2 second, float value) => first.InverseLerp(second, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Repeat(Float2 value, Float2 length) => value.Repeat(length);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Repeat(Float2 value, float length) => value.Repeat(length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Rotate(Float2 value, float degrees) => value.Rotate(degrees);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Rotate(Float2 value, float degrees, Float2 pivot) => value.Rotate(degrees, pivot);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Damp(Float2 current, Float2 target, ref Float2 velocity, Float2 smoothTime, float deltaTime) => current.Damp(target, ref velocity, smoothTime, deltaTime);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Damp(Float2 current, Float2 target, ref Float2 velocity, float smoothTime, float deltaTime) => current.Damp(target, ref velocity, smoothTime, deltaTime);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Reflect(Float2 value, Float2 normal) => value.Reflect(normal);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 Project(Float2 value, Float2 normal) => value.Project(normal);

#endregion

#region Create

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Float2 Create(int index, float value)
	{
		unsafe
		{
			if ((uint)index > 1) throw new ArgumentOutOfRangeException(nameof(index));

			Float2 result = default;
			((float*)&result)[index] = value;

			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Float2 Create(int index, float value, float other)
	{
		unsafe
		{
			if ((uint)index > 1) throw new ArgumentOutOfRangeException(nameof(index));

			Float2 result = new Float2(other, other);
			((float*)&result)[index] = value;

			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public static Float2 CreateX(float value, float other = 0f) => new(value, other);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public static Float2 CreateY(float value, float other = 0f) => new(other, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float3 CreateXY(float other = 0f) => Float3.CreateXY(this, other);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float3 CreateYZ(float other = 0f) => Float3.CreateYZ(this, other);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float3 CreateXZ(float other = 0f) => Float3.CreateXZ(this, other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Float2 Replace(int index, float value)
	{
		unsafe
		{
			if ((uint)index > 1) throw new ArgumentOutOfRangeException(nameof(index));

			Float2 result = this; //Make a copy of this struct
			((float*)&result)[index] = value;

			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float2 ReplaceX(float value) => new(value, Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining), EditorBrowsable(EditorBrowsableState.Never)] public Float2 ReplaceY(float value) => new(X, value);

#endregion

#region Operators

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 operator +(Float2 first, Float2 second) => new(first.X + second.X, first.Y + second.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 operator -(Float2 first, Float2 second) => new(first.X - second.X, first.Y - second.Y);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 operator *(Float2 first, Float2 second) => new(first.X * second.X, first.Y * second.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 operator /(Float2 first, Float2 second) => new(first.X / second.X, first.Y / second.Y);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 operator *(Float2 first, float second) => new(first.X * second, first.Y * second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 operator /(Float2 first, float second) => new(first.X / second, first.Y / second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 operator *(float first, Float2 second) => new(first * second.X, first * second.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 operator /(float first, Float2 second) => new(first / second.X, first / second.Y);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 operator +(Float2 value) => value;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 operator -(Float2 value) => new(-value.X, -value.Y);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 operator %(Float2 first, Float2 second) => new(first.X % second.X, first.Y % second.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 operator %(Float2 first, float second) => new(first.X % second, first.Y % second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Float2 operator %(float first, Float2 second) => new(first % second.X, first % second.Y);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(Float2 first, Float2 second) => first.Equals(second);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(Float2 first, Float2 second) => !first.Equals(second);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <(Float2 first, Float2 second) => first.X < second.X && first.Y < second.Y;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >(Float2 first, Float2 second) => first.X > second.X && first.Y > second.Y;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <=(Float2 first, Float2 second) => first.X <= second.X && first.Y <= second.Y;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >=(Float2 first, Float2 second) => first.X >= second.X && first.Y >= second.Y;

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
		public SeriesEnumerable(Float2 value) => enumerator = new Enumerator(value);

		readonly Enumerator enumerator;

		public Enumerator GetEnumerator() => enumerator;

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		IEnumerator<float> IEnumerable<float>.GetEnumerator() => GetEnumerator();

		public struct Enumerator : IEnumerator<float>
		{
			public Enumerator(Float2 source)
			{
				this.source = source;
				index = -1;
			}

			readonly Float2 source;
			int index;

			object IEnumerator.Current => Current;
			public float Current => source[index];

			public bool MoveNext() => index++ < 1;
			public void Reset() => index = -1;

			public void Dispose() { }
		}
	}

#endregion

}