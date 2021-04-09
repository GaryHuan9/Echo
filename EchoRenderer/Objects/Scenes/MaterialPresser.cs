using System.Collections.Generic;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Objects.Scenes
{
	public class MaterialPresser
	{
		readonly Dictionary<Material, int> sourceMaterials = new();
		readonly Dictionary<MaterialMapper, int[]> mappers = new();

		Material[] materials;

		public int GetToken(Material material)
		{
			if (material is Invisible) return -1; //Negative token used to omit invisible materials

			if (!sourceMaterials.TryGetValue(material, out int materialToken))
			{
				materialToken = sourceMaterials.Count;
				sourceMaterials.Add(material, materialToken);
			}

			return materialToken;
		}

		public Mapper GetMapper(MaterialMapper source)
		{
			if (!mappers.TryGetValue(source, out int[] array))
			{
mappers[source] =sa
			}

			return new Mapper(materials, array);
		}

		public readonly struct Mapper
		{
			public Mapper(Material[] materials, int[] map)
			{
				this.materials = materials;
				this.map = map;
			}

			readonly Material[] materials;
			readonly int[] map;

			public Material this[int token] => materials[map?[token] ?? token];
		}
	}
}