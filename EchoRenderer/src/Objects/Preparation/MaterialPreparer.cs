using System;
using System.Collections.Generic;
using System.Linq;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Objects.Preparation
{
	public class MaterialPreparer
	{
		public int Count => sourceMaterials.Count;

		readonly Dictionary<Material, int> sourceMaterials = new();
		readonly Dictionary<MaterialMapper, Mapper> mappers = new();

		Material[] materials;
		Mapper emptyMapper;

		bool prepared;

		/// <summary>
		/// Adds <paramref name="material"/> to this <see cref="MaterialPreparer"/> to prepare and returns an internal token
		/// that can be used to identify this <paramref name="material"/>. -1 is returned if <see cref="Invisible"/> is passed.
		/// </summary>
		public int GetToken(Material material)
		{
			AssertNotPrepared();
			if (material is Invisible) return -1; //Negative token used to omit invisible materials

			if (!sourceMaterials.TryGetValue(material, out int materialToken))
			{
				materialToken = sourceMaterials.Count;
				sourceMaterials.Add(material, materialToken);
			}

			return materialToken;
		}

		/// <summary>
		/// Returns a material mapper that returns material based on token generated from <paramref name="source"/>.
		/// If <paramref name="source"/> is null, a default/empty mapper is returned.
		/// </summary>
		public Mapper GetMapper(MaterialMapper source)
		{
			AssertNotPrepared();

			if (source == null) return emptyMapper ??= new Mapper(this, null);
			if (mappers.TryGetValue(source, out Mapper mapper)) return mapper;

			mapper = new Mapper(this, source);
			mappers.Add(source, mapper);

			return mapper;
		}

		/// <summary>
		/// Executes this <see cref="MaterialPreparer"/> so all of its mappers are ready for data fetching.
		/// </summary>
		public void Prepare()
		{
			AssertNotPrepared();

			materials = (from pair in sourceMaterials
						 orderby pair.Value
						 select pair.Key).ToArray();

			foreach (Material material in materials) material.Prepare();
			foreach (var pair in mappers) pair.Value.Prepare();

			emptyMapper?.Prepare();
			prepared = true;
		}

		void AssertNotPrepared()
		{
			if (!prepared) return;
			throw new Exception($"Operation unavailable on a prepared {nameof(MaterialPreparer)}!");
		}

		public class Mapper
		{
			public Mapper(MaterialPreparer preparer, MaterialMapper mapper)
			{
				this.preparer = preparer;
				this.mapper = mapper;
			}

			readonly MaterialPreparer preparer;
			readonly MaterialMapper mapper;

			Material[] materials;

			public Material this[int token] => materials[token];

			public void Prepare()
			{
				materials = (Material[])preparer.materials.Clone();
				if (mapper == null) return;

				foreach (Material from in mapper.Keys)
				{
					Material material = mapper[from];

					if (!preparer.sourceMaterials.TryGetValue(from, out int token)) continue;
					if (preparer.sourceMaterials.TryAdd(material, token)) material.Prepare();

					materials[token] = material;
				}
			}
		}
	}
}