using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using Echo.Core.Aggregation.Preparation;

namespace Echo.Core.Aggregation.Primitives;

/// <summary>
/// Represents a normal object in a <see cref="PreparedPack"/>. The actual content of that object can be found through the <see cref="Index"/> property. 
/// </summary>
/// <remarks> An <see cref="EntityToken"/> is local within a single <see cref="PreparedPack"/>. The uniqueness and meaning
/// of an <see cref="EntityToken"/>'s properties are not guaranteed once outside of that <see cref="PreparedPack"/>.</remarks>
public readonly struct EntityToken : IEquatable<EntityToken>
{
	/// <summary>
	/// Constructs a new <see cref="EntityToken"/>.
	/// </summary>
	/// <param name="type">The <see cref="TokenType"/> of this new <see cref="EntityToken"/>.</param>
	/// <param name="index">The <see cref="Index"/> of the new <see cref="EntityToken"/>,
	/// must be between 0 (inclusive) and <see cref="IndexCount"/> (exclusive).</param>
	public EntityToken(TokenType type, int index) : this(((uint)type << IndexBitLength) | (uint)index)
	{
		Assert.IsTrue(index < IndexCount);
		Assert.AreEqual(Type, type);
		Assert.AreEqual(Index, index);
	}

	public EntityToken(LightType type, int index) : this(TokenType.Light, ((int)type << LightIndexBitLength) | index)
	{
		Assert.IsTrue(index < LightIndexCount);
		Assert.AreEqual(Type, TokenType.Light);
		Assert.AreEqual(LightType, type);
		Assert.AreEqual(LightIndex, index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	EntityToken(uint data) => this.data = data;

	readonly uint data;

	/// <summary>
	/// The total number of <see cref="Index"/> an <see cref="EntityToken"/> can have.
	/// </summary>
	/// <remarks>Internally inside <see cref="EntityToken"/>, 4 bits are used to store the <see cref="TokenType"/>,
	/// while the rest 28 bits are used to store the <see cref="Index"/>. Thus, there is a limit to the total number
	/// of objects of one type inside a <see cref="PreparedPack"/>, and the construction of a <see cref="PreparedPack"/>
	/// should not exceed this limit.</remarks>
	public const uint IndexCount = IndexLength - 1;

	/// <summary>
	/// The total number of <see cref="LightIndex"/> an <see cref="EntityToken"/> can have.
	/// </summary>
	/// <remarks>Similarly to <see cref="IndexCount"/> this count is limited by the number of bits occupied by the type.
	/// If an <see cref="EntityToken"/> is of type <see cref="TokenType.Light"/>, then 4 bits of its 32-bit data stores
	/// the <see cref="TokenType"/>, and 6 more bits are used to store its <see cref="LightType"/>. The remaining 22 bits
	/// determines the value of this constant.</remarks>
	public const uint LightIndexCount = LightIndexLength - 1;

	/// <summary>
	/// The number of bytes an <see cref="EntityToken"/> occupies in memory.
	/// </summary>
	public const int Size = sizeof(uint);

	const int TotalBitLength = Size * 8;
	const int IndexBitLength = TotalBitLength - 4;
	const uint IndexLength = 1 << IndexBitLength;

	const int LightTypeBitLength = 6;
	const int LightIndexBitLength = IndexBitLength - LightTypeBitLength;
	const uint LightTypeMask = (1 << LightTypeBitLength) - 1;
	const uint LightIndexLength = 1 << LightIndexBitLength;

	const uint EmptyTokenValue = uint.MaxValue;

	/// <summary>
	/// The <see cref="TokenType"/> of this <see cref="EntityToken"/>.
	/// </summary>
	/// <remarks>The behavior of this property is undefined if <see cref="IsEmpty"/> is true.</remarks>
	public TokenType Type
	{
		get
		{
			Assert.AreNotEqual(data, EmptyTokenValue);
			return (TokenType)(data >> IndexBitLength);
		}
	}

	/// <summary>
	/// The <see cref="LightType"/> of this <see cref="EntityToken"/> when it is a <see cref="TokenType.Light"/>.
	/// </summary>
	/// <remarks>The behavior of this property is undefined if <see cref="IsEmpty"/>
	/// is true or if <see cref="Type"/> is not <see cref="TokenType.Light"/>.</remarks>
	public LightType LightType
	{
		get
		{
			Assert.AreNotEqual(data, EmptyTokenValue);
			Assert.AreEqual(Type, TokenType.Light);

			return (LightType)((data >> LightIndexBitLength) & LightTypeMask);
		}
	}

	/// <summary>
	/// The actual content of this <see cref="EntityToken"/>. This is an index
	/// that is between 0 (inclusive) and <see cref="IndexCount"/> (exclusive).
	/// </summary>
	/// <remarks>The behavior of this property is undefined if <see cref="IsEmpty"/>
	/// is true or if <see cref="Type"/> equals <see cref="TokenType.Light"/>.</remarks>
	public int Index
	{
		get
		{
			Assert.AreNotEqual(data, EmptyTokenValue);
			Assert.AreNotEqual(Type, TokenType.Light);
			return (int)(data & (IndexLength - 1));
		}
	}

	/// <summary>
	/// The actual content of this <see cref="EntityToken"/> for a <see cref="TokenType.Light"/>.
	/// This is an index that is between 0 (inclusive) and <see cref="LightIndexCount"/> (exclusive).
	/// </summary>
	/// <remarks>The behavior of this property is undefined if <see cref="IsEmpty"/>
	/// is true or if <see cref="Type"/> is not <see cref="TokenType.Light"/>.</remarks>
	public int LightIndex
	{
		get
		{
			Assert.AreNotEqual(data, EmptyTokenValue);
			Assert.AreEqual(Type, TokenType.Light);
			return (int)(data & (LightIndexLength - 1));
		}
	}

	/// <summary>
	/// Whether this <see cref="EntityToken"/> is an <see cref="Empty"/>.
	/// </summary>
	public bool IsEmpty => data == EmptyTokenValue;

	/// <summary>
	/// An <see cref="EntityToken"/> that is not defined to be any particular <see cref="TokenType"/>.
	/// </summary>
	public static EntityToken Empty => new(EmptyTokenValue);

	public bool Equals(EntityToken other) => Equals(this, other);

	public override bool Equals(object obj) => obj is EntityToken other && Equals(this, other);
	public override int GetHashCode() => (int)data;

	public static bool operator ==(EntityToken left, EntityToken right) => Equals(left, right);
	public static bool operator !=(EntityToken left, EntityToken right) => !Equals(left, right);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool Equals(EntityToken token0, EntityToken token1) => token0.data == token1.data;
}

public static class EntityTokenExtensions
{
	const byte Invalid = byte.MaxValue;

	/// <summary>
	/// Returns whether a <see cref="TokenType"/> is of type <see cref="TokenType.Triangle"/>, <see cref="TokenType.Instance"/>, or <see cref="TokenType.Sphere"/>.
	/// </summary>
	public static bool IsGeometry(this TokenType type) => AreEqual(type, TokenType.Triangle, TokenType.Sphere, TokenType.Instance);

	/// <summary>
	/// Returns whether a <see cref="TokenType"/> is of type <see cref="TokenType.Triangle"/> or <see cref="TokenType.Sphere"/>.
	/// </summary>
	public static bool IsRawGeometry(this TokenType type) => AreEqual(type, TokenType.Triangle, TokenType.Sphere);

	/// <summary>
	/// Returns whether an <see cref="EntityToken"/> can represent an area light.
	/// </summary>
	/// <remarks>This includes <see cref="EntityToken"/> where <see cref="IsRawGeometry"/> equals true.</remarks>
	public static bool IsAreaLight(this EntityToken token)
	{
		if (token.Type.IsRawGeometry()) return true;
		if (token.Type != TokenType.Light) return false;

		return AreEqual(token.LightType, LightType.Infinite);
	}

	//Automatically inlined by compiler
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool AreEqual(TokenType value,
						 TokenType other00 = (TokenType)Invalid, TokenType other01 = (TokenType)Invalid, TokenType other02 = (TokenType)Invalid, TokenType other03 = (TokenType)Invalid,
						 TokenType other04 = (TokenType)Invalid, TokenType other05 = (TokenType)Invalid, TokenType other06 = (TokenType)Invalid, TokenType other07 = (TokenType)Invalid,
						 TokenType other08 = (TokenType)Invalid, TokenType other09 = (TokenType)Invalid, TokenType other10 = (TokenType)Invalid, TokenType other11 = (TokenType)Invalid,
						 TokenType other12 = (TokenType)Invalid, TokenType other13 = (TokenType)Invalid, TokenType other14 = (TokenType)Invalid, TokenType other15 = (TokenType)Invalid)
	{
		uint constant = Once(other00) | Once(other01) | Once(other02) | Once(other03) | Once(other04) | Once(other05) | Once(other06) | Once(other07) |
						Once(other08) | Once(other09) | Once(other10) | Once(other11) | Once(other12) | Once(other13) | Once(other14) | Once(other15);

		return ((1 << (int)value) & constant) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static uint Once(TokenType type) => type == (TokenType)Invalid ? 0 : 1u << (int)type;
	}

	//Automatically inlined by compiler
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool AreEqual(LightType value,
						 LightType other00 = (LightType)Invalid, LightType other01 = (LightType)Invalid, LightType other02 = (LightType)Invalid, LightType other03 = (LightType)Invalid,
						 LightType other04 = (LightType)Invalid, LightType other05 = (LightType)Invalid, LightType other06 = (LightType)Invalid, LightType other07 = (LightType)Invalid,
						 LightType other08 = (LightType)Invalid, LightType other09 = (LightType)Invalid, LightType other10 = (LightType)Invalid, LightType other11 = (LightType)Invalid,
						 LightType other12 = (LightType)Invalid, LightType other13 = (LightType)Invalid, LightType other14 = (LightType)Invalid, LightType other15 = (LightType)Invalid)
	{
		ulong constant = Once(other00) | Once(other01) | Once(other02) | Once(other03) | Once(other04) | Once(other05) | Once(other06) | Once(other07) |
						 Once(other08) | Once(other09) | Once(other10) | Once(other11) | Once(other12) | Once(other13) | Once(other14) | Once(other15);

		return ((1ul << (int)value) & constant) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static ulong Once(LightType type) => type == (LightType)Invalid ? 0 : 1ul << (int)type;
	}
}