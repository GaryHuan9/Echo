using System;
using System.Runtime.CompilerServices;
using Echo.Common;
using Echo.Core.Aggregation.Primitives;

namespace Echo.Core.Aggregation.Acceleration;

public unsafe struct NodeToken4
{
	public NodeToken4(in NodeToken token0, in NodeToken token1, in NodeToken token2, in NodeToken token3)
	{
		ref NodeToken ptr = ref Unsafe.As<byte, NodeToken>(ref data[0]);

		Unsafe.Add(ref ptr, 0) = token0;
		Unsafe.Add(ref ptr, 1) = token1;
		Unsafe.Add(ref ptr, 2) = token2;
		Unsafe.Add(ref ptr, 3) = token3;
	}

	public NodeToken4(ReadOnlySpan<NodeToken> tokens) : this
	(
		tokens.TryGetValue(0, NodeToken.Empty), tokens.TryGetValue(1, NodeToken.Empty),
		tokens.TryGetValue(2, NodeToken.Empty), tokens.TryGetValue(3, NodeToken.Empty)
	) { }

#pragma warning disable CS0649
	fixed byte data[Width * NodeToken.Size];
#pragma warning restore CS0649

	const int Width = 4;

	public readonly ref readonly NodeToken this[int index]
	{
		get
		{
			ref readonly byte origin = ref data[0];                             //First retrieve a reference to the head of the array
			ref byte mutable = ref Unsafe.AsRef(in origin);                     //Then remove the readonly status on that reference
			ref NodeToken casted = ref Unsafe.As<byte, NodeToken>(ref mutable); //Cast it to a token type (expands its size as well)
			return ref Unsafe.Add(ref casted, index);                           //Finally offsets the reference by index
		}
	}

	public override readonly int GetHashCode()
	{
		fixed (NodeToken4* ptr = &this) return Utilities.GetHashCode(ptr);
	}
}