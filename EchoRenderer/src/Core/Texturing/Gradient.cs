using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Core.Texturing.Generative;
using EchoRenderer.Core.Texturing.Grid;

namespace EchoRenderer.Core.Texturing;

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

	public static readonly Gradient black = new() { { 0f, Float4.Ana } };
	public static readonly Gradient white = new() { { 0f, Float4.One } };
	public static readonly Gradient blend = new() { { 0f, Float4.Ana }, { 1f, Float4.One } };

	public Float4 this[float percent] => Utilities.ToFloat4(SampleVector(percent));

	/// <summary>
	/// Inserts a new <paramref name="color"/> at <paramref name="percent"/> to this <see cref="Gradient"/>.
	/// </summary>
	public void Add(float percent, in Float4 color)
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
	/// Samples this <see cref="Gradient"/> and returns the vector color value at <paramref name="percent"/>.
	/// </summary>
	public Vector128<float> SampleVector(float percent)
	{
		if (anchors.Count == 0) throw new Exception("Cannot sample with zero anchor!");
		int index = anchors.BinarySearch(percent, Comparer.instance);

		if (index < 0) index = ~index;
		else return anchors[index].color;

		Anchor head = index == 0 ? anchors[index] : anchors[index - 1];
		Anchor tail = index == anchors.Count ? anchors[index - 1] : anchors[index];

		float time = Scalars.InverseLerp(head.percent, tail.percent, percent).Clamp();
		return PackedMath.Lerp(head.color, tail.color, Vector128.Create(time));
	}

	/// <summary>
	/// Draws this <see cref="Gradient"/> on <paramref name="texture"/> from <paramref name="point0"/> to <paramref name="point1"/>.
	/// </summary>
	public void Draw(TextureGrid texture, Float2 point0, Float2 point1) => texture.CopyFrom(new GradientTexture { Gradient = this, Point0 = point0, Point1 = point1 });

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
		public Anchor(float percent, in Float4 color)
		{
			this.percent = percent;
			this.color = Utilities.ToVector(color);
		}

		public readonly float percent;
		public readonly Vector128<float> color;
	}
}