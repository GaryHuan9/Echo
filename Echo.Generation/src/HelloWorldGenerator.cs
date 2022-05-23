using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Echo.Generation;

// [Generator]
// public class HelloWorldGenerator : IIncrementalGenerator
// {
// 	public void Initialize(IncrementalGeneratorInitializationContext context)
// 	{
// 		var mainMethod = context.CompilationProvider.Select(static (compilation, token) => compilation.GetEntryPoint(token));
//
// 		context.RegisterSourceOutput(mainMethod, static (context, method) =>
// 		{
// 			string source = $@" // Auto-generated code
// using System;
//
// namespace {method.ContainingNamespace.ToDisplayString()}
// {{
//     public static partial class {method.ContainingType.Name}
//     {{
//         static partial void HelloFrom(string name) =>
//             Console.WriteLine($""Generator beautiful faces says: Hi from '{{name}}'"");
//     }}
// }}
// ";
// 			var typeName = method.ContainingType.Name;
//
// 			// Add the source code to the compilation
// 			context.AddSource($"{typeName}.g.cs", source);
// 		});
// 	}
// }

[Generator]
public class HelloWorldGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterSourceOutput(context.CompilationProvider.Select(static (_, _) => "Nice"), static (context, text) =>
		{
			string source = $@" // Auto-generated code
using System;

namespace {text}
{{
    public static class {text}
    {{
        public static void HelloFrom(string name) =>
            Console.WriteLine($""Generator beautiful faces says: Hi from '{{name}}'"");
    }}
}}
";
			// Add the source code to the compilation
			context.AddSource($"{text}.g.cs", source);
		});
	}
}