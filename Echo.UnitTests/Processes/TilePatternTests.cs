using System.Collections.Generic;
using Echo.Core.Common.Packed;
using Echo.Core.Processes.Evaluation;
using NUnit.Framework;

namespace Echo.UnitTests.Processes;

[TestFixture]
[TestFixtureSource(nameof(patternSource))]
public class TilePatternTests
{
	public TilePatternTests(ITilePattern pattern) => this.pattern = pattern;

	readonly ITilePattern pattern;

	static IEnumerable<ITilePattern> patternSource = new ITilePattern[]
	{
		new OrderedPattern(), new OrderedPattern(false), new ScrambledPattern(),
		new SpiralPattern(), new CheckerboardPattern(), new HilbertCurvePattern()
	};

	static IEnumerable<Int2> sizeSource = new Int2[] { new(10, 20), new(31, 13), new(1, 3), new(1, 1), new(123, 456) };

	[Test]
	public void CreateSequence([ValueSource(nameof(sizeSource))] Int2 size)
	{
		Int2[] positions = pattern.CreateSequence(size);
		Assert.That(positions, Has.Length.EqualTo(size.Product));
		Assert.That(positions, Is.All.Matches<Int2>(position => Int2.Zero <= position && position < size));
		Assert.That(positions, Is.Unique);
	}
}