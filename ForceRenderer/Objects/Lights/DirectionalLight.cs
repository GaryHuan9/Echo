using CodeHelpers.Vectors;

namespace ForceRenderer.Objects.Lights
{
	public class DirectionalLight : Light
	{
		public DirectionalLight()
		{
			RecalculateDirection();
			OnTransformationChanged += RecalculateDirection;
		}

		public Float3 Direction { get; private set; }
		public float ShadowHardness { get; set; } = 10f;

		void RecalculateDirection() => Direction = DirectionToWorld(Float3.forward);
	}
}