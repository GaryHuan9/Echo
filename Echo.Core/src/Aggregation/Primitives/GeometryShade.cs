using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Materials;

namespace Echo.Core.Aggregation.Primitives;

/// <summary>
/// Contains information about a point on a geometry that is necessary to shading.
/// </summary>
public readonly struct GeometryShade
{
	public GeometryShade(Material material, Float2 texcoord, in Float3 normal)
	{
		Ensure.IsNotNull(material);
		Ensure.AreEqual(normal.SquaredMagnitude, 1f);

		this.material = material;
		_texcoord = texcoord;
		_normal = normal;

		material.ApplyNormalMapping(texcoord, ref _normal);
	}

	/// <summary>
	/// The material of the geometry this <see cref="GeometryShade"/> represents.
	/// If this geometry does not support shading, the value of this field is null.
	/// </summary>
	public readonly Material material;

	readonly Float2 _texcoord;
	readonly Float3 _normal;

	/// <summary>
	/// The texture coordinate at this target point.
	/// </summary>
	public Float2 Texcoord
	{
		get
		{
			Ensure.IsNotNull(material);
			return _texcoord;
		}
	}

	/// <summary>
	/// The shading normal at this target point.
	/// </summary>
	public Float3 Normal
	{
		get
		{
			Ensure.IsNotNull(material);
			return _normal;
		}
	}
}