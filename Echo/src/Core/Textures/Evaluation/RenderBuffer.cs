using System;
using System.Collections.Generic;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Packed;
using Echo.Core.Evaluation.Operations;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grid;

namespace Echo.Core.Textures.Evaluation;

/// <summary>
/// A collection of layers of <see cref="ArrayGrid{T}"/> primarily used as a rendering destination.
/// Allows for optional auxiliary evaluation layers (such as normal) to support later reconstruction.
/// </summary>
public class RenderBuffer : ArrayGrid<RGB128>
{
	public RenderBuffer(Int2 size) : base(size) => AddLayer("main", this);

	readonly Dictionary<string, Layer> layers = new(StringComparer.OrdinalIgnoreCase);

	/// <inheritdoc cref="TryGetTexture"/>
	public bool TryGetTexture<T>(string label, out TextureGrid<T> texture) where T : unmanaged, IColor<T>
	{
		if (TryGetTexture(label, out Texture candidate))
		{
			texture = candidate as TextureGrid<T>;
			return texture != null;
		}

		texture = null;
		return false;
	}

	/// <summary>
	/// Tries to get a layer <see cref="Texture"/> from this <see cref="RenderBuffer"/>.
	/// </summary>
	/// <param name="label">The label of the layer to find.</param>
	/// <param name="texture">Outputs the texture if found.</param>
	/// <returns>Whether a matching texture is found.</returns>
	public bool TryGetTexture(string label, out Texture texture)
	{
		texture = layers.TryGetValue(label).texture;
		return texture != null;
	}

	/// <summary>
	/// Tries to get a layer <see cref="Writer"/> from this <see cref="RenderBuffer"/>.
	/// </summary>
	/// <param name="label">The label of the layer to find.</param>
	/// <param name="writer">Outputs the <see cref="Writer"/> if found.</param>
	/// <returns>Whether a matching <see cref="Writer"/> is found.</returns>
	/// <seealso cref="Writer"/>
	public bool TryGetWriter(string label, out Writer writer)
	{
		writer = layers.TryGetValue(label).writer;
		return writer != null;
	}

	/// <summary>
	/// Creates a new layer.
	/// </summary>
	/// <param name="label">The <see cref="string"/> to name this new
	/// layer. This <see cref="string"/> is case insensitive.</param>
	public void CreateLayer<T>(string label) where T : unmanaged, IColor<T>
	{
		if (!layers.ContainsKey(label)) AddLayer(label, new ArrayGrid<T>(size));
		else throw ExceptionHelper.Invalid(nameof(label), label, InvalidType.foundDuplicate);
	}

	public override void CopyFrom(Texture texture)
	{
		base.CopyFrom(texture);

		if (texture is RenderBuffer buffer)
		{
			foreach (var pair in layers)
			{
				if (pair.Value.texture == this) continue;

				var source = buffer.layers.TryGetValue(pair.Key).texture;
				if (source != null) pair.Value.texture.CopyFrom(source);
			}
		}
		else
		{
			foreach (var pair in layers)
			{
				if (pair.Value.texture == this) continue;
				pair.Value.texture.CopyFrom(texture);
			}
		}
	}

	/// <summary>
	/// Completely empties the content of all of the layers and buffers in this <see cref="RenderBuffer"/>.
	/// </summary>
	public virtual void Clear() => CopyFrom(black);

	void AddLayer<T>(string label, TextureGrid<T> texture) where T : unmanaged, IColor<T>
	{
		layers.Add(label, new Layer(texture, Write));

		void Write(Int2 position, in Accumulator accumulator) => texture[position] = default(T).FromFloat4(accumulator.Value);
	}

	/// <summary>
	/// Delegate used to write to a specific layer in a <see cref="RenderBuffer"/>.
	/// </summary>
	/// <param name="position">The position to write. This is the same as the <see cref="TextureGrid{T}.Item(Int2)"/> indexer.</param>
	/// <param name="accumulator">The value to write is exacted from this <see cref="Accumulator"/>.</param>
	public delegate void Writer(Int2 position, in Accumulator accumulator);

	readonly record struct Layer(Texture texture, Writer writer)
	{
		public readonly Texture texture = texture;
		public readonly Writer writer = writer;
	}
}