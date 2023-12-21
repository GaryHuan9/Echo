using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using Echo.Core.Common.Compute;
using Echo.Core.Common.Diagnostics;
using Echo.Core.InOut;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Processes;
using Echo.Core.Textures.Grids;
using OpenImageDenoisePrecompiled;

namespace Echo.Terminal;

class Program
{
	static int Main(string[] arguments)
	{
		Console.WriteLine
		(
			@$"
Echo Photorealistic Rendering Core [Version {GetVersion(typeof(Program)).ToString(3)}]
Terminal Render User Interface [Version {GetVersion(typeof(Device)).ToString(3)}]
.NET Runtime [Version {Environment.Version.ToString(3)}]
{Environment.OSVersion.VersionString}

Copyright (C) 2020-2023 Gary Huang, et al.
All rights reserved.
"
		);

		//Parse program arguments
		HandleSwitchHelp(arguments);
		HandleSwitchQuiet(arguments);

		var outputLayerFile = HandleSwitchOutput(arguments);
		var utilizationValue = HandleSwitchUtilization(arguments);
		var profileIdentifier = HandleSwitchProfile(arguments);

#if DEBUG
		Console.WriteLine("Running in DEBUG mode; expect significant reduction in performance.");
#endif
		if (Debugger.IsAttached) Console.WriteLine("Noted attached debugger to process.");

		using Device device = new Device(utilizationValue);
		Console.WriteLine($"Created {nameof(Device)} with {device.Population} workers.");

		EchoSource source = HandleEchoSource(arguments);
		if (source == null) return 1;
		Console.WriteLine($"Parsed {nameof(EchoSource)} file.");

		RenderProfile profile = ConstructProfile(source, ref profileIdentifier);
		if (profile == null) return 1;
		Console.WriteLine($"Constructed {nameof(RenderProfile)} '{profileIdentifier}'.");

		if (OidnPrecompiled.TryLoad()) Console.WriteLine("Successfully loaded Precompiled Oidn binary library.");
		else Console.WriteLine("Unable to load Precompiled Oidn binaries. Some operations will not be available.");
		Console.WriteLine();

		//Begin rendering
		ScheduledRender render = profile.ScheduleTo(device);
		Console.WriteLine($"Scheduled render to {nameof(Device)} with {render.operations.Length} operations.");

		render.Monitor();
		Console.WriteLine("Render finished.");

		//Save results
		bool savedAny = false;

		foreach ((string name, string file) in outputLayerFile)
		{
			if (render.texture.TryGetLayer(name, out TextureGrid texture))
			{
				texture.Save(file);
				savedAny = true;
				Console.WriteLine($"Saved render layer '{name}' to '{file}'.");
			}
			else Console.WriteLine($"No layer named as '{name}' to output; skipping.");
		}

		if (!savedAny)
		{
			render.texture.Save("render.png");
			Console.WriteLine("Saved main render layer to 'render.png'.");
		}

		return 0;
	}

	static Version GetVersion(Type type) => type.Assembly.GetName().Version;

	static bool FindSwitch(ReadOnlySpan<string> arguments, string pattern, out int index)
	{
		Ensure.IsTrue(pattern.Length > 0);

		Span<char> shortPattern = stackalloc char[] { '-', pattern[0] };
		Span<char> longPattern = stackalloc char[pattern.Length + 2];

		"--".CopyTo(longPattern[..2]);
		pattern.CopyTo(longPattern[2..]);

		for (index = 0; index < arguments.Length; index++)
		{
			ReadOnlySpan<char> argument = arguments[index];
			if (argument.SequenceEqual(shortPattern)) return true;
			if (argument.SequenceEqual(longPattern)) return true;
		}

		return false;
	}

	static void HandleSwitchHelp(ReadOnlySpan<string> arguments)
	{
		if (arguments.Length > 0 && !FindSwitch(arguments, "help", out _)) return;

		Console.WriteLine
		(
			"Usage: Echo.Terminal.* [options] <echo_source>\n" +
			"  -h, --help                     Display this help message.\n" +
			"  -q, --quiet                    Perform the render without any status and logging messages.\n" +
			"  -p, --profile <identifier>     Identifies the profile to define the render. By default,\n" +
			"                                 the first one found in the echo source file is used.\n" +
			"  -o, --output <layer> <file>    Select render layer to output as image file. Multiple output is accepted.\n" +
			"                                 If none is specified, by default the main layer is exported as render.png.\n" +
			"  -u, --utilization <fraction>   The fraction of virtual worker threads to use. By default all is used."
		);

		Environment.Exit(0);
	}

	static void HandleSwitchQuiet(ReadOnlySpan<string> arguments)
	{
		if (!FindSwitch(arguments, "quiet", out _)) return;
		Console.SetOut(new StreamWriter(Stream.Null));
	}

	static ImmutableArray<(string layer, string file)> HandleSwitchOutput(ReadOnlySpan<string> arguments)
	{
		var builder = ImmutableArray.CreateBuilder<(string layer, string file)>();

		while (FindSwitch(arguments, "output", out int index))
		{
			string layer = index + 1 < arguments.Length ? arguments[index + 1] : null;
			string file = index + 2 < arguments.Length ? arguments[index + 2] : null;

			if (!string.IsNullOrEmpty(layer) && !string.IsNullOrEmpty(file)) builder.Add((layer, file));
			else Console.WriteLine($"Invalid output from layer '{layer}' to file '{file}'; skipping.");

			arguments = arguments[(index + 1)..];
		}

		return builder.ToImmutable();
	}

	static float HandleSwitchUtilization(ReadOnlySpan<string> arguments)
	{
		if (!FindSwitch(arguments, "utilization", out int index)) return 1f;
		string argument = index < arguments.Length ? arguments[index + 1] : "";

		if (InvariantFormat.TryParse(argument, out float value) && value is >= 0f and <= 1f) return value;
		Console.WriteLine($"Cannot use '{argument}' as an argument for utilization; defaulting to 1.");

		return 1f;
	}

	static string HandleSwitchProfile(ReadOnlySpan<string> arguments)
	{
		if (!FindSwitch(arguments, "profile", out int index)) return null;
		string argument = index < arguments.Length ? arguments[index + 1] : "";

		if (!string.IsNullOrEmpty(argument) && !string.IsNullOrWhiteSpace(argument)) return argument;
		Console.WriteLine($"Cannot use '{argument}' as an argument for profile; defaulting to the first one found.");

		return null;
	}

	static EchoSource HandleEchoSource(ReadOnlySpan<string> arguments)
	{
		string path = Path.GetFullPath(arguments[^1]);

		try { return new EchoSource(path); }
		catch (FileNotFoundException) { Console.WriteLine($"Cannot find {nameof(EchoSource)} file at '{path}'."); }
		catch (FormatException exception) { Console.WriteLine($"Encountered syntax error during parse: {exception}"); }

		return null;
	}

	static RenderProfile ConstructProfile(EchoSource source, ref string profileIdentifier)
	{
		try
		{
			int index = source.IndexOf<RenderProfile>(profileIdentifier);
			RenderProfile profile = index < 0 ?
				source.ConstructFirst<RenderProfile>(out profileIdentifier) :
				source[index].Construct<RenderProfile>();

			if (profile != null) return profile;

			if (profileIdentifier == null) Console.WriteLine($"No {nameof(RenderProfile)} in {nameof(EchoSource)}.");
			else Console.WriteLine($"No {nameof(RenderProfile)} in {nameof(EchoSource)} identified as {profileIdentifier}.");
		}
		catch (FormatException exception) { Console.WriteLine($"Encountered semantics error during construction: {exception}"); }

		return null;
	}
}