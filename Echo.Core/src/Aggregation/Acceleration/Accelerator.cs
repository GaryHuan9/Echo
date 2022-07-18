using System.Runtime.CompilerServices;
using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;

namespace Echo.Core.Aggregation.Acceleration;

/// <summary>
/// An aggregate of geometries that is able to efficiently calculate intersections and handle various queries.
/// </summary>
public abstract class Accelerator
{
	protected Accelerator(GeometryCollection geometries) => this.geometries = geometries;

	protected readonly GeometryCollection geometries;

	protected const MethodImplOptions ImplementationOptions = MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining;

	BoxBound? _boxBound;
	SphereBound? _sphereBound;

	public BoxBound BoxBound
	{
		get
		{
			if (_boxBound == null)
			{
				var fill = new SpanFill<BoxBound>(stackalloc BoxBound[2]);
				FillBounds(1, ref fill); //Only fill at the lowest depth because there is no transform
				_boxBound = new BoxBound(fill.Filled);
			}

			return _boxBound.Value;
		}
	}

	public SphereBound SphereBound
	{
		get
		{
			if (_sphereBound == null)
			{
				const int FetchDepth = 6; //How deep do we go into our accelerator to get the nodes
				using var _0 = Pool<BoxBound>.Fetch(1 << FetchDepth, out var bounds);

				SpanFill<BoxBound> fill = bounds;
				FillBounds(FetchDepth, ref fill);
				bounds = bounds[..fill.Count];

				using var _1 = Pool<Float3>.Fetch(bounds.Length * 8, out View<Float3> points);
				for (int i = 0; i < bounds.Length; i++) bounds[i].FillVertices(points[(i * 8)..]);

				_sphereBound = new SphereBound(points);
			}

			return _sphereBound.Value;
		}
	}

	/// <summary>
	/// Calculates a <see cref="BoxBound"/> that bounds this <see cref="Accelerator"/> while transformed.
	/// </summary>
	/// <param name="transform">The <see cref="Float4x4"/> used to transform this <see cref="Accelerator"/>.</param>
	/// <remarks>This transformation is usually performed inversely.</remarks>
	public BoxBound GetTransformedBound(in Float4x4 transform)
	{
		//Potentially find a smaller bounds by encapsulating
		//transformed children nodes instead of the full tree

		const int FetchDepth = 6; //How deep do we go to get the box bounds of the nodes
		using var _ = Pool<BoxBound>.Fetch(1 << FetchDepth, out var bounds);

		SpanFill<BoxBound> fill = bounds;
		FillBounds(FetchDepth, ref fill);

		return new BoxBound(fill.Filled, transform);
	}

	/// <summary>
	/// Traverses and finds the closest intersection of <paramref name="query"/> with this <see cref="Accelerator"/>.
	/// The intersection is recorded in <paramref name="query"/>, and only intersections that are closer than the
	/// initial <paramref name="query.distance"/> value are tested.
	/// </summary>
	public abstract void Trace(ref TraceQuery query);

	/// <summary>
	/// Traverses this <see cref="Accelerator"/> and returns whether <paramref name="query"/> is occluded by anything.
	/// NOTE: this operation will terminate at any occlusion, so it will be more performant than <see cref="Trace"/>.
	/// </summary>
	public abstract bool Occlude(ref OccludeQuery query);

	/// <summary>
	/// Calculates and returns the number of approximated intersection tests performed before a result is determined.
	/// NOTE: the returned value is the cost for a full <see cref="TraceQuery"/>, not an <see cref="OccludeQuery"/>.
	/// </summary>
	public abstract uint TraceCost(in Ray ray, ref float distance);

	/// <summary>
	/// Fills a <see cref="SpanFill{T}"/> with the <see cref="BoxBound"/> in this <see cref="Accelerator"/>.
	/// </summary>
	/// <param name="depth">How deep to gather all of the <see cref="BoxBound"/>s (with the root node at 1).</param>
	/// <param name="fill">The destination <see cref="SpanFill{T}"/>; it should not be smaller than 2^<paramref name="depth"/>.</param>
	/// <remarks>It is guaranteed that the entirety of this <see cref="Accelerator"/> will be
	/// enclosed by all the <see cref="BoxBound"/>s that are filled.</remarks>
	public abstract void FillBounds(uint depth, ref SpanFill<BoxBound> fill);

	/// <summary>
	/// Creates a new <see cref="EntityToken"/> that is of type <see cref="TokenType.Node"/>.
	/// </summary>
	/// <param name="index">The <see cref="int"/> index of the new <see cref="EntityToken"/>.</param>
	/// <returns>The newly created <see cref="EntityToken"/>.</returns>
	protected static EntityToken NewNodeToken(int index) => new(TokenType.Node, index);

	/// <summary>
	/// Swaps two <typeparamref name="T"/> pointers.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static unsafe void Swap<T>(ref T* pointer0, ref T* pointer1) where T : unmanaged
	{
		var storage = pointer0;
		pointer0 = pointer1;
		pointer1 = storage;
	}
}