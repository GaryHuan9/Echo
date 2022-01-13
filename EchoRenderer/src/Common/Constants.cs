using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Common
{
	public static class Constants
	{
		public const float EpsilonPDF = Scalars.Epsilon;

		public static Vector128<float> LuminanceVector => Vector128.Create(0.2126f, 0.7152f, 0.0722f, 0f);
		public static readonly Float3 epsilonRadiance = Utilities.ToFloat3(Utilities.CreateLuminance(12E-3f));
	}
}