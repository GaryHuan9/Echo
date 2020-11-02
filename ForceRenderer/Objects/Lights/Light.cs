using CodeHelpers.Vectors;

namespace ForceRenderer.Objects.Lights
{
	public abstract class Light : Object
	{
		public Float3 Intensity { get; set; } = Float3.one;
	}
}