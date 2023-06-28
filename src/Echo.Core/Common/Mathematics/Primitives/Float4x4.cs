using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Common.Mathematics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Float4x4 : IEquatable<Float4x4>, IFormattable
{
	public Float4x4(float f00, float f01, float f02, float f03,
					float f10, float f11, float f12, float f13,
					float f20, float f21, float f22, float f23,
					float f30, float f31, float f32, float f33)
	{
		this.f00 = f00;
		this.f01 = f01;
		this.f02 = f02;
		this.f03 = f03;

		this.f10 = f10;
		this.f11 = f11;
		this.f12 = f12;
		this.f13 = f13;

		this.f20 = f20;
		this.f21 = f21;
		this.f22 = f22;
		this.f23 = f23;

		this.f30 = f30;
		this.f31 = f31;
		this.f32 = f32;
		this.f33 = f33;
	}

	public Float4x4(Float4x4 source) : this
	(
		source.f00, source.f01, source.f02, source.f03,
		source.f10, source.f11, source.f12, source.f13,
		source.f20, source.f21, source.f22, source.f23,
		source.f30, source.f31, source.f32, source.f33
	) { }

	public Float4x4(Float4 row0, Float4 row1, Float4 row2, Float4 row3) : this
	(
		row0.X, row0.Y, row0.Z, row0.W,
		row1.X, row1.Y, row1.Z, row1.W,
		row2.X, row2.Y, row2.Z, row2.W,
		row3.X, row3.Y, row3.Z, row3.W
	) { }

	// 00 01 02 03
	// 10 11 12 13
	// 20 21 22 23
	// 30 31 32 33

	public readonly float f00;
	public readonly float f01;
	public readonly float f02;
	public readonly float f03;

	public readonly float f10;
	public readonly float f11;
	public readonly float f12;
	public readonly float f13;

	public readonly float f20;
	public readonly float f21;
	public readonly float f22;
	public readonly float f23;

	public readonly float f30;
	public readonly float f31;
	public readonly float f32;
	public readonly float f33;

#region Properties

#region Instance

	public float this[int row, int column]
	{
		get
		{
			unsafe
			{
				if (row < 0 || 3 < row || column < 0 || 3 < column) throw ExceptionHelper.Invalid($"{nameof(row)} or {nameof(column)}", new Int2(row, column), InvalidType.outOfBounds);
				fixed (Float4x4* pointer = &this) return ((float*)pointer)[row * 4 + column];
			}
		}
	}

	public float Determinant
	{
		get
		{
			// 00 01 02 03
			// 10 11 12 13
			// 20 21 22 23
			// 30 31 32 33

			//2x2 determinants
			float d21_32 = f21 * f32 - f22 * f31;
			float d21_33 = f21 * f33 - f23 * f31;
			float d22_33 = f22 * f33 - f23 * f32;

			float d20_31 = f20 * f31 - f21 * f30;
			float d20_32 = f20 * f32 - f22 * f30;
			float d20_33 = f20 * f33 - f23 * f30;

			//First row mirrors
			float m00 = f11 * d22_33 - f12 * d21_33 + f13 * d21_32;
			float m01 = f10 * d22_33 - f12 * d20_33 + f13 * d20_32;
			float m02 = f10 * d21_33 - f11 * d20_33 + f13 * d20_31;
			float m03 = f10 * d21_32 - f11 * d20_32 + f12 * d20_31;

			return f00 * m00 - f01 * m01 + f02 * m02 - f03 * m03;
		}
	}

	public Float4x4 Inverse
	{
		get
		{
			// 00 01 02 03
			// 10 11 12 13
			// 20 21 22 23
			// 30 31 32 33

			//2x2 determinants
			float d21_32 = f21 * f32 - f22 * f31;
			float d21_33 = f21 * f33 - f23 * f31;
			float d22_33 = f22 * f33 - f23 * f32;

			float d20_31 = f20 * f31 - f21 * f30;
			float d20_32 = f20 * f32 - f22 * f30;
			float d20_33 = f20 * f33 - f23 * f30;

			//3x3 determinants (first row)
			float m00 = f11 * d22_33 - f12 * d21_33 + f13 * d21_32;
			float m01 = f10 * d22_33 - f12 * d20_33 + f13 * d20_32;
			float m02 = f10 * d21_33 - f11 * d20_33 + f13 * d20_31;
			float m03 = f10 * d21_32 - f11 * d20_32 + f12 * d20_31;

			float determinant = f00 * m00 - f01 * m01 + f02 * m02 - f03 * m03;

			if (determinant.AlmostEquals())
			{
				//Invalid inverse
				return new Float4x4
				(
					float.NaN, float.NaN, float.NaN, float.NaN, float.NaN, float.NaN, float.NaN, float.NaN,
					float.NaN, float.NaN, float.NaN, float.NaN, float.NaN, float.NaN, float.NaN, float.NaN
				);
			}

			//3x3 determinants (second row)
			float m10 = f01 * d22_33 - f02 * d21_33 + f03 * d21_32;
			float m11 = f00 * d22_33 - f02 * d20_33 + f03 * d20_32;
			float m12 = f00 * d21_33 - f01 * d20_33 + f03 * d20_31;
			float m13 = f00 * d21_32 - f01 * d20_32 + f02 * d20_31;

			//2x2 determinants
			float d01_12 = f01 * f12 - f02 * f11;
			float d01_13 = f01 * f13 - f03 * f11;
			float d02_13 = f02 * f13 - f03 * f12;

			float d00_11 = f00 * f11 - f01 * f10;
			float d00_12 = f00 * f12 - f02 * f10;
			float d00_13 = f00 * f13 - f03 * f10;

			//3x3 determinants (third row)
			float m20 = f31 * d02_13 - f32 * d01_13 + f33 * d01_12;
			float m21 = f30 * d02_13 - f32 * d00_13 + f33 * d00_12;
			float m22 = f30 * d01_13 - f31 * d00_13 + f33 * d00_11;
			float m23 = f30 * d01_12 - f31 * d00_12 + f32 * d00_11;

			//3x3 determinants (fourth row)
			float m30 = f21 * d02_13 - f22 * d01_13 + f23 * d01_12;
			float m31 = f20 * d02_13 - f22 * d00_13 + f23 * d00_12;
			float m32 = f20 * d01_13 - f21 * d00_13 + f23 * d00_11;
			float m33 = f20 * d01_12 - f21 * d00_12 + f22 * d00_11;

			float determinantR = 1f / determinant;

			return new Float4x4
			(
				m00 * determinantR, -m10 * determinantR, m20 * determinantR, -m30 * determinantR,
				-m01 * determinantR, m11 * determinantR, -m21 * determinantR, m31 * determinantR,
				m02 * determinantR, -m12 * determinantR, m22 * determinantR, -m32 * determinantR,
				-m03 * determinantR, m13 * determinantR, -m23 * determinantR, m33 * determinantR
			);
		}
	}

	public Float4x4 Absoluted => new
	(
		Math.Abs(f00), Math.Abs(f01), Math.Abs(f02), Math.Abs(f03),
		Math.Abs(f10), Math.Abs(f11), Math.Abs(f12), Math.Abs(f13),
		Math.Abs(f20), Math.Abs(f21), Math.Abs(f22), Math.Abs(f23),
		Math.Abs(f30), Math.Abs(f31), Math.Abs(f32), Math.Abs(f33)
	);

	public Float4x4 Transposed => new
	(
		f00, f10, f20, f30,
		f01, f11, f21, f31,
		f02, f12, f22, f32,
		f03, f13, f23, f33
	);

#endregion

#region Static

	/// <summary>
	/// The idempotent <see cref="Float4x4"/> value.
	/// </summary>
	public static Float4x4 Identity => new
	(
		1f, 0f, 0f, 0f,
		0f, 1f, 0f, 0f,
		0f, 0f, 1f, 0f,
		0f, 0f, 0f, 1f
	);

#endregion

#endregion

#region Methods

#region Instance

	public Float4 GetRow(int row)
	{
		unsafe
		{
			if (row < 0 || 3 < row) throw ExceptionHelper.Invalid(nameof(row), row, InvalidType.outOfBounds);
			fixed (Float4x4* pointer = &this) return ((Float4*)pointer)[row];
		}
	}

	public Float4 GetColumn(int column)
	{
		unsafe
		{
			if (column < 0 || 3 < column) throw ExceptionHelper.Invalid(nameof(column), column, InvalidType.outOfBounds);

			fixed (Float4x4* pointer = &this)
			{
				float* p = (float*)pointer;
				return new Float4(p[column], p[column + 4], p[column + 8], p[column + 12]);
			}
		}
	}

	public Float3 MultiplyPoint(Float3 point) => new
	(
		f00 * point.X + f01 * point.Y + f02 * point.Z + f03,
		f10 * point.X + f11 * point.Y + f12 * point.Z + f13,
		f20 * point.X + f21 * point.Y + f22 * point.Z + f23
	);

	public Float3 MultiplyDirection(Float3 direction) => new
	(
		f00 * direction.X + f01 * direction.Y + f02 * direction.Z,
		f10 * direction.X + f11 * direction.Y + f12 * direction.Z,
		f20 * direction.X + f21 * direction.Y + f22 * direction.Z
	);

	public void MultiplyBounds(ref Float3 center, ref Float3 extend)
	{
		center = MultiplyPoint(center);
		extend = Absoluted.MultiplyDirection(extend);
	}

	public override string ToString() => ToString(default);

	public string ToString(string format, IFormatProvider provider = null) =>
		$"{f00.ToString(format, provider)}\t{f01.ToString(format, provider)}\t{f02.ToString(format, provider)}\t{f03.ToString(format, provider)}\n" +
		$"{f10.ToString(format, provider)}\t{f11.ToString(format, provider)}\t{f12.ToString(format, provider)}\t{f13.ToString(format, provider)}\n" +
		$"{f20.ToString(format, provider)}\t{f21.ToString(format, provider)}\t{f22.ToString(format, provider)}\t{f23.ToString(format, provider)}\n" +
		$"{f30.ToString(format, provider)}\t{f31.ToString(format, provider)}\t{f32.ToString(format, provider)}\t{f33.ToString(format, provider)}\n";

	public override bool Equals(object obj) => obj is Float4x4 other && EqualsFast(other);

	public bool Equals(Float4x4 other) => EqualsFast(other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool EqualsFast(Float4x4 other) => f00.AlmostEquals(other.f00) && f01.AlmostEquals(other.f01) && f02.AlmostEquals(other.f02) && f03.AlmostEquals(other.f03) &&
												 f10.AlmostEquals(other.f10) && f11.AlmostEquals(other.f11) && f12.AlmostEquals(other.f12) && f13.AlmostEquals(other.f13) &&
												 f20.AlmostEquals(other.f20) && f21.AlmostEquals(other.f21) && f22.AlmostEquals(other.f22) && f23.AlmostEquals(other.f23) &&
												 f30.AlmostEquals(other.f30) && f31.AlmostEquals(other.f31) && f32.AlmostEquals(other.f32) && f33.AlmostEquals(other.f33);

	public override int GetHashCode()
	{
		unchecked
		{
			int hashCode = f00.GetHashCode();
			hashCode = (hashCode * 397) ^ f01.GetHashCode();
			hashCode = (hashCode * 397) ^ f02.GetHashCode();
			hashCode = (hashCode * 397) ^ f03.GetHashCode();
			hashCode = (hashCode * 397) ^ f10.GetHashCode();
			hashCode = (hashCode * 397) ^ f11.GetHashCode();
			hashCode = (hashCode * 397) ^ f12.GetHashCode();
			hashCode = (hashCode * 397) ^ f13.GetHashCode();
			hashCode = (hashCode * 397) ^ f20.GetHashCode();
			hashCode = (hashCode * 397) ^ f21.GetHashCode();
			hashCode = (hashCode * 397) ^ f22.GetHashCode();
			hashCode = (hashCode * 397) ^ f23.GetHashCode();
			hashCode = (hashCode * 397) ^ f30.GetHashCode();
			hashCode = (hashCode * 397) ^ f31.GetHashCode();
			hashCode = (hashCode * 397) ^ f32.GetHashCode();
			hashCode = (hashCode * 397) ^ f33.GetHashCode();
			return hashCode;
		}
	}

#endregion

#region Static

	/// <summary>
	/// Creates and returns a positional matrix.
	/// </summary>
	public static Float4x4 Position(Float3 position) => new
	(
		1f, 0f, 0f, position.X,
		0f, 1f, 0f, position.Y,
		0f, 0f, 1f, position.Z,
		0f, 0f, 0f, 1f
	);

	/// <summary>
	/// Returns a combined transformation matrix. Scaling is applied first, then rotation, finally translation.
	/// </summary>
	public static Float4x4 Transformation(Float3 position, Float3 rotation, Float3 scale) => Transformation(position, new Versor(rotation), scale);

	/// <summary>
	/// Returns a combined transformation matrix. Scaling is applied first, then rotation, finally translation.
	/// </summary>
	public static Float4x4 Transformation(Float3 position, Versor rotation, Float3 scale) => Position(position) * (Float3x3)rotation * Float3x3.Scale(scale);

#endregion

#region Operators

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Float4x4 operator *(Float4x4 first, Float4x4 second) => new
	(
		first.f00 * second.f00 + first.f01 * second.f10 + first.f02 * second.f20 + first.f03 * second.f30, first.f00 * second.f01 + first.f01 * second.f11 + first.f02 * second.f21 + first.f03 * second.f31, first.f00 * second.f02 + first.f01 * second.f12 + first.f02 * second.f22 + first.f03 * second.f32, first.f00 * second.f03 + first.f01 * second.f13 + first.f02 * second.f23 + first.f03 * second.f33,
		first.f10 * second.f00 + first.f11 * second.f10 + first.f12 * second.f20 + first.f13 * second.f30, first.f10 * second.f01 + first.f11 * second.f11 + first.f12 * second.f21 + first.f13 * second.f31, first.f10 * second.f02 + first.f11 * second.f12 + first.f12 * second.f22 + first.f13 * second.f32, first.f10 * second.f03 + first.f11 * second.f13 + first.f12 * second.f23 + first.f13 * second.f33,
		first.f20 * second.f00 + first.f21 * second.f10 + first.f22 * second.f20 + first.f23 * second.f30, first.f20 * second.f01 + first.f21 * second.f11 + first.f22 * second.f21 + first.f23 * second.f31, first.f20 * second.f02 + first.f21 * second.f12 + first.f22 * second.f22 + first.f23 * second.f32, first.f20 * second.f03 + first.f21 * second.f13 + first.f22 * second.f23 + first.f23 * second.f33,
		first.f30 * second.f00 + first.f31 * second.f10 + first.f32 * second.f20 + first.f33 * second.f30, first.f30 * second.f01 + first.f31 * second.f11 + first.f32 * second.f21 + first.f33 * second.f31, first.f30 * second.f02 + first.f31 * second.f12 + first.f32 * second.f22 + first.f33 * second.f32, first.f30 * second.f03 + first.f31 * second.f13 + first.f32 * second.f23 + first.f33 * second.f33
	);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Float4 operator *(Float4x4 first, Float4 second) => new
	(
		first.f00 * second.X + first.f01 * second.Y + first.f02 * second.Z + first.f03 * second.W,
		first.f10 * second.X + first.f11 * second.Y + first.f12 * second.Z + first.f13 * second.W,
		first.f20 * second.X + first.f21 * second.Y + first.f22 * second.Z + first.f23 * second.W,
		first.f30 * second.X + first.f31 * second.Y + first.f32 * second.Z + first.f33 * second.W
	);

	public static bool operator ==(Float4x4 first, Float4x4 second) => first.EqualsFast(second);
	public static bool operator !=(Float4x4 first, Float4x4 second) => !first.EqualsFast(second);

	public static implicit operator Float4x4(Float3x3 value) => new
	(
		value.f00, value.f01, value.f02, 0f,
		value.f10, value.f11, value.f12, 0f,
		value.f20, value.f21, value.f22, 0f,
		0.000000f, 0.000000f, 0.000000f, 1f
	);

#endregion

#endregion

}