using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Randomization;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Objects.Instancing
{
	public class MaterialSwatch
	{
		public MaterialSwatch() => map = new Dictionary<Material, Material>();

		public MaterialSwatch(MaterialSwatch source) => map = source.map.ToDictionary(pair => pair.Key, pair => pair.Value);

		//NOTE: This swatch can only exchange materials defined on geometries in the immediate ObjectPack, it cannot be used to modify materials in subsequent instances!

		readonly Dictionary<Material, Material> map;

		uint hash;

		public Material this[Material material]
		{
			get => map.TryGetValue(material);
			set
			{
				if (value == null || value == material)
				{
					var old = this[material];
					if (old == null) return;

					hash ^= ~Hash(old) ^ Hash(material);
					bool removed = map.Remove(material);
					Assert.IsTrue(removed);
				}
				else
				{
					var old = this[material];
					hash ^= ~Hash(value);

					if (old == null)
					{
						hash ^= Hash(material);
						map.Add(material, value);
					}
					else
					{
						hash ^= ~Hash(old);
						map[material] = value;
					}
				}
			}
		}

		public bool Equals(MaterialSwatch other)
		{
			if (ReferenceEquals(this, other)) return true;
			if (map.Count == 0 && other is null) return true;

			if (hash != other.hash || map.Count != other.map.Count) return false;

			foreach ((Material key, Material value) in other.map)
			{
				if (map.TryGetValue(key) != value) return false;
			}

			return true;
		}

		static uint Hash(Material material) => SquirrelRandom.Mangle((uint)material.GetHashCode());

		public class EqualityComparer : IEqualityComparer<MaterialSwatch>
		{
			public bool Equals(MaterialSwatch x, MaterialSwatch y) => !ReferenceEquals(x, null) && x.Equals(y);

			public int GetHashCode(MaterialSwatch swatch) => (int)swatch.hash;
		}
	}
}