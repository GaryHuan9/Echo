using System;
using System.Collections.Concurrent;
using System.Numerics;
using System.Threading;
using Echo.Core.Common;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Textures.Evaluation;

/// <summary>
/// A collection of layers of <see cref="TextureGrid{T}"/> used as a rendering source or destination.
/// Allows for optional auxiliary evaluation layers (such as normal) to support later reconstruction.
/// </summary>
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
	/// Tries to get a layer from this <see cref="RenderTexture"/>.
	/// </summary>
	/// <param name="label">The label of the layer to find.</param>
	/// <param name="layer">Outputs the layer if found.</param>
	/// <returns>Whether a matching texture is found.</returns>
	public bool TryGetLayer<T, U>(string label, out U layer) where T : unmanaged, IColor<T>
															 where U : TextureGrid<T>
	{
		if (TryGetLayer(label, out Texture candidate))
		{
			layer = candidate as U;
			return layer != null;
		}

		layer = null;
		return false;
	}

	/// <inheritdoc cref="TryGetLayer{T,U}"/>
	/// <remarks>Since the second parameter of this method outputs a <see cref="Texture"/>,
	/// this method will always find a <see cref="Texture"/> if its label exist.</remarks>
	public bool TryGetLayer(string label, out Texture layer)
	{
		layer = layers.TryGetValue(label);
		return layer != null;
	}

	/// <summary>
	/// Creates a new layer to this <see cref="RenderTexture"/>.
	/// </summary>
	/// <param name="label">The <see cref="string"/> to name this new
	/// layer. This <see cref="string"/> is case insensitive.</param>
	/// <returns>The new <see cref="EvaluationLayer{T}"/> that was created.</returns>
	/// <exception cref="ArgumentException">Thrown if a layer named <paramref name="label"/> already exists.</exception>
	public EvaluationLayer<T> CreateLayer<T>(string label) where T : unmanaged, IColor<T>
	{
		if (!layers.ContainsKey(label))
		{
			var layer = new EvaluationLayer<T>(size, tileSize);
			if (TryAddLayer(label, layer)) return layer;
		}

		throw new ArgumentException($"A layer named '{label}' already exists.");
	}

	/// <summary>
	/// Adds a new layer to this <see cref="RenderTexture"/>.
	/// </summary>
	/// <param name="label">The <see cref="string"/> to name this new
	/// layer. This <see cref="string"/> is case insensitive.</param>
	/// <param name="layer">The layer to add.</param>
	/// <returns>Whether a new layer was added.</returns>
	public bool TryAddLayer(string label, Texture layer)
	{
		if (!layers.TryAdd(label, layer)) return false;
		Interlocked.CompareExchange(ref mainTexture, layer, null);
		return true;
	}
}