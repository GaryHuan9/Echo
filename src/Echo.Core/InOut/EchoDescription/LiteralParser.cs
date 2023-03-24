using System;
using System.Collections.Generic;
using System.IO;
using Echo.Core.Common;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.Images;
using Echo.Core.InOut.Models;
using Echo.Core.Scenic.Geometries;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.InOut.EchoDescription;

using CharSpan = ReadOnlySpan<char>;

static class LiteralParser
{
	static LiteralParser()
	{
		AddTryParser<bool>(bool.TryParse);
		AddTryParser<float>(InvariantFormat.TryParse);
		AddTryParser<int>(InvariantFormat.TryParse);
		AddTryParser<uint>(InvariantFormat.TryParse);

		AddTryParser<Float2>(InvariantFormat.TryParse);
		AddTryParser<Float3>(InvariantFormat.TryParse);
		AddTryParser<Float4>(InvariantFormat.TryParse);
		AddTryParser<Int2>(InvariantFormat.TryParse);
		AddTryParser<Int3>(InvariantFormat.TryParse);
		AddTryParser<Int4>(InvariantFormat.TryParse);

		AddTryParser<RGBA128>(RGBA128.TryParse);
		AddTryParser<RGB128>(RGBA128.TryParse);

		//We need a more sophisticated parser for different texture import options based on the string syntax
		//For now though, TextureGrid uses RGB128 without the alpha since only TextureManage actually use it
		AddPathTryParser<Texture>(path => TextureGrid.Load<RGBA128>(path));
		AddPathTryParser<TextureGrid>(path => TextureGrid.Load<RGB128>(path));

		AddPathTryParser<ITriangleSource>(path => new FileTriangleSource(path));
		AddParser(span => span);

		void AddTryParser<T>(TryParser<T> source) => parsers.Add
		(
			typeof(T), (TryParser<object>)((ReadOnlySpan<char> span, out object result) =>
			{
				bool success = source(span, out T original);
				result = original;
				return success;
			})
		);

		void AddPathTryParser<T>(Func<string, T> source) => parsers.Add(typeof(T), (PathParser<object>)(path => source(Path.GetFullPath(path))));

		void AddParser<T>(Parser<T> source) => parsers.Add(typeof(T), (Parser<object>)(span => source(span)));
	}

	static readonly Dictionary<Type, object> parsers = new();

	public static object TryGetParser(Type type) => parsers.TryGetValue(type);

	// ReSharper disable TypeParameterCanBeVariant

	public delegate bool TryParser<T>(CharSpan span, out T result);
	public delegate T PathParser<T>(string path);
	public delegate T Parser<T>(string span);

	// ReSharper restore TypeParameterCanBeVariant
}