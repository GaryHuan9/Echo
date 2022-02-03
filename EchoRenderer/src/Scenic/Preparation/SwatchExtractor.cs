using System.Collections.Generic;
using CodeHelpers.Diagnostics;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Scenic.Geometries;
using EchoRenderer.Scenic.Instancing;

namespace EchoRenderer.Scenic.Preparation
{
	/// <summary>
	/// A class that is used to extract swatches from <see cref="Material"/> on <see cref="GeometryEntity"/>
	/// and convert instanced <see cref="MaterialSwatch"/> into <see cref="PreparedSwatch"/> efficiently.
	/// </summary>
	public class SwatchExtractor
	{
		public SwatchExtractor(ScenePreparer preparer) => this.preparer = preparer;

		readonly ScenePreparer preparer;

		readonly Dictionary<Material, uint> map = new();
		readonly List<Material> materialList = new();

		PreparedSwatch emptySwatch;                                //Caches the default empty swatch with no mappings
		Dictionary<MaterialSwatch, PreparedSwatch> cachedSwatches; //Caches all the prepared swatches and their originals

		Seal seal;

		/// <summary>
		/// Registers <paramref name="material"/> into this <see cref="SwatchExtractor"/> and returns a token for this <paramref name="material"/>.
		/// That token can be used to identify and retrieve this <paramref name="material"/> (or a mapped one) later on in <see cref="PreparedSwatch"/>.
		/// NOTE: this method should not be invoked after we started invoking <see cref="Prepare"/>.
		/// </summary>
		public uint Register(Material material)
		{
			seal.AssertNotApplied();

			if (map.TryGetValue(material, out uint token)) return token;

			token = (uint)map.Count;
			map.Add(material, token);
			materialList.Add(material);

			return token;
		}

		/// <summary>
		/// Prepares <paramref name="swatch"/> into a <see cref="PreparedSwatch"/>.
		/// Note that once this method is invoked, invocation to <see cref="Register"/> is no longer supported.
		/// </summary>
		public PreparedSwatch Prepare(MaterialSwatch swatch)
		{
			seal.TryApply();

			var valueComparer = MaterialSwatch.valueEqualityComparer; //We will compare the swatches based on their content, not reference

			//If this swatch is empty or null, return the prepared default empty swatch
			if (valueComparer.Equals(swatch, null)) return emptySwatch ??= CreateSwatch(materialList.ToArray());

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
				Material material = materialList[i];
				Material mapped = swatch[material];

				result[i] = mapped ?? material;
			}

			return result;
		}
	}
}