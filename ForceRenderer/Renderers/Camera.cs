using System;
using CodeHelpers.Vectors;
using Object = ForceRenderer.Objects.Object;

namespace ForceRenderer.Renderers
{
	public class Camera : Objects.Object
	{
		public Camera(float fieldOfView) => FieldOfView = fieldOfView;

		/// <summary>
		/// X = pitch, Y = yaw
		/// </summary>
		public Float2 Angles { get; set; }

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
		public Float3 GetDirection(Float2 uv) => Rotate(uv.CreateXY(fieldDistance)).Normalized;

		Float3 Rotate(Float3 direction) => direction.RotateYZ(Angles.x).RotateXZ(Angles.y);
	}

}