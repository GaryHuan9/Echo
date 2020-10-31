using System;
using CodeHelpers.Vectors;

namespace ForceRenderer.Objects
{
	public abstract class SceneObject : Object
	{
		/// <summary>
		/// Returns the signed distance value to world-space <paramref name="point"/>.
		/// </summary>
		public float GetSignedDistance(Float3 point)
		{
			point = TransformToLocal(point);
			return GetSignedDistanceRaw(point);
		}

		/// <summary>
		/// The raw signed distance value to this object located at origin
		/// with no rotation. NOTE: Should be as optimized as possible.
		/// </summary>
		public abstract float GetSignedDistanceRaw(Float3 point);

		/// <summary>
		/// May be overriden to return the raw normal of this object at local-space <paramref name="point"/>.
		/// If this method is not implemented then a gradient approximation method will be used.
		/// NOTE: The returned normal vector should be normalized.
		/// </summary>
		public virtual Float3 GetNormalRaw(Float3 point) => throw new NotSupportedException();
	}
}