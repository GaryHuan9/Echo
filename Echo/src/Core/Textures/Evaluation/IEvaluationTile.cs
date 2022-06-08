using CodeHelpers.Packed;

namespace Echo.Core.Textures.Evaluation;

public interface IEvaluationTile
{
	Int2 Min { get; }
	Int2 Max { get; }

	sealed Int2 Size => Max - Min;

	Float4 this[Int2 position] { set; }
}