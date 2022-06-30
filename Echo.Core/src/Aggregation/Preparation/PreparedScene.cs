using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Acceleration;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic;
using Echo.Core.Scenic.Geometric;
using Echo.Core.Scenic.Lighting;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Aggregation.Preparation;

/// <summary>
/// A <see cref="Scene"/> prepared ready for fast interactions.
/// </summary>
public class PreparedScene : PreparedPack
{
	public PreparedScene(ReadOnlySpan<IGeometrySource> geometrySources, ReadOnlySpan<ILightSource> lightSources,
						 ImmutableArray<PreparedInstance> instances, in AcceleratorCreator acceleratorCreator,
						 SwatchExtractor swatchExtractor, ImmutableArray<InfiniteLight> infiniteLights)
		: base(geometrySources, lightSources, instances, in acceleratorCreator, swatchExtractor)
	{
		this.infiniteLights = infiniteLights;
		rootInstance = new PreparedInstance(this, geometries.swatch, Float4x4.identity);
	}

	public readonly ImmutableArray<InfiniteLight> infiniteLights;

	readonly PreparedInstance rootInstance;

	/// <summary>
	/// Processes the <paramref name="query"/> and returns whether it intersected with something.
	/// </summary>
	public bool Trace(ref TraceQuery query)
	{
		Assert.AreEqual(query.current, new TokenHierarchy());
		if (!FastMath.Positive(query.distance)) return false;
		float original = query.distance;

		accelerator.Trace(ref query);
		return query.distance < original;
	}

	/// <summary>
	/// Processes the <paramref name="query"/> and returns whether it is occluded by something.
	/// </summary>
	public bool Occlude(ref OccludeQuery query)
	{
		Assert.AreEqual(query.current, new TokenHierarchy());
		if (!FastMath.Positive(query.travel)) return false;
		return accelerator.Occlude(ref query);
	}

	/// <summary>
	/// Interacts with a concluded <see cref="TraceQuery"/> that was performed
	/// on this <see cref="PreparedScene"/> by creating a <see cref="Contact"/>.
	/// </summary>
	public Contact Interact(in TraceQuery query)
	{
		query.AssertHit();

		ref readonly var instance = ref FindLayer(query.token, out _, out Float4x4 inverseTransform);
		Contact.Info info = instance.pack.geometries.GetContactInfo(query.token.TopToken, query.uv);

		Float3 normal = inverseTransform.MultiplyDirection(info.normal).Normalized;
		return new Contact(query, normal, instance.swatch[info.material], info.texcoord);
	}

	ref readonly PreparedInstance FindLayer(in TokenHierarchy hierarchy, out Float4x4 forwardTransform, out Float4x4 inverseTransform)
	{
		forwardTransform = Float4x4.identity;
		inverseTransform = Float4x4.identity;

		ref readonly PreparedInstance instance = ref rootInstance;

		//Check if we can exit early
		if (hierarchy.InstanceCount == 0) return ref instance;

		//Traverse down the instancing hierarchy
		foreach (ref readonly EntityToken token in hierarchy.Instances)
		{
			Assert.AreEqual(token.Type, TokenType.Instance);
			instance = ref instance.pack.geometries.instances.ItemRef(token.Index);

			//Because we traverse in reverse, we must also multiply the transform in reverse
			forwardTransform = instance.forwardTransform * forwardTransform;
			inverseTransform = instance.inverseTransform * inverseTransform;
		}

		return ref instance;
	}
}