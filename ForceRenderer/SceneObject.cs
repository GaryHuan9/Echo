using CodeHelpers.Vectors;

namespace ForceRenderer
{
	public abstract class SceneObject : Object
	{
		public abstract float SignedDistance(Float3 point);
	}
}