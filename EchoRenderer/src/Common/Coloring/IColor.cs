﻿namespace EchoRenderer.Common.Coloring;

public interface IColor<out T> where T : IColor<T>
{
	/// <summary>
	/// Returns this <see cref="IColor{T}"/> converted as an <see cref="RGBA128"/>.
	/// </summary>
	RGBA128 ToRGBA128();

	/// <summary>
	/// Creates a new <typeparamref name="T"/> from an <see cref="RGBA128"/>.
	/// </summary>
	T FromRGBA128(in RGBA128 value); //OPTIMIZE: convert to static method when we upgrade to dotnet 7
}