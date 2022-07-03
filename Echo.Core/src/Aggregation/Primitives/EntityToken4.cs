using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Echo.Core.Common;

namespace Echo.Core.Aggregation.Primitives;

/// <summary>
/// A pack of four <see cref="EntityToken"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct EntityToken4
{
	public EntityToken4(EntityToken token0, EntityToken token1, EntityToken token2, EntityToken token3)
	{
		this.token0 = token0;
		this.token1 = token1;
		this.token2 = token2;
		this.token3 = token3;
	}

	public EntityToken4(ReadOnlySpan<EntityToken> tokens) : this
	(
		tokens.TryGetValue(0, EntityToken.Empty), tokens.TryGetValue(1, EntityToken.Empty),
		tokens.TryGetValue(2, EntityToken.Empty), tokens.TryGetValue(3, EntityToken.Empty)
	) { }

	// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
	readonly EntityToken token0;
	readonly EntityToken token1;
	readonly EntityToken token2;
	readonly EntityToken token3;
	// ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

	/// <summary>
	/// Retrieves an <see cref="EntityToken"/> in this <see cref="EntityToken4"/>.
	/// </summary>
	/// <param name="index">The index of the <see cref="EntityToken"/> to get. Must be between 0 (inclusive) and 4 (exclusive).</param>
	public EntityToken this[int index] => Unsafe.Add(ref Unsafe.AsRef(in token0), index);

	public override readonly int GetHashCode()
	{
		fixed (EntityToken4* ptr = &this) return Utility.GetHashCode(ptr);
	}
}