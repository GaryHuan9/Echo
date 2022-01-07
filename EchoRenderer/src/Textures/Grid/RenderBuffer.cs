using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;

namespace EchoRenderer.Textures.Grid
{
	/// <summary>
	/// A regular <see cref="ArrayGrid"/> with albedo and normal auxiliary data.
	/// </summary>
	public class RenderBuffer : ArrayGrid
	{
		public RenderBuffer(Int2 size) : base(size)
		{
			albedos = new Float3[length];
			normals = new Float3[length];
			zDepths = new float[length];
		}

		readonly Float3[] albedos;
		readonly Float3[] normals;
		readonly float[] zDepths;

		public Float3 GetAlbedo(Int2 position) => albedos[ToIndex(position)];
		public Float3 GetNormal(Int2 position) => normals[ToIndex(position)];
		public float GetZDepth(Int2 position) => zDepths[ToIndex(position)];

		public void SetAlbedo(Int2 position, Float3 value) => albedos[ToIndex(position)] = value;
		public void SetNormal(Int2 position, Float3 value) => normals[ToIndex(position)] = value;
		public void SetZDepth(Int2 position, float value) => zDepths[ToIndex(position)] = value;

		/// <summary>
		/// Creates and returns an <see cref="ArrayGrid"/> texture to visualize the <see cref="albedos"/> data.
		/// </summary>
		public ArrayGrid CreateAlbedoTexture() => CreateTexture(albedos);

		/// <summary>
		/// Creates and returns an <see cref="ArrayGrid"/> texture to visualize the <see cref="normals"/> data.
		/// </summary>
		public ArrayGrid CreateNormalTexture() => CreateTexture(normals);

		public override void CopyFrom(Texture texture, bool parallel = true)
		{
			base.CopyFrom(texture, parallel);

			if (texture is not RenderBuffer buffer) return;
			AssertAlignedSize(buffer); //TODO: remove this assert and interpolate the axillary data as well

			Array.Copy(buffer.albedos, albedos, length);
			Array.Copy(buffer.normals, normals, length);
			Array.Copy(buffer.zDepths, zDepths, length);
		}

		/// <summary>
		/// Completely empties this <see cref="RenderBuffer"/>
		/// </summary>
		public virtual void Clear()
		{
			Array.Clear(pixels, 0, length);
			Array.Clear(albedos, 0, length);
			Array.Clear(normals, 0, length);
			Array.Clear(zDepths, 0, length);
		}

		/// <summary>
		/// Pins this <see cref="RenderBuffer"/> for various unmanaged access or pointer shenanigans.
		/// NOTE: Remember to dispose <see cref="Pin"/> after use to unpin the buffers and handles.
		/// </summary>
		public Pin CreatePin() => new(this);

		ArrayGrid CreateTexture(IReadOnlyList<Float3> data)
		{
			ArrayGrid texture = new ArrayGrid(size);
			texture.ForEach(SetPixel);

			return texture;

			void SetPixel(Int2 position)
			{
				Float4 color = Utilities.ToColor(data[ToIndex(position)]);
				texture[position] = Utilities.ToVector(color);
			}
		}

		//TODO: add serialization methods

		public class Pin : IDisposable
		{
			/// <summary>
			/// Pins data inside <paramref name="buffer"/> for unmanaged use and holds pins until disposed.
			/// </summary>
			public Pin(RenderBuffer buffer)
			{
				colourPointer = CreatePointer(buffer.pixels, out colourHandle);
				albedoPointer = CreatePointer(buffer.albedos, out albedoHandle);
				normalPointer = CreatePointer(buffer.normals, out normalHandle);
			}

			GCHandle colourHandle; //Cannot be readonly since GCHandle is a struct
			GCHandle albedoHandle;
			GCHandle normalHandle;

			public readonly IntPtr colourPointer;
			public readonly IntPtr albedoPointer;
			public readonly IntPtr normalPointer;

			public void Dispose()
			{
				colourHandle.Free();
				albedoHandle.Free();
				normalHandle.Free();
			}

			static IntPtr CreatePointer(object target, out GCHandle handle)
			{
				handle = GCHandle.Alloc(target, GCHandleType.Pinned);
				return handle.AddrOfPinnedObject();
			}
		}
	}
}