using System;

namespace EchoRenderer.Rendering.Scattering
{
	[Flags]
	public enum FunctionType
	{
		//A function can be either reflective or transitive or both
		/// <summary>
		/// Light bounces off of the interacting surface
		/// </summary>
		reflective = 1 << 0,

		/// <summary>
		/// Light goes through the interacting surface
		/// </summary>
		transmissive = 1 << 1,

		//A function can either be diffuse, glossy, or specular
		/// <summary>
		/// Light is randomly scattered from the surface
		/// </summary>
		diffuse = 1 << 2,

		/// <summary>
		/// Light is clustered towards one direction
		/// </summary>
		glossy = 1 << 3,

		/// <summary>
		/// Light only bounces in one direction
		/// </summary>
		specular = 1 << 4,

		/// <summary>
		/// All of the types
		/// </summary>
		all = reflective | transmissive | diffuse | glossy | specular,

		/// <summary>
		/// None of the types
		/// </summary>
		none = 0
	}

	public static class FunctionTypeExtensions
	{
		/// <summary>
		/// Returns whether all the flags turned on in <paramref name="type"/> is turned on in <paramref name="other"/>.
		/// </summary>
		public static bool Fits(this FunctionType type, FunctionType other) => (type & other) == type;

		/// <summary>
		/// Returns whether any flags turned on in <paramref name="type"/> is turned on in <paramref name="other"/>.
		/// </summary>
		public static bool Any(this FunctionType type, FunctionType other) => (type & other) != 0;
	}
}