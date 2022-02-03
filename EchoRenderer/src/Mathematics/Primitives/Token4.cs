using System;
using System.Runtime.CompilerServices;
using EchoRenderer.Common;

namespace EchoRenderer.Mathematics.Primitives
{
	public unsafe struct Token4
	{
		public Token4(in Token token0, in Token token1, in Token token2, in Token token3)
		{
			ref Token ptr = ref Unsafe.As<byte, Token>(ref data[0]);

			Unsafe.Add(ref ptr, 0) = token0;
			Unsafe.Add(ref ptr, 1) = token1;
			Unsafe.Add(ref ptr, 2) = token2;
			Unsafe.Add(ref ptr, 3) = token3;
		}

		public Token4(ReadOnlySpan<Token> tokens) : this
		(
			tokens.TryGetValue(0, Token.empty), tokens.TryGetValue(1, Token.empty),
			tokens.TryGetValue(2, Token.empty), tokens.TryGetValue(3, Token.empty)
		) { }

#pragma warning disable CS0649
		fixed byte data[Width * Token.Size];
#pragma warning restore CS0649

		const int Width = 4;

		public readonly ref readonly Token this[int index]
		{
			get
			{
				ref readonly byte origin = ref data[0];                     //First retrieve a reference to the head of the array
				ref byte mutable = ref Unsafe.AsRef(in origin);             //Then remove the readonly status on that reference
				ref Token casted = ref Unsafe.As<byte, Token>(ref mutable); //Cast it to a token type (expands its size as well)
				return ref Unsafe.Add(ref casted, index);                   //Finally offsets the reference by index
			}
		}

		public override readonly int GetHashCode()
		{
			fixed (Token4* ptr = &this) return Utilities.GetHashCode(ptr);
		}
	}
}