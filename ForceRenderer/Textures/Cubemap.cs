using CodeHelpers.Mathematics;

namespace ForceRenderer.Textures
{
	public abstract class Cubemap
	{
		/// <summary>
		/// Samples the <see cref="Cubemap"/> at a particular <paramref name="direction"/>.
		/// NOTE: <paramref name="direction"/> should be normalized.
		/// </summary>
		public abstract Float3 Sample(Float3 direction);
	}
}