using System;
using System.Runtime.CompilerServices;
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
	public Versor(Float3 angles) : this(Sin(angles *= Scalars.ToRadians(0.5f)), Cos(angles)) { }

	/// <summary>
	/// Creates a <see cref="Versor"/> from an <paramref name="axis"/> and an <paramref name="angle"/> (degrees).
	/// </summary>
	public Versor(Float3 axis, float angle)
	{
		float radians = Scalars.ToRadians(angle / 2f);

		float sin = (float)Math.Sin(radians);
		float cos = (float)Math.Cos(radians);

		axis = axis.Normalized;

		d = new Float4
		(
			axis.X * sin,
			axis.Y * sin,
			axis.Z * sin,
			cos
		);
	}

	/// <summary>
	/// Creates a <see cref="Versor"/> in the ZXY rotation order based on XYZ sin and cos values.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Versor(in Float3 sin, in Float3 cos) : this
	(
		sin.X * cos.Y * cos.Z + cos.X * sin.Y * sin.Z,
		cos.X * sin.Y * cos.Z - sin.X * cos.Y * sin.Z,
		cos.X * cos.Y * sin.Z - sin.X * sin.Y * cos.Z,
		cos.X * cos.Y * cos.Z + sin.X * sin.Y * sin.Z
	) { }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Versor(in Float4 d) => this.d = d;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Versor(float x, float y, float z, float w) : this(new Float4(x, y, z, w)) { }

	internal readonly Float4 d;

	public Versor Conjugate => new Versor(-d.X, -d.Y, -d.Z, d.W);
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

	public static Versor Identity = new Versor(Float4.Ana);

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

	public bool Equals(in Versor other) => Math.Abs(Dot(other)).AlmostEquals(1f);
	public override bool Equals(object obj) => obj is Versor other && Equals(other);

	public override int GetHashCode() => d.GetHashCode();
	public override string ToString() => ToString(default);

	bool IEquatable<Versor>.Equals(Versor other) => Equals(other);

	public string ToString(string format, IFormatProvider provider = null) => d.ToString(format, provider);

	public static float Dot(in Float4 value, in Float4 other) => value.Dot(other);

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

	public static Versor operator *(in Versor first, in Versor second) => Apply(first, second, false);
	public static Versor operator /(in Versor first, in Versor second) => Apply(first, second, true);

	public static Float3 operator *(in Versor first, in Float3 second) => Apply(first, second, false);
	public static Float3 operator /(in Versor first, in Float3 second) => Apply(first, second, true);

	public static bool operator ==(in Versor left, in Versor right) => left.Equals(right);
	public static bool operator !=(in Versor left, in Versor right) => !left.Equals(right);

	public static implicit operator Float3x3(in Versor value)
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

	static Float3 Sin(in Float3 radians) => new Float3((float)Math.Sin(radians.X), (float)Math.Sin(radians.Y), (float)Math.Sin(radians.Z));
	static Float3 Cos(in Float3 radians) => new Float3((float)Math.Cos(radians.X), (float)Math.Cos(radians.Y), (float)Math.Cos(radians.Z));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static Versor Apply(in Versor first, in Versor second, bool conjugate)
	{
		Float3 xyz0 = first.d.XYZ;
		Float3 xyz1 = second.d.XYZ;

		if (conjugate) xyz1 = -xyz1;

		float w0 = first.d.W;
		float w1 = second.d.W;

		Float3 cross = xyz0.Cross(xyz1);

		return new Versor
		(
			w0 * xyz1.X + w1 * xyz0.X + cross.X,
			w0 * xyz1.Y + w1 * xyz0.Y + cross.Y,
			w0 * xyz1.Z + w1 * xyz0.Z + cross.Z,
			w0 * w1 - xyz0.Dot(xyz1)
		);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static Float3 Apply(in Versor first, in Float3 second, bool conjugate)
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