using System;
using System.Collections.Generic;
using System.Threading;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Threading;
using Echo.Core.InOut;
using Echo.UserInterface.Core.Common;

namespace Echo.UserInterface.Core;

public static class LogList
{
	public static readonly ILogger Logger = new LoggerImpl();

	static ConcurrentList<string> logs = new();

	public static IEnumerable<string> Logs => logs;

	public static void Add(string value) => logs.ImmediateAdd($"[{DateTime.Now.ToInvariant()}] {value}");
	public static void AddWarning(string value) => logs.ImmediateAdd($"[{DateTime.Now.ToInvariant()}] [warning] {value}");
	public static void AddError(string value) => logs.ImmediateAdd($"[{DateTime.Now.ToInvariant()}] [ERROR] {value}");

	public static void Clear() => Interlocked.Exchange(ref logs, new ConcurrentList<string>());

	class LoggerImpl : ILogger
	{
		public void Write(string text) => Add(text);
		public void WriteWarning(string text) => AddWarning(text);
		public void WriteError(string text) => AddError(text);
	}
}