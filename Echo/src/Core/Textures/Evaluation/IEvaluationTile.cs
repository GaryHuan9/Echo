using CodeHelpers.Packed;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures.Evaluation;

public interface IEvaluationTile
{
	Int2 Min { get; }
	Int2 Max { get; }

	sealed Int2 Size => Max - Min;
}

public interface IEvaluationWriteTile : IEvaluationTile
{
	Float4 this[Int2 position] { set; }
}

public interface IEvaluationReadTile : IEvaluationTile
{
	RGBA128 this[Int2 position] { get; }
}