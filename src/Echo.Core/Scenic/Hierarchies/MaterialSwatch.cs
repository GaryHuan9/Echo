using System.Collections.Generic;
using System.Linq;
using Echo.Core.Common;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Evaluation.Materials;

namespace Echo.Core.Scenic.Hierarchies;

/// <summary>
/// A swatch of <see cref="Material"/>s that can be used to optionally map some original materials to different alternative materials in a <see cref="PackInstance"/>.
/// Note that it can only exchange materials defined on geometries in the immediate <see cref="EntityPack"/> and cannot be used to modify materials in subsequent instances!
/// </summary>
public class MaterialSwatch
{
	public MaterialSwatch() => map = new Dictionary<Material, Material>();

	public MaterialSwatch(MaterialSwatch source) => map = source.map.ToDictionary(pair => pair.Key, pair => pair.Value);

	readonly Dictionary<Material, Material> map;

	/// <summary>
	/// A simple hash for <see cref="map"/>.
	/// Used for fast comparison and hashing.
	/// </summary>
	uint hash;

	public static readonly ValueEqualityComparer valueEqualityComparer = new();

	/// <summary>
	/// Accesses this <see cref="MaterialSwatch"/> at <paramref name="material"/>. To map a new <see cref="Material"/>,
	/// simply index and assign it to a different <see cref="Material"/>. A mapping can be removed by assigning either
	/// null or the <see cref="Material"/> itself to a <see cref="Material"/> index.
	/// </summary>
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
				Ensure.IsTrue(removed);
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

	static uint Hash(Material material) => SquirrelPrng.Mangle((uint)material.GetHashCode());

	/// <summary>
	/// A <see cref="IEqualityComparer{T}"/> that compares the actual value/content of two different <see cref="MaterialSwatch"/> (rather than just references).
	/// NOTE: if the value of a <see cref="MaterialSwatch"/> is changed, then obviously the results of this <see cref="IEqualityComparer{T}"/> can change as well.
	/// </summary>
	public class ValueEqualityComparer : IEqualityComparer<MaterialSwatch>
	{
		public bool Equals(MaterialSwatch x, MaterialSwatch y)
		{
			if (ReferenceEquals(x, y)) return true;

			if (ReferenceEquals(x, null)) return y.map.Count == 0;
			if (ReferenceEquals(y, null)) return x.map.Count == 0;

			if (x.hash != y.hash || x.map.Count != y.map.Count) return false;

			foreach ((Material key, Material value) in y.map)
			{
				if (x.map.TryGetValue(key) != value) return false;
			}

			return true;
		}

		public int GetHashCode(MaterialSwatch swatch) => (int)swatch.hash;
	}
}