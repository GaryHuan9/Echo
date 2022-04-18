using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Common.Mathematics;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Generative;
using Echo.Core.Textures.Grid;

namespace Echo.Core.Textures;

/// <summary>
/// A blend of colors defined at specific percentages which linearly fade between each other.
/// </summary>
public class Gradient : IEnumerable<float>
{
	static Gradient()
	{
		black.seal.Apply();
		white.seal.Apply();
		blend.seal.Apply();
	}

	readonly List<Anchor> anchors = new();

	Seal seal;

	public static readonly Gradient black = new() { { 0f, RGBA128.Black } };
	public static readonly Gradient white = new() { { 0f, RGBA128.White } };
	public static readonly Gradient blend = new() { { 0f, RGBA128.Black }, { 1f, RGBA128.White } };

	/// <summary>
	/// Samples this <see cref="Gradient"/> and returns the <see cref="RGBA128"/> color value at <paramref name="percent"/>.
	/// </summary>
	public RGBA128 this[float percent]
	{
		get
		{
			{
				if (anchors.Count == 0) throw new Exception("Cannot sample with zero anchor!");
				int index = anchors.BinarySearch(percent, Comparer.instance);

				if (index < 0) index = ~index;
				else return anchors[index].color;

				Anchor head = index == 0 ? anchors[index] : anchors[index - 1];
				Anchor tail = index == anchors.Count ? anchors[index - 1] : anchors[index];

				float time = Scalars.InverseLerp(head.percent, tail.percent, percent);
				return (RGBA128)Float4.Lerp(head.color, tail.color, FastMath.Clamp01(time));
			}
		}
	}

	/// <summary>
	/// Inserts a new <paramref name="color"/> at <paramref name="percent"/> to this <see cref="Gradient"/>.
	/// </summary>
	public void Add(float percent, in RGBA128 color)
	{
		seal.AssertNotApplied();

		int index = anchors.BinarySearch(percent, Comparer.instance);
		Anchor anchor = new Anchor(percent, color);

		if (index >= 0) anchors[index] = anchor;
		else anchors.Insert(~index, anchor);
	}

	/// <summary>
	/// Removes a color that was added with <see cref="Add"/> at <paramref name="percent"/>.
	/// Returns whether the <see cref="Remove"/> operation was successfully completed.
	/// </summary>
	public bool Remove(float percent)
	{
		seal.AssertNotApplied();

		int index = anchors.BinarySearch(percent, Comparer.instance);
		if (index < 0) return false;

		anchors.RemoveAt(index);
		return true;
	}

	/// <summary>
	/// Draws this <see cref="Gradient"/> on <paramref name="texture"/> from <paramref name="point0"/> to <paramref name="point1"/>.
	/// </summary>
	public void Draw(TextureGrid<RGBA128> texture, Float2 point0, Float2 point1) => texture.CopyFrom(new GradientTexture { Gradient = this, Point0 = point0, Point1 = point1 });

	IEnumerator<float> IEnumerable<float>.GetEnumerator() => anchors.Select(anchor => anchor.percent).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<float>)this).GetEnumerator();

	class Comparer : IDoubleComparer<Anchor, float>
	{
		public static readonly Comparer instance = new();

		public int CompareTo(Anchor first, float second)
		{
			if (first.percent.AlmostEquals(second)) return 0;
			return first.percent.CompareTo(second);
		}
	}

	readonly struct Anchor
	{
		public Anchor(float percent, in RGBA128 color)
		{
			this.percent = percent;
			this.color = color;
		}

		public readonly float percent;
		public readonly RGBA128 color;
	}
}