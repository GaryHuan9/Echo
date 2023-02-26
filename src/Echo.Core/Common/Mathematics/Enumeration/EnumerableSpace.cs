using System.Collections;
using System.Collections.Generic;
using Echo.Core.Common.Packed;

namespace Echo.Core.Common.Mathematics.Enumeration;

public readonly struct EnumerableSpace3D : IEnumerable<Int3>
{
	/// <summary>
	/// Creates a foreach-loop compatible IEnumerable which yields all position/vector inside a 3D rectangular space
	/// starts at <paramref name="from"/> and ends at <paramref name="to"/> (Both inclusive). 
	/// </summary>
	public EnumerableSpace3D(Int3 from, Int3 to) => enumerator = new Enumerator(from, to);

	readonly Enumerator enumerator;

	public Enumerator GetEnumerator() => enumerator;

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<Int3> IEnumerable<Int3>.GetEnumerator() => GetEnumerator();

	public struct Enumerator : IEnumerator<Int3>
	{
		internal Enumerator(Int3 from, Int3 to)
		{
			offset = from;

			Int3 difference = to - from;
			difference += difference.Signed; //Make inclusive

			enumerator = new Int3.LoopEnumerable.Enumerator(difference, true);
		}

		readonly Int3 offset;
		Int3.LoopEnumerable.Enumerator enumerator;

		object IEnumerator.Current => Current;
		public Int3 Current => enumerator.Current + offset;

		public bool MoveNext() => enumerator.MoveNext();

		public void Reset() => enumerator.Reset();
		public void Dispose() => enumerator.Dispose();
	}
}

public readonly struct EnumerableSpace2D : IEnumerable<Int2>
{
	/// <summary>
	/// Creates a foreach-loop compatible IEnumerable which yields all position/vector inside a 2D rectangular space
	/// starts at <paramref name="from"/> and ends at <paramref name="to"/> (Both inclusive). 
	/// </summary>
	public EnumerableSpace2D(Int2 from, Int2 to) => enumerator = new Enumerator(from, to);

	readonly Enumerator enumerator;

	public Enumerator GetEnumerator() => enumerator;

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<Int2> IEnumerable<Int2>.GetEnumerator() => GetEnumerator();

	public struct Enumerator : IEnumerator<Int2>
	{
		internal Enumerator(Int2 from, Int2 to)
		{
			offset = from;

			Int2 difference = to - from;
			difference += difference.Signed; //Make inclusive

			enumerator = new Int2.LoopEnumerable.LoopEnumerator(difference, true);
		}

		readonly Int2 offset;
		Int2.LoopEnumerable.LoopEnumerator enumerator;

		object IEnumerator.Current => Current;
		public Int2 Current => enumerator.Current + offset;

		public bool MoveNext() => enumerator.MoveNext();

		public void Reset() => enumerator.Reset();
		public void Dispose() => enumerator.Dispose();
	}
}