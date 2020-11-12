using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Vectors;

namespace ForceRenderer.Terminals
{
	public class Terminal
	{
		public Terminal()
		{
			displayThread = new Thread(DisplayThread)
							{
								IsBackground = true,
								Priority = ThreadPriority.AboveNormal,
								Name = "Terminal"
							};

			stopwatch = Stopwatch.StartNew();
			displayThread.Start();
		}

		public float UpdateFrequency { get; set; } = 30f;
		public double AliveTime => stopwatch.Elapsed.TotalMilliseconds;

		readonly List<StringBuilder> outputGrid = new List<StringBuilder>();
		readonly List<Section> sections = new List<Section>();

		readonly Thread displayThread;
		readonly Stopwatch stopwatch;

		char this[Int2 index]
		{
			get
			{
				StringBuilder builder = outputGrid[index.y];
				return builder == null || index.x >= builder.Length ? default : builder[index.x];
			}
			set
			{
				StringBuilder builder = outputGrid[index.y] ?? (outputGrid[index.y] = new StringBuilder());
				if (builder.Length < index.x) builder.Append(stackalloc char[index.x - builder.Length]);

				builder[index.x] = value;
			}
		}

		StringBuilder this[int index]
		{
			get => outputGrid[index] ?? (outputGrid[index] = new StringBuilder());
			set => outputGrid[index] = value;
		}

		void DisplayThread()
		{
			while (true)
			{
				double startTime = AliveTime;
				Console.Clear();

				for (int i = 0; i < sections.Count; i++) sections[i].Update();
				Console.WriteLine(string.Join('\n', outputGrid));

				double elapsed = AliveTime - startTime;
				double remain = 1000d / UpdateFrequency - elapsed;

				if (remain >= 1d) Thread.Sleep((int)remain);
			}
		}

		public void AddSection<T>(T section) where T : Section
		{
			int index = sections.BinarySearch(typeof(T), SectionComparer.instance);
			if (index >= 0) throw ExceptionHelper.Invalid(nameof(section), section, InvalidType.foundDuplicate);

			sections.Insert(~index, section);

			outputGrid.Capacity = Math.Max(outputGrid.Capacity, section.displayDomain.max);
			while (outputGrid.Count < section.displayDomain.max) outputGrid.Add(null);
		}

		public T GetSection<T>() where T : Section
		{
			int index = sections.BinarySearch(typeof(T), SectionComparer.instance);
			return index < 0 ? null : (T)sections[index];
		}

		class SectionComparer : IDoubleComparer<Section, Type>
		{
			public static readonly SectionComparer instance = new SectionComparer();
			public int CompareTo(Section first, Type second) => first.GetType().GetHashCode().CompareTo(second.GetHashCode());
		}

		public abstract class Section
		{
			protected Section(Terminal terminal, MinMaxInt displayDomain)
			{
				this.terminal = terminal;
				this.displayDomain = displayDomain;
			}

			protected readonly Terminal terminal;
			public readonly MinMaxInt displayDomain; //Min inclusive; max exclusive

			protected int Height => displayDomain.Range;

			protected char this[Int2 index]
			{
				get
				{
					if (index.y >= 0 && index.y < Height) return terminal[index + Int2.up * displayDomain.min];
					throw ExceptionHelper.Invalid(nameof(index.y), index.y, InvalidType.outOfBounds);
				}
				set
				{
					if (index.y >= 0 && index.y < Height) terminal[index + Int2.up * displayDomain.min] = value;
					else throw ExceptionHelper.Invalid(nameof(index.y), index.y, InvalidType.outOfBounds);
				}
			}

			protected StringBuilder this[int index]
			{
				get
				{
					if (index >= 0 && index < Height) return terminal[index + displayDomain.min];
					throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.outOfBounds);
				}
				set
				{
					if (index >= 0 && index < Height) terminal[index + displayDomain.min] = value;
					else throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.outOfBounds);
				}
			}

			public abstract void Update();
		}
	}
}