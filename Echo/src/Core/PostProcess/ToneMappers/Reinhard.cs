namespace Echo.Core.PostProcess.ToneMappers;

public class Reinhard : ToneMapper
{
	public Reinhard(PostProcessingEngine engine) : base(engine) { }

	/// <summary>
	/// The smallest exposure adjusted luminance that should be mapped to 1.
	/// </summary>
	public float WhitePoint { get; set; } = 1.5f;

	float inverse;

	//http://www.cmap.polytechnique.fr/~peyre/cours/x2005signal/hdr_photographic.pdf

	public override void Dispatch()
	{
		inverse = 1f / (WhitePoint * WhitePoint);
		base.Dispatch();
	}

	protected override float MapLuminance(float luminance)
	{
		float numerator = 1f + luminance * inverse;
		float denominator = 1f + luminance;

		return luminance * numerator / denominator;
	}
}