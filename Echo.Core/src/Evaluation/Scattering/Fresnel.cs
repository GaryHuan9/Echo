using System.Runtime.CompilerServices;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

public readonly struct FresnelDielectric
{
	public FresnelDielectric(float etaAbove, float etaBelow)
	{
		this.etaAbove = etaAbove;
		this.etaBelow = etaBelow;
	}

	readonly float etaAbove;
	readonly float etaBelow;

	public RGB128 Evaluate(float cosI)
	{
		//Get the indices of reflection
		GetIndices(ref cosI, out float etaI, out float etaT);

		//Apply Snell's law
		if (GetCosineTransmittance(cosI, etaI / etaT, out float cosT)) return RGB128.White;

		//Fresnel equation
		return Apply(cosI, cosT, etaI, etaT);
	}

	public RGB128 Evaluate(in Float3 incoming, out Float3 transmit)
	{
		//Get the indices of reflection
		float cosI = BxDF.CosineP(incoming);
		GetIndices(ref cosI, out float etaI, out float etaT);

		//Apply Snell's law
		float eta = etaI / etaT;

		if (GetCosineTransmittance(cosI, eta, out float cosT))
		{
			transmit = default;
			return RGB128.White;
		}

		//Fresnel equation
		Float3 normal = BxDF.Normal(incoming);
		transmit = (eta * cosI - cosT) * normal - eta * incoming;
		transmit = transmit.Normalized;

		return Apply(cosI, cosT, etaI, etaT);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void GetIndices(ref float cosI, out float etaI, out float etaT)
	{
		etaI = etaAbove;
		etaT = etaBelow;
		cosI = FastMath.Clamp11(cosI);

		//Swap indices of refraction if needed
		if (cosI >= 0f) return;

		etaI = etaBelow;
		etaT = etaAbove;
		cosI = -cosI;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool GetCosineTransmittance(float cosI, float eta, out float cosT)
	{
		float sinT = eta * FastMath.Identity(cosI);

		if (sinT >= 1f)
		{
			//Total internal reflection
			cosT = 0f;
			return true;
		}

		cosT = FastMath.Identity(sinT);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static RGB128 Apply(float cosI, float cosT, float etaI, float etaT)
	{
		float paraHead = etaT * cosI;
		float paraTail = etaI * cosT;

		float perpHead = etaI * cosI;
		float perpTail = etaT * cosT;

		float para = (paraHead - paraTail) / (paraHead + paraTail);
		float perp = (perpHead - perpTail) / (perpHead + perpTail);

		return new RGB128(para * para + perp * perp) / 2f;

		// var m0 = Vector128.Create(etaT, etaI, etaI, etaT);
		// var m1 = Vector128.Create(cosI, cosT, cosI, cosT);
		// var m = Sse.Multiply(m0, m1);
		//
		// var s0 = Avx.Permute(m, 0b1111_0101);
		// var s1 = Avx.Permute(m, 0b1010_0000);
		// var s = Sse3.AddSubtract(s0, s1);
		//
		// var d0 = Avx.Permute(s, 0b1101_1101);
		// var d1 = Avx.Permute(s, 0b1000_1000);
		// var d = Sse.Divide(d0, d1);
		//
		// var dot = Sse.Multiply(Sse.Multiply(d, d), Vector128.Create(2f));
	}
}

public readonly struct FresnelConductor
{
	public FresnelConductor(in RGB128 etaAbove, in RGB128 etaBelow, in RGB128 absorption)
	{
		Float4 etaIncidentR = 1f / (Float4)etaAbove;

		eta2 = etaBelow * etaIncidentR;
		etaK2 = absorption * etaIncidentR;

		eta2 *= eta2;
		etaK2 *= etaK2;
	}

	readonly Float4 eta2;  //(etaBelow   / etaAbove)^2
	readonly Float4 etaK2; //(absorption / etaAbove)^2

	public RGB128 Evaluate(float cosI) //https://seblagarde.wordpress.com/2013/04/29/memo-on-fresnel-equations/
	{
		cosI = FastMath.Clamp11(cosI);

		float cosI2 = cosI * cosI;
		float sinI2 = 1f - cosI2;

		Float4 term = eta2 - etaK2 - (Float4)sinI2;
		Float4 a2b2 = Sqrt(term * term + 4f * eta2 * etaK2);

		//Parallel terms
		Float4 a = Sqrt((a2b2 + term) / 2f);
		Float4 paraHead = a2b2 + (Float4)cosI2;
		Float4 paraTail = cosI * a * 2f;

		//Perpendicular terms
		Float4 perpHead = cosI2 * a2b2 + (Float4)(sinI2 * sinI2);
		Float4 perpTail = paraTail * sinI2;

		//Combine the two terms
		Float4 para = (paraHead - paraTail) / (paraHead + paraTail);
		Float4 perp = (perpHead - perpTail) / (perpHead + perpTail);

		return (RGB128)(para * perp + para) / 2f;
	}

	static Float4 Sqrt(in Float4 value) => new(FastMath.Sqrt0(value.X), FastMath.Sqrt0(value.Y), FastMath.Sqrt0(value.Z), 1f);
}