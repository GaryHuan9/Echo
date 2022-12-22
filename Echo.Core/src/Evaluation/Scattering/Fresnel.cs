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
		Ensure.IsTrue(etaAbove > 0f);
		Ensure.IsTrue(etaBelow > 0f);

		this.etaAbove = etaAbove;
		this.etaBelow = etaBelow;
	}

	readonly float etaAbove;
	readonly float etaBelow;

	public RGB128 Evaluate(float cosO)
	{
		//Get the indices of reflection
		var packet = CreateIncomplete(cosO);

		//Apply Snell's law & the Fresnel equation
		packet = packet.Complete;
		return new RGB128(packet.Value);
	}

	public Packet CreateIncomplete(float cosOutgoing) =>
		cosOutgoing > 0f //Swap indices of refraction for when outgoing is below
			? new Packet(etaAbove, etaBelow, cosOutgoing)
			: new Packet(etaBelow, etaAbove, cosOutgoing);

	public readonly struct Packet
	{
		/// <summary>
		/// Creates a new incomplete <see cref="Packet"/>.
		/// </summary>
		/// <remarks>The <see cref="cosIncident"/> of an incomplete <see cref="Packet"/> is <see cref="float.NaN"/>.</remarks>
		public Packet(float etaOutgoing, float etaIncident, float cosOutgoing) : this
		(
			etaOutgoing, etaIncident, cosOutgoing, float.NaN
		) { }

		Packet(float etaOutgoing, float etaIncident, float cosOutgoing, float cosIncident)
		{
			Ensure.IsTrue(etaOutgoing > 0f);
			Ensure.IsTrue(etaIncident > 0f);
			Ensure.IsTrue(cosOutgoing is >= -1f and <= 1f);

			this.etaOutgoing = etaOutgoing;
			this.etaIncident = etaIncident;
			this.cosOutgoing = cosOutgoing;
			this.cosIncident = cosIncident;
		}

		public readonly float etaOutgoing;
		public readonly float etaIncident;
		public readonly float cosOutgoing;
		public readonly float cosIncident;

		public Packet Complete
		{
			get
			{
				Ensure.IsTrue(IsIncomplete);

				float cosI = CalculateCosineIncident();
				Ensure.IsTrue(cosI is >= -1f and <= 1f);

				return new Packet(etaOutgoing, etaIncident, cosOutgoing, cosI);
			}
		}

		public bool TotalInternalReflection
		{
			get
			{
				Ensure.IsFalse(IsIncomplete);
				return FastMath.AlmostZero(cosIncident);
			}
		}

		public float Value
		{
			get
			{
				Ensure.IsFalse(IsIncomplete);
				if (TotalInternalReflection) return 1f;

				float cosO = FastMath.Abs(cosOutgoing);
				float cosI = FastMath.Abs(cosIncident);

				float para0 = etaIncident * cosO;
				float para1 = etaOutgoing * cosI;

				float perp0 = etaOutgoing * cosO;
				float perp1 = etaIncident * cosI;

				float para = (para0 - para1) / (para0 + para1);
				float perp = (perp0 - perp1) / (perp0 + perp1);

				return (para * para + perp * perp) / 2f;
			}
		}

		bool IsIncomplete => float.IsNaN(cosIncident);

		public Float3 Refract(in Float3 outgoing, in Float3 normal)
		{
			Ensure.IsFalse(IsIncomplete);
			Ensure.IsFalse(TotalInternalReflection);
			Ensure.AreEqual(outgoing.Dot(normal), cosOutgoing);

			float eta = etaOutgoing / etaIncident;
			float cosI = cosOutgoing < 0f ? cosIncident : -cosIncident;
			return (normal * (eta * cosOutgoing + cosI) - eta * outgoing).Normalized;
		}

		float CalculateCosineIncident()
		{
			float eta = etaOutgoing / etaIncident;
			float sinOutgoing2 = FastMath.OneMinus2(cosOutgoing);
			float sinIncident2 = eta * eta * sinOutgoing2;

			if (sinIncident2 >= 1f) return 0f;
			return FastMath.Sqrt0(1f - sinIncident2);
		}
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