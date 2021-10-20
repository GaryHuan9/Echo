using System;

namespace EchoRenderer.Rendering.Scattering
{
	[Flags]
	public enum BidirectionalDistributionFunctionType
	{
		reflection   = 1 << 0,
		transmission = 1 << 1,
		diffuse      = 1 << 2,
		glossy       = 1 << 3,
		specular     = 1 << 4,
		all          = diffuse | glossy | specular | reflection | transmission
	}
}