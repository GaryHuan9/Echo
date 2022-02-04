using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Terminals;

public class CommandsController : Terminal.Section, ILogger
{
	public CommandsController(Terminal terminal) : base(terminal)
	{
		logs = new string[SectionHeight - 1];
		DebugHelper.Logger = this;
	}

	static CommandsController() => commands = new ReadOnlyDictionary<string, MethodInfo>
	(
		(from type in Assembly.GetCallingAssembly().GetTypes()
		 from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
		 where method.GetCustomAttribute<CommandAttribute>() != null
		 where method.ReturnType == typeof(CommandResult) ? true : throw ExceptionHelper.Invalid(nameof(method), method, $"does not have a return type of {nameof(CommandResult)}")
		 select new KeyValuePair<string, MethodInfo>(method.Name, method)).ToDictionary(pair => pair.Key, pair => pair.Value)
	);

	public override int Height => SectionHeight;
	const int SectionHeight = 5;

	int inputLength;
	double blinkOffset;

	int cursorPosition;
	char cursorChar;

	readonly string[] logs;

	static readonly ReadOnlyDictionary<string, MethodInfo> commands;

	const int CursorY = 0;
	const char CursorCharacter = '\u2588';
	const float BlinkPeriod = 500f; //In milliseconds
	const string CommandPrefix = "/";

	Int2 Cursor => new(cursorPosition, CursorY);

	public override void Update()
	{
		builders[Cursor] = cursorChar;
		if (Console.KeyAvailable) ProcessInput();

		cursorChar = builders[Cursor];

		//Add logs
		lock (logs)
		{
			for (int i = 0; i < logs.Length; i++) builders.SetLine(i + 1, logs[i]);
		}

		//Replace cursor character when blinking
		if ((terminal.AliveTime - blinkOffset) % (BlinkPeriod * 2d) < BlinkPeriod) builders[Cursor] = CursorCharacter;
	}

	public void Log(string log)
	{
		lock (logs) logs.Insert(0, log);
	}

	public void ProcessCommand(string input)
	{
		Log(input);

		if (!input.StartsWith(CommandPrefix)) return;
		input = input.Substring(CommandPrefix.Length);

		string[] parts = input.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);

		if (parts.Length == 0)
		{
			Log("Empty command!");
			return;
		}

		if (!commands.TryGetValue(parts[0], out MethodInfo command))
		{
			Log($"Cannot find command with signature '{parts[0]}'");
			return;
		}

		var parameters = new object[parts.Length - 1];

		for (int i = 0; i < parameters.Length; i++)
		{
			string part = parts[i + 1];
			object parameter;

			if (bool.TryParse(part, out bool boolValue)) parameter = boolValue;
			else if (int.TryParse(part, NumberStyles.Any, CultureInfo.InvariantCulture, out int intValue)) parameter = intValue;
			else if (float.TryParse(part, NumberStyles.Any, CultureInfo.InvariantCulture, out float floatValue)) parameter = floatValue;
			else parameter = part;

			parameters[i] = parameter;
		}

		object returned = command.Invoke(null, parameters) ?? ExceptionHelper.NotPossible;
		CommandResult result = (CommandResult)returned;

		Log(result.message);
	}

	void ProcessInput()
	{
		ConsoleKeyInfo keyInfo = Console.ReadKey(true);

		switch (keyInfo.Key)
		{
			case ConsoleKey.Backspace:
			{
				if (cursorPosition == 0) break;

				inputLength--;
				cursorPosition--;

				builders.Remove(Cursor);
				break;
			}
			case ConsoleKey.Delete:
			{
				if (cursorPosition == inputLength) break;

				inputLength--;
				builders.Remove(Cursor);

				break;
			}
			case ConsoleKey.Enter:
			{
				ProcessCommand(new string(builders.GetSlice(Int2.up * CursorY, inputLength)));

				builders.Clear(CursorY);
				inputLength = cursorPosition = 0;

				break;
			}
			case ConsoleKey.LeftArrow:
			{
				cursorPosition = Math.Max(0, cursorPosition - 1);
				break;
			}
			case ConsoleKey.RightArrow:
			{
				cursorPosition = Math.Min(inputLength, cursorPosition + 1);
				break;
			}
			default:
			{
				char current = keyInfo.KeyChar;

				if (char.IsControl(current)) break;
				builders.Insert(Cursor, current);

				inputLength++;
				cursorPosition++;

				break;
			}
		}

		blinkOffset = terminal.AliveTime;
	}

	void ILogger.Write(string text) => Log(text);
	void ILogger.WriteWarning(string text) => Log($"Warning: {text}");
	void ILogger.WriteError(string text) => Log($"Error: {text}");
}

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute { }

public readonly struct CommandResult
{
	public CommandResult(string message, bool successful)
	{
		this.message = message;
		this.successful = successful;
	}

	public readonly string message;
	public readonly bool successful;
}