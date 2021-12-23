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
	public class Cubemap : DirectionalTexture
	{
		public Cubemap(string path) : this(path, Float3.one) { }

		public Cubemap(string path, Float3 multiplier)
		{
			Exception error = null;

			textures = new NotNull<Texture>[names.Length];
			Parallel.For(0, names.Length, LoadSingle);
			if (error != null) throw error;

			multiplierVector = Utilities.ToVector(multiplier);

			void LoadSingle(int index, ParallelLoopState state)
			{
				try
				{
					string fullPath = Path.Combine(path, names[index]);
					textures[index] = TextureGrid.Load(fullPath);
				}
				catch (FileNotFoundException exception)
				{
					error = exception;
					state.Stop();
				}
			}
		}

		public Texture this[Direction direction]
		{
			get => textures[direction.Index];
			set => textures[direction.Index] = value;
		}

		readonly NotNull<Texture>[] textures;
		readonly Vector128<float> multiplierVector;

		static readonly string[] names = { "px", "py", "pz", "nx", "ny", "nz" };

		public override Vector128<float> Sample(in Float3 direction)
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