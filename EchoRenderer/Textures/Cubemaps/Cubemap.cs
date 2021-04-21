using CodeHelpers.Mathematics;

namespace EchoRenderer.Textures.Cubemaps
{
	public abstract class Cubemap
	{
		/// <summary>
		/// Samples the <see cref="Cubemap"/> at a particular <paramref name="direction"/>.
		/// NOTE: <paramref name="direction"/> should be normalized.
		/// </summary>
		public abstract Float3 Sample(in Float3 direction);
	}
}