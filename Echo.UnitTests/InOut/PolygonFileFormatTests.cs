using System;
using System.Diagnostics;
using Echo.Core.InOut;
using Echo.Core.InOut.Models;
using NUnit.Framework;

namespace Echo.UnitTests.InOut;

public class PolygonFileFormatTests
{
	[Test]
	public void TestPolygonReader()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		PolygonFileFormatReader reader = new PolygonFileFormatReader("ext/Scenes/monke.ply");
		for (int i = 0; i < reader.header.triangleAmount; i++)
			reader.ReadTriangle();
		stopwatch.Stop();
		Console.WriteLine($"Time taken to load {reader.header.triangleAmount} triangles was {stopwatch.ElapsedMilliseconds}ms.");
	}
}