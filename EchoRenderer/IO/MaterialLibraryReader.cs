using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EchoRenderer.IO
{
	public class MaterialLibraryReader : IDisposable
	{
		public MaterialLibraryReader(string path)
		{
			reader = new StreamReader(File.OpenRead(path));
			library = new MaterialLibrary();
		}

		static MaterialLibraryReader()
		{
			tagActions = new ITag[] {new KeywordTag(), new MaterialTag()}.ToDictionary(tag => tag.Tag, tag => tag);
			processorActions = new IProcessor[] {new MaterialProcessor(), new ShaderProcessor()}.ToDictionary(processor => processor.GetType(), processor => processor);
		}

		readonly StreamReader reader;
		readonly MaterialLibrary library;

		readonly Dictionary<string, object> keywords = new();

		static readonly IReadOnlyDictionary<string, ITag> tagActions;
		static readonly IReadOnlyDictionary<Type, IProcessor> processorActions;

		public MaterialLibrary Load()
		{
			for (int height = 0;; height++)
			{
				Line line = new Line(reader.ReadLine(), height);

				Preprocess(ref line);
				if (line.IsEmpty) break;

				Line identifier = Eat(ref line);

				//TODO
			}

			return library;
		}

		public void Dispose()
		{
			reader?.Dispose();
		}

		static void Preprocess(ref Line line)
		{
			ReadOnlySpan<char> span = line;
			int end = span.IndexOf('#');
			if (end >= 0) line = line[..end];

			line = line.Trim();
		}

		static Line Eat(ref Line line)
		{
			int index = -1;
			bool quote = false;

			for (int i = 0; i < line.length; i++)
			{
				char current = line[i];

				if (current == '"')
				{
					if (quote)
					{
						index = i + 1;
						break;
					}

					quote = true;
				}

				if (current == ' ' && !quote)
				{
					index = i;
					break;
				}
			}

			Line result;

			if (index < 0)
			{
				result = line;
				line = default;
			}
			else
			{
				result = line[..index];
				line = line[index..].Trim();
			}

			if (quote) result = result.Trim('\"');
			return result;
		}

		interface IOperator
		{
			void Operate(MaterialLibraryReader reader, Line line);
		}

		interface ITag : IOperator
		{
			string Tag { get; }
		}

		interface IProcessor : IOperator { }

		class KeywordTag : ITag
		{
			public string Tag => "Keyword";

			public void Operate(MaterialLibraryReader reader, Line line)
			{
				Line keyword = Eat(ref line);

				if (keyword.IsEmpty) throw new Exception($"Invalid keyword '{keyword}' on line '{line}'!");
				if (reader.keywords.ContainsKey(keyword)) throw new Exception($"Duplicated keyword '{keyword}' on line '{line}'!");
			}
		}

		class MaterialTag : ITag
		{
			public string Tag => "Material";

			public void Operate(MaterialLibraryReader reader, Line line) { }
		}

		class MaterialProcessor : IProcessor
		{
			public void Operate(MaterialLibraryReader reader, Line line)
			{
				throw new NotImplementedException();
			}
		}

		class ShaderProcessor : IProcessor
		{
			public void Operate(MaterialLibraryReader reader, Line line)
			{
				throw new NotImplementedException();
			}
		}
	}
}