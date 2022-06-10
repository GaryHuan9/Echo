using System;
using Echo.Common.Mathematics.Randomization;
using Echo.Common.Memory;
using NUnit.Framework;

namespace Echo.UnitTests.View;

public abstract class ViewBaseTests<T>
{
	[SetUp]
	public void SetUp()
	{
		Prng random = Utility.NewRandom();

		T[] reference = GetReference();
		random.Shuffle<T>(reference);

		array = reference[Range];

		if (UseSlice)
		{
			roView = reference.AsView()[Range];
			rgView = reference.AsView()[Range];
		}
		else
		{
			roView = reference.AsView(Range);
			rgView = reference.AsView(Range);
		}
	}

	T[] array;              //Reference
	View<T> rgView;         //Regular view
	ReadOnlyView<T> roView; //Read only view

	protected bool UseSlice { private get; init; }

	protected Range Range { private get; init; }

	[Test]
	public void Length()
	{
		Assert.That(rgView.Length, Is.EqualTo(array.Length));
		Assert.That(roView.Length, Is.EqualTo(array.Length));
	}

	[Test]
	public void IntIndexer()
	{
		for (int i = 0; i < array.Length; i++)
		{
			Assert.That(rgView[i], Is.EqualTo(array[i]));
			Assert.That(roView[i], Is.EqualTo(array[i]));
		}
	}

	[Test]
	public void IndexIndexer()
	{
		for (int i = 1; i <= array.Length; i++)
		{
			Index index = ^i;

			Assert.That(rgView[index], Is.EqualTo(array[index]));
			Assert.That(roView[index], Is.EqualTo(array[index]));
		}
	}

	[Test]
	public void ForEach()
	{
		var buffer0 = new T[rgView.Length];
		var buffer1 = new T[roView.Length];

		var fill0 = new SpanFill<T>(buffer0);
		var fill1 = new SpanFill<T>(buffer1);

		foreach (T item in rgView) fill0.Add(item);
		foreach (T item in roView) fill1.Add(item);

		Assert.That(buffer0, Is.EqualTo(array));
		Assert.That(buffer1, Is.EqualTo(array));
	}

	[Test]
	public void IsEmpty()
	{
		bool isEmpty = array.Length == 0;

		Assert.That(rgView.IsEmpty, Is.EqualTo(isEmpty));
		Assert.That(roView.IsEmpty, Is.EqualTo(isEmpty));
	}

	[Test]
	public void AsSpan()
	{
		Span<T> reference = array;

		Span<T> rgSpan = rgView;
		ReadOnlySpan<T> roSpan = roView;

		Assert.That(rgSpan.SequenceEqual(reference));
		Assert.That(roSpan.SequenceEqual(reference));

		rgSpan = rgView.AsSpan();
		roSpan = roView.AsSpan();

		Assert.That(rgSpan.SequenceEqual(reference));
		Assert.That(roSpan.SequenceEqual(reference));
	}

	[Test]
	public void AsSpanSlice()
	{
		AsSpanSliceOne(Range.All);
		AsSpanSliceOne(..0);

		if (array.Length < 1) return;

		AsSpanSliceOne(1..);
		AsSpanSliceOne(..^1);

		if (array.Length < 2) return;

		AsSpanSliceOne(2..);
		AsSpanSliceOne(..^2);
		AsSpanSliceOne(1..^1);
	}

	protected abstract T[] GetReference();


	void AsSpanSliceOne(Range range)
	{
		Span<T> reference = array.AsSpan(range);

		Span<T> rgSpan = rgView[range];
		ReadOnlySpan<T> roSpan = roView[range];

		Assert.That(rgSpan.SequenceEqual(reference));
		Assert.That(roSpan.SequenceEqual(reference));

		(int offset, int length) = range.GetOffsetAndLength(array.Length);

		rgSpan = rgView.AsSpan(offset, length);
		roSpan = roView.AsSpan(offset, length);

		Assert.That(rgSpan.SequenceEqual(reference));
		Assert.That(roSpan.SequenceEqual(reference));

		//TODO: reenable after we added the overloads
		rgSpan = rgView.AsSpan(range);
		roSpan = roView.AsSpan(range);

		Assert.That(rgSpan.SequenceEqual(reference));
		Assert.That(roSpan.SequenceEqual(reference));
	}
}