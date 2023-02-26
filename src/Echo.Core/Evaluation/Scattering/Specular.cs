using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

public sealed class SpecularReflection<TFresnel> : BxDF where TFresnel : IFresnel
{
	public SpecularReflection() : base(FunctionType.Specular | FunctionType.Reflective) { }

	public void Reset(in TFresnel newFresnel) => fresnel = newFresnel;

	TFresnel fresnel;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
	{
		incident = Reflect(outgoing);

		float cosO = CosineP(outgoing);
		float cosI = CosineP(incident);

		RGB128 evaluated = fresnel.Evaluate(cosO);
		return (evaluated / FastMath.Abs(cosI), 1f);
	}

	public static Float3 Reflect(in Float3 outgoing) => new(-outgoing.X, -outgoing.Y, outgoing.Z);
}

public sealed class SpecularTransmission : BxDF
{
	public SpecularTransmission() : base(FunctionType.Specular | FunctionType.Transmissive) { }

	public void Reset(RealFresnel newFresnel) => fresnel = newFresnel;

	RealFresnel fresnel;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
	{
		var packet = fresnel.CreateIncomplete(CosineP(outgoing)).Complete;

		if (packet.TotalInternalReflection)
		{
			incident = default;
			return Probable<RGB128>.Impossible;
		}

		float evaluated = 1f - packet.Value;

		incident = packet.Refract(outgoing, Float3.Forward);
		evaluated /= FastMath.Abs(CosineP(incident));

		return (new RGB128(evaluated), 1f);
	}
}

public sealed class SpecularFresnel : BxDF
{
	public SpecularFresnel() : base(FunctionType.Specular | FunctionType.Reflective | FunctionType.Transmissive) { }

	public void Reset(RealFresnel newFresnel) => fresnel = newFresnel;

	RealFresnel fresnel;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => RGB128.Black;
	public override float ProbabilityDensity(in Float3 outgoing, in Float3 incident) => 0f;

	public override Probable<RGB128> Sample(Sample2D sample, in Float3 outgoing, out Float3 incident)
	{
		var packet = fresnel.CreateIncomplete(CosineP(outgoing)).Complete;

		float evaluated = packet.Value;

		if (sample.x < evaluated)
		{
			//Perform specular reflection
			incident = SpecularReflection<RealFresnel>.Reflect(outgoing);
		}
		else
		{
			//Perform specular transmission
			evaluated = 1f - evaluated;
			incident = packet.Refract(outgoing, Float3.Forward);
		}

		return (new RGB128(evaluated) / FastMath.Abs(CosineP(incident)), evaluated);
	}
}