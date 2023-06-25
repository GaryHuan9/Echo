using System;
using System.Runtime.CompilerServices;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Common.Mathematics.Primitives;

/// <summary>
/// A unit-length quaternion for 3D ZXY rotation.
/// </summary>
public readonly struct Versor : IEquatable<Versor>, IFormattable
{
	/// <summary>
	/// Creates a <see cref="Versor"/> from three angles (degrees) in the ZXY rotation order.
	/// </summary>
	public Versor(float angleX, float angleY, float angleZ) : this(new Float3(angleX, angleY, angleZ)) { }

	/// <summary>
	/// Creates a <see cref="Versor"/> from three angles (degrees) in the ZXY rotation order.
	/// </summary>
	public Versor(in Float3 angles)
	{
		Float3 halfRadians = angles * Scalars.ToRadians(0.5f);
		FastMath.SinCos(halfRadians.X, out float sinX, out float cosX);
		FastMath.SinCos(halfRadians.Y, out float sinY, out float cosY);
		FastMath.SinCos(halfRadians.Z, out float sinZ, out float cosZ);

		d = new Float4
		(
			sinX * cosY * cosZ + cosX * sinY * sinZ,
			cosX * sinY * cosZ - sinX * cosY * sinZ,
			cosX * cosY * sinZ - sinX * sinY * cosZ,
			cosX * cosY * cosZ + sinX * sinY * sinZ
		);
	}

	/// <summary>
	/// Creates a <see cref="Versor"/> from an <paramref name="axis"/> and an <paramref name="angle"/> (degrees).
	/// </summary>
	public Versor(in Float3 axis, float angle)
	{
		Ensure.AreEqual(axis.SquaredMagnitude, 1f);
		float radians = Scalars.ToRadians(angle / 2f);

		float sin = (float)Math.Sin(radians);
		float cos = (float)Math.Cos(radians);

		d = new Float4
		(
			axis.X * sin,
			axis.Y * sin,
			axis.Z * sin,
			cos
		);
	}

	/// <summary>
	/// Creates a <see cref="Versor"/> that rotates from <paramref name="value0"/> to <see cref="value1"/>.
	/// </summary>
	public Versor(in Float3 value0, in Float3 value1)
	{
		Ensure.AreEqual(value0.SquaredMagnitude, 1f);
		Ensure.AreEqual(value1.SquaredMagnitude, 1f);

		float dot = Float3.Dot(value0, value1);

		if (FastMath.Abs(dot) < 1f - FastMath.Epsilon)
		{
			Float3 axis = Float3.Cross(value0, value1);
			d = new Float4(axis.X, axis.Y, axis.Z, 1f + dot).Normalized;
		}
		else if (dot < 0f)
		{
			Float3 axis = FastMath.Abs(value0.X) > 0.9f ?
				Float3.Cross(Float3.Forward, value0) :
				Float3.Cross(Float3.Right, value0);
			d = new Float4(axis.X, axis.Y, axis.Z, 0f).Normalized;
		}
		else this = Identity;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Versor(in Float4 d) => this.d = d;

	internal readonly Float4 d;

	public Versor Conjugate => new(new Float4(-d.X, -d.Y, -d.Z, d.W));
	public Versor Inverse => Conjugate;

	public Float3 Angles
	{
		get
		{
			Float4 d2 = d * 2f;
			Float4 xs = d2.X * d;
			Float4 ys = d2.Y * d;
			Float4 zs = d2.Z * d;

			float xw_yz = xs.W - ys.Z;
			float abs = Math.Abs(xw_yz);

			float x = abs >= 1f ? 90f * Math.Sign(xw_yz) : Scalars.ToDegrees((float)Math.Asin(xw_yz));

			if (abs.AlmostEquals(1f))
			{
				//Singularity
				float y = (float)Math.Atan2(ys.W - xs.Z, 1f - ys.Y - zs.Z);
				return new Float3(x, Scalars.ToDegrees(y), 0f);
			}
			else
			{
				//General cases
				float y = Scalars.ToDegrees((float)Math.Atan2(xs.Z + ys.W, 1f - xs.X - ys.Y));
				float z = Scalars.ToDegrees((float)Math.Atan2(xs.Y + zs.W, 1f - xs.X - zs.Z));

				return new Float3(x, y, z);
			}
		}
	}

	public static Versor Identity => new(Float4.Ana);

	public float Dot(in Versor other) => d.Dot(other.d);

	/// <summary>
	/// Returns the smallest angle between this <see cref="Versor"/> and <paramref name="other"/>.
	/// </summary>
	public float Angle(in Versor other)
	{
		float dot = Math.Abs(Dot(other));
		if (dot.AlmostEquals()) return 0f;

		return Scalars.ToDegrees((float)Math.Acos(dot) * 2f);
	}

	public Versor Damp(in Versor target, ref Float4 velocity, float smoothTime, float deltaTime) => Damp(this, target, ref velocity, smoothTime, deltaTime);

	public bool Equals(in Versor other) => FastMath.Abs(Dot(other)).AlmostEquals(1f);
	public override bool Equals(object obj) => obj is Versor other && Equals(other);

	public override int GetHashCode() => d.GetHashCode();
	public override string ToString() => ToString(default);

	bool IEquatable<Versor>.Equals(Versor other) => Equals(other);

	public string ToString(string format, IFormatProvider provider = null) => d.ToString(format, provider);

	/// <summary>
	/// Returns the smallest angle between this <see cref="Versor"/> and <paramref name="other"/>.
	/// </summary>
	public static float Angle(in Versor value, in Versor other) => value.Angle(other);

	public static Versor Damp(in Versor current, in Versor target, ref Float4 velocity, float smoothTime, float deltaTime)
	{
		//Code based on: https://gist.github.com/maxattack/4c7b4de00f5c1b95a33b

		if (deltaTime < Scalars.Epsilon) return current;
		ref readonly Float4 currentData = ref current.d;

		Float4 targetData = target.d;

		if (currentData.Dot(targetData) < 0f) targetData = -targetData;

		Float4 result = currentData.Damp(targetData, ref velocity, smoothTime, deltaTime).Normalized;
		velocity -= velocity.Project(result);

		return new Versor(result);
	}

	public static Versor operator *(in Versor first, in Versor second) => Join(first, second, false);
	public static Versor operator /(in Versor first, in Versor second) => Join(first, second, true);

	public static Float3 operator *(in Versor first, in Float3 second) => Join(first, second, false);
	public static Float3 operator /(in Versor first, in Float3 second) => Join(first, second, true);

	public static bool operator ==(in Versor left, in Versor right) => left.Equals(right);
	public static bool operator !=(in Versor left, in Versor right) => !left.Equals(right);

	public static explicit operator Float3x3(in Versor value)
	{
		ref readonly Float4 d = ref value.d;

		Float3 d2 = d.XYZ * 2f;
		Float4 xs = d2.X * d;

		float yy = d2.Y * d.Y;
		float yz = d2.Y * d.Z;
		float yw = d2.Y * d.W;

		float zz = d2.Z * d.Z;
		float zw = d2.Z * d.W;

		return new Float3x3
		(
			1.0f - yy - zz, xs.Y - zw, xs.Z + yw,
			xs.Y + zw, 1f - xs.X - zz, yz - xs.W,
			xs.Z - yw, yz + xs.W, 1f - xs.X - yy
		);
	}

	static Versor Join(in Versor first, in Versor second, bool conjugate)
	{
		Float3 xyz0 = first.d.XYZ;
		Float3 xyz1 = second.d.XYZ;

		if (conjugate) xyz1 = -xyz1;

		float w0 = first.d.W;
		float w1 = second.d.W;

		Float3 cross = xyz0.Cross(xyz1);
		Float4 result = new Float4
		(
			w0 * xyz1.X + w1 * xyz0.X + cross.X,
			w0 * xyz1.Y + w1 * xyz0.Y + cross.Y,
			w0 * xyz1.Z + w1 * xyz0.Z + cross.Z,
			w0 * w1 - xyz0.Dot(xyz1)
		);

		return new Versor(result);
	}

	static Float3 Join(in Versor first, in Float3 second, bool conjugate)
	{
		ref readonly Float4 d = ref first.d;

		Float4 dd = d * d;
		Float3 dw = d.W * 2f * d.XYZ;
		Float2 dz = d.Z * 2f * d.XY;
		float dy_x = d.Y * 2f * d.X;

		if (conjugate) dw = -dw;

		return new Float3
		(
			dd.W * second.X + dd.X * second.X - dw.Z * second.Y + dy_x * second.Y + dw.Y * second.Z + dz.X * second.Z - dd.Z * second.X - dd.Y * second.X,
			dy_x * second.X + dw.Z * second.X + dd.Y * second.Y - dd.Z * second.Y + dz.Y * second.Z - dw.X * second.Z + dd.W * second.Y - dd.X * second.Y,
			dz.X * second.X - dw.Y * second.X + dz.Y * second.Y + dw.X * second.Y + dd.Z * second.Z - dd.Y * second.Z - dd.X * second.Z + dd.W * second.Z
		);
	}
}