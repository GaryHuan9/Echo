using CodeHelpers;
using CodeHelpers.Vectors;
using ForceRenderer.IO;

namespace ForceRenderer.Renderers
{
	public class Denoiser
	{
		public Denoiser(Texture source, Texture buffer)
		{
			if (source.size != buffer.size) throw ExceptionHelper.Invalid(nameof(buffer), buffer, $"does not match the size of '{nameof(source)}'!");

			this.source = source;
			this.buffer = buffer;
		}

		public readonly Texture source;
		public readonly Texture buffer;

		const int KernelRadius = 1;

		public void Dispatch()
		{
			foreach (Int2 position in source.size.Loop())
			{
				Float3 total = default;
				int pixelCount = 0;

				for (int x = -KernelRadius; x <= KernelRadius; x++)
				{
					for (int y = -KernelRadius; y <= KernelRadius; y++)
					{
						if (!TryGetPixel(position + new Int2(x, y), out Float3 pixel)) continue;

						total += pixel;
						pixelCount++;
					}
				}

				buffer.SetPixel(position, (Color32)(total / pixelCount));
			}
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