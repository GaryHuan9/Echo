using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Packed;
using EchoRenderer.Common.Coloring;

namespace EchoRenderer.Core.Texturing.Grid;

/// <summary>
/// A <see cref="ArrayGrid{T}"/> of type <see cref="RGB128"/> primarily used as a rendering destination.
/// Allows for optional auxiliary layers for data such as albedo, normal, or depth to support later reconstruction.
/// </summary>
public class RenderBuffer : ArrayGrid<RGB128>
{
	public RenderBuffer(Int2 size) : base(size) => layers.Add("main", this);

	readonly Dictionary<string, ArrayGrid<RGB128>> layers = new(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Accesses the <see cref="ArrayGrid{T}"/> buffer layer named <paramref name="label"/>.
	/// Exception is thrown if <paramref name="label"/> is not the name of any layer.
	/// </summary>
	public TextureGrid<RGB128> this[string label] => layers[label];

	/// <summary>
	/// Creates a new layer named <paramref name="label"/>.
	/// </summary>
	public void CreateLayer(string label)
	{
		if (!layers.ContainsKey(label)) layers.Add(label, new ArrayGrid<RGB128>(size));
		else throw ExceptionHelper.Invalid(nameof(label), label, InvalidType.foundDuplicate);
	}

	/// <summary>
	/// Returns whether this <see cref="RenderBuffer"/> contains a buffer named <paramref name="label"/>.
	/// </summary>
	/// <param name="label"></param>
	/// <returns></returns>
	public bool HasLayer(string label) => layers.ContainsKey(label);

	public override void CopyFrom(Texture texture)
	{
		base.CopyFrom(texture);

		if (texture is not RenderBuffer buffer) return;

		Parallel.ForEach(layers, pair =>
		{
			var source = buffer.layers.TryGetValue(pair.Key);
			if (source != null) pair.Value.CopyFrom(source);
		});
	}

	/// <summary>
	/// Completely empties the content of all of the layers of this <see cref="RenderBuffer"/>.
	/// </summary>
	public virtual void Clear() => Parallel.ForEach(layers, pair => Array.Clear(AccessArray(pair.Value)));

	/// <summary>
	/// Pins this <see cref="RenderBuffer"/> for various unmanaged access or pointer shenanigans.
	/// NOTE: Remember to dispose <see cref="Pin"/> after use to unpin the buffers and handles.
	/// </summary>
	public Pin CreatePin() => new(this);

	public sealed class Pin : IDisposable
	{
		/// <summary>
		/// Pins data inside <paramref name="buffer"/> for unmanaged use and holds pins until disposed.
		/// </summary>
		public Pin(RenderBuffer buffer)
		{
			layers = buffer.layers.ToDictionary(pair => pair.Key, pair =>
			{
				RGB128[] array = AccessArray(pair.Value);
				var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
				return (handle.AddrOfPinnedObject(), handle);
			}, StringComparer.InvariantCultureIgnoreCase);
		}

		readonly Dictionary<string, (IntPtr pointer, GCHandle handle)> layers;

		public IntPtr this[string label] => layers[label].pointer;

		public void Dispose()
		{
			foreach (var pair in layers) pair.Value.handle.Free();
			layers.Clear();
		}
	}
}