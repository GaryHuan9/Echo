using System;
using System.Collections.Generic;
using CodeHelpers.Mathematics;
using Echo.Terminal.Core;
using Echo.Terminal.Core.Display;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal.Application;

public class CommandTI : AreaTI
{
	readonly Prompt prompt = new();
	readonly History history = new();

	bool cursorVisible = true;
	TimeSpan cursorTime;

	static readonly TimeSpan cursorBlinkPeriod = TimeSpan.FromMilliseconds(500f);

	//NOTE: console shortcuts: https://www.bigsmoke.us/readline/shortcuts

	public override void Update(in Moment moment)
	{
		base.Update(moment);

		//Read keys
		while (Console.KeyAvailable)
		{
			ConsoleKeyInfo info = Console.ReadKey(true);

			if (IsValidCharacter(info.KeyChar))
			{
				prompt.Add(info.KeyChar);
				ResetCursorBlink();
			}
			else ProcessButton(info.Key);
		}

		//Update cursor visibility
		cursorTime += moment.delta;

		while (cursorTime > cursorBlinkPeriod)
		{
			cursorTime -= cursorBlinkPeriod;
			cursorVisible = !cursorVisible;
		}
	}

	protected override void Paint(in Canvas canvas)
	{
		var brush = new Brush(new TextOptions(WrapOptions.LineBreak));

		canvas.Write(ref brush, "echo™ >> ");

		Brush cursorBrush = brush;
		var input = prompt.Current;
		canvas.Write(ref brush, input);

		if (cursorVisible)
		{
			int shift = prompt.Position;
			canvas.Shift(ref cursorBrush, shift);

			if (!cursorBrush.CheckBounds(canvas.size))
			{
				canvas[cursorBrush] = '▂';

				if (shift == input.Length)
				{
					brush = cursorBrush;
					canvas.Shift(ref brush, 1);
				}
			}
		}

		canvas.FillAll(ref brush);
	}

	void ProcessButton(ConsoleKey key)
	{
		switch (key)
		{
			case ConsoleKey.Delete:
			{
				if (prompt.Position < prompt.Count)
				{
					++prompt.Position;
					prompt.Remove();
				}

				break;
			}
			case ConsoleKey.Backspace:
			{
				prompt.Remove();
				break;
			}
			case ConsoleKey.Escape:
			{
				prompt.Clear();
				break;
			}
			case ConsoleKey.Enter:
			{
				history.Add(prompt.Current);
				prompt.Clear();
				break;
			}
			case ConsoleKey.Tab:
			{
				break;
			}
			case ConsoleKey.RightArrow:
			{
				++prompt.Position;
				break;
			}
			case ConsoleKey.LeftArrow:
			{
				--prompt.Position;
				break;
			}
			case ConsoleKey.UpArrow:
			{
				prompt.Replace(history.Move(-1));
				break;
			}
			case ConsoleKey.DownArrow:
			{
				prompt.Replace(history.Move(1));
				break;
			}
			default: return;
		}

		ResetCursorBlink();
	}

	void ResetCursorBlink()
	{
		cursorVisible = true;
		cursorTime = TimeSpan.Zero;
	}

	/// <summary>
	/// Checks an input <see cref="char"/> against https://cs.smu.ca/~porter/csc/ref/ascii.html
	/// </summary>
	/// <param name="character">The <see cref="char"/> to check.</param>
	/// <returns>Whether the input <see cref="char"/> is valid.</returns>
	static bool IsValidCharacter(char character) => 32 <= character && character <= 126;

	class Prompt
	{
		char[] array = new char[16];

		public int Count { get; private set; }

		int _position;

		public int Position
		{
			get => _position;
			set => _position = value.Clamp(0, Count);
		}

		public ReadOnlySpan<char> Current => array.AsSpan(0, Count);

		public void Add(char character)
		{
			EnsureCapacity(Count + 1);

			Current[Position..].CopyTo(array.AsSpan(Position + 1));
			array[Position] = character;

			++Count;
			++Position;
		}

		public void Remove()
		{
			if (Position == 0) return;
			if (Position < Count) Current[Position..].CopyTo(array.AsSpan(Position - 1));

			--Count;
			--Position;
		}

		public void Replace(ReadOnlySpan<char> value)
		{
			Count = 0;
			EnsureCapacity(value.Length);
			Position = Count = value.Length;
			value.CopyTo(array);
		}

		public void Clear() => Position = Count = 0;

		void EnsureCapacity(int capacity)
		{
			if (array.Length >= capacity) return;

			int length = array.Length;
			do length *= 2;
			while (length < capacity);

			var newArray = new char[length];
			Current.CopyTo(newArray);
			array = newArray;
		}
	}

	class History
	{
		readonly List<char[]> list = new();
		readonly List<int> commands = new();

		int position;

		public void Add(ReadOnlySpan<char> value, bool isCommand = false)
		{
			if (isCommand) commands.Add(list.Count);
			list.Add(value.ToArray());
		}

		public ReadOnlySpan<char> Move(int amount)
		{
			position = (position + amount).Clamp(0, list.Count);
			if (position < list.Count) return list[position];
			return ReadOnlySpan<char>.Empty;
		}

		public void ResetPosition() => position = list.Count;
	}
}