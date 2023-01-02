using System.Threading;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

/// <summary>
/// Stamps the <see cref="Echo"/> logo on the bottom right corner of the image.
/// </summary>
public record Watermark : ICompositeLayer
{
	/// <summary>
	/// The label of the layer to operate on.
	/// </summary>
	public string TargetLayer { get; init; } = "main";

	/// <summary>
	/// The size of the watermark.
	/// </summary>
	public float Scale { get; init; } = 0.15f;
	
	CropGrid<RGB128> cropWorker;
	CropGrid<RGB128> cropTarget;

	float tint;

	const float MarginSize = 0.14f;
	const float BlurDeviation = 0.38f;
	const float BackgroundTint = 0.2f;
	const float LuminanceThreshold = 0.35f;

	static TextureGrid<RGBA128> _logoTexture;

	static TextureGrid<RGBA128> LogoTexture
	{
		get
		{
			var texture = Volatile.Read(ref _logoTexture);
			if (texture != null) return texture;
			
			texture = TextureGrid.Load<RGBA128>("ext/Logo/logo-white.png");
			Volatile.Write(ref _logoTexture, texture);
			return texture;
		}
	}

	public async ComputeTask ExecuteAsync(ICompositeContext context)
	{
		var sourceTexture = context.GetWriteTexture<RGB128>(TargetLayer);
		TextureGrid<RGBA128> logoTexture = LogoTexture;
		
		//Find size and position
		float width = sourceTexture.LogSize * Scale;
		Float2 margin = (Float2)MarginSize * width;
		Float2 size = new Float2(width, width * logoTexture.aspects.Y);
		Float2 center = new Float2(sourceTexture.size.X - size.X);

		// Float2 size = new Float2(logoTexture.aspects.X * width, width) + margin;
		// Float2 position = sourceTexture.size.X_ + (size / 2f + margin) * new Float2(-1f, 1f);
		//
		// Int2 min = (position - size / 2f).Floored;
		// Int2 max = (position + size / 2f).Ceiled + Int2.One;
		//
		// //Allocate resources for full resolution Gaussian blur
		// float deviation = width * BlurDeviation;
		//
		// using var handle = CopyTemporaryBuffer(out ArrayGrid<RGB128> workerBuffer);
		// using var blur = new GaussianBlur(this, workerBuffer, deviation);
		//
		// //Start watermark stamping passes
		// cropWorker = workerBuffer.Crop(min, max);
		// cropTarget = renderBuffer.Crop(min, max);
		//
		// using var grab = new LuminanceGrab(this, cropWorker);
		//
		// blur.Run(); //Run Gaussian blur
		// grab.Run(); //Run luminance grab
		//
		// bool lightMode = grab.Luminance > LuminanceThreshold;
		// tint = lightMode ? 1f + BackgroundTint : 1f - BackgroundTint;
		//
		// RunPass(TintPass, cropWorker); //Copies buffer
		//
		// //Write label
		// style = style with { Color = new RGBA128(lightMode ? 0f : 1f) };
		// font.Draw(renderBuffer, Label, position, style);
	}

	void TintPass(Int2 position) => cropTarget[position] = cropWorker[position] * tint;
}