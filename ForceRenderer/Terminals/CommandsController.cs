using System;
using System.Diagnostics;
using System.Text;
using CodeHelpers;
using CodeHelpers.Vectors;

namespace ForceRenderer.Terminals
{
	public class CommandsController : Terminal.Section
	{
		public CommandsController(Terminal terminal, MinMaxInt displayDomain) : base(terminal, displayDomain) { }

		StringBuilder InputBuilder => this[0];
		readonly Stopwatch time = Stopwatch.StartNew();

		int inputLength;
		int cursorPosition;
		char cursorChar;

		const char CursorCharacter = '\u2588';
		const float CursorBlinkDuration = 500f; //In milliseconds

		public override void Update()
		{
			StringBuilder inputBuilder = InputBuilder;

			if (cursorPosition < inputBuilder.Length) inputBuilder[cursorPosition] = cursorChar;
			if (Console.KeyAvailable) ProcessInput();

			cursorChar = cursorPosition == inputBuilder.Length ? ' ' : inputBuilder[cursorPosition];

			//Replace cursor character
			if (time.Elapsed.TotalMilliseconds.Repeat(CursorBlinkDuration * 2f) < CursorBlinkDuration)
			{
				if (cursorPosition >= inputBuilder.Length) inputBuilder.Append(CursorCharacter);
				else inputBuilder[cursorPosition] = CursorCharacter;
			}

			Console.CursorVisible = false;
		}

		void ProcessInput()
		{
			ConsoleKeyInfo keyInfo = Console.ReadKey(true);
			StringBuilder builder = InputBuilder;

			switch (keyInfo.Key)
			{
				case ConsoleKey.Backspace:
				{
					if (cursorPosition == 0) break;
					builder.Remove(cursorPosition - 1, 1);

					inputLength--;
					cursorPosition--;

					break;
				}
				case ConsoleKey.Delete:
				{
					if (cursorPosition == inputLength) break;

					builder.Remove(cursorPosition, 1);
					inputLength--;

					break;
				}
				case ConsoleKey.Enter:
				{
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
					builder.Insert(cursorPosition, keyInfo.KeyChar);

					inputLength++;
					cursorPosition++;

					break;
				}
			}

			time.Restart();
		}
	}
}