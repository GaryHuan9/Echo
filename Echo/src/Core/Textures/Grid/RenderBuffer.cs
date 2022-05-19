using System;
using System.Collections.Generic;
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
	public bool TryGetLayer<T>(string label, out TextureGrid<T> layer) where T : unmanaged, IColor<T>
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
	public void CreateLayer<T>(string label) where T : unmanaged, IColor<T>
	{
		if (!layers.ContainsKey(label)) layers.Add(label, new ArrayGrid<T>(size));
		else throw ExceptionHelper.Invalid(nameof(label), label, InvalidType.foundDuplicate);
	}

	public override void CopyFrom(Texture texture)
	{
		base.CopyFrom(texture);

		if (texture is RenderBuffer buffer)
		{
			foreach (var pair in layers)
			{
				if (pair.Value == this) continue;

				var source = buffer.layers.TryGetValue(pair.Key);
				if (source != null) pair.Value.CopyFrom(source);
			}
		}
		else
		{
			foreach (var pair in layers)
			{
				if (pair.Value == this) continue;
				pair.Value.CopyFrom(texture);
			}
		}
	}

	/// <summary>
	/// Completely empties the content of all of the layers and buffers in this <see cref="RenderBuffer"/>.
	/// </summary>
	public virtual void Clear() => CopyFrom(black);
}