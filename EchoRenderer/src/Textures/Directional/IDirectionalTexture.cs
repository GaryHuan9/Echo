using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Textures.Directional
{
	/// <summary>
	/// A special texture that can only be sampled based on directions.
	/// </summary>
	public interface IDirectionalTexture
	{
		/// <summary>
		/// Invoked prior to rendering begins to perform any initialization work this <see cref="IDirectionalTexture"/> need.
		/// Other methods defined in this interface will/should not be invoked before this method is invoked at least once.
		/// </summary>
		virtual void Prepare() { }

		/// <summary>
		/// Evaluates this <see cref="IDirectionalTexture"/> at <paramref name="direction"/>.
		/// NOTE: <paramref name="direction"/> should be normalized and is not zero.
		/// </summary>
		Vector128<float> Evaluate(in Float3 direction);
	}
}