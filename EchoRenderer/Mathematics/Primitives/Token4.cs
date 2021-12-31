using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EchoRenderer.Mathematics.Primitives
{
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct Token4
	{
		public Token4(in Token token0, in Token token1, in Token token2, in Token token3)
		{
			At(0) = token0;
			At(1) = token1;
			At(2) = token2;
			At(3) = token3;
		}

		public Token4(ReadOnlySpan<Token> tokens) : this
		(
			tokens.TryGetValue(0), tokens.TryGetValue(1),
			tokens.TryGetValue(2), tokens.TryGetValue(3)
		) { }

		fixed uint data[Width];

		const int Width = 4;

		//NOTE: This method signature is really weird, it is not a readonly method! We need to wait for a better C# specification
		//to come out. Currently be careful with using this indexer because it might accidentally create a lot of defensive copies
		//See also: https://github.com/dotnet/csharplang/issues/1710

		public ref readonly Token this[int index] => ref At(index);

		ref Token At(int index) => ref Unsafe.As<uint, Token>(ref data[index]);
	}
}