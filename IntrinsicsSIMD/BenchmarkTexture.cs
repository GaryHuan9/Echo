using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using CodeHelpers.Mathematics;
using ForceRenderer.Textures;

namespace IntrinsicsSIMD
{
	public class BenchmarkTexture
	{
		public BenchmarkTexture()
		{
			Random random = new Random(42);

			const string Path = @"C:\Users\MMXXXVIII\Things\CodingStuff\C#\ForceRenderer\ForceRenderer\Assets\Textures\WikiNormalMap.png";

			texture = Texture2D.Load(Path);
			uvs = new Float2[65536];

			textureOld = Texture2D.Load(Path);
			textureOld.Filter = new Old();

			textureNoPrefetch = Texture2D.Load(Path);
			textureNoPrefetch.Filter = new NoPrefetch();

			textureSseOnly = Texture2D.Load(Path);
			textureSseOnly.Filter = new SseOnly();

			for (int i = 0; i < uvs.Length; i++)
			{
				uvs[i] = new Float2((float)random.NextDouble(), (float)random.NextDouble());
			}
		}

		readonly Texture texture;
		readonly Texture textureOld;
		readonly Texture textureNoPrefetch;
		readonly Texture textureSseOnly;

		readonly Float2[] uvs;

		//Bilinear:		12.75 ms per 65536 samples = 194.55ns per sample
		//BilinearOld:	20.67 ms per 65536 samples = 315.40ns per sample

		[Benchmark]
		public Float4 Sample()
		{
			Float4 result = default;

			for (int i = 0; i < uvs.Length; i++) result = texture[uvs[i]];

			return result;
		}

		[Benchmark]
		public Float4 SampleOld()
		{
			Float4 result = default;

			for (int i = 0; i < uvs.Length; i++) result = textureOld[uvs[i]];

			return result;
		}

		[Benchmark]
		public Float4 SampleNoPrefetch()
		{
			Float4 result = default;

			for (int i = 0; i < uvs.Length; i++) result = textureNoPrefetch[uvs[i]];

			return result;
		}

		[Benchmark]
		public Float4 SampleSseOnly()
		{
			Float4 result = default;

			for (int i = 0; i < uvs.Length; i++) result = textureSseOnly[uvs[i]];

			return result;
		}

		class Old : IFilter
		{
			public unsafe Vector128<float> Convert(Texture texture, Float2 uv)
			{
				uv *= texture.size;
				Int2 rounded = uv.Rounded;

				Int2 upperRight = rounded.Min(texture.oneLess);
				Int2 bottomLeft = rounded.Max(Int2.one) - Int2.one;

				Float2 t = Int2.InverseLerp(bottomLeft, upperRight, uv - Float2.half).Clamp(0f, 1f);

				Float4 y0 = Float4.Lerp(texture[bottomLeft], texture[new Int2(upperRight.x, bottomLeft.y)], t.x);
				Float4 y1 = Float4.Lerp(texture[new Int2(bottomLeft.x, upperRight.y)], texture[upperRight], t.x);

				Float4 result = Float4.Lerp(y0, y1, t.y);
				return *(Vector128<float>*)&result;
			}
		}

		class NoPrefetch : IFilter
		{
			public Vector128<float> Convert(Texture texture, Float2 uv)
			{
				uv *= texture.size;
				Int2 rounded = uv.Rounded;

				Int2 upperRight = rounded.Min(texture.oneLess);
				Int2 bottomLeft = rounded.Max(Int2.one) - Int2.one;

				//Interpolate
				Float2 t = Int2.InverseLerp(bottomLeft, upperRight, uv - Float2.half).Clamp(0f, 1f);

				Vector128<float> timeX = Vector128.Create(t.x);
				Vector128<float> timeY = Vector128.Create(t.y);

				ref readonly Vector128<float> y0x0 = ref texture.GetPixel(bottomLeft);
				ref readonly Vector128<float> y0x1 = ref texture.GetPixel(new Int2(upperRight.x, bottomLeft.y));
				Vector128<float> y0 = Lerp(y0x0, y0x1, timeX);

				ref readonly Vector128<float> y1x0 = ref texture.GetPixel(new Int2(bottomLeft.x, upperRight.y));
				ref readonly Vector128<float> y1x1 = ref texture.GetPixel(upperRight);
				Vector128<float> y1 = Lerp(y1x0, y1x1, timeX);

				return Lerp(y0, y1, timeY);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static Vector128<float> Lerp(in Vector128<float> left, in Vector128<float> right, in Vector128<float> time)
			{
				Vector128<float> length = Sse.Subtract(right, left);

				if (Fma.IsSupported) return Fma.MultiplyAdd(length, time, left);
				return Sse.Add(Sse.Multiply(length, time), left);
			}
		}

		class SseOnly : IFilter
		{
			public Vector128<float> Convert(Texture texture, Float2 uv)
			{
				uv *= texture.size;
				Int2 rounded = uv.Rounded;

				Int2 upperRight = rounded.Min(texture.oneLess);
				Int2 bottomLeft = rounded.Max(Int2.one) - Int2.one;

				//Prefetch color data
				ref readonly Vector128<float> y0x0 = ref texture.GetPixel(bottomLeft);
				ref readonly Vector128<float> y0x1 = ref texture.GetPixel(new Int2(upperRight.x, bottomLeft.y));

				ref readonly Vector128<float> y1x0 = ref texture.GetPixel(new Int2(bottomLeft.x, upperRight.y));
				ref readonly Vector128<float> y1x1 = ref texture.GetPixel(upperRight);

				//Interpolate
				Float2 t = Int2.InverseLerp(bottomLeft, upperRight, uv - Float2.half).Clamp(0f, 1f);

				Vector128<float> timeX = Vector128.Create(t.x);
				Vector128<float> timeY = Vector128.Create(t.y);

				Vector128<float> y0 = Lerp(y0x0, y0x1, timeX);
				Vector128<float> y1 = Lerp(y1x0, y1x1, timeX);

				return Lerp(y0, y1, timeY);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static Vector128<float> Lerp(in Vector128<float> left, in Vector128<float> right, in Vector128<float> time)
			{
				Vector128<float> length = Sse.Subtract(right, left);
				return Sse.Add(Sse.Multiply(length, time), left);
			}
		}
	}
}