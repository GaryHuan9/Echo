using System;
using System.Collections.Generic;
using System.Linq;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Objects.Scenes
{
	public class MaterialPresser
	{
		public int Count => sourceMaterials.Count;

		readonly Dictionary<Material, int> sourceMaterials = new();
		readonly Dictionary<MaterialMapper, Mapper> mappers = new();

		Material[] materials;
		Mapper emptyMapper;

		bool pressed;

		/// <summary>
		/// Adds <paramref name="material"/> to this presser to press and returns an internal token
		/// that can be used to identify that material. -1 is returned if material <see cref="Invisible"/> is passed.
		/// </summary>
		public int GetToken(Material material)
		{
			AssertNotPressed();
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
			AssertNotPressed();

			if (source == null) return emptyMapper ??= new Mapper(this, null);
			if (mappers.TryGetValue(source, out Mapper mapper)) return mapper;

			mapper = new Mapper(this, source);
			mappers.Add(source, mapper);

			return mapper;
		}

		/// <summary>
		/// Presses this presser and all of its mappers to be ready for data fetching.
		/// </summary>
		public void Press()
		{
			AssertNotPressed();

			materials = (from pair in sourceMaterials
						 orderby pair.Value
						 select pair.Key).ToArray();

			foreach (Material material in materials) material.Press();
			foreach (var pair in mappers) pair.Value.Press();

			emptyMapper?.Press();
			pressed = true;
		}

		void AssertNotPressed()
		{
			if (!pressed) return;
			throw new Exception($"Operation unavailable on a pressed {nameof(MaterialPresser)}!");
		}

		public class Mapper
		{
			public Mapper(MaterialPresser presser, MaterialMapper mapper)
			{
				this.presser = presser;
				this.mapper = mapper;
			}

			readonly MaterialPresser presser;
			readonly MaterialMapper mapper;

			Material[] materials;

			public Material this[int token] => materials[token];

			public void Press()
			{
				materials = (Material[])presser.materials.Clone();
				if (mapper == null) return;

				foreach (Material from in mapper.Keys)
				{
					Material material = mapper[from];

					if (!presser.sourceMaterials.TryGetValue(from, out int token)) continue;
					if (presser.sourceMaterials.TryAdd(material, token)) material.Press();

					materials[token] = material;
				}
			}
		}
	}
}