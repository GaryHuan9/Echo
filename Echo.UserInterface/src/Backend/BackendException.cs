using System;
using SDL2;

namespace Echo.UserInterface.Backend;

public sealed class BackendException : Exception
{
	public BackendException() : base("An error occured from the SDL2 backend.") { }
	public BackendException(string message) : base($"An error occured from the SDL2 backend: {message}") { }
	public BackendException(int code) : base($"An error occured from the SDL2 backend: {SDL.SDL_GetError()} ({code})") { }
}

public static class BackendExceptionExtensions
{
	public static void ThrowOnError(this int code)
	{
		if (code >= 0) return;
		throw new BackendException(code);
	}
}