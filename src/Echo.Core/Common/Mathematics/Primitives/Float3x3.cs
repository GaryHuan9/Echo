using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Common.Mathematics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Float3x3 : IEquatable<Float3x3>
{

	public Float3x3(float f00, float f01, float f02,
					float f10, float f11, float f12,
					float f20, float f21, float f22)
	{
		this.f00 = f00;
		this.f01 = f01;
		this.f02 = f02;

		this.f10 = f10;
		this.f11 = f11;
		this.f12 = f12;

		this.f20 = f20;
		this.f21 = f21;
		this.f22 = f22;
	}

	public Float3x3(Float3x3 source) : this
	(
		source.f00, source.f01, source.f02,
		source.f10, source.f11, source.f12,
		source.f20, source.f21, source.f22
	) { }

	public Float3x3(Float3 row0, Float3 row1, Float3 row2) : this
	(
		row0.X, row0.Y, row0.Z,
		row1.X, row1.Y, row1.Z,
		row2.X, row2.Y, row2.Z
	) { }

	// 00 01 02
	// 10 11 12
	// 20 21 22

	public readonly float f00;
	public readonly float f01;
	public readonly float f02;

	public readonly float f10;
	public readonly float f11;
	public readonly float f12;

	public readonly float f20;
	public readonly float f21;
	public readonly float f22;

#region Properties

#region Instance

	public float this[int row, int column]
	{
		get
		{
			unsafe
			{
				if (row < 0 || 2 < row || column < 0 || 2 < column) throw ExceptionHelper.Invalid($"{nameof(row)} or {nameof(column)}", new Int2(row, column), InvalidType.outOfBounds);
				fixed (Float3x3* pointer = &this) return ((float*)pointer)[row * 3 + column];
			}
		}
	}

	public float Determinant
	{
		get
		{
			// 00 01 02
			// 10 11 12
			// 20 21 22

			//2x2 determinants
			float d11_22 = f11 * f22 - f12 * f21;
			float d10_22 = f10 * f22 - f12 * f20;
			float d10_21 = f10 * f21 - f11 * f20;

			return f00 * d11_22 - f01 * d10_22 + f02 * d10_21;
		}
	}

	public Float3x3 Inverse
	{
		get
		{
			// 00 01 02
			// 10 11 12
			// 20 21 22

			//2x2 determinants (first row)
			float m00 = f11 * f22 - f12 * f21;
			float m01 = f10 * f22 - f12 * f20;
			float m02 = f10 * f21 - f11 * f20;

			float determinant = f00 * m00 - f01 * m01 + f02 * m02;

			if (determinant.AlmostEquals())
			{
				//Invalid inverse
				return new Float3x3
				(
					float.NaN, float.NaN, float.NaN,
					float.NaN, float.NaN, float.NaN,
					float.NaN, float.NaN, float.NaN
				);
			}

			//2x2 determinants (second row)
			float m10 = f01 * f22 - f02 * f21;
			float m11 = f00 * f22 - f02 * f20;
			float m12 = f00 * f21 - f01 * f20;

			//2x2 determinants (third row)
			float m20 = f01 * f12 - f02 * f11;
			float m21 = f00 * f12 - f02 * f10;
			float m22 = f00 * f11 - f01 * f10;

			float determinantR = 1f / determinant;

			return new Float3x3
			(
				m00 * determinantR, -m10 * determinantR, m20 * determinantR,
				-m01 * determinantR, m11 * determinantR, -m21 * determinantR,
				m02 * determinantR, -m12 * determinantR, m22 * determinantR
			);
		}
	}

	public Float3x3 Absoluted => new
	(
		Math.Abs(f00), Math.Abs(f01), Math.Abs(f02),
		Math.Abs(f10), Math.Abs(f11), Math.Abs(f12),
		Math.Abs(f20), Math.Abs(f21), Math.Abs(f22)
	);

	public Float3x3 Transposed => new
	(
		f00, f10, f20,
		f01, f11, f21,
		f02, f12, f22
	);

#endregion

#region Static

	/// <summary>
	/// The idempotent <see cref="Float3x3"/> value.
	/// </summary>
	public static Float3x3 Identity => new
	(
		1f, 0f, 0f,
		0f, 1f, 0f,
		0f, 0f, 1f
	);

#endregion

#endregion

#region Methods

#region Instance

	public Float3 GetRow(int row)
	{
		unsafe
		{
			if (row < 0 || 2 < row) throw ExceptionHelper.Invalid(nameof(row), row, InvalidType.outOfBounds);
			fixed (Float3x3* pointer = &this) return ((Float3*)pointer)[row];
		}
	}

	public Float3 GetColumn(int column)
	{
		unsafe
		{
			if (column < 0 || 2 < column) throw ExceptionHelper.Invalid(nameof(column), column, InvalidType.outOfBounds);

			fixed (Float3x3* pointer = &this)
			{
				float* p = (float*)pointer;
				return new Float3(p[column], p[column + 3], p[column + 6]);
			}
		}
	}

	public override string ToString() => ToString(default);

	public string ToString(string format, IFormatProvider provider = null) =>
		$"{f00.ToString(format, provider)}\t{f01.ToString(format, provider)}\t{f02.ToString(format, provider)}\n" +
		$"{f10.ToString(format, provider)}\t{f11.ToString(format, provider)}\t{f12.ToString(format, provider)}\n" +
		$"{f20.ToString(format, provider)}\t{f21.ToString(format, provider)}\t{f22.ToString(format, provider)}\n";

	public override bool Equals(object obj) => obj is Float3x3 other && EqualsFast(other);

	public bool Equals(Float3x3 other) => EqualsFast(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool EqualsFast(Float3x3 other) => f00.AlmostEquals(other.f00) && f01.AlmostEquals(other.f01) && f02.AlmostEquals(other.f02) &&
												 f10.AlmostEquals(other.f10) && f11.AlmostEquals(other.f11) && f12.AlmostEquals(other.f12) &&
												 f20.AlmostEquals(other.f20) && f21.AlmostEquals(other.f21) && f22.AlmostEquals(other.f22);

	public override int GetHashCode()
	{
		unchecked
		{
			int hashCode = f00.GetHashCode();
			hashCode = (hashCode * 397) ^ f01.GetHashCode();
			hashCode = (hashCode * 397) ^ f02.GetHashCode();
			hashCode = (hashCode * 397) ^ f10.GetHashCode();
			hashCode = (hashCode * 397) ^ f11.GetHashCode();
			hashCode = (hashCode * 397) ^ f12.GetHashCode();
			hashCode = (hashCode * 397) ^ f20.GetHashCode();
			hashCode = (hashCode * 397) ^ f21.GetHashCode();
			hashCode = (hashCode * 397) ^ f22.GetHashCode();
			return hashCode;
		}
	}

#endregion

#region Static

	/// <summary>
	/// Creates and returns a 3D rotational matrix that applies in ZXY order.
	/// </summary>
	public static Float3x3 Rotation(Float3 rotation) => (Float3x3)new Versor(rotation);

	/// <summary>
	/// Creates and returns a 3D scaling matrix.
	/// </summary>
	public static Float3x3 Scale(Float3 scale) => new
	(
		scale.X, 0f, 0f,
		0f, scale.Y, 0f,
		0f, 0f, scale.Z
	);

#endregion

#region Operators

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Float3x3 operator *(Float3x3 first, Float3x3 second) => new
	(
		first.f00 * second.f00 + first.f01 * second.f10 + first.f02 * second.f20, first.f00 * second.f01 + first.f01 * second.f11 + first.f02 * second.f21, first.f00 * second.f02 + first.f01 * second.f12 + first.f02 * second.f22,
		first.f10 * second.f00 + first.f11 * second.f10 + first.f12 * second.f20, first.f10 * second.f01 + first.f11 * second.f11 + first.f12 * second.f21, first.f10 * second.f02 + first.f11 * second.f12 + first.f12 * second.f22,
		first.f20 * second.f00 + first.f21 * second.f10 + first.f22 * second.f20, first.f20 * second.f01 + first.f21 * second.f11 + first.f22 * second.f21, first.f20 * second.f02 + first.f21 * second.f12 + first.f22 * second.f22
	);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Float3 operator *(Float3x3 first, Float3 second) => new
	(
		first.f00 * second.X + first.f01 * second.Y + first.f02 * second.Z,
		first.f10 * second.X + first.f11 * second.Y + first.f12 * second.Z,
		first.f20 * second.X + first.f21 * second.Y + first.f22 * second.Z
	);

	public static bool operator ==(Float3x3 first, Float3x3 second) => first.EqualsFast(second);
	public static bool operator !=(Float3x3 first, Float3x3 second) => !first.EqualsFast(second);

	public static explicit operator Float3x3(Float4x4 value) => new
	(
		value.f00, value.f01, value.f02,
		value.f10, value.f11, value.f12,
		value.f20, value.f21, value.f22
	);

#endregion

#endregion

}