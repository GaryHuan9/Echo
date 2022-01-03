using System;

namespace EchoRenderer.Rendering.Scattering
{
	[Flags]
	public enum FunctionType
	{
		//A function can be either reflective or transitive or both
		reflective = 1 << 0,   //Light bounces off of the interacting surface
		transmissive = 1 << 1, //Light goes through the interacting surface

		//A function can either be diffuse, glossy, or specular
		diffuse = 1 << 2,  //Light is randomly scattered from the surface
		glossy = 1 << 3,   //Light is clustered towards one direction
		specular = 1 << 4, //Light only bounces in one direction

		//Combination of all attributes or none
		all = reflective | transmissive | diffuse | glossy | specular,
		none = 0
	}

	public static class FunctionTypeExtensions
	{
		public static bool Fit(this FunctionType type, FunctionType fit) => (type & fit) == type;
		public static bool Has(this FunctionType type, FunctionType has) => (type & has) == has;
	}
}