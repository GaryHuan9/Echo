using System.Collections.Generic;
using CodeHelpers.Collections;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Objects.Preparation
{
	public class MaterialMapper
	{
		public Material this[Material from]
		{
			get => map.TryGetValue(from);
			set
			{
				if (value == null) map.Remove(from);
				else map[from] = value;
			}
		}

		public Dictionary<Material, Material>.KeyCollection Keys => map.Keys;

		readonly Dictionary<Material, Material> map = new();

		public MaterialMapper Clone()
		{
			MaterialMapper mapper = new MaterialMapper();

			foreach ((Material from, Material to) in map) mapper.map.Add(from, to);

			return mapper;
		}
	}
}