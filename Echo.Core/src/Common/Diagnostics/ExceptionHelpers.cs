using System;

namespace Echo.Core.Common.Diagnostics;

public static class ExceptionHelper
{
	public static Exception NotPossible => new("The impossible happened!");

	public static Exception Invalid(string memberName, InvalidType type) => GetException(memberName, GetInvalidMessage(type));
	public static Exception Invalid(string memberName, object argumentValue, InvalidType type) => GetException(memberName, argumentValue, GetInvalidMessage(type));

	public static Exception Invalid(string memberName, string customMessage) => GetException(memberName, customMessage);
	public static Exception Invalid(string memberName, object argumentValue, string customMessage) => GetException(memberName, argumentValue, customMessage);

	static Exception GetException(string name, string message) => new($"The member named '{name}' is invalid because it {message}");
	static Exception GetException(string name, object value, string message) => new($"The member named '{name}' with a value of {DebugHelper.ToString(value)} is invalid because it {message}");

	static string GetInvalidMessage(InvalidType type)
	{
		switch (type)
		{
			case InvalidType.isNull:             return "is null.";
			case InvalidType.unexpected:         return "is unexpected.";
			case InvalidType.outOfBounds:        return "is out of bounds.";
			case InvalidType.minLargerThanMax:   return "is larger than max.";
			case InvalidType.countIsZero:        return "has a count/length of 0, which is unexpected.";
			case InvalidType.unexpectedId:       return "is an unexpected identification.";
			case InvalidType.indistinctItems:    return "does not contain indistinct items.";
			case InvalidType.foundDuplicate:     return "is already present and a duplicate of a current object.";
			case InvalidType.readonlyAssignment: return "is semi-readonly (can only be assigned once).";
			case InvalidType.readonlyNoData:     return "is semi-readonly and needs an assignment before accessing.";
			case InvalidType.notFound:           return "cannot be found in the collection.";
		}

		throw NotPossible;
	}
}

public enum InvalidType
{
	isNull,
	unexpected,
	outOfBounds,
	minLargerThanMax,
	countIsZero,
	unexpectedId,
	indistinctItems,
	foundDuplicate,
	readonlyAssignment,
	readonlyNoData,
	notFound
}