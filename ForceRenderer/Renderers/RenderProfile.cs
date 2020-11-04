using ForceRenderer.IO;
using ForceRenderer.Scenes;

namespace ForceRenderer.Renderers
{
	/// <summary>
	/// A immutable structure that is stores a copy of the renderer's settings/profile.
	/// This ensures that the renderer never changes its settings when all threads are running.
	/// </summary>
	public readonly struct RenderProfile
	{
		public RenderProfile(Scene scene, PressedScene pressedScene, int pixelSample, float aspect, int maxBounce, float energyEpsilon)
		{
			this.scene = scene;
			this.pressedScene = pressedScene;

			this.pixelSample = pixelSample;
			this.aspect = aspect;

			this.maxBounce = maxBounce;
			this.energyEpsilon = energyEpsilon;
		}

		public readonly Scene scene;
		public readonly PressedScene pressedScene;

		public readonly int pixelSample;
		public readonly float aspect;

		public readonly int maxBounce;
		public readonly float energyEpsilon;

		public override string ToString() => $"{nameof(maxBounce)}: {maxBounce}, {nameof(energyEpsilon)}: {energyEpsilon}";
	}
}