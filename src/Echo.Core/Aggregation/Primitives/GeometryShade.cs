using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Materials;

namespace Echo.Core.Aggregation.Primitives;

/// <summary>
/// Contains information about a point on a geometry that is necessary to shading.
/// </summary>
public readonly struct GeometryShade
{
	public GeometryShade(Material material, Float3 normal, Float2 texcoord)
	{
		Ensure.IsNotNull(material);
		Ensure.AreEqual(normal.SquaredMagnitude, 1f);

		this.material = material;
		_normal = normal;
		_texcoord = texcoord;

		material.ApplyNormalMapping(texcoord, ref _normal);
	}

	/// <summary>
	/// The material of the geometry this <see cref="GeometryShade"/> represents.
	/// If this geometry does not support shading, the value of this field is null.
	/// </summary>
	public readonly Material material;

	readonly Float3 _normal;
	readonly Float2 _texcoord;

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
}