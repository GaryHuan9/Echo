using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Textures.Evaluation;

/// <summary>
/// A collection of layers of <see cref="TextureGrid{T}"/> used as a rendering source or destination.
/// Allows for optional auxiliary evaluation layers (such as normal) to support later reconstruction.
/// </summary>
/// <remarks>The content of this texture will be forwarded from the first <see cref="TextureGrid"/> that was added, or a layer named 'main' if it exists.</remarks>
public sealed class RenderTexture : TextureGrid<RGB128>
{
	public RenderTexture(Int2 size, int tileSize = 16) : this(size, (Int2)tileSize) { }

	public RenderTexture(Int2 size, Int2 tileSize) : base(size)
	{
		if (!BitOperations.IsPow2(tileSize.X) || !BitOperations.IsPow2(tileSize.Y)) throw new ArgumentOutOfRangeException(nameof(tileSize));

		this.tileSize = tileSize;
	}

	/// <summary>
	/// The <see cref="EvaluationLayer{T}.tileSize"/> used when creating layers through <see cref="CreateLayer{T}"/>.
	/// </summary>
	public readonly Int2 tileSize;

	/// <summary>
	/// The first texture that was added, or a layer named 'main'.
	/// </summary>
	TextureGrid mainTexture;

	readonly ConcurrentDictionary<string, TextureGrid> layers = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Returns an enumerable through all layers of this <see cref="RenderTexture"/>. 
	/// </summary>
	public IReadOnlyDictionary<string, TextureGrid> Layers => layers;

	public override RGB128 Get(Int2 position)
	{
		var main = mainTexture; //Non volatile read for better performance, probably is not a problem
		if (main == null) return RGB128.Black;
		return (RGB128)main[position];
	}

	/// <summary>
	/// Tries to get a layer from this <see cref="RenderTexture"/>.
	/// </summary>
	/// <param name="name">The name of the layer to find.</param>
	/// <param name="layer">Outputs the layer if found.</param>
	/// <returns>Whether a matching texture is found.</returns>
	/// <remarks>Since the second parameter of this method outputs a <see cref="TextureGrid"/>,
	/// this method will always find a <see cref="TextureGrid"/> if its name exist.</remarks>
	public bool TryGetLayer(string name, out TextureGrid layer) => layers.TryGetValue(name, out layer);

	/// <summary>
	/// Tries to get the first layer from this <see cref="RenderTexture"/> that contains a specific type of <see cref="IColor{T}"/>.
	/// </summary>
	/// <param name="layer">Outputs the layer if found.</param>
	/// <typeparam name="T">The type of <see cref="IColor{T}"/> to look for.</typeparam>
	/// <returns>Whether a matching texture is found.</returns>
	public bool TryGetLayer<T>(out TextureGrid<T> layer) where T : unmanaged, IColor<T>
	{
		foreach ((_, TextureGrid candidate) in layers)
		{
			layer = candidate as TextureGrid<T>;
			if (layer != null) return true;
		}

		layer = default;
		return false;
	}

	/// <summary>
	/// Adds a new layer to this <see cref="RenderTexture"/>.
	/// </summary>
	/// <param name="name">The <see cref="string"/> to name this new
	/// layer. This <see cref="string"/> is case insensitive.</param>
	/// <param name="layer">The layer to add.</param>
	/// <returns>Whether a new layer was added.</returns>
	public bool TryAddLayer(string name, TextureGrid layer)
	{
		if (layer.size != size) throw new ArgumentException($"Mismatched size '{layer.size}' with this {nameof(RenderTexture)}.", nameof(layer));

		if (!layers.TryAdd(name, layer)) return false;
		if (name == "main") Volatile.Write(ref mainTexture, layer);
		else Interlocked.CompareExchange(ref mainTexture, layer, null);

		return true;
	}

	/// <summary>
	/// Creates a new layer to this <see cref="RenderTexture"/>.
	/// </summary>
	/// <param name="name">The <see cref="string"/> to name this new
	/// layer. This <see cref="string"/> is case insensitive.</param>
	/// <returns>The new <see cref="EvaluationLayer{T}"/> that was created.</returns>
	/// <exception cref="ArgumentException">Thrown if a layer named <paramref name="name"/> already exists.</exception>
	public EvaluationLayer<T> CreateLayer<T>(string name) where T : unmanaged, IColor<T>
	{
		if (!layers.ContainsKey(name))
		{
			var layer = new EvaluationLayer<T>(size, tileSize);
			if (TryAddLayer(name, layer)) return layer;
		}

		throw new ArgumentException($"A layer named '{name}' already exists.");
	}
}