using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using static EchoRenderer.Mathematics.Utilities;

namespace EchoRenderer.Rendering.PostProcessing
{
	public class ToneMapping : PostProcessingWorker
	{
		public ToneMapping(PostProcessingEngine engine, float exposure, float smoothness) : base(engine)
		{
			exposureVector = Vector128.Create(exposure);
			smoothnessVector = Vector128.Create(smoothness);
		}

		readonly Vector128<float> exposureVector;
		readonly Vector128<float> smoothnessVector;

		static readonly Vector128<float> vector16 = Vector128.Create(1.6f);
		static readonly Vector128<float> vector5_3 = Vector128.Create(5f / 3f);

		public override void Dispatch() => RunPass(Pass0);

		//https://www.desmos.com/calculator/vxsqzz9etl
		//We offer multiple different mapping options

		void Pass0(Int2 position)
		{
			Vector128<float> source = Sse.Multiply(renderBuffer[position], exposureVector);
			Vector128<float> oneLess = Sse.Subtract(source, vector1);

			Vector128<float> a = Sse.Subtract(vector05, Sse.Multiply(vector05, Sse.Divide(oneLess, smoothnessVector)));

			Vector128<float> h = Sse.Min(Sse.Max(a, vector0), vector1);
			Vector128<float> b = Sse.Subtract(oneLess, smoothnessVector);

			renderBuffer[position] = Sse.Add(Sse.Multiply(Sse.Add(b, Sse.Multiply(smoothnessVector, h)), h), vector1);
		}

		void Pass1(Int2 position)
		{
			Vector128<float> source = Sse.Multiply(renderBuffer[position], exposureVector);

			source = Clamp(vector0, vector5_3, source);

			source = Sse.Divide(Sse.Multiply(source, vector16), Sse.Add(source, vector1));
			source = Sse.Multiply(Sse.Multiply(source, source), DoMAD(source));

			renderBuffer[position] = source;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static Vector128<float> DoMAD(in Vector128<float> source)
			{
				if (Fma.IsSupported) return Fma.MultiplyAdd(source, vectorN2, vector3);
				return Sse.Add(Sse.Multiply(source, vectorN2), vector3);
			}
		}


	}
}