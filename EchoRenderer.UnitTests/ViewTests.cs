using System.Linq;
using EchoRenderer.Common.Memory;
using NUnit.Framework;

namespace EchoRenderer.UnitTests;

public class ViewTests
{
	[SetUp]
	public void Setup()
	{
		// [ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 ]
		data = Enumerable.Range(1, 10).ToArray();
		dataView = data.AsView();
	}

	[Test]
	public void ConversionTest()
	{
		Assert.That(data.Length, Is.EqualTo(dataView.count));
		Assert.That(data[0], Is.EqualTo(dataView[0]));
		Assert.That(data[^1], Is.EqualTo(dataView[^1]));
	}

	[Test]
	public void SliceTest()
	{
		// expected result [ 2, 3 ]
		View<int> slicedViewWithLength = dataView.Slice(1, 2);


		Assert.That(slicedViewWithLength.count, Is.EqualTo(2));
		Assert.That(slicedViewWithLength[0], Is.EqualTo(2));
		Assert.That(slicedViewWithLength[^1], Is.EqualTo(3));


		// expected result [ 6, 7, 8, 9, 10 ]
		View<int> slicedViewNoLength = dataView.Slice(5);

		Assert.That(slicedViewNoLength.count, Is.EqualTo(5));
		Assert.That(slicedViewNoLength[0], Is.EqualTo(6));
		Assert.That(slicedViewNoLength[^1], Is.EqualTo(10));
	}

	int[] data;

	View<int> dataView;
}
