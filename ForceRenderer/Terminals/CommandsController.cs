using System;
using System.Diagnostics;
using System.Text;
using CodeHelpers;
using CodeHelpers.Vectors;

namespace ForceRenderer.Terminals
{
	public class CommandsController : Terminal.Section
	{
		public CommandsController(Terminal terminal) : base(terminal) { }

		public override int Height => 4;

		int inputLength;
		double blinkOffset;

		int cursorPosition;
		char cursorChar;

		const int CursorY = 0;
		const char CursorCharacter = '\u2588';
		const float BlinkPeriod = 500f; //In milliseconds

		Int2 Cursor => new Int2(cursorPosition, CursorY);

		public override void Update()
		{
			builders[Cursor] = cursorChar;
			if (Console.KeyAvailable) ProcessInput();

			cursorChar = builders[Cursor];

			//Replace cursor character when blinking
			if ((terminal.AliveTime - blinkOffset) % (BlinkPeriod * 2d) < BlinkPeriod) builders[Cursor] = CursorCharacter;
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
					builders.Insert(Cursor, keyInfo.KeyChar);

					inputLength++;
					cursorPosition++;

					break;
				}
			}

			blinkOffset = terminal.AliveTime;
		}
	}
}