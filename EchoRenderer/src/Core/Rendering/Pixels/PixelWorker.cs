using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics.Randomization;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Rendering.Distributions.Continuous;

namespace EchoRenderer.Core.Rendering.Pixels;

public abstract class PixelWorker
{
	protected ContinuousDistribution SourceDistribution { get; private set; }

	/// <summary>
	/// Invoked once before a new rendering process begin on this <see cref="PixelWorker"/>.
	/// </summary>
	public void Prepare(RenderProfile profile) => SourceDistribution = CreateDistribution(profile);

	/// <summary>
	/// Returns an object with base type <see cref="Arena"/> which will be passed into the subsequent invocations to <see cref="Render"/>.
	/// NOTE: This method will be invoked after <see cref="Prepare"/>, and it will be invoked once on every rendering thread.
	/// <param name="seed">Can be null or a unique number that varies between each thread.</param>
	/// </summary>
	public Arena CreateArena(RenderProfile profile, uint? seed)
	{
		Arena arena = CreateArena(profile);

		arena.Distribution = SourceDistribution.Replicate();
		arena.Distribution.Prng = CreateRandom(seed);

		return arena;
	}

	/// <summary>
	/// Renders a <see cref="Sample"/> at <paramref name="uv"/>.
	/// </summary>
	/// <param name="uv">
	///     The screen percentage point to work on. X should be normalized and between -0.5 to 0.5;
	///     Y should have the same scale as X and it would depend on the aspect ratio.
	/// </param>
	/// <param name="profile">The <see cref="RenderProfile"/> to be used.</param>
	/// <param name="arena">The <see cref="Arena"/> to be used.</param>
	public abstract Sample Render(Float2 uv, RenderProfile profile, Arena arena);

	/// <summary>
	/// Invoked once before a new rendering process begins on this <see cref="PixelWorker"/>.
	/// Can be used to prepare the worker for future invocations to <see cref="Render"/>.
	/// Should create and return a source <see cref="ContinuousDistribution"/> that will be used.
	/// </summary>
	protected virtual ContinuousDistribution CreateDistribution(RenderProfile profile) => new UniformDistribution(profile.TotalSample);

	/// <summary>
	/// Creates a new <see cref="Arena"/> to be used for this <see cref="PixelWorker"/>.
	/// Override this method if a different <see cref="Arena"/> child type is needed.
	/// </summary>
	protected virtual Arena CreateArena(RenderProfile profile) => new();

	/// <summary>
	/// Optionally creates a <see cref="IRandom"/> with an optional <paramref name="seed"/>. If this method does not return null,
	/// its returned value will be assigned to <see cref="ContinuousDistribution.Prng"/> created in <see cref="CreateDistribution"/>.
	/// </summary>
	protected virtual IRandom CreateRandom(uint? seed = null) => new SquirrelRandom(seed);

	public readonly struct Sample
	{
		public Sample(in Float3 colour, in Float3 albedo = default, in Float3 normal = default, float zDepth = default)
		{
			this.colour = colour;
			this.albedo = albedo;
			this.normal = normal;
			this.zDepth = zDepth;
		}

		public readonly Float3 colour; //We use the British spelling here so that all the names line up (sort of)
		public readonly Float3 albedo;
		public readonly Float3 normal;
		public readonly float zDepth;

		public bool IsNaN => float.IsNaN(colour.x) || float.IsNaN(colour.y) || float.IsNaN(colour.z) ||
							 float.IsNaN(albedo.x) || float.IsNaN(albedo.y) || float.IsNaN(albedo.z) ||
							 float.IsNaN(normal.x) || float.IsNaN(normal.y) || float.IsNaN(normal.z) ||
							 float.IsNaN(zDepth);

		public static implicit operator Sample(in Float3 colour) => new(colour);
	}
}