using System;
using System.Text;
using CodeHelpers;

namespace ForceRenderer.Terminals
{
	public class CommandsController : Terminal.Section
	{
		public CommandsController(Terminal terminal, MinMaxInt displayDomain) : base(terminal, displayDomain) { }

		readonly StringBuilder builder = new StringBuilder();
		int cursorPosition;

		public override void Update()
		{
			if (Console.KeyAvailable) ProcessInput();

			char cursorChar;

			if (cursorPosition != builder.Length)
			{
				cursorChar = builder[cursorPosition];
				builder[cursorPosition] = ' ';
			}
			else cursorChar = ' ';

			Console.Write(builder);
			Console.CursorLeft = cursorPosition;

			Console.Write(cursorChar);
			if (cursorPosition != builder.Length) builder[cursorPosition] = cursorChar;

			Console.CursorTop--;

			Console.ResetColor();
			Console.CursorVisible = false;
		}

		void ProcessInput()
		{
			ConsoleKeyInfo keyInfo = Console.ReadKey(true);

			switch (keyInfo.Key)
			{
				case ConsoleKey.Backspace:
				{
					if (cursorPosition == 0) break;

					builder.Remove(cursorPosition - 1, 1);
					cursorPosition--;

					break;
				}
				case ConsoleKey.Delete:
				{
					if (cursorPosition == builder.Length) break;
					builder.Remove(cursorPosition, 1);

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
					cursorPosition = Math.Min(builder.Length, cursorPosition + 1);
					break;
				}
				default:
				{
					builder.Insert(cursorPosition, keyInfo.KeyChar);
					cursorPosition++;
					break;
				}
			}
		}
	}
}