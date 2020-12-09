using System;
using CodeHelpers.Vectors;
using ForceRenderer.CodeHelpers.Vectors;

namespace ForceRenderer.Objects
{
	public class Camera : Object
	{
		public Camera(float fieldOfView) => FieldOfView = fieldOfView;

		float fieldOfView;
		float fieldDistance;

		/// <summary>
		/// Horizontal field of view in degrees.
		/// </summary>
		public float FieldOfView
		{
			get => fieldOfView;
			set
			{
				fieldOfView = value;
				fieldDistance = 0.5f / (float)Math.Tan(value / 2f * Scalars.DegreeToRadian);
			}
		}

		/// <summary>
		/// Returns the direction of ray emitted from camera at <paramref name="uv"/>.
		/// </summary>
		/// <param name="uv">X component from -0.5 to 0.5; Y component an aspect radio corrected version of X.</param>
		public Float3 GetDirection(Float2 uv) => LocalToWorld.MultiplyDirection(uv.CreateXY(fieldDistance)).Normalized;

		public void LookAt(Object target) => LookAt(target.Position);

		public void LookAt(Float3 target)
		{
			Float3 from = LocalToWorld.MultiplyDirection(Float3.forward);
			Float3 to = (target - Position).Normalized;

			Rotation -= Float3.up * from.XZ.SignedAngle(to.XZ);
			from = LocalToWorld.MultiplyDirection(Float3.forward);

			Rotation -= Float3.right * from.YZ.SignedAngle(to.YZ);
		}
	}
}