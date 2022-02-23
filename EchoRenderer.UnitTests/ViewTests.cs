using System.Linq;
using EchoRenderer.Common.Memory;
using NUnit.Framework;

namespace EchoRenderer.UnitTests;

[TestFixture]
public class ViewTests
{
	[SetUp]
	public void SetUp()
	{
		// [ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 ]
		data = Enumerable.Range(1, 10).ToArray();
		view = data.AsView();
	}

	int[] data;
	View<int> view;

	[Test]
	public void Conversion()
	{
		Assert.That(data.Length, Is.EqualTo(view.Length));
		Assert.That(data[0], Is.EqualTo(view[0]));
		Assert.That(data[^1], Is.EqualTo(view[^1]));
	}

	[Test]
	public void ReadOnlyConversion()
	{
		ReadOnlyView<int> readOnlyView = view;

		Assert.That(data.Length, Is.EqualTo(readOnlyView.Length));
		Assert.That(data[0], Is.EqualTo(readOnlyView[0]));
		Assert.That(data[^1], Is.EqualTo(readOnlyView[^1]));
	}

	[Test]
	public void Slice()
	{
		// expected result [ 2, 3 ]
		View<int> slicedViewWithLength = view.Slice(1, 2);

		Assert.That(slicedViewWithLength.Length, Is.EqualTo(2));
		Assert.That(slicedViewWithLength[0], Is.EqualTo(2));
		Assert.That(slicedViewWithLength[^1], Is.EqualTo(3));


		// expected result [ 6, 7, 8, 9, 10 ]
		View<int> slicedViewNoLength = view[5..];

		Assert.That(slicedViewNoLength.Length, Is.EqualTo(5));
		Assert.That(slicedViewNoLength[0], Is.EqualTo(6));
		Assert.That(slicedViewNoLength[^1], Is.EqualTo(10));
	}

	[Test]
	public void ReadOnlySlice()
	{
		// expected result [ 2, 3 ]
		ReadOnlyView<int> roSlicedViewWithLength = view.Slice(1, 2);

		Assert.That(roSlicedViewWithLength.Length, Is.EqualTo(2));
		Assert.That(roSlicedViewWithLength[0], Is.EqualTo(2));
		Assert.That(roSlicedViewWithLength[^1], Is.EqualTo(3));


		// expected result [ 6, 7, 8, 9, 10 ]
		ReadOnlyView<int> roSlicedVewNoLength = view[5..];

		Assert.That(roSlicedVewNoLength.Length, Is.EqualTo(5));
		Assert.That(roSlicedVewNoLength[0], Is.EqualTo(6));
		Assert.That(roSlicedVewNoLength[^1], Is.EqualTo(10));
	}
}