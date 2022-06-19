using System.Runtime.CompilerServices;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Scenic.Geometries;

namespace Echo.Core.Aggregation.Primitives;

/// <summary>
/// The type of an <see cref="EntityToken"/>.
/// </summary>
/// <remarks>Because of the data size <see cref="EntityToken"/>, at a maximum there can only be 16
/// different <see cref="TokenType"/>. See <see cref="EntityToken.IndexCount"/> for more.</remarks>
public enum TokenType : uint
{
	/// <summary>
	/// Represents a node inside an <see cref="Aggregator"/>.
	/// </summary>
	Node,

	/// <summary>
	/// Represents a <see cref="PreparedTriangle"/>.
	/// </summary>
	Triangle,

	/// <summary>
	/// Represents a <see cref="PreparedInstance"/>.
	/// </summary>
	Instance,

	/// <summary>
	/// Represents a <see cref="PreparedSphere"/>.
	/// </summary>
	Sphere
}

public static class TokenTypeExtensions
{
	const TokenType Invalid = (TokenType)uint.MaxValue;

	/// <summary>
	/// Returns whether a <see cref="TokenType"/> is of type <see cref="TokenType.Triangle"/>, <see cref="TokenType.Instance"/>, or <see cref="TokenType.Sphere"/>.
	/// </summary>
	public static bool IsGeometry(this TokenType type) => AreEqual(type, TokenType.Triangle, TokenType.Instance, TokenType.Sphere);

	/// <summary>
	/// Returns whether a <see cref="TokenType"/> is of type <see cref="TokenType.Triangle"/> or <see cref="TokenType.Sphere"/>.
	/// </summary>
	public static bool IsRawGeometry(this TokenType type) => AreEqual(type, TokenType.Triangle, TokenType.Sphere);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] //Constants automatically inlined by compiler
	static bool AreEqual(TokenType value,
						 TokenType other00 = Invalid, TokenType other01 = Invalid, TokenType other02 = Invalid, TokenType other03 = Invalid,
						 TokenType other04 = Invalid, TokenType other05 = Invalid, TokenType other06 = Invalid, TokenType other07 = Invalid,
						 TokenType other08 = Invalid, TokenType other09 = Invalid, TokenType other10 = Invalid, TokenType other11 = Invalid,
						 TokenType other12 = Invalid, TokenType other13 = Invalid, TokenType other14 = Invalid, TokenType other15 = Invalid)
	{
		int constant = Once(other00) | Once(other01) | Once(other02) | Once(other03) |
					   Once(other04) | Once(other05) | Once(other06) | Once(other07) |
					   Once(other08) | Once(other09) | Once(other10) | Once(other11) |
					   Once(other12) | Once(other13) | Once(other14) | Once(other15);

		return ((1 << (int)value) & constant) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int Once(TokenType type) => type == Invalid ? 0 : 1 << (int)type;
	}
}