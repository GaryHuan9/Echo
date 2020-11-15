using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Vectors;
using ForceRenderer.IO;
using ForceRenderer.Mathematics;

namespace ForceRenderer.Renderers
{
	public class PathTraceWorker : PixelWorker
	{
		public PathTraceWorker(RenderEngine.Profile profile) : base(profile) { }

		public override Float3 Render(Float2 uv)
		{
			Ray ray = new Ray(profile.camera.Position, profile.camera.GetDirection(uv));

			int bounce = 0;

			Float3 energy = Float3.one;
			Float3 color = Float3.zero;

			while (TryTrace(ray, out float distance, out int token) && bounce++ < profile.maxBounce)
			{
				ref PressedMaterial material = ref profile.pressed.GetMaterial(token);

				Float3 position = ray.GetPoint(distance);
				Float3 normal = profile.pressed.GetNormal(position, token);

				// if (pressedScene.directionalLight != null)
				// {
				// 	DirectionalLight light = pressedScene.directionalLight;
				// 	Ray lightRay = new Ray(position, -light.Direction, true);
				//
				// 	float coefficient = normal.Dot(lightRay.direction).Clamp(0f, 1f);
				// 	if (coefficient > 0f) coefficient *= TryTraceShadow(lightRay);
				//
				// 	color += coefficient * energy * bundle.material.albedo * light.Intensity;
				// }

				// ray = new Ray(position, ray.direction.Reflect(normal), true);
				// energy *= bundle.material.specular;

				//Lambert diffuse
				ray = new Ray(position, GetHemisphereDirection(normal), true);
				energy *= 2f * normal.Dot(ray.direction).Clamp(0f, 1f) * material.albedo;

				if (energy.x <= profile.energyEpsilon && energy.y <= profile.energyEpsilon && energy.z <= profile.energyEpsilon) break;
			}

			//return (Float3)((float)bounce / profile.maxBounce);

			Cubemap cubemap = profile.scene.Cubemap;
			if (cubemap == null) return color;

			//Sample skybox
			return color + energy * (Float3)cubemap.Sample(ray.direction) * 1.8f;
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