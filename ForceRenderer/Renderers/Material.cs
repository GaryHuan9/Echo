using CodeHelpers.Vectors;

namespace ForceRenderer.Renderers
{
	public class Material
	{
		public Float3 Albedo { get; set; }
		public Float3 Specular { get; set; }

		public PressedMaterial Pressed => new PressedMaterial(Albedo, Specular);
	}

	public readonly struct PressedMaterial
	{
		public PressedMaterial(Float3 albedo, Float3 specular)
		{
			this.albedo = albedo;
			this.specular = specular;
		}

		public readonly Float3 albedo;
		public readonly Float3 specular;
	}
}