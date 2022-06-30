using System;
using System.Collections.Generic;
using CodeHelpers.Collections;
using CodeHelpers.Packed;
using Echo.Core.Evaluation.Operations;
using NUnit.Framework;

namespace Echo.UnitTests;

[TestFixtureSource(nameof(sizeCandidates))]
public class TilePatternTests
{
	static List<Int2> sizeCandidates;

	[SetUp]
	public void SetUp()
	{
		sizeCandidates = new List<Int2> { new(10, 20), new(20, 10), new(1,3)};
	}
	
	[Test]
	public void TestTilePatterns(Int2 size)
	{
		List<Int2[]> patternResults = new List<Int2[]>();
		patternResults.Add(new OrderedPattern().CreateSequence(size));
		patternResults.Add(new ScrambledPattern().CreateSequence(size));
		patternResults.Add(new SpiralPattern().CreateSequence(size));
		patternResults.Add(new CheckerboardPattern().CreateSequence(size));
		patternResults.Add(new HilbertCurvePattern().CreateSequence(size));

		for (int j = 0; j < patternResults.Count; j++)
		{
			Assert.That(patternResults[j].Length == size.Product);
			foreach (var item in patternResults[j])
			{
				Assert.That(item >= Int2.Zero && item < size);
				int duplicateAmount = 0;
				Array.ForEach(patternResults[j], delegate(Int2 int2)
				{
					if (int2 == item) duplicateAmount++;
				});
				Assert.That(duplicateAmount == 1);
			}
		}
	}
}