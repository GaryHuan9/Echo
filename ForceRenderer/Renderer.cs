using System;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Vectors;

namespace ForceRenderer
{
	public class Renderer
	{
		public Renderer(Scene scene, Camera camera, Int2 resolution)
		{
			this.scene = scene;
			this.camera = camera;

			this.resolution = resolution;
			aspect = (float)resolution.x / resolution.y;
			bufferSize = resolution.Product;
		}

		public readonly Scene scene;
		public readonly Camera camera;

		public readonly Int2 resolution;
		public readonly float aspect;
		public readonly int bufferSize;

		public float Epsilon { get; set; } = Scalars.Epsilon;
		public float Range { get; set; } = 100f;
		public int MaxSteps { get; set; } = 1000;

		public int Render(Float3[] results, bool threaded)
		{
			if (results.Length < bufferSize) throw ExceptionHelper.Invalid(nameof(results), results, "is not large enough!");

			Float2 oneLess = resolution - Float2.one;

			if (!threaded)
			{
				for (int i = 0; i < bufferSize; i++) RenderPixel(i);
			}
			else Parallel.For(0, bufferSize, RenderPixel);

			return bufferSize;

			void RenderPixel(int index)
			{
				Int2 pixel = new Int2(index / resolution.y, index % resolution.y);
				results[index] = Render(pixel / oneLess);
			}
		}

		/// <summary>
		/// Renders a single pixel and returns the result.
		/// </summary>
		/// <param name="uv">Zero to one normalized raw uv without any scaling.</param>
		Float3 Render(Float2 uv)
		{
			Float2 scaled = new Float2(uv.x - 0.5f, (uv.y - 0.5f) / aspect);

			Float3 position = camera.Position;
			Float3 direction = camera.GetDirection(scaled);

			bool hit = TrySphereTrace(position, direction, out float hitDistance);
			if (!hit) return Float3.zero;

			Float3 color = Float3.zero;
			Float3 point = position + direction * hitDistance;

			color = (Float3)(1f - 1f / hitDistance);
			color = GetNormal(point);

			return color;
		}

		bool TrySphereTrace(Float3 position, Float3 direction, out float distance)
		{
			distance = 0f;

			for (int i = 0; i < MaxSteps; i++)
			{
				float step = scene.SignedDistance(position);
				distance += step;

				if (step <= Epsilon) return true;   //Trace hit
				if (distance > Range) return false; //Trace miss

				position += direction * step;
			}

			return false;
		}

		Float3 GetNormal(Float3 point)
		{
			const float E = Scalars.Epsilon;
			float center = scene.SignedDistance(point);

			return new Float3
			(
				(Sample(Float3.CreateX(E)) - Sample(Float3.CreateX(-E))) / (2f * E),
				(Sample(Float3.CreateY(E)) - Sample(Float3.CreateY(-E))) / (2f * E),
				(Sample(Float3.CreateZ(E)) - Sample(Float3.CreateZ(-E))) / (2f * E)
			);

			float Sample(Float3 epsilon) => scene.SignedDistance(point + epsilon) - center;
		}
	}
}