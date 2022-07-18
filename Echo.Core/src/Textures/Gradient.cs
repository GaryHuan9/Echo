using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;

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
				int index = anchors.BinarySearch(new Anchor(percent, default));

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
		seal.EnsureNotApplied();

		int index = anchors.BinarySearch(new Anchor(percent, default));
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
		seal.EnsureNotApplied();

		int index = anchors.BinarySearch(new Anchor(percent, default));
		if (index < 0) return false;

		anchors.RemoveAt(index);
		return true;
	}

	IEnumerator<float> IEnumerable<float>.GetEnumerator() => anchors.Select(anchor => anchor.percent).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<float>)this).GetEnumerator();

	readonly struct Anchor : IComparable<Anchor>
	{
		public Anchor(float percent, in RGBA128 color)
		{
			this.percent = percent;
			this.color = color;
		}

		public readonly float percent;
		public readonly RGBA128 color;

		public int CompareTo(Anchor other) => percent.CompareTo(other.percent);
	}
}