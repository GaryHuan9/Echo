using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
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
		Assert.AreNotEqual(Point0, Point1);
		segment = new Segment2(Point0, Point1);
	}
}