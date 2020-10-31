using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Vectors;

namespace ForceRenderer.Objects
{
	/// <summary>
	/// An ultra-lightweight struct used to transform a point by a position and a rotation
	/// </summary>
	public readonly struct Transformation
	{
		public Transformation(Float3 position, Float2 rotation)
		{
			positionX = position.x;
			positionY = position.y;
			positionZ = position.z;

			float angleX = rotation.x * Scalars.DegreeToRadian;
			float angleY = rotation.y * Scalars.DegreeToRadian;

			sinX = (float)Math.Sin(angleX);
			cosX = (float)Math.Cos(angleX);

			sinY = (float)Math.Sin(angleY);
			cosY = (float)Math.Cos(angleY);
		}

		readonly float positionX;
		readonly float positionY;
		readonly float positionZ;

		readonly float sinX;
		readonly float cosX;

		readonly float sinY;
		readonly float cosY;

		public static readonly Transformation identity = new Transformation(Float3.zero, Float2.zero);

		public Float3 Forward(Float3 point)
		{
			float x = point.x;
			float y = point.y;
			float z = point.z;

			RotateForward(ref x, ref y, ref z);
			return new Float3(x + positionX, y + positionY, z + positionZ);
		}

		public Float3 Backward(Float3 point)
		{
			float x = point.x - positionX;
			float y = point.y - positionY;
			float z = point.z - positionZ;

			RotateBackward(ref x, ref y, ref z);
			return new Float3(x, y, z);
		}

		public Float3 ForwardDirection(Float3 direction)
		{
			float x = direction.x;
			float y = direction.y;
			float z = direction.z;

			RotateForward(ref x, ref y, ref z);
			return new Float3(x, y, z);
		}

		public Float3 BackwardDirection(Float3 direction)
		{
			float x = direction.x;
			float y = direction.y;
			float z = direction.z;

			RotateBackward(ref x, ref y, ref z);
			return new Float3(x, y, z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		void RotateForward(ref float x, ref float y, ref float z)
		{
			float sourceX = x;
			float sourceY = y;

			y = cosX * sourceY - sinX * z;
			z = sinX * sourceY + cosX * z;

			x = cosY * sourceX - sinY * z;
			z = sinY * sourceX + cosY * z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		void RotateBackward(ref float x, ref float y, ref float z)
		{
			float sourceX = x;
			float sourceY = y;

			x = cosY * sourceX + sinY * z;
			z = cosY * z - sinY * sourceX;

			y = cosX * sourceY + sinX * z;
			z = cosX * z - sinX * sourceY;
		}
	}
}