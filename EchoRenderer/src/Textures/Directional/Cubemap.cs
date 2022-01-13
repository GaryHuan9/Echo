using System;
using System.IO;
using System.Runtime.Intrinsics;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Textures.Grid;

namespace EchoRenderer.Textures.Directional
{
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

		Texture this[int index]
		{
			get => textures[index];
			set => textures[index] = value;
		}

		static readonly string[] names = { "px", "nx", "py", "ny", "pz", "nz" };

		public Vector128<float> Evaluate(in Float3 direction)
		{
			Direction target = (Direction)direction;

			int index = target.Index;
			Float2 uv = index switch
			{
				0 => new Float2(-direction.z, direction.y),
				1 => direction.ZY,
				2 => new Float2(direction.x, -direction.z),
				3 => direction.XZ,
				4 => direction.XY,
				5 => new Float2(-direction.x, direction.y),
				_ => throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.unexpected)
			};

			uv /= target.ExtractComponent(direction);
			return this[index][uv / 2f + Float2.half];
		}
	}
}