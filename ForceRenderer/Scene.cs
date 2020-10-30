using System;
using CodeHelpers.Vectors;
using ForceRenderer.Objects;
using ForceRenderer.Renderers;
using Object = ForceRenderer.Objects.Object;

namespace ForceRenderer.Scenes
{
	public class Scene : SceneObject
	{
		public Cubemap Cubemap { get; set; }

		public override float SignedDistanceRaw(Float3 point)
		{
			float distance = float.PositiveInfinity;

			for (int i = 0; i < children.Count; i++) SignedDistance(children[i], point, ref distance);

			return distance;
		}

		static void SignedDistance(Object target, Float3 point, ref float distance)
		{
			if (target is SceneObject sceneObject) distance = Math.Min(distance, sceneObject.GetSignedDistance(point));
			for (int i = 0; i < target.children.Count; i++) SignedDistance(target.children[i], point, ref distance);
		}
	}
}