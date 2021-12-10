using System;
using System.Collections.Generic;
using CodeHelpers;
using EchoRenderer.Mathematics.Accelerators;
using EchoRenderer.Mathematics.Intersections;

namespace EchoRenderer.Rendering.Profiles
{
	public record TraceAcceleratorProfile : IProfile
	{
		/// <summary>
		/// Explicitly indicate the type of <see cref="TraceAccelerator"/> to use.
		/// This can be left as null for automatic accelerator determination.
		/// </summary>
		public Type AcceleratorType { get; init; }

		public void Validate()
		{
			if (AcceleratorType?.IsSubclassOf(typeof(TraceAccelerator)) == false) throw ExceptionHelper.Invalid(nameof(AcceleratorType), AcceleratorType, $"is not of type {nameof(TraceAccelerator)}");
		}

		public TraceAccelerator CreateAccelerator(PressedPack pack, IReadOnlyList<AxisAlignedBoundingBox> aabbs, IReadOnlyList<uint> tokens)
		{
			if (AcceleratorType == typeof(LinearTracer)) return new LinearTracer(pack, aabbs, tokens);
			if (AcceleratorType == typeof(BoundingVolumeHierarchy)) return new BoundingVolumeHierarchy(pack, aabbs, tokens);
			if (AcceleratorType == typeof(QuadBoundingVolumeHierarchy)) return new QuadBoundingVolumeHierarchy(pack, aabbs, tokens);

			return new QuadBoundingVolumeHierarchy(pack, aabbs, tokens);
		}
	}
}