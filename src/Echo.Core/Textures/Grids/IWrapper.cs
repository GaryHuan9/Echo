﻿using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;

namespace Echo.Core.Textures.Grids;

/// <summary>
/// A way to convert integer input texture positions for a <see cref="TextureGrid{T}"/> to acceptable bounds.
/// </summary>
public interface IWrapper
{
	public static readonly IWrapper clamp = new Clamp();
	public static readonly IWrapper repeat = new Repeat();
	public static readonly IWrapper mirror = new Mirror();

	/// <summary>
	/// Wraps the integer texture <paramref name="position"/> to be within the bounds of <paramref name="texture"/>.
	/// </summary>
	public void Wrap(TextureGrid texture, ref Int2 position);

	/// <summary>
	/// Wraps the packed integer texture coordinates from <paramref name="x"/> and
	/// <paramref name="y"/> to be within the bounds of <paramref name="texture"/>.
	/// </summary>
	public void Wrap(TextureGrid texture, ref Vector128<int> x, ref Vector128<int> y);

	private class Clamp : IWrapper
	{
		/// <inheritdoc/>
		public void Wrap(TextureGrid texture, ref Int2 position) => position = position.Clamp(Int2.Zero, texture.oneLess);

		/// <inheritdoc/>
		public void Wrap(TextureGrid texture, ref Vector128<int> x, ref Vector128<int> y)
		{
			if (Sse41.IsSupported)
			{
				x = Sse41.Max(Vector128<int>.Zero, Sse41.Min(x, Vector128.Create(texture.oneLess.X)));
				y = Sse41.Max(Vector128<int>.Zero, Sse41.Min(y, Vector128.Create(texture.oneLess.Y)));
			}
			else Fallback(texture, ref x, ref y);

			static void Fallback(TextureGrid texture, ref Vector128<int> x, ref Vector128<int> y)
			{
				Int2 max = texture.oneLess;

				//OPTIMIZE: with compare and bit mask 

				x = Vector128.Create
				(
					x.GetElement(0).Clamp(0, max.X), x.GetElement(1).Clamp(0, max.X),
					x.GetElement(2).Clamp(0, max.X), x.GetElement(3).Clamp(0, max.X)
				);

				y = Vector128.Create
				(
					y.GetElement(0).Clamp(0, max.Y), y.GetElement(1).Clamp(0, max.Y),
					y.GetElement(2).Clamp(0, max.Y), y.GetElement(3).Clamp(0, max.Y)
				);
			}
		}
	}

	private class Repeat : IWrapper
	{
		/// <inheritdoc/>
		public void Wrap(TextureGrid texture, ref Int2 position) => position = position.Repeat(texture.size);

		/// <inheritdoc/>
		public void Wrap(TextureGrid texture, ref Vector128<int> x, ref Vector128<int> y)
		{
			x = RepeatFast(x, texture.size.X, texture.power.X >= 0);
			y = RepeatFast(y, texture.size.Y, texture.power.Y >= 0);
		}
	}

	private class Mirror : IWrapper
	{
		/// <inheritdoc/>
		public void Wrap(TextureGrid texture, ref Int2 position)
		{
			Int2 length = texture.size * 2;
			Int2 repeated = position.Repeat(length);
			position = repeated.Min(length - Int2.One - repeated);
		}

		/// <inheritdoc/>
		public void Wrap(TextureGrid texture, ref Vector128<int> x, ref Vector128<int> y)
		{
			Int2 length = texture.size * 2;

			x = RepeatFast(x, length.X, texture.power.X >= 0);
			y = RepeatFast(y, length.Y, texture.power.Y >= 0);

			x = Sse41.Min(x, Sse2.Subtract(Vector128.Create(length.X - 1), x));
			y = Sse41.Min(y, Sse2.Subtract(Vector128.Create(length.Y - 1), y));
		}
	}

	static Vector128<int> RepeatFast(in Vector128<int> value, int length, bool isPowerOfTwo)
	{
		Ensure.AreEqual(length.IsPowerOfTwo(), isPowerOfTwo);

		if (!isPowerOfTwo) return Fallback(value, length);
		return Sse2.And(value, Vector128.Create(length - 1));

		static Vector128<int> Fallback(in Vector128<int> value, int length) => Vector128.Create
		(
			value.GetElement(0).Repeat(length),
			value.GetElement(1).Repeat(length),
			value.GetElement(2).Repeat(length),
			value.GetElement(3).Repeat(length)
		);
	}
}