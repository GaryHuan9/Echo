using System;
using CodeHelpers.Vectors;

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
		public Float3 GetDirection(Float2 uv) => DirectionToWorld(uv.CreateXY(fieldDistance)).Normalized;
	}

}