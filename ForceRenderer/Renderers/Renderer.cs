using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Vectors;
using ForceRenderer.Scenes;

namespace ForceRenderer.Renderers
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
		public float Range { get; set; } = 1000f;
		public int MaxSteps { get; set; } = 1000;

		PressedScene pressedScene;
		Shade[] _renderBuffer;

		public Shade[] RenderBuffer
		{
			get => _renderBuffer;
			set
			{
				if (value != null && value.Length >= bufferSize) _renderBuffer = value;
				else throw ExceptionHelper.Invalid(nameof(RenderBuffer), this, "is not large enough!");
			}
		}

		void Begin() { }

		public int Render(Shade[] results, bool threaded)
		{
			if (results.Length < bufferSize) throw ExceptionHelper.Invalid(nameof(results), results, "is not large enough!");

			Float2 oneLess = resolution - Float2.one;
			pressedScene = new PressedScene(scene);

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
		Shade Render(Float2 uv)
		{
			Float2 scaled = new Float2(uv.x - 0.5f, (uv.y - 0.5f) / aspect);

			Float3 position = camera.Position;
			Float3 direction = camera.GetDirection(scaled);

			bool hit = TrySphereTrace(position, direction, out float hitDistance);
			Shade color = Shade.black;

			if (hit)
			{
				Float3 point = position + direction * hitDistance;
				Float3 normal = GetNormal(point);

				if (scene.Cubemap == null) color = (Shade)normal;
				else color = scene.Cubemap.Sample(direction.Reflect(normal));
			}
			else
			{
				//Sample skybox
				if (scene.Cubemap != null) color = scene.Cubemap.Sample(direction);
			}

			return color;
		}

		bool TrySphereTrace(Float3 position, Float3 direction, out float distance)
		{
			distance = 0f;

			for (int i = 0; i < MaxSteps; i++)
			{
				float step = pressedScene.GetSignedDistance(position);
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
			float center = pressedScene.GetSignedDistance(point);

			return new Float3
			(
				(Sample(Float3.CreateX(E)) - Sample(Float3.CreateX(-E))) / (2f * E),
				(Sample(Float3.CreateY(E)) - Sample(Float3.CreateY(-E))) / (2f * E),
				(Sample(Float3.CreateZ(E)) - Sample(Float3.CreateZ(-E))) / (2f * E)
			);

			float Sample(Float3 epsilon) => pressedScene.GetSignedDistance(point + epsilon) - center;
		}
	}
}