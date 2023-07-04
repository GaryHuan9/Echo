using System;
using System.Runtime.CompilerServices;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Scenic.Lights;

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
		Ensure.IsTrue(index < IndexCount);
		Ensure.AreEqual(Type, type);
		Ensure.IsTrue(type == TokenType.Light || Index == index);
	}

	public EntityToken(LightType type, int index) : this(TokenType.Light, ((int)type << LightIndexBitLength) | index)
	{
		Ensure.IsTrue(index < LightIndexCount);
		Ensure.AreEqual(Type, TokenType.Light);
		Ensure.AreEqual(LightType, type);
		Ensure.AreEqual(LightIndex, index);
	}

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
			Ensure.AreNotEqual(data, EmptyTokenValue);
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
			Ensure.AreNotEqual(data, EmptyTokenValue);
			Ensure.AreEqual(Type, TokenType.Light);

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
			Ensure.AreNotEqual(data, EmptyTokenValue);
			Ensure.AreNotEqual(Type, TokenType.Light);
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
			Ensure.AreNotEqual(data, EmptyTokenValue);
			Ensure.AreEqual(Type, TokenType.Light);
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
	/// <summary>
	/// Returns whether a <see cref="TokenType"/> is of type <see cref="TokenType.Triangle"/>, <see cref="TokenType.Instance"/>, or <see cref="TokenType.Sphere"/>.
	/// </summary>
	public static bool IsGeometry(this TokenType type)
	{
		var set = new BitSet64<TokenType>();
		set = set.Union(TokenType.Triangle);
		set = set.Union(TokenType.Sphere);
		set = set.Union(TokenType.Instance);
		return set.HasIntersection(type);
	}

	/// <summary>
	/// Returns whether a <see cref="TokenType"/> is of type <see cref="TokenType.Triangle"/> or <see cref="TokenType.Sphere"/>.
	/// </summary>
	public static bool IsRawGeometry(this TokenType type)
	{
		var set = new BitSet64<TokenType>();
		set = set.Union(TokenType.Triangle);
		set = set.Union(TokenType.Sphere);
		return set.HasIntersection(type);
	}

	/// <summary>
	/// Returns whether an <see cref="EntityToken"/> can represent an area light.
	/// </summary>
	/// <remarks>This includes <see cref="EntityToken"/> where <see cref="IsRawGeometry"/> equals true.</remarks>
	public static bool IsAreaLight(this EntityToken token)
	{
		if (token.Type.IsRawGeometry()) return true;
		if (token.Type != TokenType.Light) return false;
		return token.LightType == LightType.Infinite;
	}

	/// <summary>
	/// Returns whether an <see cref="EntityToken"/> is representing an <see cref="InfiniteLight"/>.
	/// </summary>
	public static bool IsInfiniteLight(this EntityToken token)
	{
		if (token.Type != TokenType.Light) return false;

		var set = new BitSet64<LightType>();
		set = set.Union(LightType.Infinite);
		set = set.Union(LightType.InfiniteDelta);
		return set.HasIntersection(token.LightType);
	}

	/// <summary>
	/// An elaborate implementation to achieve constant time enum matching regardless of number of operators.
	/// </summary>
	/// <remarks>Under optimization, the entire set should merge into one constant that is prompted compared.</remarks>
	readonly struct BitSet64<T> where T : unmanaged, Enum
	{
		public BitSet64() : this(0) { }

		BitSet64(ulong data) => this.data = data;

		readonly ulong data;

		public BitSet64<T> Union(BitSet64<T> value) => new(value.data | data);

		public bool HasIntersection(BitSet64<T> value) => (value.data & data) != 0;

		public static implicit operator BitSet64<T>(T value)
		{
			int converted = Unsafe.As<T, int>(ref value);
			Ensure.IsTrue(converted is >= 0 and < 64);
			return new BitSet64<T>(1ul << converted);
		}
	}
}