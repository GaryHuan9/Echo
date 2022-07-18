using System.Collections;
using System.Collections.Generic;
using Echo.Core.Common.Packed;

namespace Echo.Core.Common.Mathematics.Enumeration;

public readonly struct EnumerableSphere3D : IEnumerable<Int3> //Not the most performant implementation, but fine for now
{
	/// <summary>
	/// Creates a foreach-loop compatible IEnumerable which yields all position in a 3D sphere.
	/// Centered at <paramref name="center"/> with radius <paramref name="radius"/>. Can use floats to get arbitrarily positioned spheres.
	/// </summary>
	public EnumerableSphere3D(Int3 center, float radius) => enumerator = new Enumerator(center, radius);

	/// <summary>
	/// Creates a foreach-loop compatible IEnumerable which yields all position in a 3D sphere.
	/// Centered at <paramref name="center"/> with radius <paramref name="radius"/>. Can use floats to get arbitrarily positioned spheres.
	/// </summary>
	public EnumerableSphere3D(Float3 center, float radius) => enumerator = new Enumerator(center, radius);

	readonly Enumerator enumerator;

	public Enumerator GetEnumerator() => enumerator;

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<Int3> IEnumerable<Int3>.GetEnumerator() => GetEnumerator();

	public struct Enumerator : IEnumerator<Int3>
	{
		public Enumerator(Int3 center, float radius) : this((Float3)center, radius) { }

		public Enumerator(Float3 center, float radius)
		{
			this.center = center;
			radiusSquared = radius * radius;

			Int3 min = (center - (Float3)radius).Floored;
			Int3 max = (center + (Float3)radius).Ceiled;

			enumerator = new EnumerableSpace3D.Enumerator(min, max);
			Current = Int3.MinValue;
		}

		readonly Float3 center;
		readonly float radiusSquared;

		EnumerableSpace3D.Enumerator enumerator;

		public Int3 Current { get; private set; }
		object IEnumerator.Current => Current;

		public bool MoveNext()
		{
			bool moved;

			do
			{
				moved = enumerator.MoveNext();
				Current = enumerator.Current;
			}
			while (moved && (Current - center).SquaredMagnitude > radiusSquared);

			return moved;
		}

		public void Reset()
		{
			enumerator.Reset();
			Current = Int3.MinValue;
		}

		public void Dispose() { }
	}
}

public readonly struct EnumerableSphere2D : IEnumerable<Int2> //Not the most performant implementation, but fine for now
{
	/// <summary>
	/// Creates a foreach-loop compatible IEnumerable which yields all position in a 2D sphere (aka circle but whatever).
	/// Centered at <paramref name="center"/> with radius <paramref name="radius"/>. Can use floats to get arbitrarily positioned spheres.
	/// </summary>
	public EnumerableSphere2D(Int2 center, float radius) => enumerator = new Enumerator(center, radius);

	/// <summary>
	/// Creates a foreach-loop compatible IEnumerable which yields all position in a 2D sphere (aka circle but whatever).
	/// Centered at <paramref name="center"/> with radius <paramref name="radius"/>. Can use floats to get arbitrarily positioned spheres.
	/// </summary>
	public EnumerableSphere2D(Float2 center, float radius) => enumerator = new Enumerator(center, radius);

	readonly Enumerator enumerator;

	public Enumerator GetEnumerator() => enumerator;

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<Int2> IEnumerable<Int2>.GetEnumerator() => GetEnumerator();

	public struct Enumerator : IEnumerator<Int2>
	{
		public Enumerator(Int2 center, float radius) : this((Float2)center, radius) { }

		public Enumerator(Float2 center, float radius)
		{
			this.center = center;
			radiusSquared = radius * radius;

			Int2 min = (center - (Float2)radius).Floored;
			Int2 max = (center + (Float2)radius).Ceiled;

			enumerator = new EnumerableSpace2D.Enumerator(min, max);
			Current = Int2.MinValue;
		}

		readonly Float2 center;
		readonly float radiusSquared;

		EnumerableSpace2D.Enumerator enumerator;

		public Int2 Current { get; private set; }
		object IEnumerator.Current => Current;

		public bool MoveNext()
		{
			bool moved;

			do
			{
				moved = enumerator.MoveNext();
				Current = enumerator.Current;
			}
			while (moved && (Current - center).SquaredMagnitude > radiusSquared);

			return moved;
		}

		public void Reset()
		{
			enumerator.Reset();
			Current = Int2.MinValue;
		}

		public void Dispose() { }
	}
}