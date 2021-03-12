using System;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;

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
		/// Returns a ray emitted from the camera at <paramref name="uv"/>.
		/// </summary>
		/// <param name="uv">X component from -0.5 to 0.5; Y component an aspect radio corrected version of X.</param>
		public Ray GetRay(Float2 uv) => new Ray(Position, GetDirection(uv));

		/// <summary>
		/// Returns the direction of ray emitted from camera at <paramref name="uv"/>.
		/// </summary>
		/// <param name="uv">X component from -0.5 to 0.5; Y component an aspect radio corrected version of X.</param>
		public Float3 GetDirection(Float2 uv) => LocalToWorld.MultiplyDirection(uv.CreateXY(fieldDistance)).Normalized;

		public void LookAt(Object target) => LookAt(target.Position);

		public void LookAt(Float3 target)
		{
			Float3 to = (target - Position).Normalized;

			float yAngle = -Float2.up.SignedAngle(to.XZ);
			float xAngle = -Float2.right.SignedAngle(to.RotateXZ(yAngle).ZY);

			Rotation = new Float3(xAngle, yAngle, 0f);
		}
	}
}