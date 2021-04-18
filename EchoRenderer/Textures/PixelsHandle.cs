using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Textures
{
	public class PixelsHandle : IDisposable
	{
		/// <summary>
		/// Pins <paramref name="pixels"/> for unmanaged use and hold pin until disposed.
		/// </summary>
		public PixelsHandle(Float4[] pixels) : this((object)pixels) { }

		/// <summary>
		/// Pins <paramref name="pixels"/> for unmanaged use and hold pin until disposed.
		/// </summary>
		public PixelsHandle(Vector128<float>[] pixels) : this((object)pixels) { }

		PixelsHandle(object pixels)
		{
			handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
			pointer = handle.AddrOfPinnedObject();
		}

		GCHandle handle;

		public readonly IntPtr pointer;

		public void Dispose() => handle.Free();
	}
}