using System;
using System.IO;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grid;

namespace Echo.Core.Textures.Directional;

public class Cubemap : IDirectionalTexture
{
	public Cubemap()
	{
		textures = new NotNull<Texture>[fileNames.Length];
		foreach (ref var texture in textures.AsSpan()) texture = Texture.black;
	}

	public Cubemap(ReadOnlySpan<Texture> textures)
	{
		int length = fileNames.Length;

		int min = Math.Min(length, textures.Length);
		this.textures = new NotNull<Texture>[length];

		for (int i = 0; i < length; i++) this.textures[i] = i < min ? textures[i] : Texture.black;
	}

	public Cubemap(string path)
	{
		int length = fileNames.Length;

		var tasks = new Task<ArrayGrid<RGB128>>[length];
		textures = new NotNull<Texture>[length];

		for (int i = 0; i < length; i++)
		{
			string fullPath = Path.Combine(path, fileNames[i]);
			tasks[i] = TextureGrid<RGB128>.LoadAsync(fullPath);
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

	public RGB128 Average { get; private set; }

	Texture this[int index]
	{
		get => textures[index];
		set => textures[index] = value;
	}

	static readonly string[] fileNames = { "px", "nx", "py", "ny", "pz", "nz" };

	/// <inheritdoc/>
	public void Prepare() => Average = this.ConvergeAverage();

	/// <inheritdoc/>
	public RGB128 Evaluate(in Float3 incident)
	{
		Direction target = (Direction)incident;

		int index = target.Index;
		Float2 uv = index switch
		{
			0 => new Float2(-incident.Z, incident.Y),
			1 => incident.ZY,
			2 => new Float2(incident.X, -incident.Z),
			3 => incident.XZ,
			4 => incident.XY,
			5 => new Float2(-incident.X, incident.Y),
			_ => throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.unexpected)
		};

		uv *= 0.5f / target.ExtractComponent(incident);
		return (RGB128)this[index][uv + Float2.Half];
	}
}