using System;
using System.Collections.Concurrent;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grid;

namespace Echo.Core.Textures.Evaluation;

/// <summary>
/// A collection of layers of <see cref="TextureGrid{T}"/> used as a rendering source or destination.
/// Allows for optional auxiliary evaluation layers (such as normal) to support later reconstruction.
/// </summary>
public sealed class RenderBuffer : TextureGrid<RGB128>
{
	public RenderBuffer(Int2 size, int tileSize = 32) : this(size, (Int2)tileSize) { }

	public RenderBuffer(Int2 size, Int2 tileSize) : base(size) => this.tileSize = tileSize;

	public readonly Int2 tileSize;

	Texture mainTexture;

	readonly ConcurrentDictionary<string, Texture> layers = new(StringComparer.OrdinalIgnoreCase);

	public override RGB128 this[Int2 position]
	{
		get
		{
			if (mainTexture == null) return RGB128.Black;
			return mainTexture[ToUV(position)].As<RGB128>();
		}
	}

	/// <summary>
	/// Tries to get a layer from this <see cref="RenderBuffer"/>.
	/// </summary>
	/// <param name="label">The label of the layer to find.</param>
	/// <param name="layer">Outputs the layer if found.</param>
	/// <returns>Whether a matching texture is found.</returns>
	public bool TryGetTexture<T, U>(string label, out U layer) where T : unmanaged, IColor<T>
															   where U : TextureGrid<T>
	{
		if (TryGetTexture(label, out Texture candidate))
		{
			layer = candidate as U;
			return layer != null;
		}

		layer = null;
		return false;
	}

	/// <inheritdoc cref="TryGetTexture{T, U}"/>
	/// <remarks>Since the second parameter of this method outputs a <see cref="Texture"/>,
	/// this method will always find a <see cref="Texture"/> if its label exist.</remarks>
	public bool TryGetTexture(string label, out Texture layer)
	{
		layer = layers.TryGetValue(label);
		return layer != null;
	}

	/// <summary>
	/// Creates a new layer to this <see cref="RenderBuffer"/>.
	/// </summary>
	/// <param name="label">The <see cref="string"/> to name this new
	/// layer. This <see cref="string"/> is case insensitive.</param>
	/// <returns>The new <see cref="TiledEvaluationLayer{T}"/> that was created.</returns>
	public TiledEvaluationLayer<T> CreateLayer<T>(string label) where T : unmanaged, IColor<T>
	{
		if (!layers.ContainsKey(label))
		{
			var layer = new TiledEvaluationLayer<T>(size, tileSize);
			Interlocked.CompareExchange(ref mainTexture, layer, null);
			if (layers.TryAdd(label, layer)) return layer;
		}

		throw ExceptionHelper.Invalid(nameof(label), label, InvalidType.foundDuplicate);
	}

	/// <summary>
	/// Adds a new layer to this <see cref="RenderBuffer"/>.
	/// </summary>
	/// <param name="label">The <see cref="string"/> to name this new
	/// layer. This <see cref="string"/> is case insensitive.</param>
	/// <param name="layer">The layer to add.</param>
	public void AddLayer<T>(string label, Texture layer)
	{
		if (layers.TryAdd(label, layer)) Interlocked.CompareExchange(ref mainTexture, layer, null);
		else throw ExceptionHelper.Invalid(nameof(label), label, InvalidType.foundDuplicate);
	}
}