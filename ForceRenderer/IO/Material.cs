using System;
using System.Data;
using CodeHelpers.Mathematics;
using ForceRenderer.Textures;

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

			_transparency = source.Transparency;
			_transmission = source.Transmission;
			_indexOfRefraction = source.IndexOfRefraction;

			_diffuseTexture = source.DiffuseTexture;
			_specularTexture = source.SpecularTexture;

			_emissionTexture = source.EmissionTexture;
			_smoothnessTexture = source.SmoothnessTexture;

			_transparencyTexture = source.TransparencyTexture;
			_transmissionTexture = source.TransmissionTexture;
		}

		public Material() => isReadonly = false;

		public readonly bool isReadonly;

		Float3 _diffuse = Float3.one;
		Float3 _specular = Float3.zero;

		/// <summary>
		/// Controls the diffuse color. [0, 1]
		/// </summary>
		public Float3 Diffuse
		{
			get => _diffuse;
			set => CheckedSet(ref _diffuse, value);
		}

		/// <summary>
		/// Controls the specular color. [0, 1]
		/// </summary>
		public Float3 Specular
		{
			get => _specular;
			set => CheckedSet(ref _specular, value);
		}

		Float3 _emission = Float3.zero;
		float _smoothness = 0.15f;

		/// <summary>
		/// Controls the amount of light emitted. [0, inf)
		/// </summary>
		public Float3 Emission
		{
			get => _emission;
			set => CheckedSet(ref _emission, value);
		}

		/// <summary>
		/// Controls how smooth the specular surfaces is. [0, 1]
		/// </summary>
		public float Smoothness
		{
			get => _smoothness;
			set => CheckedSet(ref _smoothness, value);
		}

		float _transparency;
		Float3 _transmission = Float3.one;

		float _indexOfRefraction = 1f;

		/// <summary>
		/// Controls the amount of transparency. [0, 1]
		/// </summary>
		public float Transparency
		{
			get => _transparency;
			set => CheckedSet(ref _transparency, value);
		}

		/// <summary>
		/// Controls the color transmitted during refraction. [0, 1]
		/// </summary>
		public Float3 Transmission
		{
			get => _transmission;
			set => CheckedSet(ref _transmission, value);
		}

		/// <summary>
		/// Controls the index of refraction; default 1. [0.0001, 10]
		/// </summary>
		public float IndexOfRefraction
		{
			get => _indexOfRefraction;
			set => CheckedSet(ref _indexOfRefraction, value);
		}

		Texture2D _diffuseTexture = Texture2D.white;
		Texture2D _specularTexture = Texture2D.white;

		/// <summary>
		/// The color texture multiplied to diffuse.
		/// </summary>
		public Texture2D DiffuseTexture
		{
			get => _diffuseTexture;
			set => CheckedSet(ref _diffuseTexture, value);
		}

		/// <summary>
		/// The color texture multiplied to specular.
		/// </summary>
		public Texture2D SpecularTexture
		{
			get => _specularTexture;
			set => CheckedSet(ref _specularTexture, value);
		}

		Texture2D _emissionTexture = Texture2D.white;
		Texture2D _smoothnessTexture = Texture2D.white;

		/// <summary>
		/// The color texture multiplied to emission.
		/// </summary>
		public Texture2D EmissionTexture
		{
			get => _emissionTexture;
			set => CheckedSet(ref _emissionTexture, value);
		}

		/// <summary>
		/// The color texture multiplied to smoothness. Only the primary/red channel is considered.
		/// </summary>
		public Texture2D SmoothnessTexture
		{
			get => _smoothnessTexture;
			set => CheckedSet(ref _smoothnessTexture, value);
		}

		Texture2D _transparencyTexture = Texture2D.white;
		Texture2D _transmissionTexture = Texture2D.white;

		/// <summary>
		/// The value multiplied to transparency. Only the primary/red channel is considered.
		/// </summary>
		public Texture2D TransparencyTexture
		{
			get => _transparencyTexture;
			set => CheckedSet(ref _transparencyTexture, value);
		}

		/// <summary>
		/// The transmission color texture applied during refraction.
		/// </summary>
		public Texture2D TransmissionTexture
		{
			get => _transmissionTexture;
			set => CheckedSet(ref _transmissionTexture, value);
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
			smoothness = new TexturePair(material.Smoothness, material.SmoothnessTexture);

			transparency = new TexturePair(material.Transparency, material.TransparencyTexture);
			transmission = new TexturePair(material.Transmission, material.TransmissionTexture);
			indexOfRefraction = material.IndexOfRefraction;

			defaultSample = default;
			varies = diffuse.Varies || specular.Varies || emission.Varies || smoothness.Varies || transparency.Varies || transmission.Varies;

			defaultSample = new Sample(this, Float2.half, true);
		}

		readonly TexturePair diffuse;
		readonly TexturePair specular;

		readonly TexturePair emission;
		readonly TexturePair smoothness;

		readonly TexturePair transparency;
		readonly TexturePair transmission;
		readonly float indexOfRefraction;

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

				emission = material.emission.Varies || calculateAll ? material.emission.GetValue(texcoord).Max(Float3.zero) : material.defaultSample.emission;

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

				transparency = material.transparency.Varies || calculateAll ? material.transparency.GetValue(texcoord).x.Clamp(0f, 1f) : material.defaultSample.transparency;
				transmission = material.transmission.Varies || calculateAll ? material.transmission.GetValue(texcoord).Clamp(0f, 1f) : material.defaultSample.transmission;
				indexOfRefraction = calculateAll ? material.indexOfRefraction.Clamp(Scalars.Epsilon, 10f) : material.defaultSample.indexOfRefraction;
			}

			public readonly Float3 diffuse;
			public readonly Float3 specular;
			public readonly Float3 emission;

			public readonly float diffuseChance;
			public readonly float specularChance;

			public readonly float phongAlpha;
			public readonly float phongMultiplier;

			public readonly float transparency;
			public readonly Float3 transmission;
			public readonly float indexOfRefraction;
		}

		readonly struct TexturePair
		{
			public TexturePair(Float3 value, Texture2D texture)
			{
				if (texture.IsReadonly)
				{
					if (texture == Texture2D.white) texture = null;
					else if (texture == Texture2D.black)
					{
						texture = null;
						value = Float3.zero;
					}
				}
				else
				{
					texture = new Texture2D(texture); //NOTE: Currently all textures might be copied into Texture2D
					texture.SetReadonly();
				}

				this.value = value.Max(Float3.zero);
				this.texture = texture;
			}

			public TexturePair(float value, Texture2D texture) : this((Float3)value, texture) { }

			public readonly Float3 value;
			public readonly Texture2D texture;

			public bool Varies => texture != null;

			public Float3 GetValue(Float2 texcoord)
			{
				if (texture == null) return value;
				return value * texture[texcoord];
			}
		}
	}
}