using Echo.Core.Common.Packed;

namespace Echo.Core.Common.Mathematics.Randomization;

public class FractionalBrownianMotion
{
	public FractionalBrownianMotion(Simplex simplex)
	{
		this.simplex = simplex;
		RecalculateMaxAmplitude();
	}

	public readonly Simplex simplex;

	uint _layerCount = 1;
	float _persistence = 0.5f;

	/// <summary>
	/// The number of layers sampled.
	/// Directly proportional to sample performance.
	/// </summary>
	public uint LayerCount
	{
		get => _layerCount;
		set
		{
			if (_layerCount == value) return;

			_layerCount = value;
			RecalculateMaxAmplitude();
		}
	}

	/// <summary>
	/// The amplitude multiplier across each layer.
	/// This is almost always 0.5 (default).
	/// </summary>
	public float Persistence
	{
		get => _persistence;
		set
		{
			if (_persistence.AlmostEquals(value)) return;

			_persistence = value;
			RecalculateMaxAmplitude();
		}
	}

	float _scale = 1f;
	float _lacunarity = 2f;

	/// <summary>
	/// The frequency of the entire fBM.
	/// The smaller the value, the more clustered the wave is.
	/// </summary>
	public float Scale
	{
		get => _scale;
		set
		{
			if (_scale.AlmostEquals(value)) return;

			_scale = value;
			inverseScale = 1f / value;
		}
	}

	/// <summary>
	/// The frequency multiplier between each layer.
	/// This is almost always 2 (default).
	/// </summary>
	public float Lacunarity
	{
		get => _lacunarity;
		set
		{
			if (_lacunarity.AlmostEquals(value)) return;

			_lacunarity = value;
		}
	}

	float inverseAmplitude; //1f / Max Amplitude
	float inverseScale = 1f;

	/// <summary>
	/// Samples this fBM using the indicated parameters.
	/// </summary>
	public float Sample(Float2 position)
	{
		float result = 0f;

		float amplitude = 1f;
		float frequency = 1f;

		for (int i = 0; i < LayerCount; i++)
		{
			Float2 point = position * inverseScale + (Float2)i;
			result += simplex.Sample(point * frequency) * amplitude;

			amplitude *= Persistence;
			frequency *= Lacunarity;
		}

		return result * inverseAmplitude;
	}

	void RecalculateMaxAmplitude()
	{
		inverseAmplitude = 0f;
		float amplitude = 1f;

		for (int i = 0; i < LayerCount; i++)
		{
			inverseAmplitude += amplitude;
			amplitude *= Persistence;
		}

		inverseAmplitude = 1f / inverseAmplitude;
	}
}