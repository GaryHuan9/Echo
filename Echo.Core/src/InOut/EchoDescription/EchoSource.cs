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

	public EchoSource(string path) : this(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Path.GetDirectoryName(path)) { }

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

	/// <inheritdoc cref="ConstructFirst{T}()"/>
	/// <param name="identifier">Outputs the <see cref="string"/> identifier of the constructed object.</param>
	public T ConstructFirst<T>(out string identifier) where T : class
	{
		identifier = default;
		int index = IndexOf<T>();
		if (index < 0) return null;

		Entry entry = this[index];
		identifier = entry.Identifier;
		return entry.Construct<T>();
	}

	/// <summary>
	/// Returns the index of the first defined object of type <typeparamref name="T"/>,
	/// or if no matching object is found, a negative number is returned.
	/// </summary>
	public int IndexOf<T>() where T : class
	{
		for (int i = 0; i < Length; i++)
		{
			if (this[i].CanConstructAs<T>()) return i;
		}

		return -1;
	}

	/// <inheritdoc cref="IndexOf{T}()"/>
	/// <param name="identifier">A <see cref="string"/> identifier specifying the object to construct.</param>
	public int IndexOf<T>(string identifier) where T : class
	{
		for (int i = 0; i < Length; i++)
		{
			Entry entry = this[i];
			if (entry.Identifier != identifier) continue;
			if (entry.CanConstructAs<T>()) return i;
		}

		return -1;
	}

	public readonly struct Entry
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

		public bool CanConstructAs<T>() where T : class => Type.IsAssignableTo(typeof(T));

		public T Construct<T>() where T : class => CanConstructAs<T>() ? (T)identifiedNode.node.Construct(objects) : null;
	}
}