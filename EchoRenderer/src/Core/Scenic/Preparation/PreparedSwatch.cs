using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CodeHelpers.Diagnostics;
using EchoRenderer.Common;
using EchoRenderer.Core.Rendering.Materials;
using EchoRenderer.Core.Scenic.Instancing;

namespace EchoRenderer.Core.Scenic.Preparation;

/// <summary>
/// A <see cref="MaterialSwatch"/> that is prepared for fast interactions.
/// </summary>
public class PreparedSwatch
{
	public PreparedSwatch(Material[] materials, ReadOnlySpan<MaterialIndex> indices)
	{
		Assert.AreEqual(materials.Length, indices.Length);

		this.materials = materials;

		for (int i = 0; i < indices.Length; i++)
		{
			Material material = materials[i];
			if (!material.Emission.PositiveRadiance()) continue;

			_emissiveIndices ??= new List<MaterialIndex>();
			_emissiveIndices.Add(indices[i]);
		}
	}

	readonly Material[] materials;
	readonly List<MaterialIndex> _emissiveIndices;

	/// <summary>
	/// Returns all <see cref="MaterialIndex"/> in this <see cref="PreparedSwatch"/> that point to emissive <see cref="Material"/>.
	/// </summary>
	public ReadOnlySpan<MaterialIndex> EmissiveIndices => CollectionsMarshal.AsSpan(_emissiveIndices);

	/// <summary>
	/// Index this <see cref="PreparedSwatch"/> at <paramref name="index"/>.
	/// </summary>
	public Material this[MaterialIndex index] => materials[index];
}