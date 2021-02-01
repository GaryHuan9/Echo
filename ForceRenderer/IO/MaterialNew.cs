using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;

namespace ForceRenderer.IO
{
	public abstract class MaterialNew
	{
		public abstract Float3 BidirectionalDistribution(in Ray ray, in Hit hit, in Float3 normal, out Float3 direction);
	}
}