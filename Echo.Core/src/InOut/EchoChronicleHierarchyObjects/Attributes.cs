using System;

namespace Echo.Core.InOut.EchoChronicleHierarchyObjects;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class EchoSourceTypeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
public sealed class EchoSourceMethodAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
public sealed class EchoSourcePropertyAttribute : Attribute { }