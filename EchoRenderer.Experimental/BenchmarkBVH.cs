using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using CodeHelpers.Mathematics;
using EchoRenderer.IO;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Accelerators;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Objects.GeometryObjects;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Profiles;

namespace EchoRenderer.Experimental
{
	public class BenchmarkBVH
	{
		public BenchmarkBVH()
		{
			Scene scene = new Scene();

			Mesh mesh = new(@"C:\Users\MMXXXVIII\Things\CodingStuff\C#\EchoRenderer\EchoRenderer\Assets\Models\BlenderBMW\BlenderBMW.obj");
			scene.children.Add(new MeshObject(mesh, new Matte()));

			queries = new TraceQuery[65536];

			ExtendedRandom random = new ExtendedRandom(42);

			const float Radius = 9f;
			const float Height = 5f;

			for (int i = 0; i < queries.Length; i++)
			{
				Float2 point = new Float2(MathF.Sqrt(Random()) * Radius, Random() * Height);
				Float3 position = point.X_Y.RotateXZ(Random() * 360f);

				Float3 target = Float3.CreateY(0.6f) + random.NextInSphere(0.25f);

				if (random.NextDouble() < 0.01f)
				{
					Float3 offset = (target - position) / 2f;

					target = position;
					position += offset;
				}

				queries[i] = new Ray(position, (target - position).Normalized);
			}

			Pairs = new[]
					{
						new Pair(new PressedScene(scene, new ScenePressProfile { AcceleratorProfile = new TraceAcceleratorProfile { AcceleratorType = typeof(BoundingVolumeHierarchy) } }), "Regular"),
						new Pair(new PressedScene(scene, new ScenePressProfile { AcceleratorProfile = new TraceAcceleratorProfile { AcceleratorType = typeof(QuadBoundingVolumeHierarchy) } }), "Quad")
					};

			float Random() => (float)random.NextDouble();
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

		[Benchmark]
		public void GetIntersection()
		{
			for (int i = 0; i < queries.Length; i++)
			{
				ref TraceQuery query = ref queries[i];
				CurrentPair.scene.Trace(ref query);
				query.distance = float.PositiveInfinity;
			}
		}

		public readonly struct Pair
		{
			public Pair(PressedScene scene, string name)
			{
				this.scene = scene;
				this.name = name;
			}

			public readonly PressedScene scene;
			public readonly string name;

			public override string ToString() => name;
		}
	}

}