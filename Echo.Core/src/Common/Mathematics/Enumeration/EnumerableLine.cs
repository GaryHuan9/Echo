using System.Collections;
using System.Collections.Generic;
using Echo.Core.Common.Packed;

namespace Echo.Core.Common.Mathematics.Enumeration;

public readonly struct EnumerableLine3D : IEnumerable<Int3>
{
	/// <summary>
	/// Creates a foreach-loop compatible IEnumerable which yields all position on a 3D line.
	/// starts at <paramref name="from"/> and ends at <paramref name="to"/> (Both inclusive).
	/// </summary>
	public EnumerableLine3D(Int3 from, Int3 to) => enumerator = new Enumerator(from, to);

	readonly Enumerator enumerator;

	public Enumerator GetEnumerator() => enumerator;

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<Int3> IEnumerable<Int3>.GetEnumerator() => GetEnumerator();

	public struct Enumerator : IEnumerator<Int3>
	{
		public Enumerator(Int3 from, Int3 to)
		{
			this.from = from;
			this.to = to;

			current = -1;
			sample = (from - to).Absoluted.MaxComponent;
		}

		readonly Int3 from;
		readonly Int3 to;

		int current;         //Current point being sampled
		readonly int sample; //Number of points need to be sampled

		object IEnumerator.Current => Current;
		public Int3 Current => sample == 0 ? from : from.Lerp(to, (float)current / sample).Rounded;

		public bool MoveNext() => ++current <= sample;

		public void Reset() => current = -1;
		public void Dispose() { }
	}
}

public readonly struct EnumerableLine2D : IEnumerable<Int2>
{
	/// <summary>
	/// Creates a foreach-loop compatible IEnumerable which yields all position on a 2D line.
	/// starts at <paramref name="from"/> and ends at <paramref name="to"/> (Both inclusive).
	/// </summary>
	public EnumerableLine2D(Int2 from, Int2 to) => enumerator = new Enumerator(from, to);

	readonly Enumerator enumerator;

	public Enumerator GetEnumerator() => enumerator;

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<Int2> IEnumerable<Int2>.GetEnumerator() => GetEnumerator();

	public struct Enumerator : IEnumerator<Int2>
	{
		public Enumerator(Int2 from, Int2 to)
		{
			this.from = from;
			this.to = to;

			current = -1;
			sample = (from - to).Absoluted.MaxComponent;
		}

		readonly Int2 from;
		readonly Int2 to;

		int current;         //Current point being sampled
		readonly int sample; //Number of points need to be sampled

		object IEnumerator.Current => Current;
		public Int2 Current => sample == 0 ? from : from.Lerp(to, (float)current / sample).Rounded;

		public bool MoveNext() => ++current <= sample;

		public void Reset() => current = -1;
		public void Dispose() { }
	}
}