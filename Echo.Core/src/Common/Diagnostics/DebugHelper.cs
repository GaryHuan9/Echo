using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Echo.Core.Common.Diagnostics;

public static class DebugHelper
{
	public static ILogger Logger { get; set; } = new ConsoleLogger();

	static readonly ThreadLocal<ObjectsBuffer> objectsBufferLocal = new(() => new ObjectsBuffer(4, DebugLogType.normal));

	static readonly Dictionary<DebugLogType, Action<string>> logActions = new()
	{
		{ DebugLogType.normal, text => Logger.Write(text) },
		{ DebugLogType.warning, text => Logger.WriteWarning(text) },
		{ DebugLogType.error, text => Logger.WriteError(text) }
	};

	internal const string NullString = "_NULL_";

#region Log Overloads

	public static void Log<T0>(DebugLogType type, T0 t0)
	{
		var buffer = new ObjectsBuffer(objectsBufferLocal.Value, type)
		{
			[0] = t0
		};
		LogInternal(buffer, 1);
	}

	public static void Log<T0, T1>(DebugLogType type, T0 t0, T1 t1)
	{
		var buffer = new ObjectsBuffer(objectsBufferLocal.Value, type)
		{
			[0] = t0,
			[1] = t1
		};
		LogInternal(buffer, 2);
	}

	public static void Log<T0, T1, T2>(DebugLogType type, T0 t0, T1 t1, T2 t2)
	{
		var buffer = new ObjectsBuffer(objectsBufferLocal.Value, type)
		{
			[0] = t0,
			[1] = t1,
			[2] = t2
		};
		LogInternal(buffer, 3);
	}

	public static void Log<T0, T1, T2, T3>(DebugLogType type, T0 t0, T1 t1, T2 t2, T3 t3)
	{
		var buffer = new ObjectsBuffer(objectsBufferLocal.Value, type)
		{
			[0] = t0,
			[1] = t1,
			[2] = t2,
			[3] = t3
		};
		LogInternal(buffer, 4);
	}

	public static void Log<T0>(T0 t0) => Log(DebugLogType.normal, t0);
	public static void Log<T0, T1>(T0 t0, T1 t1) => Log(DebugLogType.normal, t0, t1);
	public static void Log<T0, T1, T2>(T0 t0, T1 t1, T2 t2) => Log(DebugLogType.normal, t0, t1, t2);
	public static void Log<T0, T1, T2, T3>(T0 t0, T1 t1, T2 t2, T3 t3) => Log(DebugLogType.normal, t0, t1, t2, t3);

	public static void LogWarning<T0>(T0 t0) => Log(DebugLogType.warning, t0);
	public static void LogWarning<T0, T1>(T0 t0, T1 t1) => Log(DebugLogType.warning, t0, t1);
	public static void LogWarning<T0, T1, T2>(T0 t0, T1 t1, T2 t2) => Log(DebugLogType.warning, t0, t1, t2);
	public static void LogWarning<T0, T1, T2, T3>(T0 t0, T1 t1, T2 t2, T3 t3) => Log(DebugLogType.warning, t0, t1, t2, t3);

	public static void LogError<T0>(T0 t0) => Log(DebugLogType.error, t0);
	public static void LogError<T0, T1>(T0 t0, T1 t1) => Log(DebugLogType.error, t0, t1);
	public static void LogError<T0, T1, T2>(T0 t0, T1 t1, T2 t2) => Log(DebugLogType.error, t0, t1, t2);
	public static void LogError<T0, T1, T2, T3>(T0 t0, T1 t1, T2 t2, T3 t3) => Log(DebugLogType.error, t0, t1, t2, t3);

	public static void Log(DebugLogType type, params object[] objects) => LogInternal(new ObjectsBuffer(objects, type), objects.Length);

	public static void Log(params object[] objects) => Log(DebugLogType.normal, objects);
	public static void LogWarning(params object[] objects) => Log(DebugLogType.warning, objects);
	public static void LogError(params object[] objects) => Log(DebugLogType.error, objects);

#endregion

	public static string ToString(object target)
	{
		string customToString = GetCustomToString(target);
		if (customToString != null) return customToString;

		switch (target)
		{
			case null:                   return NullString;
			case string stringTarget:    return ToString(stringTarget);
			case IEnumerable enumerable: return ToString(enumerable.Cast<object>());
		}

		return $"{target} (HCode: {target.GetHashCode()})";
	}

	public static string ToString(string target) => target ?? NullString;

	public static string ToString<T>(IEnumerable<T> target)
	{
		if (target == null) return NullString;

		string[] array = target.Select(item => ToString(item)).ToArray();
		return $"{target.GetType()} + Count: {array.Length} [{string.Join(", ", array)}]";
	}

	/// <summary>
	/// Does <paramref name="target"/> has a custom to string method?
	/// Returns the custom to string if it has one, or null if is default.
	/// </summary>
	static string GetCustomToString(object target)
	{
		if (target is null) return null;
		string toString = target.ToString();
		return toString == target.GetType().ToString() ? null : toString;
	}

	/// <summary>
	/// Logs objects buffered in <paramref name="buffer"/>.
	/// Will only process objects from index 0 to <paramref name="length"/> [inclusive, exclusive)
	/// </summary>
	static void LogInternal(ObjectsBuffer buffer, int length)
	{
		Action<string> log = logActions[buffer.logType];

		if (length == 0) //Handles degenerate input
		{
			log("");
			return;
		}

		StringBuilder builder = new();

		for (int i = 0; i < length; i++)
		{
			builder.Append(ToString(buffer[i]));
			if (i + 1 != length) builder.Append("; ");
		}

		log(builder.ToString());
	}

	readonly struct ObjectsBuffer
	{
		public ObjectsBuffer(object[] objects, DebugLogType logType)
		{
			this.objects = objects;
			this.logType = logType;
		}

		public ObjectsBuffer(int bufferSize, DebugLogType logType) : this(new object[bufferSize], logType) { }
		public ObjectsBuffer(ObjectsBuffer buffer, DebugLogType logType) : this(buffer.objects, logType) { }

		readonly object[] objects;
		public readonly DebugLogType logType;

		public object this[int index]
		{
			get => objects[index];
			set => objects[index] = value;
		}

		public int Length => objects.Length;
	}
}

public enum DebugLogType
{
	normal,
	warning,
	error
}