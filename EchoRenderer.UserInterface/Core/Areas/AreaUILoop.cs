using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace EchoRenderer.UserInterface.Core.Areas;

public partial class AreaUI
{
	public ForwardLoop LoopForward(bool skipDisabled = true) => new ForwardLoop(children, skipDisabled);
	public BackwardLoop LoopBackward(bool skipDisabled = true) => new BackwardLoop(children, skipDisabled);

	public readonly struct ForwardLoop : IEnumerable<AreaUI>
	{
		public ForwardLoop(IReadOnlyList<AreaUI> source, bool skip) => enumerator = new Enumerator(source, skip);

		readonly Enumerator enumerator;

		public Enumerator GetEnumerator() => enumerator;

		IEnumerator<AreaUI> IEnumerable<AreaUI>.GetEnumerator() => GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public struct Enumerator : IEnumerator<AreaUI>
		{
			public Enumerator(IReadOnlyList<AreaUI> source, bool skip)
			{
				this.source = source;
				this.skip = skip;

				Unsafe.SkipInit(out index);
				Reset();
			}

			readonly IReadOnlyList<AreaUI> source;
			readonly bool skip;

			int index;

			public AreaUI Current => source[index];
			object IEnumerator.Current => Current;

			bool ValidIndex => source[index].Enabled || !skip;

			public bool MoveNext()
			{
				bool inside;

				do inside = ++index < source.Count;
				while (inside && !ValidIndex);

				return inside;
			}

			public void Reset() => index = -1;

			public void Dispose() { }
		}
	}

	public readonly struct BackwardLoop : IEnumerable<AreaUI>
	{
		public BackwardLoop(IReadOnlyList<AreaUI> source, bool skip) => enumerator = new Enumerator(source, skip);

		readonly Enumerator enumerator;

		public Enumerator GetEnumerator() => enumerator;

		IEnumerator<AreaUI> IEnumerable<AreaUI>.GetEnumerator() => GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public struct Enumerator : IEnumerator<AreaUI>
		{
			public Enumerator(IReadOnlyList<AreaUI> source, bool skip)
			{
				this.source = source;
				this.skip = skip;

				Unsafe.SkipInit(out index);
				Reset();
			}

			readonly IReadOnlyList<AreaUI> source;
			readonly bool skip;

			int index;

			public AreaUI Current => source[index];
			object IEnumerator.Current => Current;

			bool ValidIndex => source[index].Enabled || !skip;

			public bool MoveNext()
			{
				bool inside;

				do inside = --index >= 0;
				while (inside && !ValidIndex);

				return inside;
			}

			public void Reset() => index = source.Count;

			public void Dispose() { }
		}
	}
}