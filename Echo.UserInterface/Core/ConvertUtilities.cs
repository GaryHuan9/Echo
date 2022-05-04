﻿using CodeHelpers.Packed;
using SFML.System;
using SFML.Window;

namespace Echo.UserInterface.Core;

public static class ConvertUtilities
{
	public static Vector2f As(this Float2 value) => new Vector2f(value.X, value.Y);
	public static Vector2i As(this Int2 value) => new Vector2i(value.X, value.Y);
	public static Vector2u Cast(this Int2 value) => new Vector2u((uint)value.X, (uint)value.Y);

	public static Float2 As(this Vector2f value) => new Float2(value.X, value.Y);
	public static Int2 As(this Vector2i value) => new Int2(value.X, value.Y);
	public static Int2 Cast(this Vector2u value) => new Int2((int)value.X, (int)value.Y);

	public static Int2 As(this MouseMoveEventArgs args) => new Int2(args.X, args.Y);
	public static Int2 As(this MouseButtonEventArgs args) => new Int2(args.X, args.Y);
}