using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Objects.Scenes;

namespace EchoRenderer.Objects.Preparation
{
	public class PreparedInstanceRoot : PreparedInstance
	{
		public PreparedInstanceRoot(ScenePreparer preparer, Scene scene) : base(preparer, scene, null, uint.MaxValue) { }

		/// <summary>
		/// Processes <paramref name="query"/> as a <see cref="PreparedInstance"/> root.
		/// </summary>
		public void TraceRoot(ref TraceQuery query)
		{
			Assert.AreEqual(query.current, default);
			pack.aggregator.Trace(ref query);
		}

		/// <summary>
		/// Processes <paramref name="query"/> as a <see cref="PreparedInstance"/> root and returns the result.
		/// </summary>
		public bool OccludeRoot(ref OccludeQuery query)
		{
			Assert.AreEqual(query.current, default);
			return pack.aggregator.Occlude(ref query);
		}
	}
}