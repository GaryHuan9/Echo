using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Textures.Colors;
using NUnit.Framework;

namespace Echo.UnitTests;

[TestFixture]
public class RGBA128ParserTests
{
	static readonly string[] hexPrefixes = { "0x", "0X", "#", "##" };

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
		string[] candidates = { "10, 25, 10", "255, 255, 255", "255, 255, 255, 25", "0, 0, 0, 0" };

		Color32[] expects =
		{
			new(10, 25, 10), new(255, 255, 255), new(255, 255, 255, 25), new(0, 0, 0, 0)
		};
		
		ParsePass("rgb({0})", candidates, from expect in expects
										  select  (RGBA128)(Float4)expect);
	}

	[Test]
	public void ParseRGBPassRandom()
	{
		const int CandidateAmount = 100;
		
		List<string> randomCandidateList = new List<string>();
		List<Color32> randomCandidateExpectationsList = new List<Color32>();
		Random random = new Random();

		for (int i = 0; i < CandidateAmount; i++)
		{
			StringBuilder sb = new StringBuilder();

			bool hasRandomAlpha = random.Next(0, 100) < 50;

			byte r = (byte)random.Next(0, 0xFF);
			byte g = (byte)random.Next(0, 0xFF);
			byte b = (byte)random.Next(0, 0xFF);
			byte a = (byte)random.Next(0, 0xFF);
			
			Color32 color = hasRandomAlpha ? new Color32(r, g, b, a) : new Color32(r, g, b);

			sb.Append($"{color.r}, {color.g}, {color.b}");

			if (hasRandomAlpha)
				sb.Append($", {color.a}");

			randomCandidateList.Add(sb.ToString());
			randomCandidateExpectationsList.Add(color);
			TestContext.WriteLine(sb.ToString());
		}
		
		ParsePass("rgb({0})", randomCandidateList, from expect in randomCandidateExpectationsList
												   select (RGBA128)(Float4)expect);
	}

	[Test]
	public void ParseRGBFail()
	{
		string[] candidates = { "10,", ",", ",,,", "1000, 10, 10", "10, 10, 10, 10000", ",10" };
		ParseFail($"rgb({{0}})", candidates);
	}

	[Test]
	public void ParseRGBFailRandom()
	{
		const int CandidateAmount = 100;
		
		List<string> randomCandidateList = new List<string>();
		const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		Random random = new Random();
		
		for (int i = 0; i < CandidateAmount; i++)
		{
			int length = random.Next(0, 255);
			string str = new(Enumerable.Range(1, length).Select(_ => Chars[random.Next(Chars.Length)]).ToArray());
			randomCandidateList.Add(str);
			
			TestContext.WriteLine(str);
		}
		
		ParseFail("rgb({0})", randomCandidateList);
	}

	[Test]
	public void ParseHDRPass()
	{
		string[] candidates = { "1, 1, 1, 1", "10, 1, 0.1", "0.10, 0.001, 0.102", "0, 1, 10, 100" };

		RGBA128[] expects =
		{
			new(1, 1, 1), new(10, 1, 0.1f), new(0.10f, 0.001f, 0.102f), new(0, 1, 10, 100)
		};
		
		ParsePass("hdr({0})", candidates, from expect in expects
										  select expect);
	}

	[Test]
	public void ParseHDRPassRandom()
	{
		const int CandidateAmount = 100;
		
		List<string> randomCandidateList = new List<string>();
		List<RGBA128> randomCandidateExpectationList = new List<RGBA128>();
		Random random = new Random();
		for (int i = 0; i < CandidateAmount; i++)
		{
			float r = (float)random.NextDouble() * 100f;
			float g = (float)random.NextDouble() * 100f;
			float b = (float)random.NextDouble() * 100f;
			float a = (float)random.NextDouble() * 100f;
			bool hasRandomAlpha = random.Next(0, 100) < 50;

			Float4 color = hasRandomAlpha ? new Float4(r, g, b, a) : new Float4(r, g, b, 1f);
			
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat($"{r}, {g}, {b}");

			if (hasRandomAlpha)
			{
				sb.AppendFormat($", {a}");
			}

			randomCandidateList.Add(sb.ToString());
			randomCandidateExpectationList.Add((RGBA128)color);
			TestContext.WriteLine(sb.ToString());
		}
		ParsePass("hdr({0})", randomCandidateList, from expect in randomCandidateExpectationList
												   select expect);
	}
	
	[Test]
	public void ParseHDRFail()
	{
		string[] candidates = { "", ",,,", ". . .", "1, 5,", "1.5.1" };
		ParseFail("hdr({0})", candidates);
	}

	[Test]
	public void ParseHDRFailRandom()
	{
		List<string> randomCandidateList = new List<string>();
		const int CandidateAmount = 1_000_000;
		const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		Random random = new Random();
		
		for (int i = 0; i < CandidateAmount; i++)
		{
			int length = random.Next(0, 255);
			string str = new(Enumerable.Range(1, length).Select(_ => Chars[random.Next(Chars.Length)]).ToArray());
			randomCandidateList.Add(str);
			
			TestContext.WriteLine(str);
		}
		
		ParseFail("hdr({0})", randomCandidateList);
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