using System;
using CodeHelpers.Diagnostics;
using EchoRenderer.Core.Aggregation.Preparation;
using EchoRenderer.Core.Scenic.Geometries;

namespace EchoRenderer.Core.Aggregation.Primitives;

/// <summary>
/// Represents either a geometry (including <see cref="PreparedInstance"/>) or a node, localized inside an <see cref="Aggregator"/>.
/// NOTE: this <see cref="NodeToken"/> is local within a single <see cref="Aggregator"/> and is meaningless once taken out of the constrain.
/// </summary>
public readonly struct NodeToken : IEquatable<NodeToken>
{
	NodeToken(uint data) => this.data = data;

	readonly uint data;

	/// <summary>
	/// Returns whether this <see cref="NodeToken"/> is an <see cref="Aggregator"/> node.
	/// </summary>
	public bool IsNode => data >= NodeThreshold;

	/// <summary>
	/// Returns whether this <see cref="NodeToken"/> represents a geometric object at a leaf node.
	/// </summary>
	public bool IsGeometry => data < NodeThreshold;

	/// <summary>
	/// Returns whether this <see cref="NodeToken"/> represents an empty (null) node inside an <see cref="Aggregator"/>.
	/// Note that if <see cref="IsGeometry"/>, which can never be empty, so the result of this property is always false.
	/// </summary>
	public bool IsEmpty => data == EmptyNode;

	/// <summary>
	/// Returns whether this <see cref="NodeToken"/> represents a <see cref="PreparedTriangle"/>.
	/// NOTE: <see cref="IsGeometry"/> must be true or the result of this property is undefined.
	/// </summary>
	public bool IsTriangle
	{
		get
		{
			Assert.IsTrue(IsGeometry);
			return data >= TriangleThreshold;
		}
	}

	/// <summary>
	/// Returns whether this <see cref="NodeToken"/> represents a <see cref="PreparedSphere"/>.
	/// NOTE: <see cref="IsTriangle"/> must be false or the result of this property is undefined.
	/// </summary>
	public bool IsSphere
	{
		get
		{
			Assert.IsTrue(IsGeometry);
			Assert.IsFalse(IsTriangle);
			return data >= SphereThreshold;
		}
	}

	/// <summary>
	/// Returns whether this <see cref="NodeToken"/> represents a <see cref="PreparedInstance"/>.
	/// NOTE: <see cref="IsSphere"/> must be false or the result of this property is undefined.
	/// </summary>
	public bool IsInstance
	{
		get
		{
			Assert.IsTrue(IsGeometry);
			Assert.IsFalse(IsTriangle);
			Assert.IsFalse(IsSphere);

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			return data >= InstanceThreshold;
		}
	}

	/// <summary>
	/// If <see cref="IsNode"/>, returns the node value that this <see cref="NodeToken"/> contains, otherwise the result is undefined.
	/// </summary>
	public uint NodeValue
	{
		get
		{
			Assert.IsTrue(IsNode);
			return data - NodeThreshold;
		}
	}

	/// <summary>
	/// If <see cref="IsTriangle"/>, returns the value that this <see cref="NodeToken"/> contains, otherwise the result is undefined.
	/// </summary>
	public uint TriangleValue
	{
		get
		{
			Assert.IsTrue(IsTriangle);
			return data - TriangleThreshold;
		}
	}

	/// <summary>
	/// If <see cref="IsSphere"/>, returns the value that this <see cref="NodeToken"/> contains, otherwise the result is undefined.
	/// </summary>
	public uint SphereValue
	{
		get
		{
			Assert.IsTrue(IsSphere);
			return data - SphereThreshold;
		}
	}

	/// <summary>
	/// If <see cref="IsInstance"/>, returns the value that this <see cref="NodeToken"/> contains, otherwise the result is undefined.
	/// </summary>
	public uint InstanceValue
	{
		get
		{
			Assert.IsTrue(IsInstance);
			return data - InstanceThreshold;
		}
	}

	/// <summary>
	/// If <see cref="IsNode"/>, returns a <see cref="NodeToken"/> that represents the node
	/// that is immediate next to this node, otherwise the result returns is undefined.
	/// NOTE: This is identical to invoking <see cref="CreateNode"/> with <see cref="NodeValue"/> plus one.
	/// </summary>
	public NodeToken Next
	{
		get
		{
			Assert.IsTrue(IsNode);
			Assert.IsTrue(NodeValue + 1 < NodeThreshold);
			return new NodeToken(data + 1);
		}
	}

	/// <summary>
	/// The <see cref="NodeToken"/> that represents the root node in an <see cref="Aggregator"/>,
	/// which contains an internal <see cref="data"/> of zero.
	/// </summary>
	public static NodeToken Root => CreateNode(0);

	/// <summary>
	/// A <see cref="NodeToken"/> that <see cref="IsEmpty"/> (an <see cref="Aggregator"/> node that is null).
	/// </summary>
	public static NodeToken Empty => new(EmptyNode);

	/// <summary>
	/// The number of bytes a <see cref="NodeToken"/> occupies in memory.
	/// </summary>
	public const int Size = sizeof(uint);

	/// <summary>
	/// If the internal <see cref="data"/> is greater than or equals to this value, then this <see cref="NodeToken"/>
	/// is an <see cref="Aggregator"/> node or branch rather than a geometric object (a leaf).
	/// </summary>
	const uint NodeThreshold = 0x8000_0000u;

	/// <summary>
	/// If the internal <see cref="data"/> is greater than or equals to this value, and is less than the value
	/// for the next threshold, then this <see cref="NodeToken"/> represents a <see cref="PreparedTriangle"/>.
	/// </summary>
	const uint TriangleThreshold = 0x4000_0000u;

	/// <summary>
	/// If the internal <see cref="data"/> is greater than or equals to this value, and is less than the value
	/// for the next threshold, then this <see cref="NodeToken"/> represents a <see cref="PreparedSphere"/>.
	/// </summary>
	const uint SphereThreshold = 0x2000_0000u;

	/// <summary>
	/// If the internal <see cref="data"/> is greater than or equals to this value, and is less than the value
	/// for the next threshold, then this <see cref="NodeToken"/> represents a <see cref="PreparedInstance"/>.
	/// </summary>
	const uint InstanceThreshold = 0x0000_0000u;

	/// <summary>
	/// If the internal <see cref="data"/> has this value, it means this
	/// <see cref="NodeToken"/> is a null node inside an <see cref="Aggregator"/>.
	/// </summary>
	const uint EmptyNode = ~0u;

	public bool Equals(NodeToken other) => EqualsFast(other);

	public bool EqualsFast(in NodeToken other) => data == other.data;

	public override bool Equals(object obj) => obj is NodeToken other && Equals(other);

	public override int GetHashCode() => (int)data;

	/// <summary>
	/// Creates a <see cref="NodeToken"/> that represents a <see cref="Aggregator"/> node with <paramref name="value"/>.
	/// </summary>
	public static NodeToken CreateNode(uint value)
	{
		Assert.IsTrue(value < uint.MaxValue + 1L - NodeThreshold);
		return new NodeToken(value + NodeThreshold);
	}

	/// <summary>
	/// Creates a <see cref="NodeToken"/> that represents a <see cref="PreparedTriangle"/> with <paramref name="value"/>.
	/// </summary>
	public static NodeToken CreateTriangle(uint value)
	{
		Assert.IsTrue(value < NodeThreshold - TriangleThreshold);
		return new NodeToken(value + TriangleThreshold);
	}

	/// <summary>
	/// Creates a <see cref="NodeToken"/> that represents a <see cref="PreparedSphere"/> with <paramref name="value"/>.
	/// </summary>
	public static NodeToken CreateSphere(uint value)
	{
		Assert.IsTrue(value < TriangleThreshold - SphereThreshold);
		return new NodeToken(value + SphereThreshold);
	}

	/// <summary>
	/// Creates a <see cref="NodeToken"/> that represents a <see cref="PreparedInstance"/> with <paramref name="value"/>.
	/// </summary>
	public static NodeToken CreateInstance(uint value)
	{
		Assert.IsTrue(value < SphereThreshold - InstanceThreshold);
		return new NodeToken(value + InstanceThreshold);
	}

	public static bool operator ==(in NodeToken left, in NodeToken right) => left.EqualsFast(right);
	public static bool operator !=(in NodeToken left, in NodeToken right) => !left.EqualsFast(right);
}