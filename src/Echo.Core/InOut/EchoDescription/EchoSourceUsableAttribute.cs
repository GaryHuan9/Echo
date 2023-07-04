using System;

namespace Echo.Core.InOut.EchoDescription;

/// <summary>
/// Attribute added to members that should be usable in <see cref="EchoSource"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct |
				AttributeTargets.Method | AttributeTargets.Constructor |
				AttributeTargets.Property, Inherited = false)]
public sealed class EchoSourceUsableAttribute : Attribute { }