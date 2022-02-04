using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;

namespace EchoRenderer.PostProcess;

/// <summary>
/// Denoiser using Intel's Open Image Denoise
/// https://www.openimagedenoise.org
/// </summary>
public class DenoiseOidn : PostProcessingWorker
{
	public DenoiseOidn(PostProcessingEngine engine) : base(engine) { }

	Float3[] colors; //Unmanaged buffer for Oidn

	const string DllPath = "OpenImageDenoise";

	public override unsafe void Dispatch()
	{
		try
		{
			using OidnDevice device = OidnDevice.CreateNew(OidnDevice.Type.automatic);
			using OidnFilter filter = OidnFilter.CreateNew(device);

			Int2 size = renderBuffer.size;
			colors = new Float3[size.Product];

			RunPass(ForwardPass); //Copies color to unmanaged buffer

			fixed (Float3* colourPointer = colors)
			{
				using var pin = renderBuffer.CreatePin();

				filter.Set("color", colourPointer, size);
				filter.Set("albedo", pin.albedoPointer, size);
				filter.Set("normal", pin.normalPointer, size);

				//Output to the same buffer
				filter.Set("output", colourPointer, size);

				filter.Set("hdr", true);
				filter.Commit();

				filter.Execute();
			}

			RunPass(BackwardPass); //Copies denoised data back to renderBuffer
		}
		catch (DllNotFoundException exception)
		{
			Console.WriteLine($"Oidn libraries not found: {exception}");
		}
	}

	void ForwardPass(Int2 position)
	{
		int index = renderBuffer.ToIndex(position);
		Vector128<float> source = renderBuffer[position];
		colors[index] = Utilities.ToFloat4(source).XYZ;
	}

	void BackwardPass(Int2 position)
	{
		Float3 data = colors[renderBuffer.ToIndex(position)];
		renderBuffer[position] = Utilities.ToVector(Utilities.ToColor(data));
	}

	readonly struct OidnDevice : IDisposable
	{
		readonly IntPtr handle;

		public static OidnDevice CreateNew(Type type)
		{
			OidnDevice device = oidnNewDevice(type);
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

		static void ProcessError(IntPtr pointer, OidnError code, string message)
		{
			DebugHelper.Log($"{nameof(DenoiseOidn)} error! {message} ({code})");
		}

		[DllImport(DllPath)]
		static extern OidnDevice oidnNewDevice(Type type);

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
		static extern void oidnSetDeviceErrorFunction(OidnDevice device, ErrorAction func, IntPtr userPtr);

		public enum Type
		{
			automatic = 0,
			cpu = 1
		}

		delegate void ErrorAction(IntPtr pointer, OidnError code, string message);
	}

	readonly struct OidnFilter : IDisposable
	{
		readonly IntPtr handle;

		public static OidnFilter CreateNew(OidnDevice device) => oidnNewFilter(device, "RT");

		/// <inheritdoc cref="Set(string,IntPtr,Int2)"/>
		public unsafe void Set(string name, Float3* texture, Int2 size) => Set(name, new IntPtr((float*)texture), size);

		/// <summary>
		/// Assigns <paramref name="texture"/> with <paramref name="size"/> under the name <paramref name="name"/>.
		/// </summary>
		public void Set(string name, IntPtr texture, Int2 size)
		{
			oidnSetSharedFilterImage(this, name, texture, Format.float3, size.x, size.y, 0, 0, 0);
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
		static extern void oidnSetSharedFilterImage(OidnFilter filter, string name, IntPtr ptr, Format format, int width, int height, int byteOffset, int bytePixelStride, int byteRowStride);

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

		enum Format
		{
			undefined = 0,
			float1 = 1,
			float2 = 2,
			float3 = 3,
			float4 = 4
		}
	}

	enum OidnError
	{
		none = 0,
		unknown = 1,
		invalidArgument = 2,
		invalidOperation = 3,
		outOfMemory = 4,
		unsupportedHardware = 5,
		cancelled = 6
	}
}