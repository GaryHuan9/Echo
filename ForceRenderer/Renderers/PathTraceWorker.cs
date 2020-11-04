using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Vectors;
using ForceRenderer.Mathematics;

namespace ForceRenderer.Renderers
{
	public class PathTraceWorker : PixelWorker
	{
		public PathTraceWorker(RenderProfile profile) : base(profile) { }

		public override Float3 Render(Float2 uv)
		{
			Float2 scaled = new Float2(uv.x - 0.5f, (uv.y - 0.5f) / profile.buffer.aspect);
			Ray ray = new Ray(pressedScene.camera.Position, pressedScene.camera.GetDirection(scaled));

			int bounce = 0;

			Float3 energy = Float3.one;
			Float3 color = Float3.zero;

			while (TryTrace(ray, out float distance, out int token) && bounce++ < profile.maxBounce)
			{
				ref PressedBundle bundle = ref pressedScene.GetPressedBundle(token);

				Float3 position = ray.GetPoint(distance);
				Float3 normal = pressedScene.GetNormal(position, token);

				//Lambert diffuse
				ray = new Ray(position, GetHemisphereDirection(normal), true);
				energy *= 2f * normal.Dot(ray.direction).Clamp(0f, 1f) * bundle.material.albedo;

				if (energy.x <= profile.energyEpsilon && energy.y <= profile.energyEpsilon && energy.z <= profile.energyEpsilon) break;
			}

			//return (Float3)((float)bounce / profile.maxBounce);

			if (scene.Cubemap == null) return color;
			return color + energy * (Float3)scene.Cubemap.Sample(ray.direction) * 1.8f; //Sample skybox
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		Float3 GetHemisphereDirection(Float3 normal)
		{
			//Uniformly sample directions in a hemisphere
			float cos = RandomValue;
			float sin = (float)Math.Sqrt(Math.Max(0f, 1f - cos * cos));
			float phi = Scalars.TAU * RandomValue;

			float x = (float)Math.Cos(phi) * sin;
			float y = (float)Math.Sin(phi) * sin;
			float z = cos;

			//Transform local direction to world-space based on normal
			Float3 helper = Math.Abs(normal.x) >= 0.9f ? Float3.forward : Float3.right;

			Float3 tangent = Float3.Cross(normal, helper).Normalized;
			Float3 binormal = Float3.Cross(normal, tangent).Normalized;

			//Transforms using matrix multiplication. 3x3 matrix instead of 4x4 because direction only
			return new Float3
			(
				x * tangent.x + y * binormal.x + z * normal.x,
				x * tangent.y + y * binormal.y + z * normal.y,
				x * tangent.z + y * binormal.z + z * normal.z
			);
		}
	}
}