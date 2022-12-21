using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

public interface IFresnel
{
	RGB128 Evaluate(float cosO);
}

public readonly struct RealFresnel : IFresnel
{
	public RealFresnel(float etaAbove, float etaBelow)
	{
		this.etaAbove = etaAbove;
		this.etaBelow = etaBelow;
	}

	public readonly float etaAbove;
	public readonly float etaBelow;

	public RGB128 Evaluate(float cosO) => new(Evaluate(ref cosO, out _, out _));

	public float Evaluate(ref float cosO, out float cosI, out float eta)
	{
		//Get the indices of reflection
		GetIndices(cosO, out float etaO, out float etaI);
		cosO = FastMath.Clamp01(FastMath.Abs(cosO));

		//Apply Snell's law
		eta = etaO / etaI;
		if (GetCosineTransmittance(cosO, eta, out cosI)) return 1f;

		//Fresnel equation
		return Apply(cosO, cosI, etaO, etaI);
	}

	/// <summary>
	/// Gets index of refraction values from an outgoing direction.
	/// </summary>
	/// <param name="cosO">The cosine phi value of the outgoing direction.</param>
	/// <param name="etaO">The index of refraction of the outgoing material.</param>
	/// <param name="etaI">The index of refraction of the incident material..</param>
	public void GetIndices(float cosO, out float etaO, out float etaI)
	{
		if (cosO >= 0f)
		{
			etaO = etaAbove;
			etaI = etaBelow;
		}
		else
		{
			//Swap indices of refraction for when outgoing is below
			etaO = etaBelow;
			etaI = etaAbove;
		}
	}

	static bool GetCosineTransmittance(float cosO, float eta, out float cosI)
	{
		float sinO2 = FastMath.OneMinus2(cosO);
		float sinI2 = eta * eta * sinO2;

		if (sinI2 >= 1f)
		{
			//Total internal reflection
			cosI = 0f;
			return true;
		}

		cosI = FastMath.Sqrt0(1f - sinI2);
		return false;
	}

	static float Apply(float cosO, float cosI, float etaO, float etaI)
	{
		float para0 = etaI * cosO;
		float para1 = etaO * cosI;

		float perp0 = etaO * cosO;
		float perp1 = etaI * cosI;

		float para = (para0 - para1) / (para0 + para1);
		float perp = (perp0 - perp1) / (perp0 + perp1);

		return (para * para + perp * perp) / 2f;
	}
}

public readonly struct ComplexFresnel : IFresnel
{
	public ComplexFresnel(in RGB128 etaAbove, in RGB128 etaBelow, in RGB128 extinction)
	{
		Ensure.IsFalse(etaAbove.IsZero);
		Ensure.IsFalse(etaBelow.IsZero);

		Float4 etaAboveR = RGB128.White / etaAbove;

		eta2 = etaBelow * etaAboveR;
		etaK2 = extinction * etaAboveR;

		eta2 *= eta2;
		etaK2 *= etaK2;
	}

	readonly Float4 eta2;  //(etaBelow   / etaAbove)^2
	readonly Float4 etaK2; //(extinction / etaAbove)^2

	public RGB128 Evaluate(float cosO) //https://seblagarde.wordpress.com/2013/04/29/memo-on-fresnel-equations/
	{
		cosO = FastMath.Clamp01(FastMath.Abs(cosO));

		float cosO2 = cosO * cosO;
		float sinO2 = 1f - cosO2;

		Float4 term = eta2 - etaK2 - (Float4)sinO2;
		Float4 a2b2 = Sqrt(term * term + 4f * eta2 * etaK2);

		//Parallel terms
		Float4 para0 = a2b2 + (Float4)cosO2;
		Float4 para1 = cosO * Scalars.Root2 * Sqrt(a2b2 + term);

		//Perpendicular terms
		Float4 perp0 = cosO2 * a2b2 + (Float4)(sinO2 * sinO2);
		Float4 perp1 = para1 * sinO2;

		//Combine the two terms
		Float4 para = (para0 - para1) / (para0 + para1);
		Float4 perp = (perp0 - perp1) / (perp0 + perp1);

		return (RGB128)(para * perp + para) / 2f;

		//OPTIMIZE:
		static Float4 Sqrt(in Float4 value) => new(FastMath.Sqrt0(value.X), FastMath.Sqrt0(value.Y), FastMath.Sqrt0(value.Z), 1f);
	}
}

public readonly struct PassthroughFresnel : IFresnel
{
	public RGB128 Evaluate(float cosO) => RGB128.White;
}