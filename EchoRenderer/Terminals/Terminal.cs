using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Terminals
{
	public class Terminal : IDisposable
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

		public float UpdateFrequency { get; set; } = 24f;
		public double AliveTime => stopwatch.Elapsed.TotalMilliseconds;

		readonly List<Section> sections = new List<Section>();
		readonly List<int> heights = new List<int>();

		readonly Thread displayThread;
		readonly Stopwatch stopwatch;

		int _disposed;
		int _rewrite;

		public bool Disposed
		{
			get => Interlocked.CompareExchange(ref _disposed, default, default) == 1;
			set => Interlocked.Exchange(ref _disposed, value ? 1 : 0);
		}

		public bool Rewrite
		{
			get => Interlocked.CompareExchange(ref _rewrite, default, default) == 1;
			set => Interlocked.Exchange(ref _rewrite, value ? 1 : 0);
		}

		void DisplayThread()
		{
			Console.Clear();

			while (!Disposed)
			{
				double startTime = AliveTime;
				bool fullRewrite = Rewrite;

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

					for (int i = 0; i < sections.Count; i++) sections[i].Write(fullRewrite);
				}

				Console.CursorTop = 0;
				Rewrite = false;

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

		void IDisposable.Dispose()
		{
			if (Disposed) return;

			Disposed = true;
			displayThread.Join();
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

			readonly List<Line> lines = new List<Line>();
			char[] writeBuffer;

			public char this[Int2 index]
			{
				get
				{
					CheckHeight(index);

					if (lines.Count <= index.y) return default;
					return lines[index.y]?[index.x] ?? default;
				}
				set
				{
					CheckHeight(index);

					if (value == default)
					{
						Line line = lines.TryGetValue(index.y);
						if (line == null) return;
					}

					EnsureCapacity(index.y);
					lines[index.y][index.x] = value;
				}
			}

			/// <inheritdoc cref="Line.GetSlice"/>
			public ReadOnlySpan<char> GetSlice(Int2 index, int length)
			{
				CheckHeight(index);

				Line line = lines.Count < index.y ? null : lines[index.y];
				return line == null ? default : line.GetSlice(index.x, length);
			}

			public void SetSlice(Int2 index, ReadOnlySpan<char> slice) //NOTE: Currently indexers do not support stackalloc assignment, so we have to use methods
			{
				CheckHeight(index);
				EnsureCapacity(index.y);

				lines[index.y].SetSlice(index.x, slice);
			}

			public void SetLine(int index, ReadOnlySpan<char> slice)
			{
				CheckHeight(index);
				EnsureCapacity(index);

				if (lines[index].Count > slice.Length) Clear(index);
				SetSlice(Int2.up * index, slice);
			}

			public void Insert(Int2 index, char value) => Insert(index, stackalloc char[1] {value});

			public void Insert(Int2 index, ReadOnlySpan<char> value)
			{
				CheckHeight(index);
				EnsureCapacity(index.y);

				Line line = lines[index.y];
				int length = value.Length;

				Span<char> slice = stackalloc char[line.Count - index.x];
				line.GetSlice(index.x, slice.Length).CopyTo(slice);

				line.SetSlice(index.x + length, slice);
				line.SetSlice(index.x, value);
			}

			public void Remove(Int2 index, int length = 1)
			{
				CheckHeight(index);

				Line line = lines.TryGetValue(index.y);
				if (line == null || line.Count <= index.x) return;

				Span<char> slice = stackalloc char[line.Count - index.x - length];
				line.GetSlice(index.x + length, slice.Length).CopyTo(slice);

				line.SetSlice(index.x, slice);
				line.SetSlice(index.x + slice.Length, stackalloc char[length]);
			}

			public void Clear(int index)
			{
				CheckHeight(index);

				Line line = lines.TryGetValue(index);
				line?.SetSlice(0, stackalloc char[line.Count]);
			}

			public void Clear()
			{
				RemoveExtraLines();
				for (int i = 0; i < lines.Count; i++) Clear(i);
			}

			void RemoveExtraLines()
			{
				int extra = lines.Count - section.Height;
				if (extra > 0) lines.RemoveRange(section.Height, extra);
			}

			public void Write(bool fullRewrite)
			{
				RemoveExtraLines();
				for (int i = 0; i < section.Height; i++) WriteLine(i, fullRewrite);
			}

			void WriteLine(int index, bool fullRewrite)
			{
				Line line = lines.TryGetValue(index);

				if (line == null || !fullRewrite && !line.Dirtied)
				{
					Console.WriteLine();
					return; //No edits nor need full rewrite
				}

				line.ClearDirtied();
				int width = Console.WindowWidth - 1;

				if (writeBuffer?.Length != width) writeBuffer = new char[width];
				else Array.Clear(writeBuffer, 0, writeBuffer.Length);

				line.GetSlice(0, Math.Min(line.Count, writeBuffer.Length)).CopyTo(writeBuffer);

				Console.Write('\r');
				Console.WriteLine(writeBuffer);
			}

			void CheckHeight(Int2 index) => CheckHeight(index.y);

			void CheckHeight(int index)
			{
				if (0 <= index && index < section.Height) return;
				throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.outOfBounds);
			}

			/// <summary>
			/// Prepares the lines array for indexing at <paramref name="index"/>.
			/// </summary>
			void EnsureCapacity(int index)
			{
				lines.Capacity = Math.Max(lines.Capacity, index + 1);
				while (lines.Count <= index) lines.Add(new Line());
			}

			class Line
			{
				char[] chars;

				public int Count { get; private set; }    //The number of chars actually used. Everything after this number should be default(char)
				public bool Dirtied { get; private set; } //Returns whether this line has been modified. This flag can be reset to false.

				const int MinimumCapacity = 16;

				public char this[int index]
				{
					get => chars == null || chars.Length <= index ? default : chars[index];
					set
					{
						char old = this[index];
						if (old == value) return;

						Dirtied = true;

						if (value == default)
						{
							chars[index] = default;
							if (Count - 1 > index) return; //If there are other real chars after this one

							//Reduce count until hit another real character
							while (Count > 0 && chars[Count - 1] == default) Count--;
						}
						else
						{
							EnsureCapacity(index + 1);
							chars[index] = value;

							Count = Math.Max(Count, index + 1);
						}
					}
				}

				/// <summary>
				/// Returns the slice of chars in memory at <paramref name="index"/> with <paramref name="length"/>.
				/// NOTE: Returned span might be shorter than <paramref name="length"/>. Only available memory is returned.
				/// </summary>
				public ReadOnlySpan<char> GetSlice(int index, int length)
				{
					if (chars == null || chars.Length <= index) return ReadOnlySpan<char>.Empty;

					length = Math.Min(length, chars.Length - index);
					return new ReadOnlySpan<char>(chars, index, length);
				}

				public void SetSlice(int index, ReadOnlySpan<char> slice)
				{
					if (index < 0) throw ExceptionHelper.Invalid(nameof(index), index, InvalidType.outOfBounds);
					for (int i = slice.Length - 1; i >= 0; i--) this[index + i] = slice[i]; //Might optimize/inline this later if needed
				}

				public void ClearDirtied() => Dirtied = false;

				/// <summary>
				/// Ensures that the line is longer than or equals to <paramref name="length"/>.
				/// </summary>
				void EnsureCapacity(int length)
				{
					if (length <= (chars?.Length ?? 0)) return;

					int capacity = GetSmallestPowerOf2(length);
					capacity = Math.Max(MinimumCapacity, capacity);

					if (chars != null)
					{
						char[] newChars = new char[capacity];
						Array.Copy(chars, newChars, chars.Length);

						chars = newChars;
					}
					else chars = new char[capacity];
				}

				/// <summary>
				/// Returns the smallest power of two larger than <paramref name="largerThan"/>.
				/// Source: https://stackoverflow.com/a/365068/9196958
				/// </summary>
				static int GetSmallestPowerOf2(int largerThan)
				{
					int value = largerThan - 1;
					value |= value >> 1;
					value |= value >> 2;
					value |= value >> 4;
					value |= value >> 8;
					value |= value >> 16;
					return value + 1;
				}
			}
		}
	}
}