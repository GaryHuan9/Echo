using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeHelpers.Diagnostics;
using Echo.Common.Mathematics.Randomization;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Scenic.Instancing;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Aggregation.Primitives;

/// <summary>
/// Represents a unique geometry in the <see cref="PreparedScene"/>. Note that this
/// uniqueness transcends <see cref="EntityPack"/> and <see cref="PackInstance"/>.
/// </summary>
public unsafe struct GeometryToken : IEquatable<GeometryToken>
{
	NodeToken _geometry;

	/// <summary>
	/// The unique token for one geometry inside a <see cref="PreparedPack"/>.
	/// </summary>
	public NodeToken Geometry
	{
		readonly get => _geometry;
		set
		{
			Assert.IsTrue(value.IsGeometry);
			_geometry = value;
		}
	}

	/// <summary>
	/// The number of layers of <see cref="PreparedInstance"/> that needs to be traversed before reaching the unique geometry this
	/// <see cref="GeometryToken"/> represents. Equivalent to the <see cref="ReadOnlySpan{T}.Length"/> of <see cref="Instances"/>.
	/// </summary>
	public int InstanceCount { get; private set; }

	/// <summary>
	/// A combined hash value of <see cref="Instances"/>.
	/// </summary>
	uint instancesHash;

	/// <summary>
	/// A fixed sized array that actually stores the data of the <see cref="NodeToken"/> in <see cref="Instances"/>.
	/// </summary>
#pragma warning disable CS0649
	fixed byte data[NodeToken.Size * EntityPack.MaxLayer];
#pragma warning restore CS0649

	/// <summary>
	/// Returns the <see cref="NodeToken"/> that represents the topmost <see cref="PreparedInstance"/>, which is the one that immediately contains
	/// <see cref="Geometry"/> in its <see cref="PreparedPack"/>. Accessing this property is undefined if <see cref="InstanceCount"/> is zero.
	/// </summary>
	public readonly ref readonly NodeToken FinalInstance
	{
		get
		{
			Assert.IsTrue(InstanceCount > 0);
			return ref this[InstanceCount - 1];
		}
	}

	/// <summary>
	/// Returns the <see cref="NodeToken"/> of the <see cref="PreparedInstance"/> branches held in this <see cref="GeometryToken"/>.
	/// These branches are needed to be traversed to get to this unique geometry that this <see cref="GeometryToken"/> is representing.
	/// </summary>
	public readonly ReadOnlySpan<NodeToken> Instances => MemoryMarshal.CreateReadOnlySpan(ref Origin, InstanceCount);

	/// <summary>
	/// Access <see cref="data"/> as <see cref="NodeToken"/>. Note that this property
	/// is quite dangerous, because it is readonly but returns a non-readonly reference.
	/// </summary>
	readonly ref NodeToken Origin => ref Unsafe.As<byte, NodeToken>(ref Unsafe.AsRef(in data[0]));

	/// <summary>
	/// Access the <see cref="NodeToken"/> at <paramref name="index"/> in <see cref="data"/>. Note that
	/// this indexer is quite dangerous, because it is readonly but returns a non-readonly reference.
	/// </summary>
	readonly ref NodeToken this[int index] => ref Unsafe.Add(ref Origin, index);

	/// <summary>
	/// Pushes a new <paramref name="instance"/> onto this <see cref="GeometryToken"/> as a new layer.
	/// </summary>
	public void Push(PreparedInstance instance)
	{
		Assert.IsTrue(InstanceCount < EntityPack.MaxLayer);

		int layer = InstanceCount++;
		this[layer] = instance.token;
		instancesHash ^= Hash(instance.token, layer);
	}

	/// <summary>
	/// Pops a <see cref="PreparedInstance"/> from the top of this <see cref="GeometryToken"/>.
	/// </summary>
	public void Pop()
	{
		Assert.IsTrue(InstanceCount > 0);

		int layer = --InstanceCount;

		ref readonly var token = ref this[layer];
		instancesHash ^= Hash(token, layer);
	}

	/// <summary>
	/// Hashes a <see cref="NodeToken"/> at <paramref name="layer"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static uint Hash(in NodeToken token, int layer)
	{
		uint content = 0;

		Unsafe.As<uint, NodeToken>(ref content) = token;
		Assert.IsTrue(NodeToken.Size <= sizeof(uint));

		uint hash = SquirrelPrng.Mangle(content);
		return BitOperations.RotateLeft(hash, layer);
	}

	public readonly bool Equals(in GeometryToken other) => Equals(this, other);

	public override readonly bool Equals(object obj) => obj is GeometryToken other && Equals(this, other);

	public override readonly int GetHashCode()
	{
		unchecked
		{
			int hashCode = Geometry.GetHashCode();
			hashCode = (hashCode * 397) ^ (int)instancesHash;
			hashCode = (hashCode * 397) ^ InstanceCount;
			return hashCode;
		}
	}

	readonly bool IEquatable<GeometryToken>.Equals(GeometryToken other) => Equals(this, other);

	public static bool operator ==(in GeometryToken left, in GeometryToken right) => Equals(left, right);
	public static bool operator !=(in GeometryToken left, in GeometryToken right) => !Equals(left, right);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool Equals(in GeometryToken token0, in GeometryToken token1)
	{
		if ((token0.Geometry != token1.Geometry) |
			(token0.InstanceCount != token1.InstanceCount) |
			(token0.instancesHash != token1.instancesHash)) return false;

		int count = token0.InstanceCount * NodeToken.Size;

		var span0 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in token0.data[0]), count);
		var span1 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in token1.data[0]), count);

		return span0.SequenceEqual(span1);
	}
}