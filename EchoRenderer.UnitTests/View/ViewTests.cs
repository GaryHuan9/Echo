using System;
using System.Linq;
using NUnit.Framework;

namespace EchoRenderer.UnitTests.View;

[TestFixture]
[TestFixtureSource(nameof(fixtureSource))]
public class ViewTests : ViewBaseTests<int>
{
	public ViewTests(bool useSlice, Range range, int[] array)
	{
		UseSlice = useSlice;
		Range = range;
		this.array = array;
	}

	readonly int[] array;

	static readonly object[][] fixtureSource =
	(
		from source in new[] { Enumerable.Empty<int>(), Enumerable.Range(1, 10), Enumerable.Repeat(3, 42) }
		let array = source.ToArray()
		from useSlice in new[] { true, false }
		from range in new[] { Range.All, 1.., 5.., ..^1, ..^5, ..1, ^1.., ..0 }
		where ValidRange(range, array)
		select new object[] { useSlice, range, array }
	).ToArray();

	protected override int[] GetReference() => array;

	static bool ValidRange<T>(Range range, T[] array)
	{
		try
		{
			_ = range.GetOffsetAndLength(array.Length);
		}
		catch (ArgumentOutOfRangeException)
		{
			return false;
		}

		return true;
	}
}