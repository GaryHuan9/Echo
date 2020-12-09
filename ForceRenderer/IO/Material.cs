using System;
using System.Data;
using System.Drawing.Drawing2D;
using CodeHelpers.Vectors;

namespace ForceRenderer.IO
{
	public class Material
	{
		public Material(Material source, bool isReadonly)
		{
			this.isReadonly = isReadonly;

			_diffuse = source.Diffuse;
			_specular = source.Specular;

			_emission = source.Emission;
			_smoothness = source.Smoothness;

			_diffuseTexture = source.DiffuseTexture;
			_specularTexture = source.SpecularTexture;

			_emissionTexture = source.EmissionTexture;
			_smoothnessTexture = source.SmoothnessTexture;
		}

		public Material() => isReadonly = false;

		public readonly bool isReadonly;

		Float3 _diffuse;
		Float3 _specular;

		public Float3 Diffuse
		{
			get => _diffuse;
			set => CheckedSet(ref _diffuse, value);
		}

		public Float3 Specular
		{
			get => _specular;
			set => CheckedSet(ref _specular, value);
		}

		Float3 _emission;
		float _smoothness;

		public Float3 Emission
		{
			get => _emission;
			set => CheckedSet(ref _emission, value);
		}

		public float Smoothness
		{
			get => _smoothness;
			set => CheckedSet(ref _smoothness, value);
		}

		Texture _diffuseTexture = Texture.white;
		Texture _specularTexture = Texture.white;

		public Texture DiffuseTexture
		{
			get => _diffuseTexture;
			set => CheckedSet(ref _diffuseTexture, value);
		}

		public Texture SpecularTexture
		{
			get => _specularTexture;
			set => CheckedSet(ref _specularTexture, value);
		}

		Texture _emissionTexture = Texture.white;
		Texture _smoothnessTexture = Texture.white;

		public Texture EmissionTexture
		{
			get => _emissionTexture;
			set => CheckedSet(ref _emissionTexture, value);
		}

		public Texture SmoothnessTexture
		{
			get => _smoothnessTexture;
			set => CheckedSet(ref _smoothnessTexture, value);
		}

		void CheckedSet<T>(ref T location, T value)
		{
			if (!isReadonly) location = value;
			else throw new ReadOnlyException();
		}
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

		public Sample GetSample(Float2 texcoord) => varies ? new Sample(this, texcoord, false) : defaultSample;

		public readonly struct Sample
		{
			public Sample(in PressedMaterial material, Float2 texcoord, bool calculateAll)
			{
				diffuse = material.diffuse.Varies || calculateAll ? material.diffuse.GetValue(texcoord).Clamp(0f, 1f) : material.defaultSample.diffuse;
				specular = material.specular.Varies || calculateAll ? material.specular.GetValue(texcoord).Clamp(0f, 1f) : material.defaultSample.specular;

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

				emission = material.emission.Varies || calculateAll ? material.emission.GetValue(texcoord) : material.defaultSample.emission;

				if (material.smoothness.Varies || calculateAll)
				{
					float smoothness = material.smoothness.GetValue(texcoord).x.Clamp(0f, 1f);

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

			public Float3 GetValue(Float2 texcoord)
			{
				if (texture == null) return value;
				return value * (Float3)texture.GetPixel(texcoord);
			}
		}
	}
}