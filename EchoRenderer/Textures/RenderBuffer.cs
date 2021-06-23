using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Textures
{
	/// <summary>
	/// A regular <see cref="Array2D"/> with albedo and normal auxiliary data.
	/// </summary>
	public class RenderBuffer : Array2D
	{
		public RenderBuffer(Int2 size) : base(size)
		{
			albedos = new Float3[length];
			normals = new Float3[length];
		}

		readonly Float3[] albedos;
		readonly Float3[] normals;

		public Float3 GetAlbedo(Int2 position) => albedos[ToIndex(position)];
		public Float3 GetNormal(Int2 position) => normals[ToIndex(position)];

		public void SetAlbedo(Int2 position, Float3 value) => albedos[ToIndex(position)] = value;
		public void SetNormal(Int2 position, Float3 value) => normals[ToIndex(position)] = value;

		/// <summary>
		/// Creates and returns an <see cref="Array2D"/> texture to visualize the <see cref="albedos"/> data.
		/// </summary>
		public Array2D CreateAlbedoTexture() => CreateTexture(albedos);

		/// <summary>
		/// Creates and returns an <see cref="Array2D"/> texture to visualize the <see cref="normals"/> data.
		/// </summary>
		public Array2D CreateNormalTexture() => CreateTexture(normals);

		public override void CopyFrom(Texture texture, bool parallel = true)
		{
			base.CopyFrom(texture, parallel);

			if (texture is not RenderBuffer buffer) return;

			Array.Copy(buffer.albedos, albedos, length);
			Array.Copy(buffer.normals, normals, length);
		}

		/// <summary>
		/// Completely empties this <see cref="RenderBuffer"/>
		/// </summary>
		public void Clear()
		{
			Array.Clear(pixels, 0, length);
			Array.Clear(albedos, 0, length);
			Array.Clear(normals, 0, length);
		}

		/// <summary>
		/// Pins this <see cref="RenderBuffer"/> for various unmanaged access or pointer shenanigans.
		/// NOTE: Remember to dispose <see cref="Pin"/> after use to unpin the buffers and handles.
		/// </summary>
		public Pin CreatePin() => new Pin(this);

		Array2D CreateTexture(IReadOnlyList<Float3> data)
		{
			Array2D texture = new Array2D(size);
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