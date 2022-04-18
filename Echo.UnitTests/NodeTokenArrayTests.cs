using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using NUnit.Framework;

namespace Echo.UnitTests;

[TestFixture]
public class NodeTokenArrayTests
{
	[SetUp]
	public void SetUp()
	{
		array = new NodeTokenArray(stackalloc int[] { 3, 1, 2 });
	}

	NodeTokenArray array;

	[Test]
	public void Length()
	{
		Assert.That(array.TotalLength, Is.EqualTo(3 + 1 + 2));
		Assert.That(array.PartitionLength, Is.EqualTo(3));
	}

	[Test]
	public void Add0()
	{
		Assert.That(array.Add(0, NodeToken.CreateTriangle(0)), Is.EqualTo(0));
		Assert.That(array.Add(0, NodeToken.CreateTriangle(1)), Is.EqualTo(1));
		Assert.That(array.Add(0, NodeToken.CreateTriangle(2)), Is.EqualTo(2));

		Assert.That(array.Add(1, NodeToken.CreateSphere(0)), Is.EqualTo(3));

		Assert.That(array.Add(2, NodeToken.CreateInstance(1)), Is.EqualTo(4));
		Assert.That(array.Add(2, NodeToken.CreateInstance(2)), Is.EqualTo(5));

		Assert.That(array.IsFull, Is.EqualTo(true));
	}

	[Test]
	public void Add1()
	{
		Assert.That(array.Add(2, NodeToken.CreateInstance(1)), Is.EqualTo(4));
		Assert.That(array.Add(0, NodeToken.CreateTriangle(0)), Is.EqualTo(0));

		Assert.That(array.Add(1, NodeToken.CreateSphere(0)), Is.EqualTo(3));

		Assert.That(array.Add(0, NodeToken.CreateTriangle(1)), Is.EqualTo(1));
		Assert.That(array.Add(2, NodeToken.CreateInstance(2)), Is.EqualTo(5));
		Assert.That(array.Add(0, NodeToken.CreateTriangle(2)), Is.EqualTo(2));

		Assert.That(array.IsFull, Is.EqualTo(true));
	}
}