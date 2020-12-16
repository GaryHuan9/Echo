using System;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Mathematics;
using ForceRenderer.IO;

namespace ForceRenderer.Renderers
{
	public class Denoiser
	{
		public Denoiser(Texture source, Texture destination)
		{
			if (source.size != destination.size) throw ExceptionHelper.Invalid(nameof(destination), destination, $"does not match the size of '{nameof(source)}'!");

			this.source = source;
			this.destination = destination;
		}

		public readonly Texture source;
		public readonly Texture destination;

		const int KernelRadius = 2;

		public void Dispatch()
		{
			Parallel.ForEach
			(
				source.size.Loop(),
				position =>
				{
					Float3 totalRender = default;
					int pixelCount = 0;

					for (int x = -KernelRadius; x <= KernelRadius; x++)
					{
						for (int y = -KernelRadius; y <= KernelRadius; y++)
						{
							Int2 offset = position + new Int2(x, y);
							if (!TryGetPixel(offset, out Float3 pixel)) continue;

							totalRender += pixel;
							pixelCount++;
						}
					}

					Float3 average = totalRender / pixelCount;
					float distance = 0f;

					for (int x = -KernelRadius; x <= KernelRadius; x++)
					{
						for (int y = -KernelRadius; y <= KernelRadius; y++)
						{
							Int2 offset = position + new Int2(x, y);
							if (!TryGetPixel(offset, out Float3 pixel)) continue;

							float differenceRender = (pixel - average).MaxComponent;
							distance += differenceRender * differenceRender;
						}
					}

					const float Sensitivity = 0.483f;  //Between 0 and 1
					distance = distance.Clamp(0f, 1f); //https://www.desmos.com/calculator/mydvwdv5oz

					float sigmoid = CurveHelper.Sigmoid(distance + 0.43f, 9f) * (1f - Sensitivity);
					float exponent = 1.7f + Sensitivity / 3f - MathF.Pow(50f, distance - 0.95f);

					distance = MathF.Sqrt(distance) * exponent * (sigmoid + Sensitivity);
					destination.SetPixel(position, (Color32)(Float3)distance.Clamp(0f, 1f));
				}
			);
		}

		bool TryGetPixel(Int2 position, out Float3 pixel)
		{
			if (position.x < 0 || position.y < 0 || position.x >= source.size.x || position.y >= source.size.y)
			{
				pixel = default;
				return false;
			}

			pixel = (Float3)source.GetPixel(position);
			return true;
		}
	}
}