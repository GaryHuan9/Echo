using System;
using System.IO;

namespace Echo.Core.InOut.Scenes;

public sealed partial class EchoChronicleHierarchyObjects
{
	public EchoChronicleHierarchyObjects(string path)
	{
		directory = Path.GetDirectoryName(path);
		typeMap = TypeMap.Instance;

		using SegmentReader reader = new(File.OpenRead(path));

		root = RootNode.Create(reader);
		Length = root.Children.Length;
	}

	readonly string directory;
	readonly TypeMap typeMap;
	readonly RootNode root;

	public int Length { get; }

	public Entry this[int index]
	{
		get
		{
			if ((uint)index < Length) return new Entry(this, index);
			throw new ArgumentOutOfRangeException(nameof(index));
		}
	}

	public T ConstructFirst<T>() where T : class => ConstructFirst<T>(out _);

	public T ConstructFirst<T>(out string identifier) where T : class
	{
		for (int i = 0; i < Length; i++)
		{
			Entry entry = this[i];
			T constructed = entry.Construct<T>();
			if (constructed == null) continue;

			identifier = entry.Identifier;
			return constructed;
		}

		identifier = default;
		return null;
	}

	static bool EqualsSingle(ReadOnlySpan<char> span, char target) => span.Length == 1 && span[0] == target;

	public readonly ref struct Entry
	{
		internal Entry(EchoChronicleHierarchyObjects objects, int index)
		{
			this.objects = objects;
			identifiedNode = objects.root.Children[index];
		}

		readonly EchoChronicleHierarchyObjects objects;
		readonly Identified<TypedNode> identifiedNode;

		public string Identifier => identifiedNode.identifier;

		public Type Type => identifiedNode.node.GetType(objects);

		public T Construct<T>() where T : class
		{
			if (!Type.IsAssignableFrom(typeof(T))) return null;
			return (T)identifiedNode.node.Construct(objects);
		}
	}
}