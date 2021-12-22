using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
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
			var names = IndividualTextureNames;
			Texture[] sources = new Texture[names.Count];

			Exception error = null;

			Parallel.For(0, names.Count, Load);
			if (error != null) throw error;

			textures = new ReadOnlyCollection<Texture>(sources);
			multiplierVector = Utilities.ToVector(multiplier);

			void Load(int index, ParallelLoopState state)
			{
				try
				{
					sources[index] = TextureGrid.Load(Path.Combine(path, names[index]));
				}
				catch (FileNotFoundException exception)
				{
					error = exception;
					state.Stop();
				}
			}
		}

		readonly ReadOnlyCollection<Texture> textures;
		readonly Vector128<float> multiplierVector;

		public static readonly ReadOnlyCollection<string> IndividualTextureNames = new(new[] {"px", "py", "pz", "nx", "ny", "nz"});

		public override Vector128<float> Sample(in Float3 direction)
		{
			int index = direction.Absoluted.MaxIndex;

			float component = direction[index];
			if (direction[index] < 0f) index += 3;

			Float2 uv = index switch
						{
							0 => new Float2(-direction.z, direction.y),
							1 => new Float2(direction.x, -direction.z),
							2 => direction.XY,
							3 => direction.ZY,
							4 => direction.XZ,
							_ => new Float2(-direction.x, direction.y)
						};

			component = 0.5f / Math.Abs(component);
			return Sample(index, uv * component);
		}

		/// <summary>
		/// Samples a specific bitmap at <paramref name="uv"/>.
		/// <paramref name="uv"/> is between -0.5 to 0.5 with zero in the middle.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		Vector128<float> Sample(int index, Float2 uv) => Sse.Multiply(textures[index][uv + Float2.half], multiplierVector);
	}
}