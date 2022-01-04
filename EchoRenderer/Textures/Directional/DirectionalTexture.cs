using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Textures.Directional
{
	/// <summary>
	/// A special texture that can only be sampled based on directions.
	/// </summary>
	public abstract class DirectionalTexture
	{
		/// <summary>
		/// Evaluates this <see cref="DirectionalTexture"/> at <paramref name="direction"/>.
		/// NOTE: <paramref name="direction"/> should be normalized and is not zero.
		/// </summary>
		public abstract Vector128<float> Evaluate(in Float3 direction);
	}
}