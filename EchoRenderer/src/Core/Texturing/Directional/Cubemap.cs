using System;
using System.IO;
using System.Runtime.Intrinsics;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.Texturing.Grid;

namespace EchoRenderer.Core.Texturing.Directional;

public class Cubemap : IDirectionalTexture
{
	public Cubemap()
	{
		int length = names.Length;

		textures = new NotNull<Texture>[length];
		for (int i = 0; i < length; i++) textures[i] = Texture.black;
	}

	public Cubemap(ReadOnlySpan<Texture> textures)
	{
		int length = names.Length;

		int min = Math.Min(length, textures.Length);
		this.textures = new NotNull<Texture>[length];

		for (int i = 0; i < length; i++) this.textures[i] = i < min ? textures[i] : Texture.black;
	}

	public Cubemap(string path)
	{
		int length = names.Length;

		var tasks = new Task<ArrayGrid>[length];
		textures = new NotNull<Texture>[length];

		for (int i = 0; i < length; i++)
		{
			string fullPath = Path.Combine(path, names[i]);
			tasks[i] = TextureGrid.LoadAsync(fullPath);
		}

		for (int i = 0; i < length; i++) textures[i] = tasks[i].Result;
	}

	readonly NotNull<Texture>[] textures;

	/// <summary>
	/// Accesses the <see cref="Texture"/> at a specific <paramref name="direction"/>.
	/// </summary>
	public Texture this[Direction direction]
	{
		get => this[direction.Index];
		set => this[direction.Index] = value;
	}

	public Tint Tint
	{
		set
		{
			foreach (var texture in textures) texture.Value.Tint = value;
		}
	}

	public RGBA128 Average { get; private set; }

	Texture this[int index]
	{
		get => textures[index];
		set => textures[index] = value;
	}

	static readonly string[] names = { "px", "nx", "py", "ny", "pz", "nz" };

	/// <inheritdoc/>
	public void Prepare() => Average = this.ConvergeAverage();

	/// <inheritdoc/>
	public RGBA128 Evaluate(in Float3 direction)
	{
		Direction target = (Direction)direction;

		int index = target.Index;
		Float2 uv = index switch
		{
			0 => new Float2(-direction.Z, direction.Y),
			1 => direction.ZY,
			2 => new Float2(direction.X, -direction.Z),
			3 => direction.XZ,
			4 => direction.XY,
			5 => new Float2(-direction.X, direction.Y),
			_ => throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.unexpected)
		};

		uv /= target.ExtractComponent(direction);
		return this[index][uv / 2f + Float2.Half];
	}
}