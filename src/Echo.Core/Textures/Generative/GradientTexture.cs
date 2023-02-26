using System;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures.Generative;

public class GradientTexture : CacheableTexture
{
	public GradientTexture() => UpdateSegment();

	NotNull<Gradient> _gradient = Gradient.black;

	public Gradient Gradient
	{
		get => _gradient;
		set => _gradient = value;
	}

	Float2 _point0 = Float2.Zero;
	Float2 _point1 = Float2.One;

	public Float2 Point0
	{
		get => _point0;
		set
		{
			_point0 = value;
			UpdateSegment();
		}
	}

	public Float2 Point1
	{
		get => _point1;
		set
		{
			_point1 = value;
			UpdateSegment();
		}
	}

	Segment2 segment;

	protected override RGBA128 Sample(Float2 position) => Gradient[segment.InverseLerp(position)];

	void UpdateSegment()
	{
		Ensure.AreNotEqual(Point0, Point1);
		segment = new Segment2(Point0, Point1);
	}

	readonly struct Segment2 : IEquatable<Segment2>
	{
		public Segment2(Float2 point0, Float2 point1)
		{
			this.point0 = point0;
			this.point1 = point1;
		}

		public readonly Float2 point0;
		public readonly Float2 point1;

		public Float2 LengthVector => point1 - point0;
		public Float2 Direction => LengthVector.Normalized;

		public float Length => LengthVector.Magnitude;
		public double LengthDouble => LengthVector.MagnitudeDouble;

		public float SquaredLength => LengthVector.SquaredMagnitude;
		public double SquaredLengthDouble => LengthVector.SquaredMagnitudeDouble;

		/// <summary>
		/// Linearly interpolates between <see cref="point0"/> and <see cref="point1"/> based on <paramref name="value"/>.
		/// NOTE: <paramref name="value"/> has a normal scale of between zero and one and does not need to be clamped.
		/// </summary>
		public Float2 Lerp(float value) => Float2.Lerp(point0, point1, value);

		/// <summary>
		/// Get the inverse lerp value to a point that is the closest to <paramref name="point"/>.
		/// NOTE: this value is not clamped; if needed you can clamp the value between 0 and 1.
		/// </summary>
		public float InverseLerp(Float2 point)
		{
			Float2 length = LengthVector;
			float lengthR = 1f / length.Magnitude;
			return Float2.Dot(point - point0, length * lengthR) * lengthR;
		}

		/// <summary>
		/// Get the unclamped inverse lerp to the point on this <see cref="Segment3"/> that is the closest to <paramref name="point"/>.
		/// </summary>
		public float ClosestInverseLerp(Float2 point)
		{
			Float2 length = LengthVector;
			float lengthR = 1f / length.Magnitude;
			return Float2.Dot(point - point0, length * lengthR) * lengthR;
		}

		public bool Equals(in Segment2 other) => point0 == other.point0 && point1 == other.point1;
		public override bool Equals(object obj) => obj is Segment2 other && Equals(other);

		bool IEquatable<Segment2>.Equals(Segment2 other) => Equals(other);

		public override int GetHashCode()
		{
			unchecked
			{
				return (point0.GetHashCode() * 397) ^ point1.GetHashCode();
			}
		}

		public override string ToString() => $"{point0} - {point1}";
	}
}