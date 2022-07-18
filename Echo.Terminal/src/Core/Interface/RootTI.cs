using System;
using Echo.Core.Common.Packed;
using Echo.Terminal.Core.Display;

namespace Echo.Terminal.Core.Interface;

public class RootTI : BisectionTI, IDisposable
{
	Domain domain;

	public virtual void ProcessArguments(string[] arguments) { }

	public void DrawToConsole()
	{
		Draw(domain);
		domain.CopyToConsole();
	}

	protected override void Reorient()
	{
		base.Reorient();

		Int2 size = Max - Min;
		if (!(size > Int2.Zero)) return;
		domain = domain.Resize(size);
	}

	protected virtual void Dispose(bool disposing) { }

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}