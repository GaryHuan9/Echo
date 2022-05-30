using System;

namespace Echo.Common.Compute.Statistics;

/// <summary>
/// An attribute that marks an unmanaged struct implementing <see cref="IStatistics{T}"/> to be automatically source generated.
/// </summary>
/// <remarks>This attribute only functions in the main <see cref="Echo"/> project.
/// Note that the attributed struct must be partial to allow for the generation.</remarks>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class GeneratedStatisticsAttribute : Attribute { }