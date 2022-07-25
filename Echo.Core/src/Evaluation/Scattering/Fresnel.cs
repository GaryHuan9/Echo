using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

public interface IFresnel
{
	RGB128 Evaluate(float cosI);
}

public readonly struct DielectricFresnel : IFresnel
{
	public DielectricFresnel(float etaAbove, float etaBelow)
	{
		this.etaAbove = etaAbove;
		this.etaBelow = etaBelow;
	}

	public readonly float etaAbove;
	public readonly float etaBelow;

	public RGB128 Evaluate(float cosI) => new(Evaluate(ref cosI, out _, out _));

	public float Evaluate(ref float cosI, out float cosT, out float eta)
	{
		//Get the indices of reflection
		GetIndices(ref cosI, out float etaI, out float etaT);

		//Apply Snell's law
		eta = etaI / etaT;
		if (GetCosineTransmittance(cosI, eta, out cosT)) return 1f;

		//Fresnel equation
		return Apply(cosI, cosT, etaI, etaT);
	}

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

	static bool GetCosineTransmittance(float cosI, float eta, out float cosT)
	{
		float sinI2 = FastMath.OneMinus2(cosI);
		float sinT2 = eta * eta * sinI2;

		if (sinT2 >= 1f)
		{
			//Total internal reflection
			cosT = 0f;
			return true;
		}

		cosT = FastMath.Sqrt0(1f - sinT2);
		return false;
	}

	static float Apply(float cosI, float cosT, float etaI, float etaT)
	{
		float para0 = etaT * cosI;
		float para1 = etaI * cosT;

		float perp0 = etaI * cosI;
		float perp1 = etaT * cosT;

		float para = (para0 - para1) / (para0 + para1);
		float perp = (perp0 - perp1) / (perp0 + perp1);

		return (para * para + perp * perp) / 2f;
	}
}

public readonly struct ConductorFresnel : IFresnel
{
	public ConductorFresnel(in RGB128 etaAbove, in RGB128 etaBelow, in RGB128 absorption)
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

		//OPTIMIZE:
		static Float4 Sqrt(in Float4 value) => new(FastMath.Sqrt0(value.X), FastMath.Sqrt0(value.Y), FastMath.Sqrt0(value.Z), 1f);
	}
}

public readonly struct PassthroughFresnel : IFresnel
{
	public RGB128 Evaluate(float cosI) => RGB128.White;
}