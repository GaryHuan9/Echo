using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using CodeHelpers.Mathematics;
using ForceRenderer.Rendering.Materials;
using ForceRenderer.Textures;

namespace ForceRenderer.IO
{
	public class MaterialLibrary : ILoadableAsset
	{
		public MaterialLibrary(string path)
		{
			path = this.GetAbsolutePath(path); //Formulate path
			using StreamReader reader = new StreamReader(File.OpenRead(path));

			materials = new Dictionary<string, Material>();
			Material processing = null;

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
					Type type = Type.GetType($"ForceRenderer.Rendering.Materials.{typeName}");

					if (type != null) processing = (Material)Activator.CreateInstance(type);
					else throw new Exception($"Invalid material type name: {typeName}");

					string name = new string(Eat(ref line));

					if (!materials.ContainsKey(name)) materials.Add(name, processing);
					else throw new Exception($"Duplicated material name: {name}");
				}
				else
				{
					//Assigning material attribute
					if (processing == null) throw new Exception("Assigning attributes before declaring a material!");
					PropertyInfo info = processing.GetType().GetProperty(new string(token));

					object parsed = Parse(ref line);

					if (info == null) throw new Exception($"No attribute named: {token.ToString()}!");
					if (!info.PropertyType.IsInstanceOfType(parsed)) throw new Exception($"Invalid input type to attribute {info}!");

					info.SetValue(processing, parsed);
				}
			}

			first = materials.FirstOrDefault().Value;

			object Parse(ref ReadOnlySpan<char> line)
			{
				ReadOnlySpan<char> piece0 = Eat(ref line);
				ReadOnlySpan<char> piece1 = Eat(ref line);
				ReadOnlySpan<char> piece2 = Eat(ref line);

				if (piece0.IsEmpty) throw new Exception("Cannot set attribute with empty parameters!");

				if (!float.TryParse(piece0, out float float0))
				{
					string sibling = new string(piece0.Trim('"'));
					return Texture2D.Load(this.GetSiblingPath(path, sibling));
				}

				if (!float.TryParse(piece1, out float float1)) return float0;
				if (!float.TryParse(piece2, out float float2)) return new Float2(float0, float1);

				return new Float3(float0, float1, float2);
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
		public Material this[string name]
		{
			get => materials[name];
			set => materials[name] = value;
		}

		static ReadOnlySpan<char> Eat(ref ReadOnlySpan<char> line)
		{
			int index = -1;
			int quote = 0; //Count of quotation characters

			for (int i = 0; i < line.Length; i++)
			{
				char current = line[i];
				if (current == '"') quote++;

				if (current == ' ' && quote % 2 == 0)
				{
					index = i;
					break;
				}
			}

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