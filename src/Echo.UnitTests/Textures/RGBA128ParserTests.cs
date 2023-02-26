using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using NUnit.Framework;

namespace Echo.UnitTests.Textures;

[TestFixture]
public class RGBA128ParserTests
{
	static readonly string[] hexPrefixes = { "0x", "0X", "#", "##" };
	const int CandidateAmount = 100;
	const string InvalidChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+-=";

	[Test]
	public void ParseHexPass([ValueSource(nameof(hexPrefixes))] string prefix)
	{
		string[] candidates = { "12345678", "DD444C", "9879", "987", "3", "0", "DEADBEEF", "FFFFFF", "00FF0600" };

		Color32[] expects =
		{
			new(0x12, 0x34, 0x56, 0x78), new(0xDD, 0x44, 0x4C), new(0x99, 0x88, 0x77, 0x99), new(0x99, 0x88, 0x77),
			new(0x33, 0x33, 0x33), Color32.black, new(0xDE, 0xAD, 0xBE, 0xEF), Color32.white, new(0, 0xFF, 0x06, 0)
		};

		ParsePass($"{prefix}{{0}}", candidates, from expect in expects
												select (RGBA128)(Float4)expect);
	}

	[Test]
	public void ParseHexFail([ValueSource(nameof(hexPrefixes))] string prefix)
	{
		string[] candidates = { "", "12", "1.2", "36E58", "DEADBEEG", "DG7" };
		ParseFail($"{prefix}{{0}}", candidates);
	}

	[Test]
	public void ParseRGBPass()
	{
		string[] candidates = { "10, 25, 10", "255, 255, 255", "255, 255, 255, 25", "0, 0, 0, 0", "123, 45, 67, 89" };

		Color32[] expects =
		{
			new(10, 25, 10), new(255, 255, 255), new(255, 255, 255, 25), new(0, 0, 0, 0), new(123, 45, 67, 89)
		};

		ParsePass("rgb({0})", candidates, from expect in expects
										  select (RGBA128)(Float4)expect);
	}

	[Test]
	public void ParseRGBPassRandom()
	{
		var random = Utility.NewRandom();
		var candidates = new List<string>();
		var expects = new List<Color32>();

		var builder = new StringBuilder();

		for (int i = 0; i < CandidateAmount; i++)
		{
			bool hasRandomAlpha = random.Next1(0, 100) < 50;

			byte r = (byte)random.Next1(0, byte.MaxValue + 1);
			byte g = (byte)random.Next1(0, byte.MaxValue + 1);
			byte b = (byte)random.Next1(0, byte.MaxValue + 1);
			byte a = (byte)random.Next1(0, byte.MaxValue + 1);

			Color32 color = hasRandomAlpha ? new Color32(r, g, b, a) : new Color32(r, g, b);

			builder.Append($"{color.r}, {color.g}, {color.b}");
			if (hasRandomAlpha) builder.Append($", {color.a}");

			candidates.Add(builder.ToString());
			expects.Add(color);
			TestContext.WriteLine(builder.ToString());

			builder.Clear();
		}

		ParsePass("rgb({0})", candidates, from expect in expects
										  select (RGBA128)(Float4)expect);
	}

	[Test]
	public void ParseRGBFail()
	{
		string[] candidates =
		{
			"", "10,", ",10", "10", ",", ",,,", "1000, 10, 10", "10, 10, 10, 10000",
			"1.2, 3.4, 5.6", "1, 2, 3, 4.5", "-12, 3, 4", "123, 45, 67, 89,"
		};

		ParseFail("rgb({0})", candidates);
	}

	[Test]
	public void ParseRGBFailRandom()
	{
		var random = Utility.NewRandom();
		var candidates = new List<string>();

		for (int i = 0; i < CandidateAmount; i++)
		{
			int length = random.Next1(0, 16);
			string str = new(Enumerable.Range(1, length).Select(_ => InvalidChars[random.Next1(InvalidChars.Length)]).ToArray());
			candidates.Add(str);

			TestContext.WriteLine(str);
		}

		ParseFail("rgb({0})", candidates);
	}

	[Test]
	public void ParseHDRPass()
	{
		string[] candidates = { "1, 1, 1, 1", "1.23, 4.56, 7.8", "10, 1, 0.1", "0.10, 0.001, 0.102", ".10, .001, .102", "0, 1, 10, 100" };

		RGBA128[] expects =
		{
			new(1f, 1f, 1f), new(1.23f, 4.56f, 7.8f), new(10f, 1f, 0.1f), new(0.10f, 0.001f, 0.102f), new(0.10f, 0.001f, 0.102f), new(0f, 1f, 10f, 100f)
		};

		ParsePass("hdr({0})", candidates, from expect in expects
										  select expect);
	}

	[Test]
	public void ParseHDRPassRandom()
	{
		var random = Utility.NewRandom();
		var candidates = new List<string>();
		var expects = new List<RGBA128>();

		var builder = new StringBuilder();

		for (int i = 0; i < CandidateAmount; i++)
		{
			float r = random.Next1() * 10f;
			float g = random.Next1() * 10f;
			float b = random.Next1() * 10f;
			float a = random.Next1() * 10f;
			bool hasRandomAlpha = random.Next1() < 0.5f;

			Float4 color = hasRandomAlpha ? new Float4(r, g, b, a) : new Float4(r, g, b, 1f);

			builder.Append($"{r}, {g}, {b}");
			if (hasRandomAlpha) builder.Append($", {a}");

			candidates.Add(builder.ToString());
			expects.Add((RGBA128)color);
			TestContext.WriteLine(builder.ToString());

			builder.Clear();
		}

		ParsePass("hdr({0})", candidates, from expect in expects
										  select expect);
	}

	[Test]
	public void ParseHDRFail()
	{
		string[] candidates =
		{
			"", "10,", ",10", "10", ",", ",,,", ". . .", "1, 5,",
			"1.5.1", "1000, 10, 10,", "-12, 3, 4", "123, 45, 67, 89,"
		};

		ParseFail("hdr({0})", candidates);
	}

	[Test]
	public void ParseHDRFailRandom()
	{
		var random = Utility.NewRandom();
		var candidates = new List<string>();

		for (int i = 0; i < CandidateAmount; i++)
		{
			int length = random.Next1(0, 16);
			string str = new(Enumerable.Range(1, length).Select(_ => InvalidChars[random.Next1(InvalidChars.Length)]).ToArray());
			candidates.Add(str);

			TestContext.WriteLine(str);
		}

		ParseFail("hdr({0})", candidates);
	}

	static void ParsePass(string format, IEnumerable<string> contents, IEnumerable<RGBA128> expects)
	{
		foreach ((string content, RGBA128 expect) in contents.Zip(expects))
		foreach (string variant in GetCaseVariants(new[] { content }))
		{
			string input = string.Format(format, variant);

			Assert.That(RGBA128.Parse(input), Is.EqualTo(expect));
			Assert.That(RGBA128.TryParse(input, out RGBA128 result));
			Assert.That(result, Is.EqualTo(expect));
		}
	}

	static void ParseFail(string format, IEnumerable<string> contents)
	{
		foreach (string content in GetCaseVariants(contents))
		{
			string input = string.Format(format, content);

			try
			{
				RGBA128.Parse(input);
				Assert.Fail();
			}
			catch (FormatException) { }

			Assert.That(RGBA128.TryParse(input, out RGBA128 result), Is.False);
			Assert.That(result, Is.EqualTo(default(RGBA128)));
		}
	}

	static IEnumerable<string> GetCaseVariants(IEnumerable<string> values) => (from value in values
																			   from variant in new[] { value, value.ToLowerInvariant(), value.ToUpperInvariant() }
																			   orderby variant
																			   select value).Distinct();
}