using System;
using System.Runtime.CompilerServices;
using CodeHelpers.Vectors;
using ForceRenderer.IO;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects;

namespace ForceRenderer.Renderers
{
	public class PathTraceWorker : PixelWorker
	{
		public PathTraceWorker(RenderEngine.Profile profile) : base(profile) { }

		public override Float3 Render(Float2 screenUV)
		{
			Ray ray = new Ray(profile.camera.Position, profile.camera.GetDirection(screenUV));

			Float3 energy = Float3.one;
			Float3 light = Float3.zero;

			int bounce;

			for (bounce = 0; bounce <= profile.maxBounce && TryTrace(ray, out float distance, out int token, out Float2 uv); bounce++)
			{
				ref PressedMaterial material = ref profile.pressed.GetMaterial(token);

				Float3 position = ray.GetPoint(distance);
				Float3 normal = profile.pressed.GetNormal(uv, token);

				light += energy * material.emission;

				//Randomly choose between diffuse and specular BSDF
				if (RandomValue <= material.specularChance) //Cannot be <, must be <=, because < will let a diffuseChance of 0f pass causing NaNs
				{
					//Phong specular reflection
					ray = new Ray(position, GetHemisphereDirection(ray.direction.Reflect(normal), material.phongAlpha), true);
					energy *= 1f / material.specularChance * (normal.Dot(ray.direction) * material.phongMultiplier).Clamp(0f, 1f) * material.specular;
				}
				else
				{
					//Lambert diffuse reflection
					ray = new Ray(position, GetHemisphereDirection(normal, 1f), true); //Using cosine distribution
					energy *= 1f / material.diffuseChance * material.albedo;
				}

				if (energy.x <= profile.energyEpsilon && energy.y <= profile.energyEpsilon && energy.z <= profile.energyEpsilon) break;
			}

			//return (Float3)((float)bounce / profile.maxBounce);

			Cubemap skybox = profile.scene.Cubemap;
			PressedDirectionalLight sun = profile.pressed.directionalLight;

			if (skybox != null) light += energy * (Float3)skybox.Sample(ray.direction);
			if (sun.direction != default) light += energy * sun.intensity * -sun.direction.Dot(ray.direction).Clamp(-1f, 0f); //Sun not really working right now

			return light;
		}

		/// <summary>
		/// Samples a hemisphere pointing towards <paramref name="normal"/>.
		/// <paramref name="alpha"/> determines whether the sampling distribution is uniform or cosine.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		Float3 GetHemisphereDirection(Float3 normal, float alpha)
		{
			//Samples hemisphere based on alpha
			float cos = MathF.Pow(RandomValue, 1f / (alpha + 1f));
			float sin = MathF.Sqrt(1f - cos * cos);
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