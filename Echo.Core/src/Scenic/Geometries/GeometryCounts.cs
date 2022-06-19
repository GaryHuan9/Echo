namespace Echo.Core.Scenic.Geometries;

public readonly struct GeometryCounts
{
	public GeometryCounts(in ulong triangle, in ulong instance, in ulong sphere)
	{
		this.triangle = triangle;
		this.instance = instance;
		this.sphere = sphere;
	}

	public GeometryCounts(int triangle, int instance, int sphere) : this
	(
		checked((ulong)triangle),
		checked((ulong)instance),
		checked((ulong)sphere)
	) { }

	public readonly ulong triangle;
	public readonly ulong instance;
	public readonly ulong sphere;

	public ulong Total => triangle + instance + sphere;

	public static GeometryCounts operator +(in GeometryCounts first, in GeometryCounts second) =>
		new(first.triangle + second.triangle,
			first.instance + second.instance,
			first.sphere + second.sphere);

	public static GeometryCounts operator *(in GeometryCounts counts, uint value) =>
		new(counts.triangle * value,
			counts.instance * value,
			counts.sphere * value);
}