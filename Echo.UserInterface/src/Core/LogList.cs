using System;
using System.Collections.Generic;
using System.Threading;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using Echo.UserInterface.Core.Common;

namespace Echo.UserInterface.Core;

public static class LogList
{
	public static readonly ILogger Logger = new LoggerImpl();

	static ConcurrentList<string> logs = new();

	public static IReadOnlyList<string> Logs => logs;

	public static void Add(string value) => logs.ImmediateAdd($"[{DateTime.Now.ToStringDefault()}] {value}");

	public static void Clear() => Interlocked.Exchange(ref logs, new ConcurrentList<string>());

	class LoggerImpl : ILogger
	{
		public void Write(string text) => Add(text);
		public void WriteWarning(string text) => Add($"Warning: {text}");
		public void WriteError(string text) => Add($"ERROR: {text}");
	}
}