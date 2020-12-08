using System;
using System.Drawing.Drawing2D;
using CodeHelpers.Vectors;

namespace ForceRenderer.IO
{
	public class Material
	{
		public Float3 Diffuse { get; set; }
		public Float3 Specular { get; set; }

		public Float3 Emission { get; set; }
		public float Smoothness { get; set; }

		public Texture DiffuseTexture { get; set; } = Texture.white;
		public Texture SpecularTexture { get; set; } = Texture.white;

		public Texture EmissionTexture { get; set; } = Texture.white;
		public Texture SmoothnessTexture { get; set; } = Texture.white;
	}

	public readonly struct PressedMaterial
	{
		public PressedMaterial(Material material)
		{
			diffuse = new TexturePair(material.Diffuse, material.DiffuseTexture);
			specular = new TexturePair(material.Specular, material.SpecularTexture);

			emission = new TexturePair(material.Emission, material.EmissionTexture);
			smoothness = new TexturePair((Float3)material.Smoothness, material.SmoothnessTexture);

			defaultSample = default;
			varies = diffuse.Varies || specular.Varies || emission.Varies || smoothness.Varies;

			defaultSample = new Sample(this, Float2.half, true);
		}

		readonly TexturePair diffuse;
		readonly TexturePair specular;

		readonly TexturePair emission;
		readonly TexturePair smoothness;

		readonly Sample defaultSample;
		readonly bool varies;

		public Sample GetSample(Float2 uv) => varies ? new Sample(this, uv, false) : defaultSample;

		public readonly struct Sample
		{
			public Sample(in PressedMaterial material, Float2 uv, bool calculateAll)
			{
				diffuse = material.diffuse.Varies || calculateAll ? material.diffuse.GetValue(uv).Clamp(0f, 1f) : material.defaultSample.diffuse;
				specular = material.specular.Varies || calculateAll ? material.specular.GetValue(uv).Clamp(0f, 1f) : material.defaultSample.specular;

				if (material.diffuse.Varies || material.specular.Varies || calculateAll)
				{
					diffuse = diffuse.Min(Float3.one - specular); //Albedo and specular combined cannot be larger than one

					diffuseChance = diffuse.Average;
					specularChance = specular.Average;

					float sum = diffuseChance + specularChance;

					if (Scalars.AlmostEquals(sum, 0f))
					{
						diffuseChance = 1f;
						specularChance = 0f;
					}
					else
					{
						diffuseChance /= sum;
						specularChance /= sum;

						if (Scalars.AlmostEquals(diffuseChance, 0f)) specularChance = 1f;
						if (Scalars.AlmostEquals(specularChance, 0f)) diffuseChance = 1f;
					}
				}
				else
				{
					diffuseChance = material.defaultSample.diffuseChance;
					specularChance = material.defaultSample.specularChance;
				}

				emission = material.emission.Varies || calculateAll ? material.emission.GetValue(uv) : material.defaultSample.emission;

				if (material.smoothness.Varies || calculateAll)
				{
					float smoothness = material.smoothness.GetValue(uv).x.Clamp(0f, 1f);

					phongAlpha = MathF.Pow(1200f, smoothness * 1.3f) - 1f;
					phongMultiplier = (phongAlpha + 2f) / (phongAlpha + 1f);
				}
				else
				{
					phongAlpha = material.defaultSample.phongAlpha;
					phongMultiplier = material.defaultSample.phongMultiplier;
				}
			}

			public readonly Float3 diffuse;
			public readonly Float3 specular;
			public readonly Float3 emission;

			public readonly float diffuseChance;
			public readonly float specularChance;

			public readonly float phongAlpha;
			public readonly float phongMultiplier;
		}

		readonly struct TexturePair
		{
			public TexturePair(Float3 value, Texture texture)
			{
				if (texture.isReadonly)
				{
					if (texture == Texture.white) texture = null;
					else if (texture == Texture.black)
					{
						texture = null;
						value = Float3.zero;
					}
				}
				else texture = new Texture(texture, true);

				this.value = value.Max(Float3.zero);
				this.texture = texture;
			}

			public readonly Float3 value;
			public readonly Texture texture;

			public bool Varies => texture != null;

			public Float3 GetValue(Float2 uv)
			{
				if (texture == null) return value;
				return value * (Float3)texture.GetPixel(uv);
			}
		}
	}
}