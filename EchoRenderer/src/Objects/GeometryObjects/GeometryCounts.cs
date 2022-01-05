namespace EchoRenderer.Objects.GeometryObjects
{
	public readonly struct GeometryCounts
	{
		public GeometryCounts(in ulong triangle, in ulong sphere, in ulong instance)
		{
			this.triangle = triangle;
			this.sphere = sphere;
			this.instance = instance;
		}

		public GeometryCounts(int triangle, int sphere, int instance) : this
		(
			checked((ulong)triangle),
			checked((ulong)sphere),
			checked((ulong)instance)
		) { }

		public readonly ulong triangle;
		public readonly ulong sphere;
		public readonly ulong instance;

		public ulong Total => triangle + sphere + instance;

		public static GeometryCounts operator +(in GeometryCounts first,  in GeometryCounts second) => new(first.triangle + second.triangle, first.sphere + second.sphere, first.instance + second.instance);
		public static GeometryCounts operator *(in GeometryCounts counts, uint              value)  => new(counts.triangle * value, counts.sphere * value, counts.instance * value);
	}
}