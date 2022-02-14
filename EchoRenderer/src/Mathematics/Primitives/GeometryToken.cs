using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Randomization;
using EchoRenderer.Scenic.Instancing;
using EchoRenderer.Scenic.Preparation;

namespace EchoRenderer.Mathematics.Primitives;

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
	/// The number of instances stored in <see cref="instances"/>.
	/// </summary>
	public int InstanceCount { get; private set; }

	/// <summary>
	/// The hash value of <see cref="instances"/>.
	/// </summary>
	uint instancesHash;

	/// <summary>
	/// A stack that holds the different <see cref="PreparedInstance.id"/> branches to reach this <see cref="Geometry"/>.
	/// </summary>
#pragma warning disable CS0649
	fixed uint instances[EntityPack.MaxLayer];
#pragma warning restore CS0649

	/// <summary>
	/// Returns the <see cref="PreparedInstance.id"/> of the topmost <see cref="PreparedInstance"/>,
	/// which is the one that immediately contains <see cref="Geometry"/> in its pack.
	/// </summary>
	public readonly uint FinalInstanceId
	{
		get
		{
			Assert.IsTrue(InstanceCount > 0);
			return instances[InstanceCount - 1];
		}
	}

	/// <summary>
	/// Returns the <see cref="PreparedInstance.id"/> of the <see cref="PreparedInstance"/> branches held in this <see cref="NodeToken"/>.
	/// </summary>
	public readonly ReadOnlySpan<uint> Instances => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in instances[0]), InstanceCount);

	/// <summary>
	/// Pushes a new <paramref name="instance"/> onto this <see cref="GeometryToken"/> as a new layer.
	/// </summary>
	public void Push(PreparedInstance instance)
	{
		Assert.IsTrue(InstanceCount < EntityPack.MaxLayer);

		int layer = InstanceCount++;
		instances[layer] = instance.id;
		instancesHash ^= Hash(instance.id, layer);
	}

	/// <summary>
	/// Pops a <see cref="PreparedInstance"/> from the top of this <see cref="GeometryToken"/>.
	/// </summary>
	public void Pop()
	{
		Assert.IsTrue(InstanceCount > 0);

		int layer = --InstanceCount;
		uint id = instances[layer];
		instancesHash ^= Hash(id, layer);
	}

	/// <summary>
	/// Hashes the <paramref name="id"/> of a <see cref="PreparedInstance"/> at <paramref name="layer"/>.
	/// </summary>
	static uint Hash(uint id, int layer)
	{
		uint hash = SquirrelRandom.Mangle(id);
		return BitOperations.RotateLeft(hash, layer);
	}

	public readonly bool Equals(in GeometryToken other) =>
		Geometry == other.Geometry && InstanceCount == other.InstanceCount &&
		instancesHash == other.instancesHash && Instances.SequenceEqual(other.Instances);

	public override readonly bool Equals(object obj) => obj is GeometryToken other && Equals(other);

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

	readonly bool IEquatable<GeometryToken>.Equals(GeometryToken other) => Equals(other);

	public static bool operator ==(in GeometryToken left, in GeometryToken right) => left.Equals(right);
	public static bool operator !=(in GeometryToken left, in GeometryToken right) => !left.Equals(right);
}