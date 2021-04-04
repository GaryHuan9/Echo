namespace EchoRenderer.Objects.Scenes
{
	public readonly struct GeometryCounts
	{
		public GeometryCounts(long triangle, long sphere, long pack)
		{
			this.triangle = triangle;
			this.sphere = sphere;
			this.pack = pack;
		}

		public readonly long triangle;
		public readonly long sphere;
		public readonly long pack;

		public long Total => triangle + sphere + pack;

		public static GeometryCounts operator +(GeometryCounts first, GeometryCounts second) => new(first.triangle + second.triangle, first.sphere + second.sphere, first.pack + second.pack);
		public static GeometryCounts operator *(GeometryCounts counts, int value) => new(counts.triangle * value, counts.sphere * value, counts.pack * value);
	}
}