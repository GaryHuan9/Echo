﻿using System;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

public sealed class Conductor : Material
{
	NotNull<Texture> _roughness = Pure.black;

	public Texture Roughness
	{
		get => _roughness;
		set => _roughness = value;
	}

	/// <summary>
	/// Whether this <see cref="Conductor"/> uses artistic parameters over physical parameters.
	/// </summary>
	/// <remarks>If this is true, <see cref="MainColor"/> and <see cref="EdgeColor"/> are used by
	/// this <see cref="Conductor"/> while <see cref="RefractiveIndex"/> and <see cref="Extinction"/>
	/// are entirely ignored. If this property is false, the opposite behavior will occur.</remarks>
	public bool Artistic { get; set; } = true;

	NotNull<Texture> _mainColor = Pure.white;
	NotNull<Texture> _edgeColor = Pure.white;

	public Texture MainColor
	{
		get => _mainColor;
		set => _mainColor = value;
	}

	public Texture EdgeColor
	{
		get => _edgeColor;
		set => _edgeColor = value;
	}

	NotNull<Texture> _refractiveIndex = Pure.white;
	NotNull<Texture> _extinction = Pure.white;

	public Texture RefractiveIndex
	{
		get => _refractiveIndex;
		set => _refractiveIndex = value;
	}

	public Texture Extinction
	{
		get => _extinction;
		set => _extinction = value;
	}

	public override BSDF Scatter(in Contact contact, Allocator allocator, in RGB128 albedo)
	{
		BSDF bsdf = NewBSDF(contact, allocator, albedo);

		RGB128 roughness = Sample(Roughness, contact);
		float alphaX = IMicrofacet.GetAlpha(roughness.R, out bool isSpecularX);
		float alphaY = IMicrofacet.GetAlpha(roughness.G, out bool isSpecularY);

		RGB128 index;
		RGB128 extinction;

		if (Artistic)
		{
			Float4 main = Sample(MainColor, contact);
			Float4 edge = Sample(EdgeColor, contact);

			//Converts artistic parameters into physical parameters based on
			//Artist Friendly Metallic Fresnel [Gulbrandsen 2014]

			main = main.Min((Float4)FastMath.OneMinusEpsilon);

			Float4 sqrt = Sqrt(main);
			Float4 eta = Float4.Lerp
			(
				(Float4.One + sqrt) / (Float4.One - sqrt),
				(Float4.One - main) / (Float4.One + main), edge
			);

			index = (RGB128)eta;

			Float4 value = main * Square(eta + Float4.One) - Square(eta - Float4.One); //OPTIMIZE FMA
			extinction = (RGB128)Sqrt((value / (Float4.One - main)).Max(Float4.Zero));

			static Float4 Sqrt(in Float4 value) => new(MathF.Sqrt(value.X), MathF.Sqrt(value.Y), MathF.Sqrt(value.Z), MathF.Sqrt(value.W)); //OPTIMIZE
			static Float4 Square(in Float4 value) => value * value;
		}
		else
		{
			index = Sample(RefractiveIndex, contact);
			extinction = Sample(Extinction, contact);
		}

		ComplexFresnel fresnel = new ComplexFresnel(RGB128.White, index, extinction);

		if (!isSpecularX || !isSpecularY)
		{
			var microfacet = new TrowbridgeReitzMicrofacet(alphaX, alphaY);

			bsdf.Add<GlossyReflection<TrowbridgeReitzMicrofacet, ComplexFresnel>>(allocator).Reset(microfacet, fresnel);
		}
		else bsdf.Add<SpecularReflection<ComplexFresnel>>(allocator).Reset(fresnel);

		return bsdf;
	}
}