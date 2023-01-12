using System;
using System.IO;

namespace Echo.Core.InOut.EchoDescription;

/// <summary>
/// A Echo Chronicle Hierarchy Objects (ECHO) source. Yes I am very smart.
/// </summary>
/// <remarks>
/// All of the 'Echo' under this namespace (including the namespace name) refers to the acronym.
/// </remarks>
public sealed class EchoSource
{
	public EchoSource(Stream stream) : this(stream, Environment.CurrentDirectory) { }

	public EchoSource(string path) : this(File.OpenRead(path), Path.GetDirectoryName(path)) { }

	public EchoSource(Stream stream, string currentDirectory)
	{
		this.currentDirectory = currentDirectory;
		using SegmentReader reader = new(stream);

		root = RootNode.Create(reader);
		Length = root.Children.Length;
	}

	/// <summary>
	/// The directory at which paths will be referenced from.
	/// </summary>
	public readonly string currentDirectory;

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

	/// <summary>
	/// Constructs the first defined object of type <typeparamref name="T"/>.
	/// </summary>
	/// <returns>The constructed object of type <typeparamref name="T"/> if found, null otherwise.</returns>
	public T ConstructFirst<T>() where T : class => ConstructFirst<T>(out _);

	/// <summary>
	/// Constructs the first defined object of type <typeparamref name="T"/>.
	/// </summary>
	/// <param name="identifier">Outputs the <see cref="string"/> identifier of the constructed object.</param>
	/// <returns>The constructed object of type <typeparamref name="T"/> if found, null otherwise.</returns>
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

	/// <summary>
	/// Constructs a labeled symbol of type <typeparamref name="T"/>.
	/// </summary>
	/// <param name="identifier">A <see cref="string"/> identifier specifying the object to construct.</param>
	/// <returns>The constructed object of type <typeparamref name="T"/> if found, null otherwise.</returns>
	public T Construct<T>(string identifier) where T : class
	{
		for (int i = 0; i < Length; i++)
		{
			Entry entry = this[i];
			if (entry.Identifier != identifier) continue;

			T constructed = entry.Construct<T>();
			if (constructed == null) continue;
			return constructed;
		}

		return null;
	}

	public readonly ref struct Entry
	{
		internal Entry(EchoSource objects, int index)
		{
			this.objects = objects;
			identifiedNode = objects.root.Children[index];
		}

		readonly EchoSource objects;
		readonly Identified<TypedNode> identifiedNode;

		public string Identifier => identifiedNode.identifier;

		public Type Type => identifiedNode.node.GetConstructType();

		public T Construct<T>() where T : class
		{
			if (!Type.IsAssignableTo(typeof(T))) return null;
			return (T)identifiedNode.node.Construct(objects);
		}
	}
}