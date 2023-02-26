using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Scenic.Hierarchies;

namespace Echo.Core.Aggregation.Primitives;

/// <summary>
/// Represents a hierarchy of <see cref="EntityToken"/> that globally points to a unique object in a <see cref="PreparedScene"/>
/// </summary>
/// <remarks>A fully and correctly constructed <see cref="TokenHierarchy"/> transcends <see cref="EntityPack"/> and <see cref="PackInstance"/>.</remarks>
public unsafe struct TokenHierarchy : IEquatable<TokenHierarchy>
{
	public TokenHierarchy()
	{
		instancesHash = 0;
		InstanceCount = 0;
		_topToken = EntityToken.Empty;
	}

	/// <summary>
	/// A fixed sized array that actually stores the data of the <see cref="EntityToken"/> in <see cref="Instances"/>.
	/// </summary>
#pragma warning disable CS0649
	fixed byte data[EntityToken.Size * MaxLayer];
#pragma warning restore CS0649

	/// <summary>
	/// A combined hash value of <see cref="Instances"/> for fast value comparison.
	/// </summary>
	uint instancesHash;

	/// <summary>
	/// The maximum number of instanced layers allowed (excluding the root).
	/// Can be increased if needed at a performance and stack memory penalty.
	/// </summary>
	public const int MaxLayer = 5;

	/// <summary>
	/// The number of layers of <see cref="PreparedInstance"/> that needs to be traversed
	/// before reaching the unique object this <see cref="TokenHierarchy"/> points. 
	/// </summary>
	/// <remarks>Equivalent to the <see cref="ReadOnlySpan{T}.Length"/> of <see cref="Instances"/>.</remarks>
	public int InstanceCount { get; private set; }

	EntityToken _topToken;

	/// <summary>
	/// The topmost <see cref="EntityToken"/> that directly points to the target object.
	/// </summary>
	/// <remarks>Localized within a <see cref="PreparedPack"/>.</remarks>
	public EntityToken TopToken
	{
		readonly get => _topToken;
		set
		{
			Ensure.AreNotEqual(value.Type, TokenType.Instance);
			Ensure.AreNotEqual(value.Type, TokenType.Node);
			_topToken = value;
		}
	}

	/// <summary>
	/// The <see cref="EntityToken"/> that represents the topmost <see cref="PreparedInstance"/>, which
	/// is the one that immediately contains <see cref="TopToken"/> in its <see cref="PreparedPack"/>. 
	/// </summary>
	/// <remarks>The behavior of accessing this property is undefined if <see cref="InstanceCount"/> is zero.</remarks>
	public readonly ref readonly EntityToken FinalInstance
	{
		get
		{
			Ensure.IsTrue(InstanceCount > 0);
			return ref this[InstanceCount - 1];
		}
	}

	/// <summary>
	/// An <see cref="ReadOnlySpan{T}"/> containing all the <see cref="EntityToken"/> representing
	/// various <see cref="PreparedInstance"/> layers held in this <see cref="TokenHierarchy"/>.
	/// </summary>
	/// <remarks>These <see cref="PreparedInstance"/> layers must be traversed to get to the
	/// unique global object that this <see cref="TokenHierarchy"/> is pointing at.</remarks>
	public readonly ReadOnlySpan<EntityToken> Instances => MemoryMarshal.CreateReadOnlySpan(ref Origin, InstanceCount);

	/// <summary>
	/// Access <see cref="data"/> as an <see cref="EntityToken"/>. Note that this property
	/// is quite dangerous, because it is readonly but returns a non-readonly reference.
	/// </summary>
	readonly ref EntityToken Origin => ref Unsafe.As<byte, EntityToken>(ref Unsafe.AsRef(in data[0]));

	/// <summary>
	/// Access the <see cref="EntityToken"/> at <paramref name="index"/> in <see cref="data"/>. Note that
	/// this indexer is quite dangerous, because it is readonly but returns a non-readonly reference.
	/// </summary>
	readonly ref EntityToken this[int index] => ref Unsafe.Add(ref Origin, index);

	/// <summary>
	/// Pushes an <see cref="EntityToken"/> that represents an <see cref="PreparedInstance"/> onto this <see cref="TokenHierarchy"/> as a new layer.
	/// </summary>
	/// <remarks>The input <see cref="EntityToken"/> to be pushed must have a <see cref="EntityToken.Type"/>
	/// of <see cref="TokenType.Instance"/>, otherwise the behavior of this method is unknown.</remarks>
	public void Push(EntityToken token)
	{
		Ensure.AreEqual(token.Type, TokenType.Instance);
		Ensure.IsTrue(InstanceCount < MaxLayer);

		int layer = InstanceCount++;
		this[layer] = token;
		instancesHash ^= Hash(token, layer);
	}

	/// <summary>
	/// Pops a <see cref="PreparedInstance"/> (represented by an <see cref="EntityToken"/>) from the top of this <see cref="TokenHierarchy"/>.
	/// </summary>
	/// <returns>The <see cref="EntityToken"/> that was popped.</returns>
	public EntityToken Pop()
	{
		Ensure.IsTrue(InstanceCount > 0);

		int layer = --InstanceCount;

		EntityToken token = this[layer];
		instancesHash ^= Hash(token, layer);
		return token;
	}

	/// <summary>
	/// Hashes an <see cref="EntityToken"/> at <paramref name="layer"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static uint Hash(EntityToken token, int layer)
	{
		uint content = 0;

		Unsafe.As<uint, EntityToken>(ref content) = token;
		Ensure.IsTrue(EntityToken.Size <= sizeof(uint));

		uint hash = SquirrelPrng.Mangle(content);
		return BitOperations.RotateLeft(hash, layer);
	}

	public readonly bool Equals(in TokenHierarchy other) => Equals(this, other);

	public override readonly bool Equals(object obj) => obj is TokenHierarchy other && Equals(this, other);

	public override readonly int GetHashCode()
	{
		unchecked
		{
			int hashCode = TopToken.GetHashCode();
			hashCode = (hashCode * 397) ^ (int)instancesHash;
			hashCode = (hashCode * 397) ^ InstanceCount;
			return hashCode;
		}
	}

	readonly bool IEquatable<TokenHierarchy>.Equals(TokenHierarchy other) => Equals(this, other);

	public static bool operator ==(in TokenHierarchy left, in TokenHierarchy right) => Equals(left, right);
	public static bool operator !=(in TokenHierarchy left, in TokenHierarchy right) => !Equals(left, right);

	static bool Equals(in TokenHierarchy token0, in TokenHierarchy token1)
	{
		if ((token0.TopToken != token1.TopToken) |
			(token0.InstanceCount != token1.InstanceCount) |
			(token0.instancesHash != token1.instancesHash)) return false;

		int count = token0.InstanceCount * EntityToken.Size;

		var span0 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in token0.data[0]), count);
		var span1 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in token1.data[0]), count);

		return span0.SequenceEqual(span1);
	}
}