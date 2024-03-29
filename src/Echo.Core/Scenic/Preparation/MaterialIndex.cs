﻿using Echo.Core.Common.Diagnostics;
using Echo.Core.Evaluation.Materials;

namespace Echo.Core.Scenic.Preparation;

/// <summary>
/// An index to a <see cref="Material"/> mapping localized to a <see cref="SwatchExtractor"/>.
/// </summary>
public readonly struct MaterialIndex
{
	/// <summary>
	/// Constructs a new <see cref="MaterialIndex"/> from the current total
	/// number of <see cref="Material"/> in this <see cref="SwatchExtractor"/>.
	/// </summary>
	public MaterialIndex(int count)
	{
		Ensure.IsTrue(count >= 0);
		data = (uint)count;
	}

	readonly uint data;

	/// <summary>
	/// Retrieves the <see cref="uint"/> value of this <see cref="MaterialIndex"/>. It is guaranteed
	/// that this value starts at zero is consecutive within a single <see cref="SwatchExtractor"/>.
	/// </summary>
	public static implicit operator int(MaterialIndex index) => (int)index.data;
}