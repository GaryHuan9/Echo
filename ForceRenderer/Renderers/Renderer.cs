using System;
using System.Threading;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Vectors;
using ForceRenderer.IO;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects.Lights;
using ForceRenderer.Objects.SceneObjects;
using ForceRenderer.Scenes;

namespace ForceRenderer.Renderers
{
	public class Renderer
	{
		public Renderer(Scene scene, int sampleCount)
		{
			this.scene = scene;
			this.sampleCount = sampleCount;

			//Create sample spiral
			sampleSpiral = new Float2[sampleCount];

			for (int i = 0; i < sampleCount; i++)
			{
				float index = i + 0.5f;

				float length = (float)Math.Sqrt(index / sampleCount);
				float angle = Scalars.PI * (1f + (float)Math.Sqrt(5d)) * index;

				Float2 sample = new Float2((float)Math.Cos(angle), (float)Math.Sin(angle));
				sampleSpiral[i] = sample * length / 2f + Float2.half;
			}
		}

		public readonly Scene scene;
		public readonly int sampleCount;

		public int MaxBounce { get; set; } = 32;
		public float EnergyEpsilon { get; set; } = 1E-3f; //Epsilon lower bound value to determine when an energy is essentially zero

		readonly Float2[] sampleSpiral;

		PressedScene pressedScene;
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

		/// <summary>
		/// Returns a random number within [0f, 1f). Thread-safe
		/// </summary>
		static float RandomValue => (float)RandomHelper.Value;

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

											Float3 color = default;

											for (int i = 0; i < sampleCount; i++)
											{
												Int2 position = RenderBuffer.ToPosition(index);
												Float2 random = sampleSpiral[i];

												color += RenderPixel(RenderBuffer.ToUV(position + random));
											}

											RenderBuffer.SetPixel(index, (Shade)(color / sampleCount));
											Interlocked.Increment(ref _completedPixelCount);
										}
			);

			CurrentState = State.completed;
		}

		/// <summary>
		/// Renders a single pixel and returns the result.
		/// </summary>
		/// <param name="uv">Zero to one normalized raw uv without any scaling.</param>
		Float3 RenderPixel(Float2 uv)
		{
			Float2 scaled = new Float2(uv.x - 0.5f, (uv.y - 0.5f) / RenderAspect);
			Ray ray = new Ray(pressedScene.camera.Position, pressedScene.camera.GetDirection(scaled));

			int bounce = 0;

			Float3 energy = Float3.one;
			Float3 color = Float3.zero;

			while (TryTrace(ray, out float distance, out int token) && bounce++ < MaxBounce)
			{
				ref PressedBundle bundle = ref pressedScene.GetPressedBundle(token);

				Float3 position = ray.GetPoint(distance);
				Float3 normal = pressedScene.GetNormal(position, token);

				if (pressedScene.directionalLight != null)
				{
					DirectionalLight light = pressedScene.directionalLight;
					Ray lightRay = new Ray(position, -light.Direction, true);

					float coefficient = normal.Dot(lightRay.direction).Clamp(0f, 1f);
					if (coefficient > 0f) coefficient *= TryTraceShadow(lightRay);

					color += coefficient * energy * bundle.material.albedo * light.Intensity;
				}

				energy *= bundle.material.specular;
				ray = new Ray(position, ray.direction.Reflect(normal), true);

				if (IsZeroEnergy(energy)) break;
			}

			return (Float3)((float)bounce / MaxBounce);

			if (scene.Cubemap == null) return color;
			return color + energy * (Float3)scene.Cubemap.Sample(ray.direction) * 1.8f; //Sample skybox
		}

		bool TryTrace(in Ray ray, out float distance, out int token)
		{
			distance = pressedScene.GetIntersection(ray, out token);
			return distance < float.PositiveInfinity;
		}

		float TryTraceShadow(in Ray ray)
		{
			float distance = pressedScene.GetIntersection(ray);
			return distance < float.PositiveInfinity ? 0f : 1f;
		}

		bool IsZeroEnergy(Float3 energy) => energy.x <= EnergyEpsilon && energy.y <= EnergyEpsilon && energy.z <= EnergyEpsilon;

		public Float3 GetHemisphereDirection(Float3 normal, float alpha)
		{
			//Transform local direction to world based on normal
		}

		static void Transform(Float3 normal)
		{
			Float3 helper = normal.x >= 0.9f ? Float3.forward : Float3.right;
			Float3 binormal = Float3.Cross(normal, helper).Normalized;
			Float3 tangent = Float3.Cross(binormal, normal).Normalized;
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