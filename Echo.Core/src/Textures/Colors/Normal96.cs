using System;
using CodeHelpers.Packed;

namespace Echo.Core.Textures.Colors;

public readonly struct Normal96 : IColor<Normal96>, IFormattable
{

    Normal96(in Float3 value) => d = value;

    readonly Float3 d;

    public RGBA128 ToRGBA128() => (RGBA128)(ToFloat4() / 2f + Float4.Half); //OPTIMIZE: fma
    public Normal96 FromRGBA128(in RGBA128 value) => FromFloat4((Float4)value * 2f - Float4.One);
    public Float4 ToFloat4() => (Float4)d;
    public Normal96 FromFloat4(in Float4 value) => (Normal96)(Float3)value;

    public string ToString(string format, IFormatProvider provider = null) => d.ToString(format, provider);
    public static explicit operator Normal96(in Float3 value) => new Normal96(value.Normalized);
    public static explicit operator Float3(in Normal96 value) => value.d;
}