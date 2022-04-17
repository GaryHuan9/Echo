using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CodeHelpers.Diagnostics;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Core.Evaluation.Materials;
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
			if (materials[i] is not IEmissive emissive || !FastMath.Positive(emissive.Power)) continue;

			_emissiveIndices ??= new List<MaterialIndex>();
			_emissiveIndices.Add(indices[i]);
		}
	}

	readonly Material[] materials;
	readonly List<MaterialIndex> _emissiveIndices;

	/// <summary>
	/// Returns all <see cref="MaterialIndex"/> in this <see cref="PreparedSwatch"/> that point to <see cref="IEmissive"/> materials.
	/// </summary>
	public ReadOnlySpan<MaterialIndex> EmissiveIndices => _emissiveIndices == null ? ReadOnlySpan<MaterialIndex>.Empty : CollectionsMarshal.AsSpan(_emissiveIndices);

	/// <summary>
	/// Index this <see cref="PreparedSwatch"/> at <paramref name="index"/>.
	/// </summary>
	public Material this[MaterialIndex index] => materials[index];
}