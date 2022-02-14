using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeHelpers.Diagnostics;
using CodeHelpers.Threads;
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

	PreparedSwatch emptySwatch;                                //Caches the default empty swatch with no mappings
	Dictionary<MaterialSwatch, PreparedSwatch> cachedSwatches; //Caches all the prepared swatches and their originals

	Seal seal;

	/// <summary>
	/// Returns the number of registered <see cref="Material"/>.
	/// </summary>
	public int Count => map.Count;

	/// <summary>
	/// Registers <paramref name="material"/> into this <see cref="SwatchExtractor"/> and returns a token for this <paramref name="material"/>.
	/// That token can be used to identify and retrieve this <paramref name="material"/> (or a mapped one) later on in <see cref="PreparedSwatch"/>.
	/// NOTE: this method should not be invoked after we started invoking <see cref="Prepare"/>.
	/// </summary>
	public uint Register(Material material, int count = 1)
	{
		Assert.IsTrue(count > 0);
		seal.AssertNotApplied();

		if (map.TryGetValue(material, out Data data))
		{
			data.Register(count);
			return data.token;
		}

		data = new Data(material, (uint)map.Count);
		data.Register(count);

		map.Add(material, data);
		materialList.Add(data);

		return data.token;
	}

	/// <summary>
	/// Registers a <paramref name="count"/> of users for the <see cref="Material"/> represented by <paramref name="token"/>.
	/// </summary>
	public void Register(uint token, int count = 1)
	{
		Assert.IsTrue(count > 0);
		materialList[(int)token].Register(count);
	}

	/// <summary>
	/// Returns the number of registered users for the <see cref="Material"/> represented by <paramref name="token"/>.
	/// </summary>
	public int GetRegistrationCount(uint token) => materialList[(int)token].Count;

	/// <summary>
	/// Prepares <paramref name="swatch"/> into a <see cref="PreparedSwatch"/>. Note that once this method is invoked,
	/// invocation to <see cref="Register(Material,int)"/> and <see cref="Register(uint,int)"/> is no longer supported.
	/// </summary>
	public PreparedSwatch Prepare(MaterialSwatch swatch)
	{
		seal.TryApply();

		var valueComparer = MaterialSwatch.valueEqualityComparer; //We will compare the swatches based on their content, not reference

		//If this swatch is empty or null, return the prepared default empty swatch
		if (valueComparer.Equals(swatch, null)) return emptySwatch ??= CreateSwatch(materialList.Select(data => data.material).ToArray());

		//Find cached swatch again, this time look through all the ones that are not empty
		cachedSwatches ??= new Dictionary<MaterialSwatch, PreparedSwatch>(valueComparer);
		if (cachedSwatches.TryGetValue(swatch, out PreparedSwatch prepared)) return prepared;

		//Create and cache if none found
		prepared = CreateSwatch(CreateMaterials(swatch));
		cachedSwatches.Add(swatch, prepared);

		return prepared;
	}

	PreparedSwatch CreateSwatch(Material[] materials)
	{
		foreach (var material in materials) preparer.PrepareMaterial(material);
		return new PreparedSwatch(materials);
	}

	Material[] CreateMaterials(MaterialSwatch swatch)
	{
		Material[] result = new Material[materialList.Count];

		for (int i = 0; i < result.Length; i++)
		{
			Material material = materialList[i].material;
			result[i] = swatch[material] ?? material;
		}

		return result;
	}

	class Data
	{
		public Data(Material material, uint token)
		{
			this.material = material;
			this.token = token;
		}

		public readonly Material material;
		public readonly uint token;

		int _count;

		public int Count => InterlockedHelper.Read(ref _count);

		public void Register(int count) => Interlocked.Add(ref _count, count);
	}
}