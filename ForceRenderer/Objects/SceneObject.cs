using CodeHelpers.Vectors;

namespace ForceRenderer.Objects
{
	public abstract class SceneObject : Object
	{
		public abstract float SignedDistance(Float3 point);
	}
}