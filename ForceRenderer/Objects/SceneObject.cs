using System;
using CodeHelpers.Vectors;

namespace ForceRenderer.Objects
{
	public abstract class SceneObject : Object
	{
		public float GetSignedDistance(Float3 point)
		{
			point = TransformToLocal(point);
			return SignedDistanceRaw(point);
		}

		/// <summary>
		/// The raw signed distance value to this object located at origin
		/// with no rotation. NOTE: Should be as optimized as possible.
		/// </summary>
		public abstract float SignedDistanceRaw(Float3 point);
	}
}