using System.Collections.Generic;
using EchoRenderer.Objects.Instancing;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Objects.Preparation
{
	public class SwatchExtractor
	{
		readonly Dictionary<Material, uint> map = new();
		readonly List<Material> materials = new();

		PreparedSwatch defaultSwatch;

		public uint Register(Material material)
		{
			if (map.TryGetValue(material, out uint token)) return token;

			token = (uint)map.Count;
			map.Add(material, token);
			materials.Add(material);

			return token;
		}

		public PreparedSwatch Prepare(MaterialSwatch swatch)
		{
			if (swatch == null || swatch.Equals(null)) return defaultSwatch ??= new PreparedSwatch(materials.ToArray());

			//TODO: prepared swatch caching

			return new PreparedSwatch(CreateSwatch(swatch));
		}

		Material[] CreateSwatch(MaterialSwatch swatch)
		{
			Material[] result = new Material[materials.Count];

			for (int i = 0; i < result.Length; i++)
			{
				Material material = materials[i];
				Material mapped = swatch[material];

				result[i] = mapped ?? material;
			}

			return result;
		}
	}
}