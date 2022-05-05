using System.Collections.Generic;
using System.Linq;
using CodeHelpers;
using CodeHelpers.Packed;
using Echo.Core.Compute;

namespace Echo.Core.Evaluation.Operations;

public class TiledEvaluationOperation : Operation
{
	public TiledEvaluationProfile Profile { get; set; }

	TiledEvaluationProfile profile;
	Int2[] tilePositionSequence;

	public override void Prepare()
	{
		base.Prepare();

		//Validate profile
		profile = Profile ?? throw ExceptionHelper.Invalid(nameof(Profile), InvalidType.isNull);
		profile.Validate();

		//Create tile sequence
		Int2 size = profile.Buffer.size.CeiledDivide(profile.TileSize);
		tilePositionSequence = profile.Pattern.CreateSequence(size);
	}

	protected override bool Execute(ulong procedure, Scheduler scheduler)
	{
		if (procedure >= (ulong)tilePositionSequence.Length) return false;

		Int2 min = tilePositionSequence[procedure] * profile.TileSize;
		Int2 max = profile.Buffer.size.Min(min + (Int2)profile.TileSize);

		for (int y = min.Y; y < max.Y; y++)
		for (int x = min.X; x < max.X; x++)
		{

		}

		return true;
	}
}