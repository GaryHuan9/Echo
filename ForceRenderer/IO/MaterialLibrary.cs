using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ForceRenderer.Rendering.Materials;

namespace ForceRenderer.IO
{
	public class MaterialLibrary : ILoadableAsset
	{
		public MaterialLibrary(string path)
		{
			path = this.GetAbsolutePath(path); //Formulate path
			using StreamReader reader = new StreamReader(File.OpenRead(path));

			materials = new Dictionary<string, Material>();
			Material processingMaterial;

			while (true)
			{
				ReadOnlySpan<char> line = reader.ReadLine();
				if (line == null) break;

				int end = line.IndexOf('#');
				if (end >= 0) line = line[..end];

				line = line.Trim();
				if (line.Length == 0) continue;

				//Process line
				var token = Eat(ref line);

				if (token.SequenceEqual("declare"))
				{
					//Declaring new material
					string typeName = new string(Eat(ref line));
					Type type = Type.GetType(typeName);

					if (type == null) throw new Exception($"Invalid material type name: {typeName}");
					processingMaterial = (Material)Activator.CreateInstance(type);

					string name = new string(Eat(ref line));

					if (!materials.ContainsKey(name)) materials.Add(name, processingMaterial);
					throw new Exception($"Duplicated material name: {name}");
				}
				else
				{
					//Assigning material attribute

				}
			}
		}

		static readonly ReadOnlyCollection<string> _acceptableFileExtensions = new ReadOnlyCollection<string>(new[] {".mat"});
		IReadOnlyList<string> ILoadableAsset.AcceptableFileExtensions => _acceptableFileExtensions;

		readonly Dictionary<string, Material> materials;

		/// <summary>
		/// Returns the first material of this library.
		/// </summary>
		public readonly Material first;

		/// <summary>
		/// Returns the material based on its name in this library.
		/// </summary>
		public Material this[string name] => materials[name];

		static ReadOnlySpan<char> Eat(ref ReadOnlySpan<char> line)
		{
			int index = line.IndexOf(' ');

			if (index < 0)
			{
				var piece = line;
				line = default;
				return piece;
			}
			else
			{
				var piece = line[..index];
				line = line[index..].Trim();
				return piece;
			}
		}
	}
}