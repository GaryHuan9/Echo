using System;
using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;

namespace EchoRenderer.Textures
{
	/// <summary>
	/// A slower <see cref="RenderBuffer"/> that can be easily displayed through a serialization byte array.
	/// </summary>
	public class DisplayBuffer : RenderBuffer
	{
		public DisplayBuffer(Int2 size) : base(size)
		{
			bytes = new byte[length * 4];
			for (int i = 3; i < bytes.Length; i += 4) bytes[i] = byte.MaxValue;
		}

		/// <summary>
		/// An array buffer that stores the color information in a nicely serializable format (8 bit per channel RGBA).
		/// NOTE: Should not be modified by outside sources! Alpha channel is always <see cref="byte.MaxValue"/>.
		/// </summary>
		public readonly byte[] bytes;

		public override Vector128<float> this[Int2 position]
		{
			get => base[position];
			set
			{
				base[position] = value;

				Color32 color = (Color32)Utilities.ToFloat4(value);
				Int2 inverted = position.ReplaceY(oneLess.y - position.y);

				int index = ToIndex(inverted) * 4;

				bytes[index + 0] = color.r;
				bytes[index + 1] = color.g;
				bytes[index + 2] = color.b;
				bytes[index + 3] = byte.MaxValue;
			}
		}

		public override void CopyFrom(Texture texture, bool parallel = true)
		{
			base.CopyFrom(texture, parallel);

			if (texture is not DisplayBuffer displayBuffer) return;
			Array.Copy(displayBuffer.bytes, bytes, length * 4);
		}

		public override void Clear()
		{
			base.Clear();

			Array.Clear(bytes, 0, length * 4);
		}
	}
}