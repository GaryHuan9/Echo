using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

/// <summary>
/// Denoiser using Intel's Open Image Denoise.
/// https://www.openimagedenoise.org
/// </summary>
[EchoSourceUsable]
public record OidnDenoise : ICompositeLayer
{
	/// <summary>
	/// The label of the layer to operate on.
	/// </summary>
	[EchoSourceUsable]
	public string TargetLayer { get; init; } = "main";

	/// <summary>
	/// The label of the clear albedo layer to read from.
	/// </summary>
	[EchoSourceUsable]
	public string AlbedoLayer { get; init; } = "albedo";

	/// <summary>
	/// The label of the clear <see cref="NormalDepth128"/> layer to read from.
	/// </summary>
	[EchoSourceUsable]
	public string NormalLayer { get; init; } = "normal_depth";

	/// <summary>
	/// Whether to denoise auxiliary input (the albedo and normal).
	/// </summary>
	/// <remarks>This can result in better denoising at the cost of longer execution times.</remarks>
	[EchoSourceUsable]
	public bool PrefilterAuxiliary { get; init; } = true;

	const string DllPath = "OpenImageDenoise";

	public async ComputeTask ExecuteAsync(ICompositeContext context)
	{
		Int2 size = context.RenderSize;

		try
		{
			//Retrieve textures and fill copy content to buffers
			var sourceTexture = context.GetWriteTexture<RGB128>(TargetLayer);
			bool hasAlbedo = context.TryGetTexture(AlbedoLayer, out TextureGrid<RGB128> albedoTexture);
			bool hasNormal = context.TryGetTexture(NormalLayer, out TextureGrid<NormalDepth128> normalTexture);

			float[] colorBuffer = await CopyToBuffer(sourceTexture);
			float[] albedoBuffer = hasAlbedo ? await CopyToBuffer(albedoTexture) : null;
			float[] normalBuffer = hasNormal ? await CopyToBuffer(normalTexture) : null;

			//Denoise and copy result back to texture
			Denoise(size, colorBuffer, albedoBuffer, normalBuffer);
			await CopyFromBuffer(colorBuffer, sourceTexture);
		}
		catch (DllNotFoundException) { throw new CompositeException("Precompiled Oidn binaries not found."); }

		async ComputeTask<float[]> CopyToBuffer<T>(TextureGrid<T> sourceTexture) where T : unmanaged, IColor<T>
		{
			Ensure.AreEqual(sourceTexture.size, size);
			float[] buffer = GC.AllocateUninitializedArray<float>(size.Product * 3);

			await context.RunAsync(MainPass, size);
			return buffer;

			void MainPass(Int2 position)
			{
				Float4 source = sourceTexture[position].ToFloat4();
				int index = (position.Y * size.X + position.X) * 3;

				buffer[index + 0] = source.X;
				buffer[index + 1] = source.Y;
				buffer[index + 2] = source.Z;
			}
		}

		ComputeTask CopyFromBuffer(float[] buffer, SettableGrid<RGB128> targetTexture)
		{
			Ensure.AreEqual(targetTexture.size, size);
			return context.RunAsync(MainPass, size);

			void MainPass(Int2 position)
			{
				int index = (position.Y * size.X + position.X) * 3;
				targetTexture.Set(position, new RGB128(buffer[index], buffer[index + 1], buffer[index + 2]));
			}
		}
	}

	unsafe void Denoise(Int2 size, float[] colorBuffer, float[] albedoBuffer, float[] normalBuffer)
	{
		using OidnDevice device = OidnDevice.CreateNew();
		using OidnFilter filter = OidnFilter.CreateNew(device, "RT");

		fixed (float* albedoPointer = albedoBuffer)
		fixed (float* normalPointer = normalBuffer)
		{
			if (PrefilterAuxiliary)
			{
				if (albedoPointer != null)
				{
					using OidnFilter albedoFilter = OidnFilter.CreateNew(device, "RT");
					albedoFilter.Set("albedo", albedoPointer, size);
					albedoFilter.Set("output", albedoPointer, size);
					albedoFilter.Commit();
					albedoFilter.Execute();
				}

				if (normalPointer != null)
				{
					using OidnFilter normalFilter = OidnFilter.CreateNew(device, "RT");
					normalFilter.Set("normal", normalPointer, size);
					normalFilter.Set("output", normalPointer, size);
					normalFilter.Commit();
					normalFilter.Execute();
				}
			}

			fixed (float* colorPointer = colorBuffer)
			{
				//Set input buffers and parameters
				filter.Set("hdr", true);
				filter.Set("cleanAux", PrefilterAuxiliary);
				filter.Set("color", colorPointer, size);
				filter.Set("albedo", albedoPointer, size);
				filter.Set("normal", normalPointer, size);

				//Output to the color buffer
				filter.Set("output", colorPointer, size);
				filter.Commit();
				filter.Execute();
			}
		}
	}

	readonly struct OidnDevice : IDisposable
	{
#pragma warning disable CS0169
		readonly IntPtr handle;
#pragma warning restore CS0169

		public static OidnDevice CreateNew()
		{
			OidnDevice device = oidnNewDevice(0);
			oidnSetDeviceErrorFunction(device, ProcessError, IntPtr.Zero);

			device.Commit();
			return device;
		}

		public void Set(string name, bool value) => oidnSetDevice1b(this, name, value);
		public void Set(string name, int value) => oidnSetDevice1i(this, name, value);

		public bool GetBoolean(string name) => oidnGetDevice1b(this, name);
		public int GetInt32(string name) => oidnGetDevice1i(this, name);

		public void Commit() => oidnCommitDevice(this);
		public void Dispose() => oidnReleaseDevice(this);

		[DoesNotReturn]
		static void ProcessError(IntPtr userPtr, OidnError code, string message) => throw new CompositeException($"Oidn error: {message} ({code}).");

		[DllImport(DllPath)]
		static extern OidnDevice oidnNewDevice(int type);

		[DllImport(DllPath)]
		static extern void oidnSetDevice1b(OidnDevice device, string name, bool value);

		[DllImport(DllPath)]
		static extern void oidnSetDevice1i(OidnDevice device, string name, int value);

		[DllImport(DllPath)]
		static extern bool oidnGetDevice1b(OidnDevice device, string name);

		[DllImport(DllPath)]
		static extern int oidnGetDevice1i(OidnDevice device, string name);

		[DllImport(DllPath)]
		static extern void oidnCommitDevice(OidnDevice device);

		[DllImport(DllPath)]
		static extern void oidnReleaseDevice(OidnDevice device);

		[DllImport(DllPath)]
		static extern void oidnSetDeviceErrorFunction(OidnDevice device, ErrorFunction func, IntPtr userPtr);

		delegate void ErrorFunction(IntPtr userPtr, OidnError code, string message);
	}

	readonly struct OidnFilter : IDisposable
	{
#pragma warning disable CS0169
		readonly IntPtr handle;
#pragma warning restore CS0169

		public static OidnFilter CreateNew(OidnDevice device, string type) => oidnNewFilter(device, type);

		public unsafe void Set(string name, float* texture, Int2 size)
		{
			if (texture == null) oidnRemoveFilterImage(this, name);
			else oidnSetSharedFilterImage(this, name, new IntPtr(texture), ImageFormat.Float3, size.X, size.Y, 0, 0, 0);
		}

		public void Set(string name, bool value) => oidnSetFilter1b(this, name, value);
		public void Set(string name, int value) => oidnSetFilter1i(this, name, value);
		public void Set(string name, float value) => oidnSetFilter1f(this, name, value);

		public bool GetBoolean(string name) => oidnGetFilter1b(this, name);
		public int GetInt32(string name) => oidnGetFilter1i(this, name);
		public float GetSingle(string name) => oidnGetFilter1f(this, name);

		public void Commit() => oidnCommitFilter(this);
		public void Execute() => oidnExecuteFilter(this);
		public void Dispose() => oidnReleaseFilter(this);

		[DllImport(DllPath)]
		static extern OidnFilter oidnNewFilter(OidnDevice device, string type);

		[DllImport(DllPath)]
		static extern void oidnCommitFilter(OidnFilter filter);

		[DllImport(DllPath)]
		static extern void oidnSetSharedFilterImage(OidnFilter filter, string name, IntPtr ptr, ImageFormat format, int width, int height, int byteOffset, int bytePixelStride, int byteRowStride);

		[DllImport(DllPath)]
		static extern void oidnRemoveFilterImage(OidnFilter filter, string name);

		[DllImport(DllPath)]
		static extern void oidnSetFilter1b(OidnFilter filter, string name, bool value);

		[DllImport(DllPath)]
		static extern void oidnSetFilter1i(OidnFilter filter, string name, int value);

		[DllImport(DllPath)]
		static extern void oidnSetFilter1f(OidnFilter filter, string name, float value);

		[DllImport(DllPath)]
		static extern bool oidnGetFilter1b(OidnFilter filter, string name);

		[DllImport(DllPath)]
		static extern int oidnGetFilter1i(OidnFilter filter, string name);

		[DllImport(DllPath)]
		static extern float oidnGetFilter1f(OidnFilter filter, string name);

		[DllImport(DllPath)]
		static extern void oidnExecuteFilter(OidnFilter filter);

		[DllImport(DllPath)]
		static extern void oidnReleaseFilter(OidnFilter filter);

		enum ImageFormat
		{
			Undefined = 0,
			Float = 1,
			Float2 = 2,
			Float3 = 3,
			Float4 = 4
		}
	}

	enum OidnError
	{
		None = 0,
		Unknown = 1,
		InvalidArgument = 2,
		InvalidOperation = 3,
		OutOfMemory = 4,
		UnsupportedHardware = 5,
		Cancelled = 6
	}
}