using CodeHelpers.Mathematics;
using EchoRenderer.Common;

namespace EchoRenderer.Mathematics
{
	/// <summary>
	/// A class containing short mathematical methods that are frequently used.
	/// </summary>
	public static class ShortMath
	{
		/// <summary>
		/// Returns whether <paramref name="pdf"/> is considered as positive.
		/// </summary>
		public static bool PositivePDF(float pdf) => pdf > Constants.EpsilonPDF;

		/// <summary>
		/// Returns whether <paramref name="radiance"/> is considered as positive/
		/// </summary>
		public static bool PositiveRadiance(in Float3 radiance) => radiance > Constants.epsilonRadiance;
	}
}