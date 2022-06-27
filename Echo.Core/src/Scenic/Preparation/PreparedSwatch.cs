using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CodeHelpers.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Instancing;

namespace Echo.Core.Scenic.Preparation;

/// <summary>
/// A <see cref="MaterialSwatch"/> that is prepared for fast interactions.
/// </summary>
public class PreparedSwatch
{
	public PreparedSwatch(Material[] materials) => this.materials = materials;

	readonly Material[] materials;

	/// <summary>
	/// Index this <see cref="PreparedSwatch"/> at <paramref name="index"/>.
	/// </summary>
	public Material this[MaterialIndex index] => materials[index];
}