using System;
using System.Threading;
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
			pixelCount = resolution.Product;
		}

		public readonly Scene scene;
		public readonly Camera camera;

		public readonly Int2 resolution;
		public readonly float aspect;
		public readonly int pixelCount;

		public float Epsilon { get; set; } = Scalars.Epsilon;
		public float Range { get; set; } = 1000f;

		public int MaxSteps { get; set; } = 1000;
		public int MaxBounce { get; set; } = 32;

		PressedScene pressedScene;
		Thread renderThread;

		Shade[] _renderBuffer;
		long _completedPixelCount;

		public Shade[] RenderBuffer
		{
			get => _renderBuffer;
			set
			{
				if (CurrentState == State.rendering) throw new Exception("Cannot modify buffer when rendering!");

				if (value != null && value.Length >= pixelCount) _renderBuffer = value;
				else throw ExceptionHelper.Invalid(nameof(RenderBuffer), this, "is not large enough!");
			}
		}

		public long CompletedPixelCount => Interlocked.Read(ref _completedPixelCount);

		public State CurrentState { get; private set; }
		public bool Completed => CurrentState == State.completed;

		public void Begin()
		{
			if (RenderBuffer == null) throw ExceptionHelper.Invalid(nameof(RenderBuffer), this, InvalidType.isNull);
			if (CurrentState != State.waiting) throw new Exception("Incorrect state! Must reset before rendering!");

			pressedScene = new PressedScene(scene);
			renderThread = new Thread(RenderThread)
						   {
							   Priority = ThreadPriority.Highest,
							   IsBackground = true
						   };

			renderThread.Start();
			CurrentState = State.rendering;
		}

		public void WaitForRender() => renderThread.Join();
		public void Stop() => CurrentState = State.stopped;

		public void Reset()
		{
			CurrentState = State.waiting;
			_completedPixelCount = 0;

			pressedScene = null;
			renderThread = null;
		}

		void RenderThread()
		{
			Float2 uvPixel = 1f / resolution;

			Parallel.For
			(
				0, pixelCount, (index, state) =>
				{
					if (CurrentState == State.stopped)
					{
						state.Break();
						return;
					}

					Int2 pixel = new Int2(index / resolution.y, index % resolution.y);
					RenderBuffer[index] = RenderPixel((pixel + Float2.half) * uvPixel);

					Interlocked.Increment(ref _completedPixelCount);
				}
			);

			CurrentState = State.completed;
		}

		/// <summary>
		/// Renders a single pixel and returns the result.
		/// </summary>
		/// <param name="uv">Zero to one normalized raw uv without any scaling.</param>
		Shade RenderPixel(Float2 uv)
		{
			Float2 scaled = new Float2(uv.x - 0.5f, (uv.y - 0.5f) / aspect);

			Float3 position = camera.Position;
			Float3 direction = camera.GetDirection(scaled);

			int bounce = 0;

			while (TrySphereTrace(position, direction, out float distance) && bounce++ < MaxBounce)
			{
				position += direction * distance;
				direction = direction.Reflect(GetNormal(position));

				position += direction * Epsilon * 10f;
			}

			//return (Shade)(direction * 10f % 1f);
			//return (Shade)(Float3)(bounce / 7f);
			return scene.Cubemap?.Sample(direction) ?? Shade.black;
		}

		bool TrySphereTrace(Float3 origin, Float3 direction, out float distance)
		{
			distance = 0f;

			for (int i = 0; i < MaxSteps; i++)
			{
				Float3 position = origin + direction * distance;
				float step = pressedScene.GetSignedDistance(position);

				distance += step;

				if (step <= Epsilon) return true;     //Trace hit
				if (distance > Range) return false; //Trace miss
			}

			return false;
		}

		Float3 GetNormal(Float3 point)
		{
			const float E = 1E-5f;
			float center = pressedScene.GetSignedDistance(point);

			return new Float3
			(
				(Sample(Float3.CreateX(E)) - Sample(Float3.CreateX(-E))) / (2f * E),
				(Sample(Float3.CreateY(E)) - Sample(Float3.CreateY(-E))) / (2f * E),
				(Sample(Float3.CreateZ(E)) - Sample(Float3.CreateZ(-E))) / (2f * E)
			).Normalized;

			float Sample(Float3 epsilon) => pressedScene.GetSignedDistance(point + epsilon) - center;
		}

		public enum State
		{
			waiting,
			rendering,
			completed,
			stopped
		}
	}
}