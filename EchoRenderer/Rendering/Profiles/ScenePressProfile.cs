using CodeHelpers;
using EchoRenderer.Mathematics.Accelerators;

namespace EchoRenderer.Rendering.Profiles
{
	public record ScenePressProfile : IProfile
	{
		/// <summary>
		/// The <see cref="TraceAcceleratorProfile"/> used for this <see cref="ScenePressProfile"/>.
		/// This determines the kind of <see cref="TraceAccelerator"/> to build. Must not be null.
		/// </summary>
		public TraceAcceleratorProfile AcceleratorProfile { get; init; } = new();

		/// <summary>
		/// How many times does the area of a triangle has to be over the average of all triangles to trigger a fragmentation.
		/// Fragmentation can cause the construction of better <see cref="TraceAccelerator"/>, however it can also backfire.
		/// </summary>
		public float FragmentationThresholdMultiplier { get; init; } = 4.8f;

		/// <summary>
		/// The maximum number of fragmentation that can happen to one source triangle.
		/// Note that we can completely disable fragmentation by setting this value to 0.
		/// </summary>
		public int FragmentationMaxIteration { get; init; } = 3;

		public void Validate()
		{
			if (AcceleratorProfile == null) throw ExceptionHelper.Invalid(nameof(AcceleratorProfile), InvalidType.isNull);
		}
	}
}