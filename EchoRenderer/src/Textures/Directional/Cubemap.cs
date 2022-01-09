using System;
using System.IO;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Textures.Grid;

namespace EchoRenderer.Textures.Directional
{
	public class Cubemap : IDirectionalTexture
	{
		public Cubemap()
		{
			Multiplier = Float3.one;
			int length = names.Length;

			textures = new NotNull<Texture>[length];
			for (int i = 0; i < length; i++) textures[i] = Texture.black;
		}

		public Cubemap(ReadOnlySpan<Texture> textures) : this()
		{
			int length = Math.Min(names.Length, textures.Length);
			for (int i = 0; i < length; i++) this.textures[i] = textures[i];
		}

		public Cubemap(string path) : this()
		{
			int length = names.Length;
			var tasks = new Task<ArrayGrid>[length];

			for (int i = 0; i < length; i++)
			{
				string fullPath = Path.Combine(path, names[i]);
				tasks[i] = TextureGrid.LoadAsync(fullPath);
			}

			for (int i = 0; i < length; i++) textures[i] = tasks[i].Result;
		}

		readonly NotNull<Texture>[] textures;
		Vector128<float> multiplierVector;

		public Texture this[Direction direction]
		{
			get => this[direction.Index];
			set => this[direction.Index] = value;
		}

		public Float3 Multiplier
		{
			get => Utilities.ToFloat3(multiplierVector);
			set => multiplierVector = Utilities.ToVector(value);
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

			uv *= 0.5f / target.ExtractComponent(direction);
			uv += Float2.half;

			Vector128<float> sample = this[index][uv];
			return Sse.Multiply(sample, multiplierVector);
		}
	}
}