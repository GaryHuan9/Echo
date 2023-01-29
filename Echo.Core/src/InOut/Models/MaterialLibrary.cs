using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Materials;
using Echo.Core.InOut.Images;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.InOut.Models;

public sealed class MaterialLibrary
{
	public MaterialLibrary(string path)
	{
		using StreamReader reader = new StreamReader(File.OpenRead(path));

		materials = new Dictionary<string, Material>();
		List<TextureLoadOperation> operations = new();

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
				var typeName = new string(Eat(ref line));
				Type type = Type.GetType($"Echo.Rendering.Materials.{typeName}");

				if (type != null) processing = (Material)Activator.CreateInstance(type);
				else throw new Exception($"Invalid material type name: {typeName}");

				var name = new string(Eat(ref line));

				if (!materials.ContainsKey(name)) materials.Add(name, processing);
				else throw new Exception($"Duplicated material name: {name}");
			}
			else if (token.SequenceEqual("define")) { }
			else
			{
				//Assigning material attribute
				if (processing == null) throw new Exception("Assigning attributes before declaring a material!");
				PropertyInfo info = processing.GetType().GetProperty(new string(token));

				object parsed = Parse(ref line);

				if (parsed is TextureLoadOperation operation)
				{
					operation.Property = info;
					operation.Target = processing;

					operations.Add(operation);
					parsed = Pure.white;
				}

				if (info == null) throw new Exception($"No attribute named: {token.ToString()}!");
				if (!info.PropertyType.IsInstanceOfType(parsed)) throw new Exception($"Invalid input type to attribute {info}!");

				info.SetValue(processing, parsed);
			}
		}

		Parallel.ForEach(operations, operation => operation.Operate());
		first = materials.FirstOrDefault().Value;

		object Parse(ref ReadOnlySpan<char> line)
		{
			ReadOnlySpan<char> piece0 = Eat(ref line);
			ReadOnlySpan<char> piece1 = Eat(ref line);
			ReadOnlySpan<char> piece2 = Eat(ref line);

			if (piece0.IsEmpty) throw new Exception("Cannot set attribute with empty parameters!");
			if (!float.TryParse(piece0, NumberStyles.Any, CultureInfo.InvariantCulture, out float float0))
			{
				if (bool.TryParse(piece0, out bool boolean)) return boolean;
				return new TextureLoadOperation(GetSiblingPath(path, new string(piece0.Trim('"'))));
			}

			if (!float.TryParse(piece1, NumberStyles.Any, CultureInfo.InvariantCulture, out float float1)) return float0;
			if (!float.TryParse(piece2, NumberStyles.Any, CultureInfo.InvariantCulture, out float float2)) return new Float2(float0, float1);

			return new Float3(float0, float1, float2);
		}
	}

	public MaterialLibrary(Material first = null)
	{
		this.first = first;
		materials = new Dictionary<string, Material>();
	}

	static readonly ReadOnlyCollection<string> acceptableFileExtensions = new(new[] { ".mat" });

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

			if (char.IsWhiteSpace(current) && quote % 2 == 0)
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

	static string GetSiblingPath(string path, string sibling)
	{
		var directory = Path.GetDirectoryName(path);
		return Path.Combine(directory ?? "", sibling);
	}

	class TextureLoadOperation
	{
		public TextureLoadOperation(string path) => this.path = path;

		readonly string path;

		public PropertyInfo Property { get; set; }
		public Material Target { get; set; }

		public void Operate()
		{
			Serializer serializer = null;

			if (Property.Name.Contains("normal", StringComparison.InvariantCultureIgnoreCase))
			{
				serializer = Serializer.Find(path); //Special case for normal maps to not use sRGB
				serializer = serializer with { sRGB = false };
			}

			Property.SetValue(Target, TextureGrid.Load<RGB128>(path, serializer));
		}
	}
}