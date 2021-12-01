namespace EchoRenderer.Objects.GeometryObjects
{
	public readonly struct GeometryCounts
	{
		public GeometryCounts(long triangle, long sphere, long instance)
		{
			this.triangle = triangle;
			this.sphere = sphere;
			this.instance = instance;
		}

		public readonly long triangle;
		public readonly long sphere;
		public readonly long instance;

		public long Total => triangle + sphere + instance;

		public static GeometryCounts operator +(GeometryCounts first, GeometryCounts second) => new(first.triangle + second.triangle, first.sphere + second.sphere, first.instance + second.instance);
		public static GeometryCounts operator *(GeometryCounts counts, int value) => new(counts.triangle * value, counts.sphere * value, counts.instance * value);
	}
}