using System.Collections.Generic;
using System.Threading;
using CodeHelpers.Diagnostics;
using CodeHelpers.Threads;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Geometries;
using Echo.Core.Scenic.Hierarchies;

namespace Echo.Core.Scenic.Preparation;

/// <summary>
/// A class that is used to extract swatches from <see cref="Material"/> on <see cref="IGeometrySource"/>
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
	static readonly MaterialSwatch emptyMaterialSwatch = new();

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
		seal.AssertNotApplied();
		materialList[index].Register(count);
	}

	/// <summary>
	/// Prepares <paramref name="swatch"/> into a <see cref="PreparedSwatch"/>. Note that once
	/// this method is invoked, invocation to the registration methods is no longer supported.
	/// </summary>
	public PreparedSwatch Prepare(MaterialSwatch swatch = null)
	{
		seal.TryApply();

		//We will compare the swatches based on their content, not reference
		var valueComparer = MaterialSwatch.valueEqualityComparer;

		swatch ??= emptyMaterialSwatch;

		//Find cached swatch again, this time look through all the ones that are not empty
		cachedSwatches ??= new Dictionary<MaterialSwatch, PreparedSwatch>(valueComparer);
		if (cachedSwatches.TryGetValue(swatch, out PreparedSwatch prepared)) return prepared;

		//Create and cache if none found
		prepared = new PreparedSwatch(CreateMaterials());
		cachedSwatches.Add(swatch, prepared);
		return prepared;

		Material[] CreateMaterials()
		{
			int count = materialList.Count;
			var result = new Material[count];

			for (int i = 0; i < count; i++)
			{
				Material material = materialList[i].material;
				Material mapped = swatch[material] ?? material;

				preparer.Prepare(mapped);
				result[i] = mapped;
			}

			return result;
		}
	}

	/// <summary>
	/// Returns the number of registered users for the <see cref="Material"/> represented by <paramref name="index"/>.
	/// </summary>
	public int GetRegistrationCount(MaterialIndex index)
	{
		seal.TryApply();
		return materialList[index].Count;
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