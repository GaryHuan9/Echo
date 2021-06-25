using System;
using System.Runtime.Intrinsics;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Textures
{
	/// <summary>
	/// The default <see cref="Texture2D"/>; stores RGBA color information with 32 bits per channel, supports full float range.
	/// </summary>
	public class Array2D : Texture2D
	{
		public Array2D(Int2 size) : base(size, Filters.bilinear)
		{
			length = size.Product;
			pixels = new Vector128<float>[length];
		}

		protected readonly int length;
		protected readonly Vector128<float>[] pixels;

		/// <summary>
		/// This is the axis in which <see cref="ToPosition"/> is going to move first if you increment the input index.
		/// </summary>
		public const int MajorAxis = 0;

		/// <summary>
		/// The opposite axis of <see cref="MajorAxis"/>.
		/// </summary>
		public const int MinorAxis = 1 - MajorAxis;

		public override Vector128<float> this[Int2 position]
		{
			get => pixels[ToIndex(position)];
			set => pixels[ToIndex(position)] = value;
		}

		/// <summary>
		/// Converts the integer pixel <paramref name="position"/> to an index [0, <see cref="length"/>)
		/// </summary>
		public int ToIndex(Int2 position)
		{
			Assert.IsTrue(Int2.zero <= position);
			Assert.IsTrue(position < size);

			return position.x + position.y * size.x;
		}

		/// <summary>
		/// Converts <paramref name="index"/> [0, <see cref="length"/>) to an integer pixel position
		/// </summary>
		public Int2 ToPosition(int index)
		{
			Assert.IsTrue(0 <= index && index < length);
			return new Int2(index % size.x, index / size.x);
		}

		public override void CopyFrom(Texture texture, bool parallel = true)
		{
			if (texture is Array2D array2D)
			{
				AssertAlignedSize(array2D);
				Array.Copy(array2D.pixels, pixels, length);
			}
			else base.CopyFrom(texture, parallel);
		}
	}
}