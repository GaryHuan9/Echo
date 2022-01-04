using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeHelpers.Mathematics;
using EchoRenderer.IO;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Mathematics.Randomization;
using EchoRenderer.Objects.GeometryObjects;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Profiles;

namespace EchoRenderer.Experimental
{
	[SimpleJob(RuntimeMoniker.NetCoreApp50)]
	public class BenchmarkBVH
	{
		public BenchmarkBVH()
		{
			Scene scene = new Scene();

			Mesh mesh = new(@"C:\Users\MMXXXVIII\Things\CodingStuff\C#\EchoRenderer\EchoRenderer\Assets\Models\BlenderBMW\BlenderBMW.obj");
			scene.children.Add(new MeshObject(mesh, new Matte()));

			queries = new TraceQuery[65536];

			IRandom random = new SystemRandom(42);

			const float Radius = 9f;
			const float Height = 5f;

			for (int i = 0; i < queries.Length; i++)
			{
				Float2 point = new Float2(MathF.Sqrt(random.Next1()) * Radius, random.Next1() * Height);
				Float3 position = point.X_Y.RotateXZ(random.Next1() * 360f);

				Float3 target = Float3.CreateY(0.6f) + random.NextInSphere(0.25f);

				if (random.Next1() < 0.01f)
				{
					Float3 offset = (target - position) / 2f;

					target = position;
					position += offset;
				}

				queries[i] = new Ray(position, (target - position).Normalized);
			}

			Pairs = new[]
					{
						new Pair(new PreparedScene(scene, new ScenePrepareProfile { AggregatorProfile = new AggregatorProfile { AggregatorType = typeof(BoundingVolumeHierarchy) } }), "Regular"),
						new Pair(new PreparedScene(scene, new ScenePrepareProfile { AggregatorProfile = new AggregatorProfile { AggregatorType = typeof(QuadBoundingVolumeHierarchy) } }), "Quad")
					};
		}

		readonly TraceQuery[] queries;

		[ParamsSource(nameof(Pairs))]
		public Pair CurrentPair { get; set; }

		public IEnumerable<Pair> Pairs { get; set; }

		//First test set. Different sets will have different timings
		//V0: 903.5us per 1000 intersections (recursive)
		//V1: 821.6us per 1000 intersections (iterative unsafe)
		//V2: 761.2us per 1000 intersections (iterative cached hit)

		//NOTE: Tests with 65536 rays will have a higher average because the rays are more distributed

		// |                   Method |     Mean |    Error |   StdDev |
		// |------------------------- |---------:|---------:|---------:|
		// | GetIntersectionOcclusion | 54.96 ms | 0.265 ms | 0.248 ms |
		// |  GetIntersectionOriginal | 58.26 ms | 0.252 ms | 0.223 ms |

		// |          Method | CurrentPair |     Mean |    Error |   StdDev |
		// |---------------- |------------ |---------:|---------:|---------:|
		// | GetIntersection |        Quad | 52.89 ms | 0.314 ms | 0.293 ms |
		// | GetIntersection |     Regular | 61.75 ms | 0.396 ms | 0.370 ms |

		// |          Method | CurrentPair |     Mean |    Error |   StdDev |
		// |---------------- |------------ |---------:|---------:|---------:|
		// | GetIntersection |        Quad | 42.93 ms | 0.192 ms | 0.170 ms |
		// | GetIntersection |     Regular | 61.47 ms | 0.581 ms | 0.543 ms |

		// New intersection system only calculating distance and uv without auxiliary data
		// |          Method | CurrentPair |     Mean |    Error |   StdDev |
		// |---------------- |------------ |---------:|---------:|---------:|
		// | GetIntersection |        Quad | 40.81 ms | 0.297 ms | 0.248 ms |
		// | GetIntersection |     Regular | 59.21 ms | 0.289 ms | 0.270 ms |

		[Benchmark]
		public bool GetIntersection()
		{
			bool result = default;

			for (int i = 0; i < queries.Length; i++)
			{
				ref readonly TraceQuery source = ref queries[i];
				TraceQuery query = source;
				result ^= CurrentPair.scene.Trace(ref query);
			}

			return result;
		}

		public readonly struct Pair
		{
			public Pair(PreparedScene scene, string name)
			{
				this.scene = scene;
				this.name = name;
			}

			public readonly PreparedScene scene;
			public readonly string name;

			public override string ToString() => name;
		}
	}

}