using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using NUnit.Framework;

namespace Echo.UnitTests;

[TestFixture]
public class EntityTokenArrayTests
{
	[SetUp]
	public void SetUp()
	{
		array = new EntityTokenArray(stackalloc int[] { 3, 1, 2 });
	}

	EntityTokenArray array;

	[Test]
	public void Length()
	{
		Assert.That(array.TotalLength, Is.EqualTo(3 + 1 + 2));
		Assert.That(array.PartitionLength, Is.EqualTo(3));
	}

	[Test]
	public void Add0()
	{
		Assert.That(array.Add(0, new EntityToken(TokenType.Triangle, 0)), Is.EqualTo(0));
		Assert.That(array.Add(0, new EntityToken(TokenType.Triangle, 1)), Is.EqualTo(1));
		Assert.That(array.Add(0, new EntityToken(TokenType.Triangle, 2)), Is.EqualTo(2));

		Assert.That(array.Add(1, new EntityToken(TokenType.Sphere, 0)), Is.EqualTo(3));

		Assert.That(array.Add(2, new EntityToken(TokenType.Instance, 1)), Is.EqualTo(4));
		Assert.That(array.Add(2, new EntityToken(TokenType.Instance, 2)), Is.EqualTo(5));

		Assert.That(array.IsFull, Is.EqualTo(true));
	}

	[Test]
	public void Add1()
	{
		Assert.That(array.Add(2, new EntityToken(TokenType.Instance, 1)), Is.EqualTo(4));
		Assert.That(array.Add(0, new EntityToken(TokenType.Triangle, 0)), Is.EqualTo(0));

		Assert.That(array.Add(1, new EntityToken(TokenType.Sphere, 0)), Is.EqualTo(3));

		Assert.That(array.Add(0, new EntityToken(TokenType.Triangle, 1)), Is.EqualTo(1));
		Assert.That(array.Add(2, new EntityToken(TokenType.Instance, 2)), Is.EqualTo(5));
		Assert.That(array.Add(0, new EntityToken(TokenType.Triangle, 2)), Is.EqualTo(2));

		Assert.That(array.IsFull, Is.EqualTo(true));
	}
}