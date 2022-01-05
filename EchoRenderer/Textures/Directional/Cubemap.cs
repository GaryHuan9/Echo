using System;
using System.IO;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
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
			get => textures[direction.Index];
			set => textures[direction.Index] = value;
		}

		public Float3 Multiplier
		{
			get => Utilities.ToFloat3(multiplierVector);
			set => multiplierVector = Utilities.ToVector(value);
		}

		static readonly string[] names = { "px", "py", "pz", "nx", "ny", "nz" };

		public Vector128<float> Evaluate(in Float3 direction)
		{
			Direction source = (Direction)direction;
			Float2 uv = source.Project(direction);

			uv *= 0.5f / source.Absoluted.ExtractComponent(direction);
			uv += Float2.half;

			Vector128<float> sample = this[source][uv];
			return Sse.Multiply(sample, multiplierVector);
		}
	}
}