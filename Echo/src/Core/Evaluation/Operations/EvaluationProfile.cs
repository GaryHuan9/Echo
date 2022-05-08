using CodeHelpers;
using Echo.Common;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Grid;

namespace Echo.Core.Evaluation.Operations;

public record EvaluationProfile : IProfile
{
	/// <summary>
	/// The <see cref="PreparedScene"/> to evaluate.
	/// </summary>
	public PreparedScene Scene { get; init; }

	/// <summary>
	/// The fundamental evaluation method used.
	/// </summary>
	public Evaluator Evaluator { get; init; }

	/// <summary>
	/// The destination <see cref="RenderBuffer"/> to store the evaluated data.
	/// </summary>
	public RenderBuffer Buffer { get; init; }

	public virtual void Validate()
	{
		if (Scene == null) throw ExceptionHelper.Invalid(nameof(Scene), InvalidType.isNull);
		if (Evaluator == null) throw ExceptionHelper.Invalid(nameof(Evaluator), InvalidType.isNull);
		if (Buffer == null) throw ExceptionHelper.Invalid(nameof(Buffer), InvalidType.isNull);
	}
}