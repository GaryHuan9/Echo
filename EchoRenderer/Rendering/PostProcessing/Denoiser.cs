using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using CodeHelpers.Mathematics.Enumerables;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.PostProcessing
{
	public class Denoiser : PostProcessingWorker
	{
		public Denoiser(PostProcessingEngine engine, Texture normalBuffer, Texture positionBuffer) : base(engine)
		{
			this.normalBuffer = normalBuffer;
			this.positionBuffer = positionBuffer;
		}

		public float ColorCoefficient { get; set; } = 0.01f;
		public float NormalCoefficient { get; set; } = 0.001f;
		public float PositionCoefficient { get; set; } = 0.001f;

		//Denoiser based on: https://jo.dreggn.org/home/2010_atrous.pdf

		readonly Texture normalBuffer;
		readonly Texture positionBuffer;

		readonly float[] kernels =
		{
			1f / 256f, 1f / 64f, 3f / 128f, 1f / 64f, 1f / 256f,
			1f / 64f, 1f / 16f, 3f / 32f, 1f / 16f, 1f / 64f,
			3f / 128f, 3f / 32f, 9f / 64f, 3f / 32f, 3f / 128f,
			1f / 64f, 1f / 16f, 3f / 32f, 1f / 16f, 1f / 64f,
			1f / 256f, 1f / 64f, 3f / 128f, 1f / 64f, 1f / 256f
		};

		readonly Int2[] offsets =
		{
			new(-2, 2), new(-1, 2), new(0, 2), new(1, 2), new(2, 2),
			new(-2, 1), new(-1, 1), new(0, 1), new(1, 1), new(2, 1),
			new(-2, 0), new(-1, 0), new(0, 0), new(1, 0), new(2, 0),
			new(-2, -1), new(-1, -1), new(0, -1), new(1, -1), new(2, -1),
			new(-2, -2), new(-1, -2), new(0, -2), new(1, -2), new(2, -2)
		};

		int kernelRadius;
		Vector128<float> sigmas;

		static readonly Vector128<float> zeroVector = Vector128.Create(0f);
		static readonly Vector128<float> oneVector = Vector128.Create(1f);

		public override void Dispatch()
		{
			sigmas = Vector128.Create
			(
				ColorCoefficient.Clamp(Scalars.Epsilon, 1f),
				NormalCoefficient.Clamp(Scalars.Epsilon, 1f),
				PositionCoefficient.Clamp(Scalars.Epsilon, 1f),
				1f
			);

			for (int i = 0; i < 7; i++)
			{
				kernelRadius = 1 << i;
				RunPass(SinglePass);

				// SinglePass(new Int2(300, 300));
			}
		}

		void SinglePass(Int2 position)
		{
			var sum = zeroVector;
			float total = 0f;

			ref Vector128<float> targetColor = ref renderBuffer.GetPixel(position);

			ref readonly Vector128<float> targetNormal = ref normalBuffer.GetPixel(position);
			ref readonly Vector128<float> targetPosition = ref positionBuffer.GetPixel(position);

			for (int i = 0; i < kernels.Length; i++)
			{
				Int2 local = position + offsets[i] * kernelRadius;
				if (renderBuffer.Restrict(local) != local) continue;

				ref readonly Vector128<float> localColor = ref renderBuffer.GetPixel(local);
				ref readonly Vector128<float> localNormal = ref normalBuffer.GetPixel(local);
				ref readonly Vector128<float> localPosition = ref positionBuffer.GetPixel(local);

				Vector128<float> deltaColor = Sse.Subtract(targetColor, localColor);
				Vector128<float> deltaNormal = Sse.Subtract(targetNormal, localNormal);
				Vector128<float> deltaPosition = Sse.Subtract(targetPosition, localPosition);

				deltaColor = Sse41.DotProduct(deltaColor, deltaColor, 0b1110_0001);
				deltaNormal = Sse41.DotProduct(deltaNormal, deltaNormal, 0b1110_0010);
				deltaPosition = Sse41.DotProduct(deltaPosition, deltaPosition, 0b1110_0100);

				unsafe
				{
					float* pointer = (float*)&deltaNormal + 1;
					*pointer /= kernelRadius * kernelRadius;
				}

				Vector128<float> weights = Sse.Add(deltaColor, Sse.Add(deltaNormal, deltaPosition));
				weights = Sse.Min(Exp(Sse.Divide(Sse.Subtract(zeroVector, weights), sigmas)), oneVector);

				float weight = kernels[i];

				unsafe
				{
					float* pointer = (float*)&weights;
					weight *= pointer[0] * pointer[1] * pointer[2];
				}

				Vector128<float> weightVector = Vector128.Create(weight);
				sum = Sse.Add(sum, Sse.Multiply(weightVector, localColor));

				total += weight;
			}

			targetColor = Sse.Divide(sum, Vector128.Create(total));
		}

		/// <summary>
		/// Calculates and returns the natural exponent of the first three numbers in <paramref name="value"/>.
		/// </summary>
		unsafe Vector128<float> Exp(Vector128<float> value)
		{
			float* pointer = (float*)&value;

			pointer[0] = MathF.Exp(pointer[0]);
			pointer[1] = MathF.Exp(pointer[1]);
			pointer[2] = MathF.Exp(pointer[2]);

			return value;
		}
	}
}