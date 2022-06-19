﻿using System;
using System.Collections.Generic;
using System.Linq;
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