using System;
using System.Threading;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Vectors;
using ForceRenderer.Objects.Lights;
using ForceRenderer.Scenes;

namespace ForceRenderer.Renderers
{
	public class Renderer
	{
		public Renderer(Scene scene) => this.scene = scene;

		public readonly Scene scene;
		PressedScene pressedScene;

		public float Range { get; set; } = 1000f;
		public int MaxSteps { get; set; } = 1000;
		public int MaxBounce { get; set; } = 8;

		public float DistanceEpsilon { get; set; } = 1E-5f;   //Epsilon value used to terminate a sphere trace hit
		public float ReflectionEpsilon { get; set; } = 3E-3f; //Epsilon value to pre trace ray for reflections

		Thread renderThread;

		volatile Texture _renderBuffer;

		long _completedPixelCount;
		long _currentState;

		public Texture RenderBuffer
		{
			get => _renderBuffer;
			set
			{
				if (CurrentState == State.rendering) throw new Exception("Cannot modify buffer when rendering!");
				Interlocked.Exchange(ref _renderBuffer, value);
			}
		}

		public long CompletedPixelCount => Interlocked.Read(ref _completedPixelCount);
		public float RenderAspect => RenderBuffer.aspect;
		public int RenderLength => RenderBuffer.length;

		public State CurrentState
		{
			get => (State)Interlocked.Read(ref _currentState);
			private set => Interlocked.Exchange(ref _currentState, (long)value);
		}

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

			if (pressedScene.camera == null) throw new Exception("No camera in scene! Cannot render without a camera!");

			CurrentState = State.rendering;
			renderThread.Start();
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
			Parallel.For
			(
				0, RenderBuffer.length, (index, state) =>
										{
											if (CurrentState == State.stopped)
											{
												state.Break();
												return;
											}

											Shade color = RenderPixel(RenderBuffer.ToUV(index));
											RenderBuffer.SetPixel(index, color);

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
			Float2 scaled = new Float2(uv.x - 0.5f, (uv.y - 0.5f) / RenderAspect);

			Float3 position = pressedScene.camera.Position;
			Float3 direction = pressedScene.camera.GetDirection(scaled);

			int token = -1;
			int bounce = 0;

			Float3 energy = Float3.one;
			Float3 color = Float3.zero;

			Float3 specular = new Float3(0.6f, 0.6f, 0.6f);
			Float3 albedo = new Float3(0.8f, 0.8f, 0.8f);

			while (TrySphereTrace(position, direction, out float distance, out token, token) && bounce++ < MaxBounce)
			{
				position += direction * distance;

				Float3 normal = pressedScene.GetNormal(position);
				direction = direction.Reflect(normal).Normalized;

				energy *= specular;

				if (pressedScene.directionalLight != null)
				{
					DirectionalLight light = pressedScene.directionalLight;
					Float3 lightDirection = -light.Direction;

					float coefficient = normal.Dot(lightDirection).Clamp(0f, 1f) * light.Intensity;
					coefficient *= TraceSoftShadow(position, lightDirection, light.ShadowHardness, token);

					color += coefficient * albedo * energy;
				}
			}

			color += energy * (Float3)scene.Cubemap.Sample(direction); //Sample skybox

			//return (Shade)(Float3)((float)bounce / MaxBounce);
			return (Shade)color;
		}

		bool TrySphereTrace(Float3 origin, Float3 direction, out float distance, out int token, int exclude = -1)
		{
			distance = 0f;
			token = -1;

			for (int i = 0; i < MaxSteps; i++)
			{
				Float3 position = origin + direction * distance;
				float step = pressedScene.GetSignedDistance(position, out token, exclude);

				distance += step;

				if (Math.Abs(step) <= DistanceEpsilon) return true; //Trace hit
				if (distance > Range) return false;                 //Trace miss
			}

			return false;
		}

		float TraceSoftShadow(Float3 origin, Float3 direction, float hardness, int exclude = -1)
		{
			float light = 1f;
			float distance = 0f;

			for (int i = 0; i < MaxSteps; i++)
			{
				Float3 position = origin + direction * distance;
				float step = pressedScene.GetSignedDistance(position, exclude);

				light = Math.Min(light, hardness * step / distance);
				distance += step;

				if (step <= DistanceEpsilon || distance > Range) break;
			}

			return light.Clamp(0f, 1f);
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