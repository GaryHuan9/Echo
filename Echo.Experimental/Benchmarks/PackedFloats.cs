using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;

namespace Echo.Experimental.Benchmarks;

// |              Method |       Mean |     Error |    StdDev |
// |-------------------- |-----------:|----------:|----------:|
// | SeeSharpInlinedBase |  0.0726 ns | 0.0061 ns | 0.0057 ns |
// |    SeeSharpSimdBase |  0.0740 ns | 0.0043 ns | 0.0036 ns |
// |          DivideBase |  9.0451 ns | 0.0291 ns | 0.0272 ns |
// |       DivideRegular | 12.3651 ns | 0.0626 ns | 0.0585 ns |
// |          DivideLoad | 12.7747 ns | 0.0398 ns | 0.0372 ns |
// |         DivideStore | 12.7687 ns | 0.0509 ns | 0.0476 ns |
// |          DivideBoth | 12.7192 ns | 0.0438 ns | 0.0410 ns |

//https://sharplab.io/#v2:C4LghgzgtgPgAgBgARwIwDoBKBXAdsASygFN0BhAeygAcCAbYgJwGUmA3AgY2IgG4BYAFCIUGHPiKkAkviYVqrRh258hItFjyES6GcEYFcELquHIN47dPwGjJ9AA0AHADYBgtQGYUAJiRkkAG8hJFCkIQBIRmIwABMKXDoATyQAMzoKMGAkAA8kAF4kAFZ0FwAWItRPVPcomPjElPTM7JTChHRPVB9PMprI6LiE5LSMrKQALwKkAHZ0HwBOTxcnfsE6ocbRlqQAd2mXdFcy1D73AfrhprHssGnu3pLyyurawYaR5vGAI2nHlw6XR6Zwum0+NyQnGmPiKZR8c0Wy1Wb0uWy+2ViB2BJWOpzWITCAHpCQAFbDRCDZTgUWLEADmxFwkTg3gAasROMAKIxuk4ADzogB8SHZnO5ABECBxaQAhSDEAAUAEpIsF1hE2GBGEg2ByuYxkIVRfreeRBsBFWAADRIb42zg22JK2oarU6vXc1DTY2enxOM0xC0KnI2pI2iY23bOoSRCJwGZIZgQUiS6WK3Vig02jMm6PrAC+BNCRaQxLlxk4YDoI2AAAtiEgIGASJCafTGUhIJ3vhRdUgAHp7Ah1xtUBvRbm02x05neElgTgAa2IsWQqYItMw9OwdC1ytVsc12oSDcKuGI+3nS5XCAV1tt9sdeYiru1wF2FGm58vC+Xq+DobhpGeaxvGSDfkgV5/reSYplKG6Kie6A5tyNrvhQyEeowSrPoWHiCGEpaEgAMgkDLatStIMrgNoQBkuxMEg1BMKk3JQGAuDcEgCoWpSK5IIYSAyoynC1uxjCLuKFDAOewAqusLKQb+K5euutIAPKpKkybAPu6xqi+R5ICeX4Xkp16xKgd42nakJPi6RnoaZP4WVZIZIGGkzAec6pgRBUEqQqsHoGpiHnphmZoR+EX6jhtR4SWxIAOrDrWFDYNkvZMLu1C0LgdJpAQxB0LEECRAA2sw+jYJyxFgEk6W6XVDUZQA0oYsToAAojk1B0Fww42swBATKeSCoC4SoALqzkg7xXI21WcuZ0EHuq5UALLEHWNJSDQdAKltO2xHtfXqdQhAJBA6AAIJ0nSFLGLqMj9bghh0tNoFzsp/7orkNp/Z5f0Rts4xRkgIBIHWBAQAqPo8n6AZZIq7meSDUZKkECXrUdaUnfth3bXjp10Odl1GLd92PVKxAvYY72fb530Wbegnw7yAo3MKKHYbGBkvtD1089MPMutjL7lQAYkVJWadp20Kgg00oN481bOzfqcy03NYfFMYEUShIpSOWWMDleUFakMulRVVWMDVwDNY1CpO21HXdb1/WcINiYjWNE2M3GquoiMlL28tAWWWtEu47tBOx/jZ0XQQV2Uw9PBPbTiT0/lgdBytgV/e5gMAxCIN/eDkOC3DWGmmQ5oo4BXl7DhWP6zHRNx31hPHSTZMpxTd3pxAmd029uczUzBeWQqbO15rQrupm8kvvzEQAKpGGAqSkMwi4ENQMjDgqjW5M+L6b02O/oHvB9H7pp9JOfG9b9ft+H29D8ZZMz+X9vu/7w/sfU+UYfIvgiILGK3IRa61jOLCIUtrZyx0orJWU0VZzRDtcHYOQXSIOKrEZBCsEBlGVopNW4IdhJDwdLAhRDdIICcGQ4OYJsHjAmDQpBWkUHdGYZg1hoNsi7HcIRURYQSyEXwbLbhxC+EUJSBrfki9RaRDwvmIAA==

public class PackedFloats
{
	public PackedFloats()
	{
		float value = Random.Shared.NextSingle();

		x += value;
		a += value;

		y += value;
		b += value;

		z += value;
		c += value;

		w += value;
		d += value;
	}

	readonly float x = 5.64513f;
	readonly float a = 12345.64513f;

	readonly float y = 0.31234f;
	readonly float b = 34560.31234f;

	readonly float z = 7.29368f;
	readonly float c = 25427.29368f;

	readonly float w = 6.86414f;
	readonly float d = 62345.86414f;

	[Benchmark]
	public Vector4 SeeSharpSimdBase()
	{
		Vector4 vector0 = new Vector4(a, b, c, d);
		Vector4 vector1 = new Vector4(x, y, z, w);

		return vector0 / vector1;
	}

	[Benchmark]
	public Vector128<float> VectorDivideBase()
	{
		var vector0 = Vector128.Create(a, b, c, d);
		var vector1 = Vector128.Create(x, y, z, w);

		return Sse.Divide(vector0, vector1);
	}

	[Benchmark]
	public Packed4 DivideRegular()
	{
		var packed0 = new Packed4(a, b, c, d);
		var packed1 = new Packed4(x, y, z, w);

		return packed0 / packed1;
	}

	public readonly struct Packed4
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Packed4(float x, float y, float z, float w) : this(Vector128.Create(x, y, z, w)) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Packed4(in Vector128<float> vector) => v = vector;

		readonly Vector128<float> v;

		public float X => v.GetElement(0);
		public float Y => v.GetElement(1);
		public float Z => v.GetElement(2);
		public float W => v.GetElement(3);

		public static Packed4 operator /(in Packed4 value0, in Packed4 value1) => new(Sse.Divide(value0.v, value1.v));
	}
}