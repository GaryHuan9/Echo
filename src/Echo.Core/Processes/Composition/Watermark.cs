using System.Threading;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

/// <summary>
/// Stamps the <see cref="Echo"/> logo on the bottom right corner of the image.
/// </summary>
[EchoSourceUsable]
public record Watermark : ICompositeLayer
{
	/// <summary>
	/// The name of the layer to operate on.
	/// </summary>
	[EchoSourceUsable]
	public string LayerName { get; init; } = "main";

	/// <summary>
	/// The size of the watermark.
	/// </summary>
	[EchoSourceUsable]
	public float Scale { get; init; } = 0.25f;

	/// <inheritdoc/>
	[EchoSourceUsable]
	public bool Enabled { get; init; } = true;

	const float MarginScale = 0.07f;
	const float DeviationIntensity = 9.5f;
	const float LuminanceThreshold = 0.1f;
	const float BlurTint = 0.115f;
	const float ShadowTint = 0.8f;

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
		var sourceTexture = context.GetWriteTexture<RGB128>(LayerName);

		//Find size and position
		float width = sourceTexture.LogSize * Scale;
		int margin = (MarginScale * width).Round();
		Int2 size = FindPlacementSize(width);

		if (!(size <= sourceTexture.size)) return; //Did not find an appropriate size, source is too small

		Int2 borderMin = new Int2(sourceTexture.size.X - size.X - margin * 2, 0);
		Int2 borderMax = new Int2(sourceTexture.size.X, size.Y + margin * 2);
		Int2 strictMin = new Int2(sourceTexture.size.X - margin - size.X, margin);
		Int2 strictMax = new Int2(sourceTexture.size.X - margin, margin + size.Y);

		//Crop out the target region and fetch needed buffers
		using var _0 = context.FetchTemporaryTexture(out var workerTexture);
		var sourceCrop = sourceTexture.Crop(borderMin, borderMax);
		var workerCrop = workerTexture.Crop(borderMin, borderMax);
		var fetchTask = FetchResizedLogo(context, sourceCrop.size, strictMax - strictMin, out var logoTexture);

		//Perform blur and determine tint
		var grabTask = GrabLuminance(context, sourceCrop);
		await context.CopyAsync(sourceCrop, workerCrop);
		await context.GaussianBlurAsync(workerCrop, DeviationIntensity);
		float tintDirection = await grabTask > LuminanceThreshold ? -1f : 1f;

		//Stamp logo using the blurred texture and add shadow to create depth
		using var _1 = await fetchTask;
		await context.RunAsync(StampPass, sourceCrop.size);
		await context.CopyAsync(logoTexture, workerCrop);
		await context.GaussianBlurAsync(workerCrop, DeviationIntensity);
		await context.RunAsync(ShadowPass, sourceCrop.size);

		void StampPass(Int2 position)
		{
			float intensity = logoTexture.Get(position).Luminance;
			Float4 source = sourceCrop.Get(position);
			Float4 blurred = workerCrop.Get(position) * (1f + tintDirection * BlurTint);
			sourceCrop.Set(position, (RGB128)Float4.Lerp(source, blurred, intensity));
		}

		void ShadowPass(Int2 position)
		{
			float intensity = logoTexture.Get(position).Luminance * tintDirection * ShadowTint;
			float multiplier = 1f + intensity * (1f - workerCrop.Get(position).Luminance);
			sourceCrop.Set(position, sourceCrop.Get(position) * multiplier);
		}
	}

	static Int2 FindPlacementSize(float width)
	{
		Int2 logo = LogoTexture.size;
		Ensure.IsTrue(logo.X >= logo.Y);
		float aspect = (float)logo.Y / logo.X;

		int x = (int)width;
		int y;

		do y = (++x * aspect).Round();
		while (y * logo.X != x * logo.Y);

		return new Int2(x, y);
	}

	static async ComputeTask<float> GrabLuminance(ICompositeContext context, TextureGrid<RGB128> sourceTexture)
	{
		var locker = new SpinLock();
		Int2 size = sourceTexture.size;
		Summation total = Summation.Zero;

		await context.RunAsync(MainPass, size.Y);
		return ((RGB128)total.Result).Luminance / size.Product;

		void MainPass(uint y)
		{
			Summation rowTotal = Summation.Zero;

			for (int x = 0; x < size.X; x++) rowTotal += sourceTexture[new Int2(x, (int)y)];

			bool lockTaken = false;

			try
			{
				locker.Enter(ref lockTaken);
				total += rowTotal;
			}
			finally
			{
				if (lockTaken) locker.Exit();
			}
		}
	}

	static ComputeTask<ICompositeContext.PoolReleaseHandle> FetchResizedLogo(ICompositeContext context, Int2 borderSize,
																			 Int2 strictSize, out SettableGrid<RGB128> logoTexture)
	{
		Int2 margin = (borderSize - strictSize) / 2;
		Ensure.IsTrue(margin.X > 0 && margin.Y > 0);
		Ensure.AreEqual(margin * 2, borderSize - strictSize);

		var handle = context.FetchTemporaryTexture(out logoTexture, borderSize);

		var logoBorder = logoTexture;
		var logoStrict = logoBorder.Crop(margin, borderSize - margin);
		Ensure.AreEqual(logoStrict.size, strictSize);

		return Convert();

		async ComputeTask<ICompositeContext.PoolReleaseHandle> Convert()
		{
			var logoAlpha = new ArrayGrid<RGBA128>(logoStrict.size);
			var copyTask0 = context.CopyAsync(Pure.black, logoBorder);
			var copyTask1 = context.CopyAsync(LogoTexture, logoAlpha);

			await copyTask0;
			await copyTask1;
			await context.RunAsync(MainPass, logoStrict.size);

			return handle;

			void MainPass(Int2 position) => logoStrict.Set(position, (RGB128)logoAlpha[position].AlphaMultiply);
		}
	}
}