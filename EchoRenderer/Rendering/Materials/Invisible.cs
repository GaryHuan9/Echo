using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;

namespace EchoRenderer.Rendering.Materials
{
	/// <summary>
	/// Represents a completely invisible material. The <see cref="PressedScene"/>
	/// should omit all geometry tagged with this material.
	/// </summary>
	public class Invisible : Material
	{
		public override Float3 Emit(in HitQuery query, ExtendedRandom random) => Float3.zero;

		public override Float3 BidirectionalScatter(in HitQuery query, ExtendedRandom random, out Float3 direction)
		{
			direction = query.ray.direction;
			return Float3.one;
		}
	}
}