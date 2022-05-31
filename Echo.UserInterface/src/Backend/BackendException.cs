using System;

namespace Echo.UserInterface.Backend;

public sealed class BackendException : Exception
{
	public BackendException() : base("An error occured from the SDL2 backend.") { }
	public BackendException(int code) : this((long)code) { }
	public BackendException(uint code) : this((long)code) { }
	BackendException(long code) : base($"An error occured from the SDL2 backend (Error code: {code}).") { }
}

public static class BackendExceptionExtensions
{
	public static void ThrowOnError(this int code)
	{
		if (code == 0) return;
		throw new BackendException(code);
	}

	public static void ThrowOnError(this uint code)
	{
		if (code == 0) return;
		throw new BackendException(code);
	}
}