using ForceRenderer.IO;

namespace ForceRenderer.Renderers
{
	/// <summary>
	/// A immutable structure that is stores a copy of the renderer's settings/profile.
	/// This ensures that the renderer never changes its settings when all threads are running.
	/// </summary>
	public readonly struct RenderProfile
	{
		public RenderProfile(Texture buffer, int maxBounce, float energyEpsilon)
		{
			this.buffer = buffer;
			this.maxBounce = maxBounce;
			this.energyEpsilon = energyEpsilon;
		}

		public readonly Texture buffer;
		public readonly int maxBounce;
		public readonly float energyEpsilon;

		public override string ToString() => $"{nameof(maxBounce)}: {maxBounce}, {nameof(energyEpsilon)}: {energyEpsilon}";
	}
}