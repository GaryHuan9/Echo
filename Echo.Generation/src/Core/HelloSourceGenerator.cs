using Microsoft.CodeAnalysis;

namespace Echo.Core;

[Generator]
public class HelloSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var mainMethod = context.CompilationProvider.Select((compilation, token) => compilation.GetEntryPoint(token));

		context.RegisterSourceOutput(mainMethod, static (context, method) =>
		{
			// Build up the source code
			string source = $@" // Auto-generated code
using System;

namespace {method.ContainingNamespace.ToDisplayString()}
{{
    public static partial class {method.ContainingType.Name}
    {{
        static partial void HelloFrom(string name) => Console.WriteLine($""Incremental Generator says: Hi from '{{name}}'"");
    }}
}}";
			var typeName = method.ContainingType.Name;

			// Add the source code to the compilation
			context.AddSource($"{typeName}.g.cs", source);
		});
	}
}