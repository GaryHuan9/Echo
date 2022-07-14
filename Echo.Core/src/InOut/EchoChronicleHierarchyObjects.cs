using System;
using System.IO;

namespace Echo.Core.InOut;

using CharSpan = ReadOnlySpan<char>;

public sealed partial class EchoChronicleHierarchyObjects
{
	public EchoChronicleHierarchyObjects(string path)
	{
		this.path = path;
		typeMap = TypeMap.Instance;

		using SegmentReader reader = new(File.OpenRead(path));

		root = RootNode.Create(reader);
		Length = root.Children.Length;
	}

	readonly string path;
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

		public Type Type => identifiedNode.node.GetType(objects.typeMap);

		public T Construct<T>() where T : class => identifiedNode.node.Construct(objects) as T;
	}
}