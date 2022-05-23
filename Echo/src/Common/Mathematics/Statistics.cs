using System;
using System.Runtime.CompilerServices;

namespace Echo.Common.Mathematics;

public partial struct Statistics
{
	public partial void Report(string label);

	// public partial void Report(string label) { } 

	public void ReportConst()
	{
		this.Report("rider");
	}
}