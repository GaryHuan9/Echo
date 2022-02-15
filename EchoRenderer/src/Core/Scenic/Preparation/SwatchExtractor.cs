﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeHelpers.Diagnostics;
using CodeHelpers.Threads;
using EchoRenderer.Core.Aggregation.Preparation;
using EchoRenderer.Core.Rendering.Materials;
using EchoRenderer.Core.Scenic.Geometries;
using EchoRenderer.Core.Scenic.Instancing;

namespace EchoRenderer.Core.Scenic.Preparation;

/// <summary>
/// A class that is used to extract swatches from <see cref="Material"/> on <see cref="GeometryEntity"/>
/// and convert instanced <see cref="MaterialSwatch"/> into <see cref="PreparedSwatch"/> efficiently.
/// </summary>
public class SwatchExtractor
{
	public SwatchExtractor(ScenePreparer preparer) => this.preparer = preparer;

	readonly ScenePreparer preparer;

	readonly Dictionary<Material, Data> map = new();
	readonly List<Data> materialList = new();

	/// <summary>
	/// Caches all the prepared swatches and their originals
	/// </summary>
	Dictionary<MaterialSwatch, PreparedSwatch> cachedSwatches;

	Seal seal;

	/// <summary>
	/// An empty <see cref="MaterialSwatch"/> used to replace null ones.
	/// </summary>
	static readonly MaterialSwatch emptySwatch = new();

	MaterialIndex[] _indices = Array.Empty<MaterialIndex>();

	/// <summary>
	/// Returns the <see cref="MaterialIndex"/> of all of the registered <see cref="Material"/> in this <see cref="SwatchExtractor"/>.
	/// </summary>
	public ReadOnlySpan<MaterialIndex> Indices
	{
		get
		{
			if (_indices.Length == materialList.Count) return _indices;
			_indices = materialList.Select(data => data.index).ToArray();

			return _indices;
		}
	}

	/// <summary>
	/// Registers <paramref name="material"/> into this <see cref="SwatchExtractor"/> and returns a <see cref="MaterialIndex"/> which
	/// can be used to identify and retrieve this <paramref name="material"/> (or a mapped one) later on in <see cref="PreparedSwatch"/>.
	/// NOTE: this method should not be invoked after we started invoking <see cref="Prepare"/>.
	/// </summary>
	public MaterialIndex Register(Material material, int count = 1)
	{
		Assert.IsTrue(count > 0);
		seal.AssertNotApplied();

		if (map.TryGetValue(material, out Data data))
		{
			data.Register(count);
			return data.index;
		}

		data = new Data(material, new MaterialIndex(map.Count));
		data.Register(count);

		map.Add(material, data);
		materialList.Add(data);

		return data.index;
	}

	/// <summary>
	/// Registers a <paramref name="count"/> of users for the <see cref="Material"/> represented by <paramref name="index"/>.
	/// </summary>
	public void Register(MaterialIndex index, int count = 1)
	{
		Assert.IsTrue(count > 0);
		materialList[index].Register(count);
	}

	/// <summary>
	/// Returns the number of registered users for the <see cref="Material"/> represented by <paramref name="index"/>.
	/// </summary>
	public int GetRegistrationCount(MaterialIndex index) => materialList[index].Count;

	/// <summary>
	/// Prepares <paramref name="swatch"/> into a <see cref="PreparedSwatch"/>. Note that once
	/// this method is invoked, invocations to the registration methods is no longer supported.
	/// </summary>
	public PreparedSwatch Prepare(MaterialSwatch swatch)
	{
		seal.TryApply();

		//We will compare the swatches based on their content, not reference
		var valueComparer = MaterialSwatch.valueEqualityComparer;

		swatch ??= emptySwatch;

		//Find cached swatch again, this time look through all the ones that are not empty
		cachedSwatches ??= new Dictionary<MaterialSwatch, PreparedSwatch>(valueComparer);
		if (cachedSwatches.TryGetValue(swatch, out PreparedSwatch prepared)) return prepared;

		//Create and cache if none found
		prepared = new PreparedSwatch(CreateMaterials(swatch));
		cachedSwatches.Add(swatch, prepared);
		return prepared;
	}

	Material[] CreateMaterials(MaterialSwatch swatch)
	{
		Material[] result = new Material[materialList.Count];

		for (int i = 0; i < result.Length; i++)
		{
			Material material = materialList[i].material;
			Material mapped = swatch[material] ?? material;

			preparer.PrepareMaterial(mapped);
			result[i] = mapped;
		}

		return result;
	}

	/// <summary>
	/// A single entry to <see cref="map"/>.
	/// </summary>
	class Data
	{
		public Data(Material material, MaterialIndex index)
		{
			this.material = material;
			this.index = index;
		}

		public readonly Material material;
		public readonly MaterialIndex index;

		int _count;

		public int Count => InterlockedHelper.Read(ref _count);

		public void Register(int count) => Interlocked.Add(ref _count, count);
	}
}