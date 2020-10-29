using System;
using CodeHelpers.Vectors;

namespace ForceRenderer.Objects
{
	public class Object
	{
		public Float3 Position { get; set; }
		public Float2 Rotation { get; set; }

		public Float3 TransformToLocal(Float3 world) => Rotate(world - Position, -Rotation);

		public Float3 TransformToWorld(Float3 local) => Rotate(local, Rotation) + Position;

		static Float3 Rotate(Float3 point, Float2 angles)
		{
			float angleX = angles.x * Scalars.DegreeToRadian;
			float angleY = angles.y * Scalars.DegreeToRadian;

			float sinX = (float)Math.Sin(angleX);
			float cosX = (float)Math.Cos(angleX);

			float sinY = (float)Math.Sin(angleY);
			float cosY = (float)Math.Cos(angleY);

			float y = cosX * point.y - sinX * point.z;
			float z = sinX * point.y + cosX * point.z;

			return new Float3
			(
				cosY * point.x - sinY * z,
				y,
				sinY * point.x + cosY * z
			);
		}
	}
}