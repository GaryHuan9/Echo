using System;
using System.Diagnostics;

namespace Echo.Core.Common.Diagnostics;

/// <summary>
/// A mutable struct that defines a one-time seal to be used for debugging, which is mostly used to indicate the semi-readonly-ness of
/// certain objects (i.e. a particular set of operations is available prior to sealing, while another set is available after sealing).
/// </summary>
public struct Seal
{
#if DEBUG
	internal volatile bool applied;
#endif

	/// <summary>
	/// Makes sure that the <see cref="Seal"/> is not <see cref="applied"/>.
	/// </summary>
	[Conditional("DEBUG")]
	public readonly void EnsureNotApplied()
	{
#if DEBUG
		if (applied) throw new InvalidOperationException($"Operation invalid after the {nameof(Seal)} has been applied.");
#endif
	}

	/// <summary>
	/// Makes sure that the <see cref="Seal"/> is <see cref="applied"/>.
	/// </summary>
	[Conditional("DEBUG")]
	public readonly void EnsureApplied()
	{
#if DEBUG
		if (!applied) throw new InvalidOperationException($"Operation invalid before the {nameof(Seal)} has been applied.");
#endif
	}
}

public static class SealExtensions
{
	//NOTE: the reason that the apply methods are defined as extension methods is to use the ref keyword,
	//which disallows potential accidental readonly declarations of the struct from invoking this method.

	/// <summary>
	/// Applies this seal; note that this is a one time operation.
	/// </summary>
	[Conditional("DEBUG")]
	public static void Apply(ref this Seal seal)
	{
#if DEBUG
		seal.EnsureNotApplied();
		TryApply(ref seal);
#endif
	}

	/// <summary>
	/// Applies this seal; note that this is a one time operation.
	/// </summary>
	[Conditional("DEBUG")]
	public static void TryApply(ref this Seal seal)
	{
#if DEBUG
		seal.applied = true;
#endif
	}
}