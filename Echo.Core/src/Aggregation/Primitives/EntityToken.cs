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

	public const uint LightIndexCount = LightIndexLength - 1;

	/// <summary>
	/// The number of bytes an <see cref="EntityToken"/> occupies in memory.
	/// </summary>
	public const int Size = sizeof(uint);

	const int TotalBitLength = Size * 8;
	const int IndexBitLength = TotalBitLength - 4;
	const uint IndexLength = 1 << IndexBitLength;

	const int LightIndexBitLength = IndexBitLength - 8;
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

			//We do not need to mask out the regular TokenType here because the size of
			//LightType is a byte, so internally the language will apply the mask for us

			return (LightType)(data >> LightIndexBitLength);
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