using System;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Common.Mathematics.Primitives;

public readonly struct Direction : IEquatable<Direction>
{
	internal Direction(byte data)
	{
		this.data = data;
		EnsureValidity();
	}

	Direction(uint data) : this((byte)data) { }

	static Direction()
	{
		crossCache = new Direction[byte.MaxValue + 1];

		foreach (Direction first in All)
		foreach (Direction second in All)
		{
			int index = CrossIndex(first, second);
			Int3 cross = Int3.Cross(first, second);
			crossCache[index] = (Direction)cross;
		}
	}

	/// <summary>
	/// Only five bits of this internal byte data are used; the layout if of the following: 00 0Z SS AA
	/// The AA part indicates the axis of this direction: 0b00 = X, 0b01 = Y, 0b10 = Z, 0b11 = invalid
	/// The SS part indicates the sign of the axis: 0b00 = -1, 0b01 = invalid, 0b10 = 1, 0b11 = invalid.
	/// Z is set to zero if <see cref="IsZero"/>, or one otherwise. Note that the three other bits must always be zero.
	/// </summary>
	internal readonly byte data;

	const byte DataPositiveX = 0b1_10_00;
	const byte DataNegativeX = 0b1_00_00;
	const byte DataPositiveY = 0b1_10_01;
	const byte DataNegativeY = 0b1_00_01;
	const byte DataPositiveZ = 0b1_10_10;
	const byte DataNegativeZ = 0b1_00_10;

	public static readonly Direction right /*    */ = new(DataPositiveX);
	public static readonly Direction left /*     */ = new(DataNegativeX);
	public static readonly Direction up /*       */ = new(DataPositiveY);
	public static readonly Direction down /*     */ = new(DataNegativeY);
	public static readonly Direction forward /*  */ = new(DataPositiveZ);
	public static readonly Direction backward /* */ = new(DataNegativeZ);

	static readonly Direction[] _all = { right, left, up, down, forward, backward };

	static readonly Direction[] crossCache;

	public int X => Axis == 0 ? Sign - 1 : 0;
	public int Y => Axis == 1 ? Sign - 1 : 0;
	public int Z => Axis == 2 ? Sign - 1 : 0;

	/// <summary>
	/// Returns the axis of this <see cref="Direction"/>, with 0 meaning x, 1 meaning y, and 2 meaning z.
	/// NOTE: undefined behavior if <see cref="IsZero"/>.
	/// </summary>
	public int Axis
	{
		get
		{
			EnsureNotZero();
			return data & 0b11;
		}
	}

	/// <summary>
	/// Returns the sign of this <see cref="Direction"/>, either 1 or -1.
	/// NOTE: undefined behavior if <see cref="IsZero"/>.
	/// </summary>
	public int Sign
	{
		get
		{
			EnsureNotZero();
			return (data >> 2) & 0b11;
		}
	}

	/// <summary>
	/// Returns whether this <see cref="Direction"/> is zero. A zero value can only be created using
	/// the default constructor or through the result of an invalid operation (such as <see cref="Cross"/>).
	/// NOTE: most operations will not accept <see cref="Direction"/> with <see cref="IsZero"/> marked as true as arguments.
	/// </summary>
	public bool IsZero => data == default;

	/// <summary>
	/// Returns whether this <see cref="Direction"/> is negative.
	/// NOTE: undefined behavior if <see cref="IsZero"/>.
	/// </summary>
	public bool IsNegative
	{
		get
		{
			EnsureNotZero();
			return Sign == 0;
		}
	}

	/// <summary>
	/// Returns the index of this <see cref="Direction"/> in <see cref="All"/>.
	/// NOTE: undefined behavior if <see cref="IsZero"/>.
	/// </summary>
	public int Index
	{
		get
		{
			EnsureNotZero();

			//TODO: we can optimize this by caching it into the three bits that we are not using
			return ((data & 0b11) << 1) + (((data >> 3) ^ 0b1) & 0b1);
		}
	}

	/// <summary>
	/// If this <see cref="Direction"/> has a Y component, make it the Z component.
	/// Or similarly if it has a Z component, make it the Y component.
	/// NOTE: undefined behavior if <see cref="IsZero"/>.
	/// </summary>
	public Direction FlipYZ
	{
		get
		{
			EnsureNotZero();

			//Nothing happens if this is the X axis
			if (Axis == 0) return this;

			//Flips Y and Z axes
			return new Direction(data ^ 0b11u);
		}
	}

	/// <summary>
	/// Returns this <see cref="Direction"/> with negatives turned into positives.
	/// NOTE: undefined behavior if <see cref="IsZero"/>.
	/// </summary>
	public Direction Absoluted
	{
		get
		{
			EnsureNotZero();

			//Sets the sign bit to positive
			return new Direction(data | 0b1000u);
		}
	}

	/// <summary>
	/// Returns a <see cref="Direction"/> that is perpendicular to this <see cref="Direction"/>.
	/// The sign stays the same, and the axis decreases from Z to Y or Y to X or X to Z.
	/// NOTE: undefined behavior if <see cref="IsZero"/>.
	/// </summary>
	public Direction Perpendicular
	{
		get
		{
			bool wrap = Axis == 0;
			int value = data + (wrap ? 2 : -1);
			return new Direction((uint)value);
		}
	}

	/// <summary>
	/// Accesses all the possible <see cref="Direction"/> values.
	/// </summary>
	public static ReadOnlySpan<Direction> All => _all;

	/// <summary>
	/// Calculates and returns the cross product (vector product) of this <see cref="Direction"/> and <paramref name="other"/>.
	/// NOTE: undefined behavior if <see cref="IsZero"/> for either this or <paramref name="other"/>.
	/// </summary>
	public Direction Cross(Direction other)
	{
		EnsureNotZero();
		other.EnsureNotZero();
		return crossCache[CrossIndex(this, other)];
	}

	/// <summary>
	/// Projects <paramref name="value"/> onto the plane located at the origin and has this <see cref="Direction"/> as its normal.
	/// NOTE: this returns a <see cref="Float2"/>, since it project as an orthographic camera looking down at <see cref="value"/>,
	/// where the plane normal is pointing towards the camera.
	/// NOTE: undefined behavior if <see cref="IsZero"/>.
	/// </summary>
	public Float2 Project(in Float3 value)
	{
		EnsureNotZero();

		return data switch
		{
			DataPositiveX => value.ZY,
			DataNegativeX => new Float2(-value.Z, value.Y),
			DataPositiveY => value.XZ,
			DataNegativeY => new Float2(-value.X, value.Z),
			DataPositiveZ => value.YX,
			DataNegativeZ => new Float2(-value.Y, value.X),
			_             => throw ExceptionHelper.NotPossible
		};
	}

	/// <inheritdoc cref="Float3"/>
	public Int2 Project(in Int3 value)
	{
		EnsureNotZero();

		return data switch
		{
			DataPositiveX => value.ZY,
			DataNegativeX => new Int2(-value.Z, value.Y),
			DataPositiveY => value.XZ,
			DataNegativeY => new Int2(-value.X, value.Z),
			DataPositiveZ => value.YX,
			DataNegativeZ => new Int2(-value.Y, value.X),
			_             => throw ExceptionHelper.NotPossible
		};
	}

	/// <summary>
	/// Multiplies this <see cref="Direction"/> with <paramref name="value"/> as a scalar result.
	/// NOTE: undefined behavior if <see cref="IsZero"/>.
	/// </summary>
	public float ExtractComponent(in Float3 value)
	{
		int index = Axis;
		float part = value[index];
		return IsNegative ? -part : part;
	}

	/// <inheritdoc cref="Float3"/>
	public int ExtractComponent(in Int3 value)
	{
		int index = Axis;
		int part = value[index];
		return IsNegative ? -part : part;
	}

	public bool Equals(Direction other) => data == other.data;

	public override bool Equals(object obj) => obj is Direction other && Equals(other);

	public override int GetHashCode() => data.GetHashCode();

	public override string ToString() => ToString(false);

	public string ToString(bool useNames)
	{
		if (useNames)
		{
			switch (data)
			{
				case DataPositiveX: return "right";
				case DataNegativeX: return "left";
				case DataPositiveY: return "up";
				case DataNegativeY: return "down";
				case DataPositiveZ: return "forward";
				case DataNegativeZ: return "backward";
			}
		}
		else
		{
			switch (data)
			{
				case DataPositiveX: return "x";
				case DataNegativeX: return "-x";
				case DataPositiveY: return "y";
				case DataNegativeY: return "-y";
				case DataPositiveZ: return "z";
				case DataNegativeZ: return "-z";
			}
		}

		Ensure.IsTrue(IsZero);
		return "zero";
	}

	void EnsureValidity()
	{
		EnsureNotZero();
		Ensure.AreEqual(data >> 4, 1);

		Ensure.AreNotEqual(data & 0b11, 0b11);
		Ensure.AreNotEqual((data >> 2) & 0b11, 0b01);
		Ensure.AreNotEqual((data >> 2) & 0b11, 0b11);
	}

	void EnsureNotZero() => Ensure.IsFalse(IsZero);

	public static Direction operator +(Direction direction)
	{
		direction.EnsureNotZero();
		return direction;
	}

	public static Direction operator -(Direction direction)
	{
		direction.EnsureNotZero();

		//Toggle the sign bit
		return new Direction(direction.data ^ 0b1000u);
	}

	public static Float3 operator *(Direction direction, in Float3 value)
	{
		int index = direction.Axis;
		float part = value[index];
		if (direction.IsNegative) part = -part;
		return Float3.Create(index, part);
	}

	public static Int3 operator *(Direction direction, in Int3 value)
	{
		int index = direction.Axis;
		int part = value[index];
		if (direction.IsNegative) part = -part;
		return Int3.Create(index, part);
	}

	public static explicit operator Direction(in Float3 value)
	{
		if (value.SquaredMagnitude.AlmostEquals()) return default;

		int axis = value.Absoluted.MaxIndex;
		int sign = value[axis] < 0f ? 0 : 1;

		return new Direction((uint)axis | (uint)(sign << 3) | 0b10000);
	}

	public static explicit operator Direction(in Float2 value) => (Direction)value.XY_;

	public static explicit operator Direction(in Int3 value) => (Direction)(Float3)value;
	public static explicit operator Direction(in Int2 value) => (Direction)(Float2)value;

	public static implicit operator Int3(Direction direction)
	{
		int index = direction.Axis;
		int value = direction.Sign - 1;

		return Int3.Create(index, value);
	}

	public static implicit operator Int2(Direction direction)
	{
		Int3 full = direction;
		if (full.Z == 0) return full.XY;
		throw new InvalidCastException();
	}

	public static implicit operator Float3(Direction direction) => (Int3)direction;
	public static implicit operator Float2(Direction direction) => (Int2)direction;

	public static bool operator ==(Direction first, Direction second) => first.Equals(second);
	public static bool operator !=(Direction first, Direction second) => !first.Equals(second);

	static int CrossIndex(Direction first, Direction second)
	{
		int data0 = first.data & 0b1111;
		int data1 = second.data & 0b1111;

		return (data0 << 4) | data1;
	}
}