using System;
using System.Runtime.CompilerServices;
using Echo.Core.Common;

namespace Echo.Core.Aggregation.Primitives;

/// <summary>
/// A pack of four <see cref="EntityToken"/>.
/// </summary>
public unsafe struct EntityToken4
{
	public EntityToken4(in EntityToken token0, in EntityToken token1, in EntityToken token2, in EntityToken token3)
	{
		ref EntityToken ptr = ref Unsafe.As<byte, EntityToken>(ref data[0]);

		Unsafe.Add(ref ptr, 0) = token0;
		Unsafe.Add(ref ptr, 1) = token1;
		Unsafe.Add(ref ptr, 2) = token2;
		Unsafe.Add(ref ptr, 3) = token3;
	}

	public EntityToken4(ReadOnlySpan<EntityToken> tokens) : this
	(
		tokens.TryGetValue(0, EntityToken.Empty), tokens.TryGetValue(1, EntityToken.Empty),
		tokens.TryGetValue(2, EntityToken.Empty), tokens.TryGetValue(3, EntityToken.Empty)
	) { }

#pragma warning disable CS0649
	fixed byte data[Width * EntityToken.Size];
#pragma warning restore CS0649

	const int Width = 4;

	/// <summary>
	/// Gets the reference to an <see cref="EntityToken"/> in this <see cref="EntityToken4"/>.
	/// </summary>
	/// <param name="index">The index of the <see cref="EntityToken"/> to get. Must be between 0 (inclusive) and 4 (exclusive).</param>
	public readonly ref readonly EntityToken this[int index]
	{
		get
		{
			ref readonly byte origin = ref data[0];                         //First retrieve a reference to the head of the array
			ref byte mutable = ref Unsafe.AsRef(in origin);                 //Then remove the readonly status on that reference
			ref var casted = ref Unsafe.As<byte, EntityToken>(ref mutable); //Cast it to a token type (expands its size as well)
			return ref Unsafe.Add(ref casted, index);                       //Finally offsets the reference by index
		}
	}

	public override readonly int GetHashCode()
	{
		fixed (byte* ptr = data) return Utility.GetHashCode(ptr);
	}
}