using System;
using System.Collections.Generic;
using CodeHelpers.Packed;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometric;

public class SphereEntity : GeometricEntity, IGeometricEntity<PreparedSphere>
{
	float _radius = 1f;

	public float Radius
	{
		get => _radius;
		set => _radius = value < 0f ? throw new ArgumentOutOfRangeException(nameof(value)) : value;
	}

	uint IGeometricEntity<PreparedSphere>.Count => 1;

	public IEnumerable<PreparedSphere> Extract(SwatchExtractor extractor)
	{
		MaterialIndex material = extractor.Register(Material);
		Float3 position = ForwardTransform.MultiplyPoint(Float3.Zero);
		yield return new PreparedSphere(position, Radius, material);
	}
}