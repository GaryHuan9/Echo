using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Packed;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures.Grid;

/// <summary>
/// A <see cref="ArrayGrid{T}"/> of type <see cref="RGB128"/> primarily used as a rendering destination.
/// Allows for optional auxiliary layers for data such as albedo or normal support later reconstruction.
/// </summary>
public class RenderBuffer : ArrayGrid<RGB128>
{
	public RenderBuffer(Int2 size) : base(size) => layers.Add("main", this);

	readonly Dictionary<string, Texture> layers = new(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Tries to access the <see cref="TextureGrid{T}"/> buffer layer named <paramref name="label"/> for type
	/// <typeparamref name="T"/> and outputs it to <paramref name="layer"/>. Returns whether the operation succeeded.
	/// </summary>
	public bool TryGetLayer<T>(string label, out TextureGrid<T> layer) where T : IColor<T>
	{
		if (layers.TryGetValue(label, out Texture candidate))
		{
			layer = candidate as TextureGrid<T>;
			return layer != null;
		}

		layer = null;
		return false;
	}

	/// <summary>
	/// Creates a new layer named <paramref name="label"/>.
	/// </summary>
	public void CreateLayer<T>(string label) where T : IColor<T>
	{
		if (!layers.ContainsKey(label)) layers.Add(label, new ArrayGrid<T>(size));
		else throw ExceptionHelper.Invalid(nameof(label), label, InvalidType.foundDuplicate);
	}

	public override void CopyFrom(Texture texture)
	{
		base.CopyFrom(texture);

		if (texture is RenderBuffer buffer)
		{
			Parallel.ForEach(layers, pair =>
			{
				if (pair.Value == this) return;

				var source = buffer.layers.TryGetValue(pair.Key);
				if (source != null) pair.Value.CopyFrom(source);
			});
		}
		else
		{
			Parallel.ForEach(layers, pair =>
			{
				if (pair.Value == this) return;
				pair.Value.CopyFrom(texture);
			});
		}
	}

	/// <summary>
	/// Completely empties the content of all of the layers and buffers in this <see cref="RenderBuffer"/>.
	/// </summary>
	public virtual void Clear() => CopyFrom(black);

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
				object array = arrayFieldInfo.GetValue(pair.Value);
				var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
				return new Bundle(handle.AddrOfPinnedObject(), handle);
			}, StringComparer.InvariantCultureIgnoreCase);
		}

		static Pin() => arrayFieldInfo = typeof(ArrayGrid<>).GetField(nameof(pixels), BindingFlags.Public | BindingFlags.NonPublic);

		readonly Dictionary<string, Bundle> layers;
		static readonly FieldInfo arrayFieldInfo;

		public IntPtr this[string label] => layers[label].pointer;

		public void Dispose()
		{
			foreach (var pair in layers) pair.Value.handle.Free();
			layers.Clear();
		}

		struct Bundle
		{
			public Bundle(IntPtr pointer, GCHandle handle)
			{
				this.pointer = pointer;
				this.handle = handle;
			}

			public readonly IntPtr pointer;
			public readonly GCHandle handle;
		}
	}
}