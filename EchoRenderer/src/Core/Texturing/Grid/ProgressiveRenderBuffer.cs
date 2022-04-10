using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Common.Mathematics.Primitives;

namespace EchoRenderer.Core.Texturing.Grid;

/// <summary>
/// A slower <see cref="RenderBuffer"/> that is designed to be rendered on
/// progressively and can be easily displayed through a serialization byte array.
/// </summary>
public class ProgressiveRenderBuffer : RenderBuffer
{
	public ProgressiveRenderBuffer(Int2 size) : base(size)
	{
		bytes = new byte[length * 4];
		ClearSerializedByteArray();

		flagWidth = size[MajorAxis].CeiledDivide(FlagBlock);
		flags = new uint[flagWidth * size[MinorAxis]];

		lockers = new object[size[MinorAxis]];
		for (int i = 0; i < lockers.Length; i++) lockers[i] = new object();
	}

	/// <summary>
	/// An array buffer that stores the color information in a nicely serializable format (8 bit per channel RGBA).
	/// NOTE: should not be modified by outside sources! Alpha channel is always <see cref="byte.MaxValue"/>.
	/// </summary>
	public readonly byte[] bytes;

	/// <summary>
	/// When writing to one pixel, this special setter will write that same color to every pixel after this pixel in
	/// the same <see cref="RenderBuffer.MajorAxis"/> that has not been written. Auxiliary data will not be assigned.
	/// </summary>
	public override unsafe RGB128 this[Int2 position]
	{
		set
		{
			int index = ToIndex(position); //The global positional index of the pixel

			int offset = index - ToIndex(position.Replace(MajorAxis, 0));     //This pixel's offset from axis origin
			int flipped = ToIndex(position.ReplaceY(oneLess.Y - position.Y)); //Serialized byte array needs flipped Y axis index

			Color32 color32 = (Color32)(Float4)value.AlphaOne;  //Convert value to color32 with max alpha
			uint color = Unsafe.As<Color32, uint>(ref color32); //Bit align color32 to uint for chunk writing

			int segment = offset / FlagBlock;  //The local segment of the uint write flag array
			int location = offset % FlagBlock; //The location of this pixel's bit on the segment

			segment += flagWidth * position[MinorAxis]; //Shifts segment to global array index
			ref uint write = ref flags[segment];        //Fetch uint segment for writing

			lock (lockers[position[MinorAxis]])
			{
				write |= 1u << location;       //Reference write to flag segment
				uint flag = write >> location; //Jump to current flag bit

				fixed (RGB128* pointer0 = &pixels[index])    //Use pointers to assign to array in chunks
				fixed (byte* pointer1 = &bytes[flipped * 4]) //Create pointer using inverted index
				{
					//Write colors
					RGB128* pPixel = pointer0 - 1;
					uint* pBytes = (uint*)pointer1 - 1;

					for (int i = offset; i < size[MajorAxis]; i++)
					{
						//Write to arrays in blocks
						*++pPixel = value;
						*++pBytes = color;

						//Advance flag bit position
						if (++location == FlagBlock)
						{
							flag = flags[++segment];
							location = 0;
						}
						else flag >>= 1;

						//Check flag status, exit if wrote
						if ((flag & 0b1u) != 0u) break;
					}
				}
			}
		}
	}

	const int FlagBlock = sizeof(uint) * 8;

	readonly int flagWidth; //The number of uint flag segments for one major axis
	readonly uint[] flags;  //Array of flat segments indicating writing status

	readonly object[] lockers; //Lockers used for concurrent writing in the setter

	public override void CopyFrom(Texture texture, bool parallel = true)
	{
		base.CopyFrom(texture, parallel);

		if (texture is not ProgressiveRenderBuffer buffer) return;

		Array.Copy(buffer.bytes, bytes, bytes.Length);
		Array.Copy(buffer.flags, flags, flags.Length);
	}

	public override void Clear()
	{
		base.Clear();

		ClearSerializedByteArray();
		ClearWrittenFlagArray();
	}

	public void ClearWrittenFlagArray() => Array.Clear(flags, 0, flags.Length);

	unsafe void ClearSerializedByteArray()
	{
		if (bytes.Length == 0) return;
		var color32 = new Color32(0, 0, 0);

		fixed (byte* p = &bytes[0])
		{
			uint* pointer = (uint*)p;

			uint color = Unsafe.As<Color32, uint>(ref color32);
			for (int i = 0; i < length; i++) pointer[i] = color;
		}
	}
}