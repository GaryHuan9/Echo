using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using CodeHelpers.Mathematics;
using CodeHelpers.ObjectPooling;

namespace ForceRenderer.IO
{
	public class MaterialTemplateLibrary : LoadableAsset
	{
		public MaterialTemplateLibrary(string path) //Loads .mtl based on http://paulbourke.net/dataformats/mtl/
		{
			path = GetAbsolutePath(path); //Formulate path

			var loading = CollectionPooler<string, Material>.dictionary.GetObject();
			string currentMaterialName = null;

			//Load all parameters from file
			foreach (string line in File.ReadAllLines(path))
			{
				string[] parts = line.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 0) continue;

				switch (parts[0])
				{
					case "newmtl":
					{
						currentMaterialName = GetRemain(parts, 1);
						loading[currentMaterialName] = new Material();

						break;
					}
					case "Ka":
					{
						CheckMaterial();
						break; //Na is currently ignored
					}
					case "map_Ka":
					{
						CheckMaterial();
						break; //Na is currently ignored
					}
					case "Kd":
					{
						CheckMaterial().Diffuse = ParseFloat3(1);
						break;
					}
					case "map_Kd":
					{
						CheckMaterial().DiffuseTexture = ParseTexture(1);
						break;
					}
					case "Ks":
					{
						CheckMaterial().Specular = ParseFloat3(1);
						break;
					}
					case "map_Ks":
					{
						CheckMaterial().SpecularTexture = ParseTexture(1);
						break;
					}
					case "Ke":
					{
						CheckMaterial().Emission = ParseFloat3(1);
						break;
					}
					case "map_Ke":
					{
						CheckMaterial().EmissionTexture = ParseTexture(1);
						break;
					}
					case "Ns":
					{
						float smoothness = float.Parse(parts[1]) / 1000f;
						CheckMaterial().Smoothness = MathF.Pow(smoothness, 0.7f); //Might to some curve mapping

						break;
					}
					case "map_Ns":
					{
						CheckMaterial().SmoothnessTexture = ParseTexture(1);
						break;
					}
					case "Ni":
					{
						CheckMaterial();
						break; //Transparency currently not supported
					}
					case "Tr":
					{
						CheckMaterial();
						break; //Transparency currently not supported
					}
					case "d":
					{
						CheckMaterial();
						break; //Transparency currently not supported
					}
					case "map_d":
					{
						CheckMaterial();
						break; //Transparency currently not supported
					}
				}

				Float3 ParseFloat3(int start) => new Float3(float.Parse(parts[start]), float.Parse(parts[start + 1]), float.Parse(parts[start + 2]));
				Texture ParseTexture(int start) => new Texture(GetSiblingPath(path, GetRemain(parts, start)));
			}

			//Finalize materials
			var materialsList = new List<Material>(loading.Count);
			var namesDictionary = new Dictionary<string, int>(loading.Count);

			int index = 0;

			foreach ((string key, Material value) in loading)
			{
				materialsList.Add(new Material(value, true));
				namesDictionary.Add(key, index++);
			}

			materials = new ReadOnlyCollection<Material>(materialsList);
			names = new ReadOnlyDictionary<string, int>(namesDictionary);

			CollectionPooler<string, Material>.dictionary.ReleaseObject(loading);

			Material CheckMaterial()
			{
				if (loading.TryGetValue(currentMaterialName ?? "", out Material material)) return material;
				throw new Exception($"Invalid .mlt file at {path}! No material assigned before setting parameters!");
			}
		}

		static readonly ReadOnlyCollection<string> _acceptableFileExtensions = new ReadOnlyCollection<string>(new[] {".mtl"});
		protected override IReadOnlyList<string> AcceptableFileExtensions => _acceptableFileExtensions;

		readonly ReadOnlyCollection<Material> materials;
		readonly ReadOnlyDictionary<string, int> names;

		/// <summary>
		/// Returns the material based on its index in this library.
		/// </summary>
		public Material this[int index] => materials[index];

		/// <summary>
		/// Returns the index of a material in this library by its name.
		/// </summary>
		public int this[string name] => names[name];
	}
}