using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

			Console.CursorVisible = false;
			Console.Title = "Render Engine";
			Console.OutputEncoding = Encoding.UTF8;
		}

		public float UpdateFrequency { get; set; } = 30f;
		public double AliveTime => stopwatch.Elapsed.TotalMilliseconds;

		readonly List<Section> sections = new List<Section>();
		readonly List<int> heights = new List<int>();

		readonly Thread displayThread;
		readonly Stopwatch stopwatch;

		void DisplayThread()
		{
			Console.Clear();

			while (true)
			{
				double startTime = AliveTime;
				bool fullRewrite = false;

				lock (sections)
				{
					for (int i = 0; i < sections.Count; i++)
					{
						Section section = sections[i];
						section.Update();

						if (section.Height == heights[i]) continue;

						heights[i] = section.Height;
						fullRewrite = true;
					}

					if (fullRewrite) Console.Clear();
					else Console.CursorTop = 0;

					for (int i = 0; i < sections.Count; i++) sections[i].Write(fullRewrite);
				}

				double elapsed = AliveTime - startTime;
				double remain = 1000d / UpdateFrequency - elapsed;

				if (remain >= 1d) Thread.Sleep((int)remain);
			}
		}

		public void AddSection<T>(T section) where T : Section
		{
			lock (sections)
			{
				if (sections.Contains(section)) throw ExceptionHelper.Invalid(nameof(section), section, InvalidType.foundDuplicate);

				sections.Add(section);
				heights.Add(-1); //Insert invalid number to cause full rewrite
			}
		}

		public abstract class Section
		{
			protected Section(Terminal terminal)
			{
				this.terminal = terminal;
				builders = new Builders(this);
			}

			protected readonly Terminal terminal;
			protected readonly Builders builders;

			public abstract int Height { get; } //This height can change dynamically

			public abstract void Update();

			public void Write(bool fullRewrite) => builders.Write(fullRewrite);
		}

		/// <summary>
		/// Represents list of <see cref="StringBuilder"/>. The builders are boundless in the x axis, meaning that any positive
		/// x index will be valid and that there is no a current length for a certain line/y value.
		/// </summary>
		public class Builders
		{
			public Builders(Section section) => this.section = section;

			readonly Section section;

			readonly List<StringBuilder> builders = new List<StringBuilder>();
			readonly List<bool> dirties = new List<bool>();

			public char this[Int2 index]
			{
				get
				{
					CheckHeight(index);

					StringBuilder builder = builders.TryGetValue(index.y);
					return builder == null || index.y >= builder.Length ? default : builder[index.x];
				}
				set
				{
					CheckHeight(index);

					StringBuilder builder = PrepareBuilder(index);
					char old = index.x < builder.Length ? builder[index.x] : default;

					if (old != value)
					{
						builder[index.x] = value;
						dirties[index.y] = true;
					}
				}
			}

			public void Insert(Int2 index, char value) => Insert(index, stackalloc char[1] {value});

			public void Insert(Int2 index, ReadOnlySpan<char> value)
			{
				CheckHeight(index);
				var builder = PrepareBuilder(index);

				builder.Insert(index.x, value);
				dirties[index.y] = true;
			}

			public void Remove(Int2 index, int length = 1)
			{
				CheckHeight(index);

				StringBuilder builder = builders.TryGetValue(index.y);
				if (builder == null || builder.Length <= index.x) return;

				length = Math.Min(length, builder.Length - index.x);
				builder.Remove(index.x, length);

				dirties[index.y] = true;
			}

			public void Clear(int index)
			{
				CheckHeight(index);

				StringBuilder builder = builders.TryGetValue(index);
				if (builder == null || builder.Length == 0) return;

				builder.Clear();
				dirties[index] = true;
			}

			StringBuilder PrepareBuilder(Int2 index)
			{
				FillTo<List<StringBuilder>, StringBuilder>(builders, index.y);
				FillTo<List<bool>, bool>(dirties, index.y);

				StringBuilder builder = builders[index.y];
				int length = index.x - builder.Length + 1;

				if (length > 0) builder.Append(stackalloc char[length]);

				return builder;
			}

			public void Write(bool fullRewrite)
			{
				for (int i = 0; i < section.Height; i++) WriteLine(i, fullRewrite);

				//Remove extra builders if section height changed
				int extra = builders.Count - section.Height;
				if (extra > 0) builders.RemoveRange(section.Height, extra);
			}

			void WriteLine(int index, bool fullRewrite)
			{
				if (!fullRewrite && !dirties.TryGetValue(index))
				{
					Console.WriteLine();
					return; //No edits nor need full rewrite
				}

				var builder = builders.TryGetValue(index);
				if (builder != null) dirties[index] = false;

				ReadOnlySpan<char> output = builder?.ToString() ?? "";
				Span<char> print = stackalloc char[Console.WindowWidth - 1];

				output.Slice(0, Math.Min(output.Length, print.Length)).CopyTo(print);

				Console.Write('\r');
				Console.WriteLine(new string(print));
			}

			void CheckHeight(Int2 index) => CheckHeight(index.y);

			void CheckHeight(int index)
			{
				if (0 <= index && index < section.Height) return;
				throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.outOfBounds);
			}

			/// <summary>
			/// Fills <paramref name="list"/> so that index at <paramref name="index"/> is valid by adding in new items.
			/// </summary>
			static void FillTo<T, U>(T list, int index) where T : List<U>
														where U : new()
			{
				list.Capacity = Math.Max(list.Capacity, index);
				for (int i = list.Count; i <= index; i++) list.Add(new U());
			}
		}
	}
}